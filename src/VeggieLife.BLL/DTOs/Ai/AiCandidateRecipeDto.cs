using System.Text.Json.Serialization;

namespace VeggieLife.BLL.DTOs.Ai;

/// <summary>
/// Công thức ứng viên gửi sang AI Service (recipeId, title, cookTimeMinutes, caloriesKcal).
/// </summary>
public class AiCandidateRecipeDto
{
    [JsonPropertyName("recipeId")]
    public int RecipeId { get; set; }

    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("cookTimeMinutes")]
    public int CookTimeMinutes { get; set; }

    [JsonPropertyName("caloriesKcal")]
    public int? CaloriesKcal { get; set; }
}
