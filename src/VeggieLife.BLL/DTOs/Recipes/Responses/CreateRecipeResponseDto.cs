namespace VeggieLife.BLL.DTOs.Recipes.Responses;

/// <summary>
/// DTO phản hồi sau khi tạo mới hoặc gửi duyệt công thức (FN-POST-03).
/// </summary>
public class CreateRecipeResponseDto
{
    public int RecipeId { get; set; }
    public string Status { get; set; } = "DRAFT";
    public double AiConfidence { get; set; }
}
