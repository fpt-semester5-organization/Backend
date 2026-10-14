using System.Threading;
using System.Threading.Tasks;
using VeggieLife.BLL.DTOs.ShoppingLists.Requests;
using VeggieLife.BLL.DTOs.ShoppingLists.Responses;

namespace VeggieLife.BLL.Services.ShoppingLists;

/// <summary>
/// Dịch vụ tổng hợp và quản lý danh sách đi chợ từ thực đơn tuần (FN-MEAL-05, FN-MEAL-06).
/// </summary>
public interface IShoppingListService
{
    /// <summary>
    /// FN-MEAL-05: Tổng hợp nguyên liệu của 21 bữa, cộng dồn nguyên liệu trùng, chuẩn hóa đơn vị và nhóm theo 4 quầy.
    /// </summary>
    Task<CreateMealPlanShoppingListResponseData> CreateMealPlanShoppingListAsync(
        int planId,
        int userId,
        CreateMealPlanShoppingListRequest request,
        CancellationToken ct = default);

    /// <summary>
    /// FN-MEAL-06: Xem danh sách đi chợ theo quầy kèm tiến độ mua sắm.
    /// </summary>
    Task<GetShoppingListResponseData> GetShoppingListByIdAsync(
        int listId,
        int userId,
        CancellationToken ct = default);

    /// <summary>
    /// FN-MEAL-06: Đánh dấu một món hàng đã mua / chưa mua.
    /// </summary>
    Task<PatchShoppingListItemResponseData> PatchShoppingListItemAsync(
        int listId,
        int itemId,
        int userId,
        PatchShoppingListItemRequest request,
        CancellationToken ct = default);

    /// <summary>
    /// FN-MEAL-06: Hoàn tất hoặc hủy bỏ danh sách đi chợ (COMPLETED / CANCELLED).
    /// </summary>
    Task<PatchShoppingListResponseData> PatchShoppingListAsync(
        int listId,
        int userId,
        PatchShoppingListRequest request,
        CancellationToken ct = default);
}
