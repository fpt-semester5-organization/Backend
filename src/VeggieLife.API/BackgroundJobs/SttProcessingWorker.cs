using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
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
/// Background Service tiêu thụ hàng đợi stt_queue (FIFO), gọi AI STT Microservice và lưu kết quả vào CSDL (FN-POST-10).
/// Chạy nền bất đồng bộ, không làm nghẽn luồng xử lý HTTP request chính của người dùng.
/// </summary>
public class SttProcessingWorker : BackgroundService
{
    private readonly IRedisService _redis;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<SttProcessingWorker> _logger;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.Create(System.Text.Unicode.UnicodeRanges.All)
    };

    public SttProcessingWorker(
        IRedisService redis,
        IServiceScopeFactory scopeFactory,
        ILogger<SttProcessingWorker> logger)
    {
        _redis = redis;
        _scopeFactory = scopeFactory;
        _logger = logger;
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

                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var aiSttClient = scope.ServiceProvider.GetRequiredService<IAiSttClient>();

                var video = await db.Videos
                    .Include(v => v.VideoTranscript)
                    .FirstOrDefaultAsync(v => v.VideoId == job.VideoId, stoppingToken);

                if (video == null)
                {
                    _logger.LogWarning("Không tìm thấy video id={VideoId} trong CSDL để xử lý.", job.VideoId);
                    continue;
                }

                var aiResult = await aiSttClient.ExtractRecipeFromVideoAsync(job.VideoUrl, stoppingToken);

                if (aiResult != null && !string.IsNullOrWhiteSpace(aiResult.RawTranscript))
                {
                    if (video.VideoTranscript == null)
                    {
                        video.VideoTranscript = new VideoTranscript
                        {
                            VideoId = video.VideoId,
                            CreatedAt = DateTime.UtcNow
                        };
                        db.VideoTranscripts.Add(video.VideoTranscript);
                    }

                    video.VideoTranscript.RawTranscript = aiResult.RawTranscript;
                    video.VideoTranscript.ExtractedRecipeJson = JsonSerializer.Serialize(aiResult.Recipe, JsonOptions);
                    video.VideoTranscript.SttModelVersion = aiResult.SttModelVersion ?? "faster-whisper-small-int8";
                    video.VideoTranscript.Status = "COMPLETED";
                    video.VideoTranscript.CompletedAt = DateTime.UtcNow;

                    video.Status = "READY";
                    video.UpdatedAt = DateTime.UtcNow;

                    await db.SaveChangesAsync(stoppingToken);
                    _logger.LogInformation("Bóc tách thành công video id={VideoId}, đã cập nhật trạng thái READY.", job.VideoId);
                }
                else
                {
                    _logger.LogWarning("AI Service không bóc tách được nội dung cho video id={VideoId}. Đánh dấu VideoTranscript FAILED.", job.VideoId);
                    if (video.VideoTranscript != null)
                    {
                        video.VideoTranscript.Status = "FAILED";
                    }
                    video.UpdatedAt = DateTime.UtcNow;
                    await db.SaveChangesAsync(stoppingToken);
                }
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
}
