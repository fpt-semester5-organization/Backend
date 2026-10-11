namespace VeggieLife.BLL.DTOs.Recipes.Responses;

/// <summary>
/// DTO phản hồi sau khi gỡ hoặc xóa mềm công thức (FN-POST-04).
/// </summary>
public class DeleteRecipeResponseDto
{
    public int RecipeId { get; set; }
    public string Status { get; set; } = "ARCHIVED";
}
