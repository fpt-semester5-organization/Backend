using System.Threading;
using System.Threading.Tasks;

namespace VeggieLife.BLL.Services.Knowledge;

/// <summary>
/// Interface hàng đợi đồng bộ kho tri thức AI (Qdrant Vector DB) khi nội dung thay đổi trạng thái (D99, D118, FN-CHAT-07).
/// </summary>
public interface IKnowledgeSyncQueue
{
    /// <summary>
    /// Đưa yêu cầu nạp hoặc cập nhật vector embedding của nội dung công khai đã duyệt vào hàng đợi xử lý nền.
    /// </summary>
    Task EnqueueUpsertAsync(string contentType, int contentId, CancellationToken ct = default);

    /// <summary>
    /// Đưa yêu cầu xóa vector embedding của nội dung khỏi kho tri thức vào hàng đợi xử lý nền khi bài viết/công thức bị gỡ hoặc xóa.
    /// </summary>
    Task EnqueueDeleteAsync(string contentType, int contentId, CancellationToken ct = default);
}
