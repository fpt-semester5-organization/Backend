using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace VeggieLife.BLL.DTOs.MealPlans.Responses;

/// <summary>
/// Dinh dưỡng theo từng ngày trong tuần của thực đơn (FN-MEAL-07).
/// </summary>
public class MealPlanDayNutritionDto
{
    public string DayOfWeek { get; set; } = null!;

    public decimal Kcal { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public decimal? DeltaKcal { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyList<MealSlotNutritionDto>? Meals { get; set; }
}
