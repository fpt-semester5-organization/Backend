namespace VeggieLife.BLL.DTOs.Posts.Requests;

/// <summary>
/// Request DTO tạo bài viết chia sẻ mới (FN-POST-07).
/// </summary>
public class CreatePostRequest
{
    /// <summary>
    /// Tiêu đề bài viết (10–150 ký tự, bắt buộc).
    /// </summary>
    public string Title { get; set; } = null!;

    /// <summary>
    /// Nội dung bài viết (không rỗng, bắt buộc).
    /// </summary>
    public string Content { get; set; } = null!;

    /// <summary>
    /// URL hình ảnh tải lên Cloudinary (tùy chọn).
    /// </summary>
    public string? ImageUrl { get; set; }

    /// <summary>
    /// Chế độ hiển thị: PUBLIC | PRIVATE (mặc định PUBLIC).
    /// </summary>
    public string? Visibility { get; set; }

    /// <summary>
    /// Cờ gửi duyệt (false = lưu nháp DRAFT, true = gửi kiểm duyệt AI). Mặc định false.
    /// </summary>
    public bool? Submit { get; set; }
}
