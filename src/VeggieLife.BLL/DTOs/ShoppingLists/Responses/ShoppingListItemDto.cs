using System.Collections.Generic;

namespace VeggieLife.BLL.DTOs.ShoppingLists.Responses;

/// <summary>
/// Chi tiết một món hàng nguyên liệu trong danh sách đi chợ (FN-MEAL-05, FN-MEAL-06).
/// </summary>
public class ShoppingListItemDto
{
    public int ItemId { get; set; }

    public int IngredientId { get; set; }

    public string Name { get; set; } = null!;

    public decimal Quantity { get; set; }

    public string Unit { get; set; } = null!;

    public bool IsPurchased { get; set; }

    public IReadOnlyList<string> RecipeSources { get; set; } = new List<string>();

    /// <summary>
    /// Định lượng chính xác theo từng ngày trong thực đơn (key là chỉ số ngày: "0", "1", "2"...).
    /// </summary>
    public Dictionary<string, decimal> DayQuantities { get; set; } = new();
}
