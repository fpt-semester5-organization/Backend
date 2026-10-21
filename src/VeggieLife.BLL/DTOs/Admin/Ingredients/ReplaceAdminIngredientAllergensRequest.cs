using System.Collections.Generic;

namespace VeggieLife.BLL.DTOs.Admin.Ingredients;

/// <summary>
/// DTO yêu cầu gán/thay thế toàn bộ nhóm dị ứng cho nguyên liệu (FN-ADM-12).
/// </summary>
public class ReplaceAdminIngredientAllergensRequest
{
    public List<int> AllergenIds { get; set; } = new();
}
