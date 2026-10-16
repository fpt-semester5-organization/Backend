using System.Collections.Generic;

namespace VeggieLife.BLL.DTOs.Recipes.Responses;

/// <summary>
/// DTO dữ liệu phản hồi danh sách nội dung liên quan khi xem chi tiết một công thức (FN-POST-11).
/// </summary>
public class RelatedRecipesResponseData
{
    /// <summary>
    /// Danh sách công thức tương đồng liên quan (cùng trường phái, chung nguyên liệu, nhiều votes).
    /// </summary>
    public IReadOnlyList<RecipeSummaryDto> Recipes { get; set; } = new List<RecipeSummaryDto>();

    /// <summary>
    /// Danh sách bài viết liên quan có từ khóa trùng tiêu đề công thức.
    /// </summary>
    public IReadOnlyList<RelatedPostDto> Posts { get; set; } = new List<RelatedPostDto>();

    /// <summary>
    /// Danh sách video nấu ăn liên quan có từ khóa trùng tiêu đề công thức.
    /// </summary>
    public IReadOnlyList<RelatedVideoDto> Videos { get; set; } = new List<RelatedVideoDto>();
}

/// <summary>
/// DTO tóm tắt bài viết liên quan (FN-POST-11).
/// </summary>
public class RelatedPostDto
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
    /// Đường dẫn ảnh đại diện bài viết.
    /// </summary>
    public string? ImageUrl { get; set; }

    /// <summary>
    /// Tên tác giả bài viết.
    /// </summary>
    public string AuthorName { get; set; } = string.Empty;

    /// <summary>
    /// Lượt vote / thích của bài viết.
    /// </summary>
    public int VoteCount { get; set; }

    /// <summary>
    /// Lượt bình luận của bài viết.
    /// </summary>
    public int CommentCount { get; set; }
}

/// <summary>
/// DTO tóm tắt video nấu ăn liên quan (FN-POST-11).
/// </summary>
public class RelatedVideoDto
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
    /// Đường dẫn video.
    /// </summary>
    public string VideoUrl { get; set; } = string.Empty;

    /// <summary>
    /// Ảnh thu nhỏ đại diện video.
    /// </summary>
    public string? ThumbnailUrl { get; set; }

    /// <summary>
    /// Thời lượng video tính bằng giây.
    /// </summary>
    public int DurationSeconds { get; set; }

    /// <summary>
    /// Lượt xem video.
    /// </summary>
    public int ViewCount { get; set; }
}
