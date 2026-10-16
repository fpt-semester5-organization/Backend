using System;

namespace VeggieLife.BLL.DTOs.ShoppingLists.Requests;

/// <summary>
/// Yêu cầu sinh danh sách đi chợ từ thực đơn tuần (FN-MEAL-05).
/// </summary>
public class CreateMealPlanShoppingListRequest
{
    /// <summary>
    /// Hệ số nhân khẩu phần (1.0 đến 10.0, mặc định 1.0).
    /// </summary>
    public decimal ServingsMultiplier { get; set; } = 1.0m;

    /// <summary>
    /// Ngày dự kiến đi chợ (YYYY-MM-DD, tùy chọn).
    /// </summary>
    public DateOnly? TargetShoppingDate { get; set; }
}
