using System.ComponentModel.DataAnnotations;

namespace VeggieLife.BLL.DTOs.Chat.Requests;

/// <summary>
/// DTO yêu cầu khởi tạo phiên hội thoại mới (FN-CHAT-01).
/// </summary>
public class CreateChatSessionRequest
{
    [MaxLength(100, ErrorMessage = "Tiêu đề phiên hội thoại không được vượt quá 100 ký tự.")]
    public string? Title { get; set; }

    public string? ContextType { get; set; }
}
