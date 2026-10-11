namespace VeggieLife.BLL.DTOs.Posts.Responses;

/// <summary>
/// DTO dữ liệu trả về khi cập nhật bài viết thành công (FN-POST-07).
/// </summary>
public class UpdatePostResponseDto
{
    /// <summary>
    /// Mã định danh bài viết được cập nhật.
    /// </summary>
    public int PostId { get; set; }

    /// <summary>
    /// Trạng thái kiểm duyệt sau khi cập nhật: PENDING_REVIEW, APPROVED_ACTIVE, QUARANTINED_HIDDEN, DRAFT...
    /// </summary>
    public string ModerationStatus { get; set; } = null!;
}
