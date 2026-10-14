using System.Collections.Generic;

namespace VeggieLife.BLL.DTOs.MealPlans.Responses;

/// <summary>
/// Dữ liệu phản hồi xem calo thực đơn (FN-MEAL-07: BASIC cho Free, DETAILED cho VIP).
/// </summary>
public class GetMealPlanNutritionResponseData
{
    public int PlanId { get; set; }

    public string Level { get; set; } = "BASIC";

    public decimal TargetKcalPerDay { get; set; }

    public decimal TotalKcalWeek { get; set; }

    public IReadOnlyList<MealPlanDayNutritionDto> Days { get; set; } = new List<MealPlanDayNutritionDto>();
}
