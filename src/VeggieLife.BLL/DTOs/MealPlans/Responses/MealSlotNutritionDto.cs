namespace VeggieLife.BLL.DTOs.MealPlans.Responses;

/// <summary>
/// Chi tiết dinh dưỡng từng món ăn trong một bữa (FN-MEAL-07 - VIP).
/// </summary>
public class MealSlotNutritionDto
{
    public string MealType { get; set; } = null!;
    public int RecipeId { get; set; }
    public string Title { get; set; } = null!;
    public decimal Kcal { get; set; }
}
