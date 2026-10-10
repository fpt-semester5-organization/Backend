namespace VeggieLife.BLL.DTOs.Posts.Responses;

/// <summary>
/// Thông tin công thức nấu ăn đính kèm bài viết (FN-POST-06).
/// </summary>
public class PostRecipeDto
{
    /// <summary>
    /// Mã định danh công thức.
    /// </summary>
    public int RecipeId { get; set; }

    /// <summary>
    /// Tiêu đề công thức nấu ăn.
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Đường dẫn ảnh minh họa công thức.
    /// </summary>
    public string? ImageUrl { get; set; }

    /// <summary>
    /// Mã trường phái chay (ví dụ: VEGAN, BUDDHIST, LACTO,...).
    /// </summary>
    public string? DietarySchool { get; set; }

    /// <summary>
    /// Thời gian nấu (phút).
    /// </summary>
    public int CookTimeMinutes { get; set; }
}
