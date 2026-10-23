using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace VeggieLife.BLL.Services.Knowledge;

/// <summary>
/// Triển khai bản mẫu/stub của IKnowledgeSyncQueue để ghi log và sẵn sàng kết nối worker nền KnowledgeSyncWorker (FN-CHAT-07).
/// </summary>
public class KnowledgeSyncQueueStub : IKnowledgeSyncQueue
{
    private readonly ILogger<KnowledgeSyncQueueStub> _logger;

    public KnowledgeSyncQueueStub(ILogger<KnowledgeSyncQueueStub> logger)
    {
        _logger = logger;
    }

    public Task EnqueueUpsertAsync(string contentType, int contentId, CancellationToken ct = default)
    {
        return EnqueueUpsertAsync(contentType, contentId.ToString(), ct);
    }

    public Task EnqueueUpsertAsync(string contentType, string sourceId, CancellationToken ct = default)
    {
        _logger.LogInformation("[KnowledgeSyncQueueStub] Đưa yêu cầu UPSERT kho tri thức: {ContentType} #{SourceId}", contentType, sourceId);
        return Task.CompletedTask;
    }

    public Task EnqueueDeleteAsync(string contentType, int contentId, CancellationToken ct = default)
    {
        return EnqueueDeleteAsync(contentType, contentId.ToString(), ct);
    }

    public Task EnqueueDeleteAsync(string contentType, string sourceId, CancellationToken ct = default)
    {
        _logger.LogInformation("[KnowledgeSyncQueueStub] Đưa yêu cầu DELETE kho tri thức: {ContentType} #{SourceId}", contentType, sourceId);
        return Task.CompletedTask;
    }
}
