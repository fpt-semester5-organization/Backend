namespace VeggieLife.BLL.DTOs.Chat.Responses;

/// <summary>
/// Món ăn gợi ý kèm theo câu trả lời tư vấn (FN-CHAT-02).
/// </summary>
public class ChatRecommendedRecipeDto
{
    public int RecipeId { get; set; }
    public string Title { get; set; } = string.Empty;
    public int CookTimeMinutes { get; set; }
}
