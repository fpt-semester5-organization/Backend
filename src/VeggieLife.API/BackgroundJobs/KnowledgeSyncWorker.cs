using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using VeggieLife.BLL.Services.Cache;
using VeggieLife.BLL.Services.Knowledge;

namespace VeggieLife.API.BackgroundJobs;

/// <summary>
/// Background Service tiêu thụ hàng đợi knowledge_sync_queue (FIFO), truy vấn CSDL và đồng bộ vector ngữ nghĩa sang AI Microservice (FN-CHAT-07).
/// </summary>
public class KnowledgeSyncWorker : BackgroundService
{
    private readonly IRedisService _redis;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<KnowledgeSyncWorker> _logger;

    public KnowledgeSyncWorker(
        IRedisService redis,
        IServiceScopeFactory scopeFactory,
        ILogger<KnowledgeSyncWorker> logger)
    {
        _redis = redis;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("[KnowledgeSyncWorker] Worker nền đồng bộ kho tri thức AI bắt đầu khởi chạy.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var job = await _redis.DequeueAsync<KnowledgeSyncJobPayload>(RedisKnowledgeSyncQueue.QueueName, stoppingToken);
                if (job == null)
                {
                    await Task.Delay(2000, stoppingToken);
                    continue;
                }

                await ProcessJobAsync(job, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[KnowledgeSyncWorker] Lỗi không mong muốn trong vòng lặp tiêu thụ hàng đợi.");
                await Task.Delay(3000, stoppingToken);
            }
        }

        _logger.LogInformation("[KnowledgeSyncWorker] Worker nền đồng bộ kho tri thức AI đã dừng.");
    }

    private async Task ProcessJobAsync(KnowledgeSyncJobPayload job, CancellationToken ct)
    {
        _logger.LogInformation("[KnowledgeSyncWorker] Bắt đầu xử lý tác vụ: {Action} [{Type} #{Id}] (Lần thử: {Retry})",
            job.Action, job.ContentType, job.SourceId, job.RetryCount);

        try
        {
            using var scope = _scopeFactory.CreateScope();
            var syncService = scope.ServiceProvider.GetRequiredService<IKnowledgeSyncService>();

            if (string.Equals(job.Action, "DELETE", StringComparison.OrdinalIgnoreCase))
            {
                await syncService.DeleteAsync(job.ContentType, job.SourceId, ct);
                return;
            }

            // Xử lý UPSERT theo từng nguồn tri thức
            switch (job.ContentType.ToUpperInvariant())
            {
                case "RECIPE":
                    if (int.TryParse(job.SourceId, out var recipeId))
                    {
                        await syncService.SyncRecipeAsync(recipeId, ct);
                    }
                    break;

                case "POST":
                    if (int.TryParse(job.SourceId, out var postId))
                    {
                        await syncService.SyncPostAsync(postId, ct);
                    }
                    break;

                case "E_NUMBER":
                    await syncService.SyncENumberAsync(job.SourceId, ct);
                    break;

                case "INGREDIENT":
                    if (int.TryParse(job.SourceId, out var ingredientId))
                    {
                        await syncService.SyncIngredientAsync(ingredientId, ct);
                    }
                    break;

                case "ALLERGEN":
                    if (int.TryParse(job.SourceId, out var allergenId))
                    {
                        await syncService.SyncAllergenAsync(allergenId, ct);
                    }
                    break;

                default:
                    _logger.LogWarning("[KnowledgeSyncWorker] Bỏ qua nguồn tri thức không hỗ trợ: {ContentType}", job.ContentType);
                    break;
            }

            _logger.LogInformation("[KnowledgeSyncWorker] Hoàn tất xử lý tác vụ {Action} [{Type} #{Id}].",
                job.Action, job.ContentType, job.SourceId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[KnowledgeSyncWorker] Thất bại khi xử lý tác vụ {Action} [{Type} #{Id}]",
                job.Action, job.ContentType, job.SourceId);

            if (job.RetryCount < 3)
            {
                job.RetryCount++;
                var backoffDelay = TimeSpan.FromSeconds(Math.Pow(2, job.RetryCount));
                _logger.LogWarning("[KnowledgeSyncWorker] Sẽ thử lại tác vụ {Action} [{Type} #{Id}] sau {Delay}s.",
                    job.Action, job.ContentType, job.SourceId, backoffDelay.TotalSeconds);

                await Task.Delay(backoffDelay, ct);
                await _redis.EnqueueAsync(RedisKnowledgeSyncQueue.QueueName, job, ct);
            }
            else
            {
                _logger.LogCritical("[KnowledgeSyncWorker] Tác vụ {Action} [{Type} #{Id}] đã thử lại 3 lần thất bại, chuyển vào log lỗi chết (Dead-letter).",
                    job.Action, job.ContentType, job.SourceId);
            }
        }
    }
}
