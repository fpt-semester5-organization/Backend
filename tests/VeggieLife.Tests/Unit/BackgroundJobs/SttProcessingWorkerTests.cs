using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using VeggieLife.API.BackgroundJobs;
using VeggieLife.BLL.DTOs.Videos.Responses;
using VeggieLife.BLL.Services.AI;
using VeggieLife.BLL.Services.Cache;
using VeggieLife.DAL.Context;
using VeggieLife.DAL.Entities;
using Xunit;

namespace VeggieLife.Tests.Unit.BackgroundJobs;

public class SttProcessingWorkerTests
{
    private readonly string _dbName = "SttProcessingWorkerTests_" + Guid.NewGuid();
    private readonly Mock<IRedisService> _mockRedis = new();
    private readonly Mock<IAiSttClient> _mockAiSttClient = new();
    private readonly ServiceProvider _serviceProvider;
    private readonly DbContextOptions<AppDbContext> _dbOptions;

    public SttProcessingWorkerTests()
    {
        _dbOptions = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(_dbName)
            .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        var services = new ServiceCollection();
        services.AddDbContext<AppDbContext>(opt =>
        {
            opt.UseInMemoryDatabase(_dbName);
            opt.ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning));
        });
        services.AddScoped(_ => _mockAiSttClient.Object);
        _serviceProvider = services.BuildServiceProvider();
    }

    private AppDbContext CreateContext() => new(_dbOptions);

    [Fact]
    public async Task ExecuteAsync_WhenValidJob_ProcessesSTTAndUpdatesDatabaseToReady_AndCreatesDraftRecipe()
    {
        // Arrange
        using (var context = CreateContext())
        {
            var user = new User { UserId = 10, Email = "chef10@veggie.vn", FullName = "Chef 10", Role = "MEMBER", AccountStatus = "ACTIVE" };
            var post = new Post { PostId = 10, UserId = 10, Title = "Post 10", Content = "Content 10", ModerationStatus = "APPROVED_ACTIVE", Visibility = "PUBLIC" };
            var video = new Video
            {
                VideoId = 100,
                PostId = 10,
                Title = "Video Nấu Chay 100",
                VideoUrl = "https://res.cloudinary.com/veggie/video/upload/v100.mp4",
                ThumbnailUrl = "https://res.cloudinary.com/veggie/video/upload/thumb.jpg",
                Status = "PROCESSING",
                DurationSeconds = 65,
                CreatedAt = DateTime.UtcNow
            };
            context.Users.Add(user);
            context.Posts.Add(post);
            context.Videos.Add(video);
            await context.SaveChangesAsync();
        }

        var job = new VideoSttJobPayload
        {
            VideoId = 100,
            VideoUrl = "https://res.cloudinary.com/veggie/video/upload/v100.mp4",
            DurationSeconds = 65
        };

        var aiResult = new AiSttResultDto
        {
            DurationSeconds = 65,
            SttModelVersion = "faster-whisper-small-int8",
            RawTranscript = "Hôm nay mình làm món đậu hũ xào nấm rơm",
            Recipe = new ExtractedRecipeDto
            {
                Title = "Đậu hũ xào nấm rơm",
                Ingredients = new List<ExtractedIngredientDto>
                {
                    new() { Name = "Đậu hũ", Quantity = 200, Unit = "g" },
                    new() { Name = "Nấm rơm", Quantity = 100, Unit = "g" }
                },
                Steps = new List<string> { "Sơ chế nấm", "Xào chín với gia vị" }
            }
        };

        var cts = new CancellationTokenSource();
        var dequeueCallCount = 0;

        _mockRedis.Setup(r => r.DequeueAsync<VideoSttJobPayload>("stt_queue", It.IsAny<CancellationToken>()))
            .ReturnsAsync(() =>
            {
                dequeueCallCount++;
                if (dequeueCallCount == 1) return job;
                cts.Cancel(); // Hủy sau 1 job để kết thúc worker
                return null;
            });

        _mockAiSttClient.Setup(a => a.ExtractRecipeFromVideoAsync(job.VideoUrl, It.IsAny<CancellationToken>()))
            .ReturnsAsync(aiResult);

        var worker = new SttProcessingWorker(
            _mockRedis.Object,
            _serviceProvider.GetRequiredService<IServiceScopeFactory>(),
            NullLogger<SttProcessingWorker>.Instance);

        // Act
        await worker.StartAsync(cts.Token);
        await Task.Delay(250); // Chờ worker xử lý
        await worker.StopAsync(CancellationToken.None);

        // Assert
        using (var assertContext = CreateContext())
        {
            var updatedVideo = await assertContext.Videos
                .Include(v => v.VideoTranscript)
                .FirstOrDefaultAsync(v => v.VideoId == 100);

            updatedVideo.Should().NotBeNull();
            updatedVideo!.Status.Should().Be("READY");
            updatedVideo.VideoTranscript.Should().NotBeNull();
            updatedVideo.VideoTranscript!.Status.Should().Be("READY");
            updatedVideo.VideoTranscript.RawTranscript.Should().Be("Hôm nay mình làm món đậu hũ xào nấm rơm");
            updatedVideo.VideoTranscript.SttModelVersion.Should().Be("faster-whisper-small-int8");
            updatedVideo.VideoTranscript.ExtractedRecipeJson.Should().Contain("Đậu hũ xào nấm rơm");
            updatedVideo.VideoTranscript.RecipeId.Should().NotBeNull();

            // Kiểm tra tự động tạo Recipe nháp (Status = DRAFT, Source = AI)
            var createdRecipe = await assertContext.Recipes
                .Include(r => r.RecipeIngredients)
                .ThenInclude(ri => ri.Ingredient)
                .FirstOrDefaultAsync(r => r.RecipeId == updatedVideo.VideoTranscript.RecipeId);

            createdRecipe.Should().NotBeNull();
            createdRecipe!.Title.Should().Be("Công thức từ video Video Nấu Chay 100");
            createdRecipe.Status.Should().Be("DRAFT");
            createdRecipe.Source.Should().Be("AI");
            createdRecipe.UserId.Should().Be(10);
            createdRecipe.ImageUrl.Should().Be("https://res.cloudinary.com/veggie/video/upload/thumb.jpg");
            createdRecipe.Instructions.Should().Contain("Sơ chế nấm");
            createdRecipe.Instructions.Should().Contain("Xào chín với gia vị");

            // Kiểm tra bóc tách RecipeIngredients
            createdRecipe.RecipeIngredients.Should().HaveCount(2);
            createdRecipe.RecipeIngredients.Should().Contain(ri => ri.Ingredient.IngredientName == "Đậu hũ" && ri.QuantityNeeded == 200 && ri.Unit == "g");
            createdRecipe.RecipeIngredients.Should().Contain(ri => ri.Ingredient.IngredientName == "Nấm rơm" && ri.QuantityNeeded == 100 && ri.Unit == "g");
        }
    }

    [Fact]
    public async Task ExecuteAsync_WhenAiFails_Retries3TimesAndMarksAsFailed()
    {
        // Arrange
        using (var context = CreateContext())
        {
            var user = new User { UserId = 11, Email = "chef11@veggie.vn", FullName = "Chef 11", Role = "MEMBER", AccountStatus = "ACTIVE" };
            var post = new Post { PostId = 11, UserId = 11, Title = "Post 11", Content = "Content 11", ModerationStatus = "APPROVED_ACTIVE", Visibility = "PUBLIC" };
            var video = new Video
            {
                VideoId = 101,
                PostId = 11,
                Title = "Video Nhạc Không Lời",
                VideoUrl = "https://res.cloudinary.com/veggie/video/upload/nospeech.mp4",
                Status = "PROCESSING",
                DurationSeconds = 30,
                CreatedAt = DateTime.UtcNow
            };
            var transcript = new VideoTranscript
            {
                VideoId = 101,
                Status = "PENDING",
                CreatedAt = DateTime.UtcNow
            };
            context.Users.Add(user);
            context.Posts.Add(post);
            context.Videos.Add(video);
            context.VideoTranscripts.Add(transcript);
            await context.SaveChangesAsync();
        }

        var job = new VideoSttJobPayload
        {
            VideoId = 101,
            VideoUrl = "https://res.cloudinary.com/veggie/video/upload/nospeech.mp4",
            DurationSeconds = 30
        };

        var cts = new CancellationTokenSource();
        var dequeueCallCount = 0;

        _mockRedis.Setup(r => r.DequeueAsync<VideoSttJobPayload>("stt_queue", It.IsAny<CancellationToken>()))
            .ReturnsAsync(() =>
            {
                dequeueCallCount++;
                if (dequeueCallCount == 1) return job;
                cts.Cancel();
                return null;
            });

        _mockAiSttClient.Setup(a => a.ExtractRecipeFromVideoAsync(job.VideoUrl, It.IsAny<CancellationToken>()))
            .ReturnsAsync((AiSttResultDto?)null); // AI service trả về null

        // Dùng retry delay = 0 cho unit test chạy ngay lập tức
        var worker = new SttProcessingWorker(
            _mockRedis.Object,
            _serviceProvider.GetRequiredService<IServiceScopeFactory>(),
            NullLogger<SttProcessingWorker>.Instance,
            _ => TimeSpan.Zero);

        // Act
        await worker.StartAsync(cts.Token);
        await Task.Delay(250);
        await worker.StopAsync(CancellationToken.None);

        // Assert
        _mockAiSttClient.Verify(a => a.ExtractRecipeFromVideoAsync(job.VideoUrl, It.IsAny<CancellationToken>()), Times.Exactly(3));

        using (var assertContext = CreateContext())
        {
            var updatedVideo = await assertContext.Videos
                .Include(v => v.VideoTranscript)
                .FirstOrDefaultAsync(v => v.VideoId == 101);

            updatedVideo.Should().NotBeNull();
            updatedVideo!.Status.Should().Be("FAILED");
            updatedVideo.VideoTranscript.Should().NotBeNull();
            updatedVideo.VideoTranscript!.Status.Should().Be("FAILED");

            var recipes = await assertContext.Recipes.ToListAsync();
            recipes.Should().BeEmpty();
        }
    }

    [Fact]
    public async Task ExecuteAsync_WhenAiFailsFirstAttempt_RetriesAndSuccessfullyCreatesDraftRecipe()
    {
        // Arrange
        using (var context = CreateContext())
        {
            var user = new User { UserId = 12, Email = "chef12@veggie.vn", FullName = "Chef 12", Role = "MEMBER", AccountStatus = "ACTIVE" };
            var post = new Post { PostId = 12, UserId = 12, Title = "Post 12", Content = "Content 12", ModerationStatus = "APPROVED_ACTIVE", Visibility = "PUBLIC" };
            var video = new Video
            {
                VideoId = 102,
                PostId = 12,
                Title = "Canh Chua Chay",
                VideoUrl = "https://res.cloudinary.com/veggie/video/upload/canhchua.mp4",
                Status = "PROCESSING",
                DurationSeconds = 45,
                CreatedAt = DateTime.UtcNow
            };
            context.Users.Add(user);
            context.Posts.Add(post);
            context.Videos.Add(video);
            await context.SaveChangesAsync();
        }

        var job = new VideoSttJobPayload
        {
            VideoId = 102,
            VideoUrl = "https://res.cloudinary.com/veggie/video/upload/canhchua.mp4",
            DurationSeconds = 45
        };

        var aiResult = new AiSttResultDto
        {
            DurationSeconds = 45,
            SttModelVersion = "faster-whisper-small-int8",
            RawTranscript = "Hướng dẫn nấu canh chua chay thanh đạm",
            Recipe = new ExtractedRecipeDto
            {
                Title = "Canh chua chay",
                Ingredients = new List<ExtractedIngredientDto>
                {
                    new() { Name = "Cà chua", Quantity = 2, Unit = "quả" },
                    new() { Name = "Đậu bắp", Quantity = 100, Unit = "g" }
                },
                Steps = new List<string> { "Cắt cà chua và đậu bắp", "Nấu sôi nước dùng và nêm me" }
            }
        };

        var cts = new CancellationTokenSource();
        var dequeueCallCount = 0;
        var aiCallCount = 0;

        _mockRedis.Setup(r => r.DequeueAsync<VideoSttJobPayload>("stt_queue", It.IsAny<CancellationToken>()))
            .ReturnsAsync(() =>
            {
                dequeueCallCount++;
                if (dequeueCallCount == 1) return job;
                cts.Cancel();
                return null;
            });

        _mockAiSttClient.Setup(a => a.ExtractRecipeFromVideoAsync(job.VideoUrl, It.IsAny<CancellationToken>()))
            .ReturnsAsync(() =>
            {
                aiCallCount++;
                if (aiCallCount == 1)
                {
                    return null; // Lần 1 thất bại
                }
                return aiResult; // Lần 2 thành công
            });

        var worker = new SttProcessingWorker(
            _mockRedis.Object,
            _serviceProvider.GetRequiredService<IServiceScopeFactory>(),
            NullLogger<SttProcessingWorker>.Instance,
            _ => TimeSpan.Zero);

        // Act
        await worker.StartAsync(cts.Token);
        await Task.Delay(250);
        await worker.StopAsync(CancellationToken.None);

        // Assert
        aiCallCount.Should().Be(2);

        using (var assertContext = CreateContext())
        {
            var updatedVideo = await assertContext.Videos
                .Include(v => v.VideoTranscript)
                .FirstOrDefaultAsync(v => v.VideoId == 102);

            updatedVideo.Should().NotBeNull();
            updatedVideo!.Status.Should().Be("READY");
            updatedVideo.VideoTranscript.Should().NotBeNull();
            updatedVideo.VideoTranscript!.Status.Should().Be("READY");
            updatedVideo.VideoTranscript.RecipeId.Should().NotBeNull();

            var recipe = await assertContext.Recipes
                .Include(r => r.RecipeIngredients)
                .ThenInclude(ri => ri.Ingredient)
                .FirstOrDefaultAsync(r => r.RecipeId == updatedVideo.VideoTranscript.RecipeId);

            recipe.Should().NotBeNull();
            recipe!.Title.Should().Be("Công thức từ video Canh Chua Chay");
            recipe.Status.Should().Be("DRAFT");
            recipe.Source.Should().Be("AI");
            recipe.RecipeIngredients.Should().HaveCount(2);
        }
    }
}
