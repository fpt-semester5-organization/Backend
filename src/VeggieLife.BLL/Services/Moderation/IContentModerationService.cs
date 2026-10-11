using System.Threading;
using System.Threading.Tasks;

namespace VeggieLife.BLL.Services.Moderation;

/// <summary>
/// Kết quả kiểm duyệt nội dung tự động qua AI (BR-USR-03, BR-ADM-CNT-002).
/// </summary>
public class ContentModerationResult
{
    /// <summary>
    /// Cho biết nội dung có được tự động phê duyệt công khai hay không (ViolationScore < 85%).
    /// </summary>
    public bool IsApproved { get; set; }

    /// <summary>
    /// Điểm số vi phạm từ mô hình AI (0.0 đến 100.0). Điểm >= 85% bị cách ly QUARANTINED_HIDDEN.
    /// </summary>
    public double ViolationScore { get; set; }

    /// <summary>
    /// Trạng thái kiểm duyệt đề xuất: APPROVED_ACTIVE hoặc QUARANTINED_HIDDEN.
    /// </summary>
    public string ModerationStatus { get; set; } = "APPROVED_ACTIVE";

    /// <summary>
    /// Loại vi phạm nếu bị gắn cờ: VIO_MEAT, VIO_LABEL, VIO_HATE, VIO_SPAM (khớp CK_ViolationReports_Type).
    /// </summary>
    public string? ViolationType { get; set; }

    /// <summary>
    /// Ghi chú chi tiết kết quả phân tích hoặc lý do gắn cờ vi phạm từ AI.
    /// </summary>
    public string? Note { get; set; }

    /// <summary>
    /// Số lượng token ước tính tiêu thụ cho lần kiểm duyệt.
    /// </summary>
    public int TokensConsumed { get; set; } = 100;

    /// <summary>
    /// Ước tính chi phí USD cho lần kiểm duyệt.
    /// </summary>
    public decimal CostEstimateUsd { get; set; } = 0.0001m;

    /// <summary>
    /// Thời gian thực thi kiểm duyệt tính theo mili-giây.
    /// </summary>
    public int ExecutionTimeMs { get; set; } = 50;
}

/// <summary>
/// Interface dịch vụ kiểm duyệt nội dung bài viết, công thức, bình luận tự động qua AI (BR-USR-03, BR-ADM-CNT-002).
/// </summary>
public interface IContentModerationService
{
    /// <summary>
    /// Quét kiểm duyệt nội dung bài viết bao gồm tiêu đề, văn bản và hình ảnh.
    /// </summary>
    /// <param name="title">Tiêu đề bài viết.</param>
    /// <param name="content">Nội dung chi tiết của bài viết.</param>
    /// <param name="imageUrl">URL hình ảnh đính kèm (nếu có).</param>
    /// <param name="ct">CancellationToken.</param>
    /// <returns>Kết quả kiểm duyệt gồm điểm vi phạm, trạng thái đề xuất và lý do.</returns>
    Task<ContentModerationResult> ModeratePostAsync(
        string title,
        string content,
        string? imageUrl = null,
        CancellationToken ct = default);
}
