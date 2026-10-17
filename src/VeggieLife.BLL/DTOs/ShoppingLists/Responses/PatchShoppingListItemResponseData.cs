namespace VeggieLife.BLL.DTOs.ShoppingLists.Responses;

/// <summary>
/// Dữ liệu phản hồi đánh dấu món hàng đã mua / chưa mua (FN-MEAL-06).
/// </summary>
public class PatchShoppingListItemResponseData
{
    public int ItemId { get; set; }

    public bool IsPurchased { get; set; }

    public string ListStatus { get; set; } = "PENDING";
}
