using System.Collections.Generic;

namespace VeggieLife.BLL.DTOs.ShoppingLists.Responses;

/// <summary>
/// Dữ liệu phản hồi xem danh sách đi chợ theo quầy và tiến độ (FN-MEAL-06).
/// </summary>
public class GetShoppingListResponseData
{
    public int ListId { get; set; }

    public string Status { get; set; } = "PENDING";

    public ShoppingListProgressDto Progress { get; set; } = new ShoppingListProgressDto();

    public IReadOnlyList<ShoppingListAisleDto> Aisles { get; set; } = new List<ShoppingListAisleDto>();
}
