using System.Collections.Generic;

namespace VeggieLife.BLL.DTOs.Admin.Ingredients;

/// <summary>
/// DTO dữ liệu trả về khi gán/thay thế nhóm dị ứng cho nguyên liệu thành công (FN-ADM-12).
/// </summary>
public class ReplaceAdminIngredientAllergensResponseData
{
    public int IngredientId { get; set; }
    public List<int> AllergenIds { get; set; } = new();
}
