using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using VeggieLife.BLL.Common.Responses;
using VeggieLife.BLL.DTOs.Admin.Moderation;

namespace VeggieLife.BLL.Services.Admin;

/// <summary>
/// Interface dịch vụ quản trị kiểm duyệt nội dung (FN-ADM-04, FN-ADM-05, FN-ADM-06).
/// </summary>
public interface IAdminModerationService
{
    /// <summary>
    /// Lấy danh sách nội dung trong hàng đợi chờ duyệt (từ AI_SCAN hoặc USER_REPORT) (FN-ADM-04).
    /// </summary>
    Task<(IReadOnlyList<AdminModerationQueueItemDto> Items, PagedMeta Meta)> GetModerationQueueAsync(
        AdminModerationQueueFilterRequest request,
        CancellationToken ct = default);

    /// <summary>
    /// Quyết định xử lý báo cáo vi phạm nội dung: Xác nhận vi phạm (UPHOLD) hoặc Bác bỏ (DISMISS) (FN-ADM-05).
    /// </summary>
    Task<DecideAdminModerationReportResponseData> DecideReportAsync(
        int reportId,
        DecideAdminModerationReportRequest request,
        int adminId,
        CancellationToken ct = default);

    /// <summary>
    /// Xóa mềm một bình luận và toàn bộ cây phản hồi con bên dưới (FN-ADM-06, BR-ADM-CMT-001).
    /// </summary>
    Task<DeleteAdminCommentResponseData> DeleteCommentAsync(
        int commentId,
        string reason,
        int adminId,
        CancellationToken ct = default);
}
