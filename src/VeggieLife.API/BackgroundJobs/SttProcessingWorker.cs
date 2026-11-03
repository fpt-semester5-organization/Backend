using System;

using System.Collections.Generic;

using System.Linq;

using System.Text.Json;

using System.Threading;

using System.Threading.Tasks;

using Microsoft.EntityFrameworkCore;

using Microsoft.Extensions.DependencyInjection;

using Microsoft.Extensions.Hosting;

using Microsoft.Extensions.Logging;

using VeggieLife.BLL.DTOs.Videos.Responses;

using VeggieLife.BLL.Services.AI;

using VeggieLife.BLL.Services.Cache;

using VeggieLife.DAL.Context;

using VeggieLife.DAL.Entities;



namespace VeggieLife.API.BackgroundJobs;



/// <summary>

/// Cấu trúc dữ liệu của công việc xử lý bóc tách video trong hàng đợi Redis stt_queue.

/// </summary>

public class VideoSttJobPayload

{

    public int VideoId { get; set; }

    public string VideoUrl { get; set; } = string.Empty;

    public int DurationSeconds { get; set; }

}



/// <summary>

/// Background Service tiêu thụ hàng đợi stt_queue (FIFO), gọi AI STT Microservice và tự động tạo Recipe nháp (FN-POST-10).

/// Chạy nền bất đồng bộ, hỗ trợ retry 3 lần với Exponential Backoff.

/// </summary>

public class SttProcessingWorker : BackgroundService

{

    private readonly IRedisService _redis;

    private readonly IServiceScopeFactory _scopeFactory;

    private readonly ILogger<SttProcessingWorker> _logger;

    private readonly Func<int, TimeSpan> _retryDelayStrategy;



    private static readonly JsonSerializerOptions JsonOptions = new()

    {

        PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase,

        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.Create(System.Text.Unicode.UnicodeRanges.All)

    };



    public SttProcessingWorker(

        IRedisService redis,

        IServiceScopeFactory scopeFactory,

        ILogger<SttProcessingWorker> logger)

        : this(redis, scopeFactory, logger, null)

    {

    }



    public SttProcessingWorker(

        IRedisService redis,

        IServiceScopeFactory scopeFactory,

        ILogger<SttProcessingWorker> logger,

        Func<int, TimeSpan>? retryDelayStrategy)

    {

        _redis = redis;

        _scopeFactory = scopeFactory;

        _logger = logger;

        _retryDelayStrategy = retryDelayStrategy ?? (attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt)));

    }



    protected override async Task ExecuteAsync(CancellationToken stoppingToken)

    {

        _logger.LogInformation("Khởi động SttProcessingWorker lắng nghe hàng đợi [stt_queue]...");



        while (!stoppingToken.IsCancellationRequested)

        {

            try

            {

                var job = await _redis.DequeueAsync<VideoSttJobPayload>("stt_queue", stoppingToken);

                if (job == null)

                {

                    // Chờ 2.5 giây trước khi thăm dò tiếp để giảm tải CPU và Redis

                    await Task.Delay(2500, stoppingToken);

                    continue;

                }



                _logger.LogInformation("Bắt đầu xử lý bóc tách STT cho video id={VideoId}, url={VideoUrl}", job.VideoId, job.VideoUrl);



                await ProcessJobAsync(job, stoppingToken);

            }

            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)

            {

                _logger.LogInformation("SttProcessingWorker nhận tín hiệu dừng graceful shutdown.");

                break;

            }

            catch (Exception ex)

            {

                _logger.LogError(ex, "Lỗi ngoại lệ trong chu trình thực thi của SttProcessingWorker");

                await Task.Delay(3000, stoppingToken);

            }

        }



        _logger.LogInformation("SttProcessingWorker đã dừng hoạt động.");

    }



    private async Task ProcessJobAsync(VideoSttJobPayload job, CancellationToken stoppingToken)

    {

        using var scope = _scopeFactory.CreateScope();

        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var aiSttClient = scope.ServiceProvider.GetRequiredService<IAiSttClient>();



        var video = await db.Videos

            .Include(v => v.Post)

            .Include(v => v.VideoTranscript)

            .FirstOrDefaultAsync(v => v.VideoId == job.VideoId, stoppingToken);



        if (video == null)

        {

            _logger.LogWarning("Không tìm thấy video id={VideoId} trong CSDL để xử lý.", job.VideoId);

            return;

        }



        const int maxRetries = 3;

        AiSttResultDto? aiResult = null;



        for (int attempt = 1; attempt <= maxRetries; attempt++)

        {

            try

            {

                aiResult = await aiSttClient.ExtractRecipeFromVideoAsync(job.VideoUrl, stoppingToken);

                if (aiResult != null && !string.IsNullOrWhiteSpace(aiResult.RawTranscript))

                {

                    break;

                }



                _logger.LogWarning("AI STT Microservice trả về kết quả rỗng cho video id={VideoId} (lần thử {Attempt}/{MaxRetries}).",

                    job.VideoId, attempt, maxRetries);

            }

            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)

            {

                throw;

            }

            catch (Exception ex)

            {

                _logger.LogWarning(ex, "Lỗi ngoại lệ khi gọi AI STT cho video id={VideoId} (lần thử {Attempt}/{MaxRetries}).",

                    job.VideoId, attempt, maxRetries);

            }



            if (attempt < maxRetries)

            {

                var delay = _retryDelayStrategy(attempt);

                _logger.LogInformation("Đang chờ {Delay}ms trước khi thử lại lần {NextAttempt} cho video id={VideoId}...",

                    delay.TotalMilliseconds, attempt + 1, job.VideoId);

                await Task.Delay(delay, stoppingToken);

            }

        }



        if (aiResult != null && !string.IsNullOrWhiteSpace(aiResult.RawTranscript))

        {

            await HandleSuccessAsync(db, video, job, aiResult, stoppingToken);

        }

        else

        {

            await HandleFailureAsync(db, video, job, maxRetries, stoppingToken);

        }

    }



    private async Task HandleSuccessAsync(

        AppDbContext db,

        Video video,

        VideoSttJobPayload job,

        AiSttResultDto aiResult,

        CancellationToken stoppingToken)

    {

        // 1. Lưu bản phiên âm vào bảng VideoTranscripts với Status = "READY"

        if (video.VideoTranscript == null)

        {

            video.VideoTranscript = new VideoTranscript

            {

                VideoId = video.VideoId,

                CreatedAt = DateTime.UtcNow

            };

            db.VideoTranscripts.Add(video.VideoTranscript);

        }



        var workerSummary = aiResult.Recipe?.Title != null

            ? $"{aiResult.Recipe.Title} (Khẩu phần: {aiResult.Recipe.Servings ?? 2} người, {aiResult.Recipe.Ingredients?.Count ?? 0} nguyên liệu, {aiResult.Recipe.Steps?.Count ?? 0} bước chế biến)"

            : null;

        video.VideoTranscript.RawTranscript = aiResult.RawTranscript;

        video.VideoTranscript.ExtractedRecipeJson = JsonSerializer.Serialize(aiResult.Recipe, JsonOptions);

        video.VideoTranscript.AiSummary = workerSummary;

        video.VideoTranscript.SttModelVersion = aiResult.SttModelVersion ?? "faster-whisper-small-int8";

        video.VideoTranscript.Status = "READY";

        video.VideoTranscript.CompletedAt = DateTime.UtcNow;



        // 2. Tự động sinh công thức nháp (Draft Recipe: Status = "DRAFT", Source = "AI")

        var defaultCategory = await db.FoodCategories.FirstOrDefaultAsync(stoppingToken);

        var defaultDietarySchool = await db.DietarySchools.FirstOrDefaultAsync(stoppingToken);



        var rawTitle = !string.IsNullOrWhiteSpace(video.Title)

            ? $"Công thức từ video {video.Title.Trim()}"

            : (aiResult.Recipe?.Title ?? "Công thức từ video");

        var recipeTitle = rawTitle.Length > 150 ? rawTitle[..150] : rawTitle;



        var description = aiResult.Recipe?.Title ?? video.Title;

        if (!string.IsNullOrWhiteSpace(description) && description.Length > 1000)

        {

            description = description[..1000];

        }



        var steps = aiResult.Recipe?.Steps ?? new List<string>();

        var instructions = steps.Count > 0

            ? JsonSerializer.Serialize(steps, JsonOptions)

            : "[]";



        var imageUrl = video.ThumbnailUrl ?? video.Post?.ImageUrl;

        if (imageUrl != null && imageUrl.Length > 255)

        {

            imageUrl = imageUrl[..255];

        }



        var draftRecipe = new Recipe

        {

            UserId = video.Post?.UserId,


            DietarySchoolId = defaultDietarySchool?.DietarySchoolId ?? 1,

            Title = recipeTitle,

            Description = description,

            Instructions = instructions,

            PrepTimeMinutes = 15,

            CookTimeMinutes = 20,

            Servings = 2,

            CaloriesKcal = 150.0m,

            DifficultyLevel = "MEDIUM",

            IsVerified = false,

            ImageUrl = imageUrl,

            Status = "DRAFT",

            ViewCount = 0,

            Source = "AI",

            CreatedAt = DateTime.UtcNow

        };



        db.Recipes.Add(draftRecipe);

        await db.SaveChangesAsync(stoppingToken);



        // 3. Tự động bóc tách và thêm các bản ghi RecipeIngredients (nguyên liệu, định lượng)

        if (aiResult.Recipe?.Ingredients != null && aiResult.Recipe.Ingredients.Count > 0)

        {

            var addedIngredientIds = new HashSet<int>();

            foreach (var item in aiResult.Recipe.Ingredients)

            {

                if (string.IsNullOrWhiteSpace(item.Name)) continue;

                var trimmedName = item.Name.Trim();



                var ingredient = await db.Ingredients

                    .FirstOrDefaultAsync(i => i.IngredientName.ToLower() == trimmedName.ToLower(), stoppingToken);



                if (ingredient == null)

                {

                    var unitStr = !string.IsNullOrWhiteSpace(item.Unit) ? item.Unit.Trim() : "g";

                    if (unitStr.Length > 20) unitStr = unitStr[..20];



                    ingredient = new Ingredient

                    {

                        IngredientName = trimmedName.Length > 100 ? trimmedName[..100] : trimmedName,

                        StandardUnit = unitStr,

                        DefaultStallType = "PRODUCE",

                        FoodGroup = "PLANT",

                        IsAllium = false,

                        CaloriesPer100g = 50.0m,

                        CreatedAt = DateTime.UtcNow

                    };

                    db.Ingredients.Add(ingredient);

                    await db.SaveChangesAsync(stoppingToken);

                }



                if (!addedIngredientIds.Contains(ingredient.IngredientId))

                {

                    addedIngredientIds.Add(ingredient.IngredientId);



                    var unit = !string.IsNullOrWhiteSpace(item.Unit) ? item.Unit.Trim() : ingredient.StandardUnit;

                    if (unit.Length > 20) unit = unit[..20];



                    var qty = item.Quantity.HasValue && item.Quantity.Value > 0 ? item.Quantity.Value : 1m;

                    if (qty > 999999.99m) qty = 999999.99m;



                    db.RecipeIngredients.Add(new RecipeIngredient

                    {

                        RecipeId = draftRecipe.RecipeId,

                        IngredientId = ingredient.IngredientId,

                        QuantityNeeded = Math.Round(qty, 2),

                        Unit = unit,

                        IsOptional = false

                    });

                }

            }

        }



        // 4. Liên kết VideoTranscript.RecipeId và Post.RecipeId

        video.VideoTranscript.RecipeId = draftRecipe.RecipeId;

        if (video.Post != null && video.Post.RecipeId == null)

        {

            video.Post.RecipeId = draftRecipe.RecipeId;

        }



        video.Status = "READY";

        video.UpdatedAt = DateTime.UtcNow;



        await db.SaveChangesAsync(stoppingToken);

        _logger.LogInformation("Bóc tách thành công video id={VideoId}, đã sinh Recipe nháp id={RecipeId} và cập nhật trạng thái READY.",

            job.VideoId, draftRecipe.RecipeId);

    }



    private async Task HandleFailureAsync(

        AppDbContext db,

        Video video,

        VideoSttJobPayload job,

        int maxRetries,

        CancellationToken stoppingToken)

    {

        _logger.LogWarning("AI Service không bóc tách được nội dung cho video id={VideoId} sau {MaxRetries} lần thử. Đánh dấu FAILED.",

            job.VideoId, maxRetries);



        if (video.VideoTranscript == null)

        {

            video.VideoTranscript = new VideoTranscript

            {

                VideoId = video.VideoId,

                CreatedAt = DateTime.UtcNow

            };

            db.VideoTranscripts.Add(video.VideoTranscript);

        }



        video.VideoTranscript.Status = "FAILED";

        video.Status = "FAILED";

        video.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(stoppingToken);

    }

}

