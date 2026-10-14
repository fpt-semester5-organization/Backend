using System.Collections.Generic;

namespace VeggieLife.BLL.DTOs.Fridge.Responses;

/// <summary>
/// DTO phản hồi gợi ý công thức món ăn từ nguyên liệu tủ lạnh ảo (FN-VIS-03).
/// </summary>
public class FridgeRecipeSuggestionDto
{
    /// <summary>
    /// Mã định danh công thức món ăn.
    /// </summary>
    public int RecipeId { get; set; }

    /// <summary>
    /// Tiêu đề công thức món ăn.
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Điểm tương đồng nguyên liệu (0.7 - 1.0).
    /// </summary>
    public double Similarity { get; set; }

    /// <summary>
    /// Danh sách nguyên liệu cần có trong công thức mà người dùng còn thiếu trong tủ lạnh.
    /// </summary>
    public IReadOnlyList<MissingIngredientDto> MissingIngredients { get; set; } = new List<MissingIngredientDto>();

    /// <summary>
    /// Đánh dấu công thức có tận dụng nguyên liệu đang NON-FRESH (cần dùng sớm) trong tủ lạnh hay không.
    /// </summary>
    public bool UsesNonFreshItems { get; set; }
}

/// <summary>
/// Chi tiết nguyên liệu còn thiếu trong công thức (FN-VIS-03).
/// </summary>
public class MissingIngredientDto
{
    /// <summary>
    /// Mã nguyên liệu trong hệ thống.
    /// </summary>
    public int IngredientId { get; set; }

    /// <summary>
    /// Tên nguyên liệu.
    /// </summary>
    public string Name { get; set; } = string.Empty;
}
