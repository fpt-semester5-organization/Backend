namespace VeggieLife.BLL.DTOs.Posts.Responses;

/// <summary>
/// Thông tin video đính kèm bài viết (FN-POST-06).
/// </summary>
public class PostVideoDto
{
    /// <summary>
    /// Mã định danh video.
    /// </summary>
    public int VideoId { get; set; }

    /// <summary>
    /// Tiêu đề video.
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Đường dẫn phát video (Cloudinary CDN URL).
    /// </summary>
    public string VideoUrl { get; set; } = string.Empty;

    /// <summary>
    /// Ảnh thu nhỏ (thumbnail) của video.
    /// </summary>
    public string? ThumbnailUrl { get; set; }

    /// <summary>
    /// Thời lượng video tính bằng giây.
    /// </summary>
    public int DurationSeconds { get; set; }
}
