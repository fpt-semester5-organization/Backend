namespace VeggieLife.BLL.DTOs.Recipes.Responses;

/// <summary>
/// DTO phản hồi sau khi cập nhật công thức (FN-POST-04).
/// </summary>
public class UpdateRecipeResponseDto
{
    public int RecipeId { get; set; }
    public string Status { get; set; } = "DRAFT";
    public bool Moderated { get; set; }
}
