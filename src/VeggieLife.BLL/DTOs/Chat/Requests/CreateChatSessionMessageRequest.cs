using System.ComponentModel.DataAnnotations;

namespace VeggieLife.BLL.DTOs.Chat.Requests;

/// <summary>
/// DTO gửi câu hỏi và yêu cầu tư vấn dinh dưỡng (FN-CHAT-02).
/// </summary>
public class CreateChatSessionMessageRequest
{
    [Required(ErrorMessage = "Nội dung câu hỏi không được để trống.")]
    [MinLength(1, ErrorMessage = "Nội dung câu hỏi phải có ít nhất 1 ký tự.")]
    [MaxLength(1000, ErrorMessage = "Nội dung câu hỏi không được vượt quá 1000 ký tự.")]
    public string Message { get; set; } = string.Empty;
}
