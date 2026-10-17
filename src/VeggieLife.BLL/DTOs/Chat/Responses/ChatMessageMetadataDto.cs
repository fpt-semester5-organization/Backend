using System.Collections.Generic;

namespace VeggieLife.BLL.DTOs.Chat.Responses;

/// <summary>
/// Cấu trúc lưu trữ JSON trong cột ChatMessages.Metadata_JSON (Decision D108).
/// </summary>
public class ChatMessageMetadataDto
{
    public List<ChatSourceDto> Sources { get; set; } = new();
    public bool RagUsed { get; set; }
    public List<ChatRecommendedRecipeDto> RecommendedRecipes { get; set; } = new();
}
