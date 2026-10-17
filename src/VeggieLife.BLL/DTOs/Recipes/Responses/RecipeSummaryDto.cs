namespace VeggieLife.BLL.DTOs.Recipes.Responses;

/// <summary>
/// DTO tóm tắt thông tin công thức / video món chay trên danh sách tìm kiếm (FN-POST-01).
/// </summary>
public class RecipeSummaryDto
{
    /// <summary>
    /// Mã định danh công thức.
    /// </summary>
    public int RecipeId { get; set; }

    /// <summary>
    /// Loại nội dung: RECIPE hoặc VIDEO.
    /// </summary>
    public string Type { get; set; } = "RECIPE";

    /// <summary>
    /// Tiêu đề công thức nấu ăn.
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Đường dẫn ảnh đại diện món ăn.
    /// </summary>
    public string? ImageUrl { get; set; }

    /// <summary>
    /// Mã trường phái chay (ví dụ: VEGAN, BUDDHIST, LACTO, ...).
    /// </summary>
    public string DietarySchool { get; set; } = string.Empty;

    /// <summary>
    /// Thời gian nấu chín (phút).
    /// </summary>
    public int CookTimeMinutes { get; set; }

    /// <summary>
    /// Năng lượng món ăn (kcal).
    /// </summary>
    public decimal CaloriesKcal { get; set; }

    /// <summary>
    /// Tổng số lượt bình chọn (thả tim) của bài viết đại diện cho công thức.
    /// </summary>
    public int VoteCount { get; set; }

    /// <summary>
    /// Huy hiệu xác nhận chuẩn chay từ Quản trị viên (Admin).
    /// </summary>
    public bool IsVerified { get; set; }

    /// <summary>
    /// Mã định danh video liên kết nếu loại nội dung là VIDEO (BUG-FL05-10).
    /// </summary>
    public int? VideoId { get; set; }
}

/// <summary>
/// Bí danh theo hợp đồng Frontend cho RecipeSummaryDto (BUG-FL05-10).
/// </summary>
public class RecipeListItemDto : RecipeSummaryDto
{
}

