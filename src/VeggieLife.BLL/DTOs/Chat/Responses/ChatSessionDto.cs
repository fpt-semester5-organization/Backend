using System;

namespace VeggieLife.BLL.DTOs.Chat.Responses;

/// <summary>
/// DTO thông tin phiên hội thoại vừa khởi tạo thành công (FN-CHAT-01).
/// </summary>
public class ChatSessionDto
{
    public long SessionId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string ContextType { get; set; } = string.Empty;
    public DateTime StartedAt { get; set; }
}
