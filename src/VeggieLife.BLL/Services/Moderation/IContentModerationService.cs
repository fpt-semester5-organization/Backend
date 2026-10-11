using System.Threading;
using System.Threading.Tasks;
using VeggieLife.BLL.DTOs.Ai;

namespace VeggieLife.BLL.Services.Moderation;

/// <summary>
/// Kết quả kiểm duyệt nội dung tự động qua AI (BR-USR-03, BR-ADM-CNT-002, VL-SPEC-AI-MOD-01).
/// </summary>
public class ContentModerationResult
{
    /// <summary>
    /// Cho biết nội dung có được tự động phê duyệt công khai hay không (ViolationScore < 0.85 và ViolationType == NONE).
    /// </summary>
    public bool IsApproved { get; set; }

    /// <summary>
    /// Điểm số vi phạm từ mô hình AI (0.0 đến 1.0 hoặc 0.0 đến 100.0). Điểm >= 85% bị cách ly QUARANTINED_HIDDEN.
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

    /// <summary>
    /// Thông tin tiêu thụ tài nguyên chi tiết từ AI Service.
    /// </summary>
    public ModerationUsage? Usage { get; set; }

    /// <summary>
    /// Dữ liệu kiểm toán prompt & raw response từ AI Service.
    /// </summary>
    public ModerationAudit? Audit { get; set; }
}

/// <summary>
/// Interface dịch vụ kiểm duyệt nội dung bài viết, công thức, bình luận tự động qua AI.
/// Tuân thủ quy chuẩn VL-SPEC-AI-MOD-01, BR-ADM-CNT-001..003, BR-REC-MOD-01.
/// </summary>
public interface IContentModerationService
{
    /// <summary>
    /// Gọi kiểm duyệt nội dung qua API endpoint /moderation/check của AI Service (VL-SPEC-AI-MOD-01).
    /// </summary>
    /// <param name="request">Payload chứa tiêu đề, mô tả, danh sách nguyên liệu, các bước, trường phái, ảnh.</param>
    /// <param name="cancellationToken">CancellationToken.</param>
    /// <returns>Dữ liệu phản hồi điểm số vi phạm, loại vi phạm, lý do và thông tin kiểm toán.</returns>
    Task<ModerationCheckResponse> CheckContentAsync(
        ModerationCheckRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Quét kiểm duyệt nội dung bài viết nhanh bao gồm tiêu đề, văn bản và hình ảnh.
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
