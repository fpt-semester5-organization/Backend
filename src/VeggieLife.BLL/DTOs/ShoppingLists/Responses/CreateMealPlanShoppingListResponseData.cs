using System.Collections.Generic;

namespace VeggieLife.BLL.DTOs.ShoppingLists.Responses;

/// <summary>
/// Dữ liệu phản hồi sinh danh sách đi chợ thành công (FN-MEAL-05).
/// </summary>
public class CreateMealPlanShoppingListResponseData
{
    public int ListId { get; set; }

    public int PlanId { get; set; }

    public string Status { get; set; } = "PENDING";

    public IReadOnlyList<ShoppingListAisleDto> Aisles { get; set; } = new List<ShoppingListAisleDto>();
}
