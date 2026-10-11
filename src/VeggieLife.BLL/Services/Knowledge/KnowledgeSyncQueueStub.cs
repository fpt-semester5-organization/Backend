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
        _logger.LogInformation("[KnowledgeSyncQueueStub] Đưa yêu cầu UPSERT kho tri thức: {ContentType} #{ContentId}", contentType, contentId);
        return Task.CompletedTask;
    }

    public Task EnqueueDeleteAsync(string contentType, int contentId, CancellationToken ct = default)
    {
        _logger.LogInformation("[KnowledgeSyncQueueStub] Đưa yêu cầu DELETE kho tri thức: {ContentType} #{ContentId}", contentType, contentId);
        return Task.CompletedTask;
    }
}
