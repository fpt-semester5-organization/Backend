namespace VeggieLife.BLL.DTOs.ShoppingLists.Requests;

/// <summary>
/// Yêu cầu cập nhật trạng thái đã mua của một món hàng trong danh sách đi chợ (FN-MEAL-06).
/// </summary>
public class PatchShoppingListItemRequest
{
    /// <summary>
    /// Trạng thái đã mua hàng hay chưa.
    /// </summary>
    public bool IsPurchased { get; set; }
}
