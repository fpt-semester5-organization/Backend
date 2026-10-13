namespace VeggieLife.BLL.DTOs.Chat.Responses;

/// <summary>
/// Metadata phân trang cuộn vô tận cho lịch sử tin nhắn (FN-CHAT-04).
/// </summary>
public class ChatMessagesMeta
{
    public int Size { get; set; }
    public bool HasMore { get; set; }
}
