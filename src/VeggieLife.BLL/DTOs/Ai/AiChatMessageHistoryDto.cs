namespace VeggieLife.BLL.DTOs.Ai;

/// <summary>
/// Tin nhắn lịch sử gửi kèm trong ngữ cảnh sang AI Service.
/// </summary>
public class AiChatMessageHistoryDto
{
    public string SenderType { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
}
