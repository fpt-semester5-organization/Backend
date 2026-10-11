namespace VeggieLife.BLL.DTOs.Posts.Responses;

/// <summary>
/// DTO dữ liệu trả về khi đăng bài viết thành công (FN-POST-07).
/// </summary>
public class CreatePostResponseDto
{
    /// <summary>
    /// Mã định danh bài viết vừa tạo.
    /// </summary>
    public int PostId { get; set; }

    /// <summary>
    /// Trạng thái kiểm duyệt của bài viết: DRAFT, PENDING_REVIEW, APPROVED_ACTIVE, QUARANTINED_HIDDEN, REJECTED, DELETED.
    /// </summary>
    public string ModerationStatus { get; set; } = null!;
}
