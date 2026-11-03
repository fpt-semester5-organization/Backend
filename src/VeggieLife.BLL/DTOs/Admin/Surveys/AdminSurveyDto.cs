using System;

namespace VeggieLife.BLL.DTOs.Admin.Surveys;

/// <summary>
/// DTO thông tin chi tiết một phiếu khảo sát câu trả lời Chatbot AI (FN-ADM-15, FN-CHAT-06).
/// </summary>
public class AdminSurveyDto
{
    public long SurveyId { get; set; }
    public int UserId { get; set; }
    public string UserFullName { get; set; } = string.Empty;
    public string UserEmail { get; set; } = string.Empty;
    public string? UserAvatarUrl { get; set; }
    public long? SessionId { get; set; }
    public string? SessionTitle { get; set; }
    public int RatingScore { get; set; }
    public string ContextQuestion { get; set; } = string.Empty;
    public string? FeedbackContent { get; set; }
    public DateTime CreatedAt { get; set; }
}
