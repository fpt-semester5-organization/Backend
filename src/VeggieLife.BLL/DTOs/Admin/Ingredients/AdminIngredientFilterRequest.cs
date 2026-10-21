namespace VeggieLife.BLL.DTOs.Admin.Ingredients;

/// <summary>
/// DTO tham số lọc danh sách nguyên liệu cho Quản trị viên (FN-ADM-12).
/// </summary>
public class AdminIngredientFilterRequest
{
    public string? Keyword { get; set; }
    public int? AllergenId { get; set; }
    public int Page { get; set; } = 1;
    public int Size { get; set; } = 20;
}
