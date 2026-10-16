using System.Collections.Generic;

namespace VeggieLife.BLL.DTOs.MealPlans.Requests;

/// <summary>
/// Yêu cầu đổi món trong thực đơn (FN-MEAL-03).
/// </summary>
public class UpdateMealPlanSlotRequest
{
    /// <summary>
    /// Lý do đổi món: TASTE_PREFERENCE | MISSING_INGREDIENT | TIME_LIMIT.
    /// </summary>
    public string? SwapReason { get; set; }

    /// <summary>
    /// Danh sách mã công thức không muốn chọn làm món thay thế.
    /// </summary>
    public List<int>? ExcludedRecipeIds { get; set; }
}
