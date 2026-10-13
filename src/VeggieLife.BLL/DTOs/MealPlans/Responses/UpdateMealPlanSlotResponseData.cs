namespace VeggieLife.BLL.DTOs.MealPlans.Responses;

/// <summary>
/// Dữ liệu phản hồi đổi món thành công (FN-MEAL-03).
/// </summary>
public class UpdateMealPlanSlotResponseData
{
    public MealSlotDto Slot { get; set; } = null!;
    public int SwapsRemaining { get; set; }
}
