namespace VeggieLife.BLL.DTOs.Admin.Ingredients;

/// <summary>
/// DTO dữ liệu trả về khi cập nhật nguyên liệu thành công (FN-ADM-12).
/// </summary>
public class UpdateAdminIngredientResponseData
{
    public int IngredientId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string StandardUnit { get; set; } = string.Empty;
    public string DefaultStallType { get; set; } = string.Empty;
    public string FoodGroup { get; set; } = string.Empty;
    public decimal CaloriesPer100g { get; set; }
    public bool IsAllium { get; set; }
}
