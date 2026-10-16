namespace VeggieLife.BLL.DTOs.MealPlans.Responses;

/// <summary>
/// Tóm tắt thực đơn dinh dưỡng trong danh sách (FN-MEAL-02).
/// </summary>
public class MealPlanSummaryDto
{
    public int PlanId { get; set; }
    public string PlanName { get; set; } = null!;
    public string StartDate { get; set; } = null!;
    public string PlanStatus { get; set; } = null!;
}
