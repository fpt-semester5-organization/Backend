using System.Collections.Generic;

namespace VeggieLife.BLL.DTOs.Ai;

/// <summary>
/// DTO gửi yêu cầu trả lời tư vấn sang AI Service (POST /chat/answer).
/// Tuân thủ tài liệu đặc tả Backend & AI Service (Decision D118, S5-N1).
/// </summary>
public class AiChatAnswerRequest
{
    public long SessionId { get; set; }
    public string UserQuestion { get; set; } = string.Empty;
    public List<AiChatMessageHistoryDto> History { get; set; } = new();
    public AiUserProfileDto? UserProfile { get; set; }
    public List<AiCandidateRecipeDto> CandidateRecipes { get; set; } = new();
}
