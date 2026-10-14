namespace VeggieLife.BLL.DTOs.Community.Requests;

/// <summary>
/// Payload gửi báo cáo vi phạm nội dung cộng đồng.
/// Hỗ trợ báo cáo bài viết, công thức, video hoặc bình luận.
/// </summary>
public class CreateReportRequest
{
    /// <summary>
    /// Loại đối tượng bị báo cáo (BẮT BUỘC): POST | RECIPE | VIDEO | COMMENT.
    /// </summary>
    /// <example>POST</example>
    public string TargetType { get; set; } = string.Empty;

    /// <summary>
    /// Mã định danh đối tượng bị báo cáo (BẮT BUỘC, > 0).
    /// </summary>
    /// <example>6</example>
    public int TargetId { get; set; }

    /// <summary>
    /// Loại vi phạm (BẮT BUỘC): VIO_MEAT | VIO_LABEL | VIO_HATE | VIO_SPAM.
    /// </summary>
    /// <example>VIO_MEAT</example>
    public string ViolationType { get; set; } = string.Empty;

    /// <summary>
    /// Tóm tắt chứng cứ vi phạm (BẮT BUỘC, từ 10 đến 500 ký tự).
    /// </summary>
    /// <example>Bài viết hướng dẫn có sử dụng nước mắm cá cơm mặn, vi phạm chuẩn chay VeggieLife.</example>
    public string EvidenceSummary { get; set; } = string.Empty;
}
