using System.Collections.Generic;

namespace VeggieLife.BLL.DTOs.Admin.Ingredients;

/// <summary>
/// DTO đại diện cho một nguyên liệu trong danh sách trả về cho Quản trị viên (FN-ADM-12).
/// </summary>
public class AdminIngredientListItemDto
{
    public int IngredientId { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal CaloriesPer100g { get; set; }
    public bool IsAllium { get; set; }
    public string DefaultStallType { get; set; } = string.Empty;
    public string FoodGroup { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public bool IsLocked { get; set; }
    /// <summary>Số ảnh ứng viên PENDING từ tủ lạnh người dùng đang chờ duyệt (BR-ADM-ING-02).</summary>
    public int CandidateCount { get; set; }
    public List<AdminIngredientAllergenItemDto> Allergens { get; set; } = new();
}

/// <summary>
/// DTO thông tin nhóm dị ứng gắn với nguyên liệu (FN-ADM-12).
/// </summary>
public class AdminIngredientAllergenItemDto
{
    public int AllergenId { get; set; }
    public string Name { get; set; } = string.Empty;
}
