using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using VeggieLife.BLL.Services.Cache;

namespace VeggieLife.BLL.Services.Knowledge;

/// <summary>
/// Hàng đợi đồng bộ kho tri thức AI (Qdrant Vector DB) sử dụng Redis List (knowledge_sync_queue) với fallback In-Memory (FN-CHAT-07).
/// </summary>
public class RedisKnowledgeSyncQueue : IKnowledgeSyncQueue
{
    public const string QueueName = "knowledge_sync_queue";
    private readonly IRedisService _redis;
    private readonly ILogger<RedisKnowledgeSyncQueue> _logger;

    public RedisKnowledgeSyncQueue(
        IRedisService redis,
        ILogger<RedisKnowledgeSyncQueue> logger)
    {
        _redis = redis;
        _logger = logger;
    }

    public Task EnqueueUpsertAsync(string contentType, int contentId, CancellationToken ct = default)
    {
        return EnqueueUpsertAsync(contentType, contentId.ToString(), ct);
    }

    public async Task EnqueueUpsertAsync(string contentType, string sourceId, CancellationToken ct = default)
    {
        var job = new KnowledgeSyncJobPayload
        {
            Action = "UPSERT",
            ContentType = contentType.Trim().ToUpperInvariant(),
            SourceId = sourceId.Trim()
        };

        _logger.LogInformation("[RedisKnowledgeSyncQueue] Đưa tác vụ UPSERT vào hàng đợi: {ContentType} #{SourceId}", job.ContentType, job.SourceId);
        await _redis.EnqueueAsync(QueueName, job, ct);
    }

    public Task EnqueueDeleteAsync(string contentType, int contentId, CancellationToken ct = default)
    {
        return EnqueueDeleteAsync(contentType, contentId.ToString(), ct);
    }

    public async Task EnqueueDeleteAsync(string contentType, string sourceId, CancellationToken ct = default)
    {
        var job = new KnowledgeSyncJobPayload
        {
            Action = "DELETE",
            ContentType = contentType.Trim().ToUpperInvariant(),
            SourceId = sourceId.Trim()
        };

        _logger.LogInformation("[RedisKnowledgeSyncQueue] Đưa tác vụ DELETE vào hàng đợi: {ContentType} #{SourceId}", job.ContentType, job.SourceId);
        await _redis.EnqueueAsync(QueueName, job, ct);
    }
}
