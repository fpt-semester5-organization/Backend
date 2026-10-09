namespace VeggieLife.BLL.DTOs.MasterData.Responses;

/// <summary>
/// DTO nguyên liệu thực phẩm chuẩn hóa (FN-PROF-08).
/// </summary>
public class IngredientDto
{
    public int IngredientId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string StandardUnit { get; set; } = string.Empty;
    public string FoodGroup { get; set; } = string.Empty;
    public bool IsAllium { get; set; }
    public decimal CaloriesPer100g { get; set; }
}
