namespace VeggieLife.BLL.DTOs.MealPlans.Responses;

/// <summary>
/// Dữ liệu phản hồi kích hoạt thực đơn (FN-MEAL-02).
/// </summary>
public class ActivateMealPlanResponseData
{
    public int PlanId { get; set; }
    public string PlanStatus { get; set; } = "ACTIVE";
}
