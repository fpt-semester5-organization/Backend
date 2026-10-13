namespace VeggieLife.BLL.DTOs.MealPlans.Responses;

/// <summary>
/// Hạn mức và số lần chỉnh sửa món ăn trong thực đơn (DRAFT & ACTIVE).
/// </summary>
public class MealPlanEditsDto
{
    public int PreDeployUsed { get; set; } = 0;
    public int PreDeployLimit { get; set; } = 21;
    public int PostDeployUsed { get; set; } = 0;
    public int PostDeployLimit { get; set; } = 21;
}
