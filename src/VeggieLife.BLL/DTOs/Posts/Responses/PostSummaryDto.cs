using System;

namespace VeggieLife.BLL.DTOs.Posts.Responses;

/// <summary>
/// DTO tóm tắt bài viết trên danh sách tìm kiếm (FN-POST-05).
/// </summary>
public class PostSummaryDto
{
    /// <summary>
    /// Mã định danh bài viết.
    /// </summary>
    public int PostId { get; set; }

    /// <summary>
    /// Tiêu đề bài viết.
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Họ tên tác giả bài viết.
    /// </summary>
    public string AuthorName { get; set; } = string.Empty;

    /// <summary>
    /// Đường dẫn ảnh đại diện bài viết (nếu có).
    /// </summary>
    public string? ImageUrl { get; set; }

    /// <summary>
    /// Tổng số lượt bình chọn (thả tim) của bài viết.
    /// </summary>
    public int VoteCount { get; set; }

    /// <summary>
    /// Tổng số bình luận đang hoạt động của bài viết.
    /// </summary>
    public int CommentCount { get; set; }

    /// <summary>
    /// Thời điểm khởi tạo bài viết.
    /// </summary>
    public DateTime CreatedAt { get; set; }
}
