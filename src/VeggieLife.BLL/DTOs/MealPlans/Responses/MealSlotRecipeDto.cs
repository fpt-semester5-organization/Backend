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
    public int? FoodId { get; set; }
    public string? FoodName { get; set; }
    public string? FoodImageUrl { get; set; }
    public int? CategoryId { get; set; }
    public string? CategoryName { get; set; }
}
