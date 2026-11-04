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
    /// Mã định danh công thức nếu bài viết đại diện hoặc liên kết với công thức nấu ăn.
    /// </summary>
    public int? RecipeId { get; set; }

    /// <summary>
    /// Tiêu đề bài viết.
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Họ tên tác giả bài viết.
    /// </summary>
    public string AuthorName { get; set; } = string.Empty;

    /// <summary>
    /// Đường dẫn ảnh đại diện của tác giả bài viết (nếu có).
    /// </summary>
    public string? AuthorAvatarUrl { get; set; }

    /// <summary>
    /// Đường dẫn ảnh đại diện bài viết (nếu có).
    /// </summary>
    public string? ImageUrl { get; set; }

    /// <summary>
    /// Tổng số lượt bình chọn (thả tim) của bài viết.
    /// </summary>
    public int VoteCount { get; set; }

    /// <summary>
    /// Người dùng hiện tại đã thả tim nội dung này chưa (luôn false với Khách).
    /// </summary>
    public bool HasVoted { get; set; }

    /// <summary>
    /// Tổng số bình luận đang hoạt động của bài viết.
    /// </summary>
    public int CommentCount { get; set; }

    /// <summary>
    /// Thời điểm khởi tạo bài viết.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Trạng thái kiểm duyệt bài viết (DRAFT, PENDING, APPROVED_ACTIVE, REJECTED, QUARANTINED_HIDDEN).
    /// Chỉ hiển thị khi tác giả tra cứu danh sách bài viết của chính mình (authorId == currentUserId).
    /// </summary>
    public string? ModerationStatus { get; set; }

    /// <summary>
    /// Phạm vi hiển thị bài viết (PUBLIC, PRIVATE).
    /// Chỉ hiển thị khi tác giả tra cứu danh sách bài viết của chính mình (authorId == currentUserId).
    /// </summary>
    public string? Visibility { get; set; }

    /// <summary>
    /// Đánh dấu bài viết có kèm video hay không.
    /// </summary>
    public bool HasVideo { get; set; }

    /// <summary>
    /// Phân loại bài viết ("POST" hoặc "VIDEO").
    /// </summary>
    public string Type { get; set; } = "POST";

    /// <summary>
    /// Mã định danh video đính kèm (nếu có).
    /// </summary>
    public int? VideoId { get; set; }

    /// <summary>
    /// Đường dẫn phát video đính kèm (nếu có).
    /// </summary>
    public string? VideoUrl { get; set; }
}
