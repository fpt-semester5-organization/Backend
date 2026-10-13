using System.Collections.Generic;

namespace VeggieLife.BLL.DTOs.MealPlans.Responses;

/// <summary>
/// Dữ liệu phản hồi chi tiết sau khi tạo thực đơn 7 ngày bằng AI (FN-MEAL-01).
/// Chuẩn hóa Envelope Response (D52).
/// </summary>
public class CreateMealPlanResponseData
{
    public int PlanId { get; set; }
    public string PlanName { get; set; } = null!;
    public string StartDate { get; set; } = null!;
    public string EndDate { get; set; } = null!;
    public string BudgetTier { get; set; } = null!;
    public string? CustomInstructions { get; set; }
    public decimal TargetDailyKcal { get; set; }
    public string PlanStatus { get; set; } = "DRAFT";
    public bool GeneratedByAi { get; set; }
    public List<MealSlotDto> Slots { get; set; } = new();
    public MealPlanEditsDto Edits { get; set; } = new();
}
