namespace VeggieLife.BLL.DTOs.Posts.Requests;

/// <summary>
/// Request DTO cập nhật bài viết chia sẻ (FN-POST-07).
/// </summary>
public class UpdatePostRequest
{
    /// <summary>
    /// Tiêu đề bài viết mới (10–150 ký tự, tùy chọn).
    /// </summary>
    public string? Title { get; set; }

    /// <summary>
    /// Nội dung bài viết mới (không rỗng, tùy chọn).
    /// </summary>
    public string? Content { get; set; }

    /// <summary>
    /// URL hình ảnh mới trên Cloudinary (tùy chọn).
    /// </summary>
    public string? ImageUrl { get; set; }

    /// <summary>
    /// Chế độ hiển thị mới: PUBLIC | PRIVATE (tùy chọn).
    /// </summary>
    public string? Visibility { get; set; }

    /// <summary>
    /// Cờ gửi duyệt khi tác giả hoàn thiện bản nháp hoặc cập nhật (tùy chọn).
    /// </summary>
    public bool? Submit { get; set; }
}
