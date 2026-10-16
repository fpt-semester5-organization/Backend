namespace VeggieLife.BLL.DTOs.Community.Responses;

/// <summary>
/// Thông tin tác giả của bình luận (FN-COMM-01, FN-COMM-02, FN-COMM-03).
/// </summary>
public class CommentAuthorDto
{
    /// <summary>
    /// Mã người dùng.
    /// </summary>
    public int UserId { get; set; }

    /// <summary>
    /// Họ tên hiển thị của người dùng.
    /// </summary>
    public string FullName { get; set; } = string.Empty;
}
