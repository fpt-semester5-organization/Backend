namespace VeggieLife.BLL.DTOs.Chat.Responses;

/// <summary>
/// Kết quả phản hồi của FN-CHAT-02: Tin nhắn trợ lý AI + Hạn mức còn lại.
/// </summary>
public class CreateChatMessageResultDto
{
    public ChatSessionMessageDto Message { get; set; } = new();
    public ChatQuotaDto Quota { get; set; } = new();
}
