using System.Collections.Generic;

namespace VeggieLife.BLL.DTOs.Chat.Responses;

/// <summary>
/// DTO chứa danh sách tin nhắn và metadata phân trang (FN-CHAT-04).
/// </summary>
public class ListChatSessionMessagesResponseDto
{
    public List<ChatSessionMessageDto> Messages { get; set; } = new();
    public ChatMessagesMeta Meta { get; set; } = new();
}
