using System.ComponentModel.DataAnnotations;

namespace VeggieLife.BLL.DTOs.Surveys.Requests;

/// <summary>
/// DTO đánh giá câu trả lời của AI hoặc tính năng AI (FN-CHAT-06).
/// </summary>
public class CreateSurveyRequest
{
    public long? ChatSessionId { get; set; }

    [Required(ErrorMessage = "Câu hỏi hoặc ngữ cảnh đánh giá không được để trống.")]
    [MaxLength(255, ErrorMessage = "Ngữ cảnh câu hỏi không được vượt quá 255 ký tự.")]
    public string ContextQuestion { get; set; } = string.Empty;

    [Range(1, 5, ErrorMessage = "Điểm đánh giá phải từ 1 đến 5 sao.")]
    public int RatingScore { get; set; }

    [MaxLength(1000, ErrorMessage = "Nội dung góp ý không được vượt quá 1000 ký tự.")]
    public string? FeedbackContent { get; set; }
}
