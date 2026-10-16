using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace VeggieLife.BLL.DTOs.Ai;

/// <summary>
/// DTO gửi yêu cầu trả lời tư vấn sang AI Service (POST /chat/answer).
/// Khớp 100% với schema ChatAnswerRequest của AI-Service (question, history, profile, candidateRecipes, userId, guestId).
/// </summary>
public class AiChatAnswerRequest
{
    [JsonPropertyName("question")]
    public string Question { get; set; } = string.Empty;

    [JsonIgnore]
    public string UserQuestion
    {
        get => Question;
        set => Question = value;
    }

    [JsonPropertyName("history")]
    public List<AiChatMessageHistoryDto> History { get; set; } = new();

    [JsonPropertyName("profile")]
    public AiUserProfileDto? Profile { get; set; }

    [JsonIgnore]
    public AiUserProfileDto? UserProfile
    {
        get => Profile;
        set => Profile = value;
    }

    [JsonPropertyName("candidateRecipes")]
    public List<AiCandidateRecipeDto> CandidateRecipes { get; set; } = new();

    [JsonPropertyName("userId")]
    public int? UserId { get; set; }

    [JsonPropertyName("guestId")]
    public string? GuestId { get; set; }
}
