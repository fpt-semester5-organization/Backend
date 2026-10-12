using System.Collections.Generic;

namespace VeggieLife.BLL.DTOs.Recipes.Requests;

/// <summary>
/// DTO định lượng nguyên liệu cấu thành trong công thức nấu ăn.
/// </summary>
public class RecipeIngredientItemRequest
{
    public int IngredientId { get; set; }
    public decimal Quantity { get; set; }
    public string Unit { get; set; } = string.Empty;
    public bool IsOptional { get; set; } = false;
}

/// <summary>
/// Yêu cầu tạo mới công thức nấu ăn món chay (FN-POST-03, BR-USR-05).
/// </summary>
public class CreateRecipeRequest
{
    public string Title { get; set; } = string.Empty;
    public int CategoryId { get; set; }
    public int DietarySchoolId { get; set; }
    public string? Description { get; set; }
    public List<string> Instructions { get; set; } = new();
    public int PrepTimeMinutes { get; set; } = 15;
    public int CookTimeMinutes { get; set; } = 20;
    public int Servings { get; set; } = 2;
    public string DifficultyLevel { get; set; } = "MEDIUM";
    public List<RecipeIngredientItemRequest> Ingredients { get; set; } = new();
    public string? ImageUrl { get; set; }
    public bool Submit { get; set; } = false;
}
