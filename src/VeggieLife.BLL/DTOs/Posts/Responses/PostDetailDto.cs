using System.Collections.Generic;

namespace VeggieLife.BLL.DTOs.Posts.Responses;

/// <summary>
/// DTO chi tiết bài viết chia sẻ kinh nghiệm cộng đồng (FN-POST-06).
/// </summary>
public class PostDetailDto
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
    /// Nội dung toàn văn bài viết.
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// Đường dẫn ảnh đại diện bài viết (nếu có).
    /// </summary>
    public string? ImageUrl { get; set; }

    /// <summary>
    /// Thông tin tác giả bài viết.
    /// </summary>
    public PostAuthorDto Author { get; set; } = new();

    /// <summary>
    /// Danh sách video đính kèm bài viết.
    /// </summary>
    public List<PostVideoDto> Videos { get; set; } = new();

    /// <summary>
    /// Danh sách công thức đính kèm bài viết.
    /// </summary>
    public List<PostRecipeDto> Recipes { get; set; } = new();

    /// <summary>
    /// Tổng số lượt bình chọn (thả tim) của bài viết.
    /// </summary>
    public int VoteCount { get; set; }

    /// <summary>
    /// Tổng số bình luận của bài viết.
    /// </summary>
    public int CommentCount { get; set; }

    /// <summary>
    /// Quyền tương tác của người xem hiện tại (BR-AUTH-03).
    /// </summary>
    public PostMemberActionsDto MemberActions { get; set; } = new();
}
