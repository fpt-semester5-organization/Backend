namespace VeggieLife.BLL.DTOs.Posts.Responses;

/// <summary>
/// Thông tin tác giả bài viết (FN-POST-06).
/// </summary>
public class PostAuthorDto
{
    /// <summary>
    /// Mã định danh người dùng.
    /// </summary>
    public int UserId { get; set; }

    /// <summary>
    /// Họ và tên người dùng.
    /// </summary>
    public string FullName { get; set; } = string.Empty;
}
