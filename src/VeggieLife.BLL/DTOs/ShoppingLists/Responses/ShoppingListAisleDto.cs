using System.Collections.Generic;

namespace VeggieLife.BLL.DTOs.ShoppingLists.Responses;

/// <summary>
/// Quầy hàng đi chợ phân nhóm nguyên liệu (PRODUCE, CONDIMENTS, DRY_GOODS, PLANT_PROTEIN).
/// </summary>
public class ShoppingListAisleDto
{
    public string Aisle { get; set; } = null!;

    public IReadOnlyList<ShoppingListItemDto> Items { get; set; } = new List<ShoppingListItemDto>();
}
