using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace VeggieLife.BLL.DTOs.Chat.Responses;

/// <summary>
/// Chi tiết tin nhắn trong phiên hội thoại (FN-CHAT-02, FN-CHAT-04).
/// </summary>
public class ChatSessionMessageDto
{
    public long MessageId { get; set; }
    public string SenderType { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Disclaimer { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<ChatSourceDto>? Sources { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? RagUsed { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<ChatRecommendedRecipeDto>? RecommendedRecipes { get; set; }

    public DateTime CreatedAt { get; set; }
}
