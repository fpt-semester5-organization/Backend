namespace VeggieLife.BLL.DTOs.Admin.Ingredients;

/// <summary>
/// DTO yêu cầu cập nhật nguyên liệu thực phẩm (FN-ADM-12).
/// </summary>
public class UpdateAdminIngredientRequest
{
    public string? Name { get; set; }
    public string? StandardUnit { get; set; }
    public string? DefaultStallType { get; set; }
    public string? FoodGroup { get; set; }
    public decimal? CaloriesPer100g { get; set; }
    public bool? IsAllium { get; set; }
}
