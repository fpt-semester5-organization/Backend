namespace VeggieLife.BLL.DTOs.ShoppingLists.Responses;

/// <summary>
/// Dữ liệu phản hồi cập nhật trạng thái danh sách đi chợ (FN-MEAL-06).
/// </summary>
public class PatchShoppingListResponseData
{
    public int ListId { get; set; }

    public string Status { get; set; } = null!;
}
