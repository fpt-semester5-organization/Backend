namespace VeggieLife.BLL.DTOs.MealPlans.Responses;

/// <summary>
/// Thông tin công thức nấu ăn được gán vào bữa ăn trong thực đơn.
/// </summary>
public class MealSlotRecipeDto
{
    public int RecipeId { get; set; }
    public string Title { get; set; } = null!;
    public decimal CaloriesKcal { get; set; }
    public string? ImageUrl { get; set; }
}
