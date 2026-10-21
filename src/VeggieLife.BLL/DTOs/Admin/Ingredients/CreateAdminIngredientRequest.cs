namespace VeggieLife.BLL.DTOs.Admin.Ingredients;

/// <summary>
/// DTO yêu cầu thêm nguyên liệu mới (FN-ADM-12).
/// </summary>
public class CreateAdminIngredientRequest
{
    public string Name { get; set; } = string.Empty;
    public string? StandardUnit { get; set; } = "g";
    public string DefaultStallType { get; set; } = string.Empty;
    public string FoodGroup { get; set; } = "PLANT";
    public decimal CaloriesPer100g { get; set; }
    public bool IsAllium { get; set; }
}
