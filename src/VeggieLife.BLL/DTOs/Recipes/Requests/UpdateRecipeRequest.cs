using System.Collections.Generic;

namespace VeggieLife.BLL.DTOs.Recipes.Requests;

/// <summary>
/// Yêu cầu cập nhật, chỉnh sửa nội dung công thức của mình (FN-POST-04, BR-USR-REC-01).
/// </summary>
public class UpdateRecipeRequest
{
    public string? Title { get; set; }
    public int? CategoryId { get; set; }
    public int? DietarySchoolId { get; set; }

    /// <summary>Mã định danh món ăn (Food) nếu muốn chuyển sang món khác.</summary>
    public int? FoodId { get; set; }

    /// <summary>Tên món ăn mới hoặc cập nhật.</summary>
    public string? FoodName { get; set; }

    public string? Description { get; set; }
    public List<string>? Instructions { get; set; }
    public int? PrepTimeMinutes { get; set; }
    public int? CookTimeMinutes { get; set; }
    public int? Servings { get; set; }
    public string? DifficultyLevel { get; set; }
    public List<RecipeIngredientItemRequest>? Ingredients { get; set; }
    public string? ImageUrl { get; set; }
    public bool Submit { get; set; } = false;
}
