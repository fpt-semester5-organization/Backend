using System;

namespace VeggieLife.BLL.DTOs.Videos.Responses;

/// <summary>
/// DTO thông tin chi tiết siêu dữ liệu của một video độc lập (FN-POST-12).
/// Cho phép phát video trực tiếp qua /videos/:id mà không cần kèm theo query param ?postId=.
/// </summary>
public class VideoDetailResponseData
{
    public int VideoId { get; set; }
    public int PostId { get; set; }
    public int? RecipeId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string VideoUrl { get; set; } = string.Empty;
    public string? ThumbnailUrl { get; set; }
    public int DurationSeconds { get; set; }
    public string Status { get; set; } = string.Empty;
    public int ViewCount { get; set; }
    public VideoAuthorDto? Author { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

/// <summary>
/// DTO tác giả đăng tải video.
/// </summary>
public class VideoAuthorDto
{
    public int UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
}
