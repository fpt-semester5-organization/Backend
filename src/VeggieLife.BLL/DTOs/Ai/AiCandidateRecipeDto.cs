namespace VeggieLife.BLL.DTOs.Ai;

/// <summary>
/// Công thức ứng viên phù hợp trường phái và an toàn dị ứng gửi sang AI Service để khuyến nghị.
/// </summary>
public class AiCandidateRecipeDto
{
    public int RecipeId { get; set; }
    public string Title { get; set; } = string.Empty;
    public int CookTimeMinutes { get; set; }
    public decimal CaloriesKcal { get; set; }
}
