namespace VeggieLife.BLL.DTOs.Recipes.Responses;

/// <summary>
/// DTO chi tiết nguyên liệu và định lượng thực tế trong công thức nấu ăn (FN-POST-02).
/// </summary>
public class RecipeIngredientDetailDto
{
    /// <summary>
    /// Mã nguyên liệu.
    /// </summary>
    public int IngredientId { get; set; }

    /// <summary>
    /// Tên nguyên liệu thực phẩm.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Số lượng nguyên liệu cần dùng trong món ăn.
    /// </summary>
    public decimal Quantity { get; set; }

    /// <summary>
    /// Đơn vị đo thực tế (g, ml, muỗng canh, quả, ...).
    /// </summary>
    public string Unit { get; set; } = string.Empty;
}
