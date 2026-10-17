using System;

namespace VeggieLife.BLL.DTOs.Chat.Responses;

/// <summary>
/// DTO tóm tắt phiên hội thoại trong danh sách lịch sử (FN-CHAT-03).
/// </summary>
public class ChatSessionSummaryDto
{
    public long SessionId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string LastMessagePreview { get; set; } = string.Empty;
    public DateTime UpdatedAt { get; set; }
}
