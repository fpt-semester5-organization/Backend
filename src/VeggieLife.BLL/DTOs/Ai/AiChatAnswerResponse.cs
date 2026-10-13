using System.Collections.Generic;

namespace VeggieLife.BLL.DTOs.Ai;

/// <summary>
/// DTO phản hồi từ AI Service (POST /chat/answer).
/// </summary>
public class AiChatAnswerResponse
{
    public string Answer { get; set; } = string.Empty;
    public List<AiSourceDto> Sources { get; set; } = new();
    public bool RagUsed { get; set; }
    public List<int> RecommendedRecipeIds { get; set; } = new();
    public ModerationUsage Usage { get; set; } = new();
    public ModerationAudit Audit { get; set; } = new();
}
