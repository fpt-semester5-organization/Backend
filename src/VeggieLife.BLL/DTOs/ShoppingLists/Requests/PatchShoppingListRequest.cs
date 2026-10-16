namespace VeggieLife.BLL.DTOs.ShoppingLists.Requests;

/// <summary>
/// Yêu cầu cập nhật trạng thái của danh sách đi chợ (FN-MEAL-06).
/// </summary>
public class PatchShoppingListRequest
{
    /// <summary>
    /// Trạng thái mới của danh sách: COMPLETED hoặc CANCELLED.
    /// </summary>
    public string Status { get; set; } = null!;
}
