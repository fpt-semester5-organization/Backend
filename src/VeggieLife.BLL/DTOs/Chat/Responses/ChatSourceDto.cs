namespace VeggieLife.BLL.DTOs.Chat.Responses;

/// <summary>
/// Nguồn tham chiếu công khai (RECIPE, POST, KNOWLEDGE_BASE...) được AI dẫn chứng trong câu trả lời (FN-CHAT-02, D119).
/// </summary>
public class ChatSourceDto
{
    public string Type { get; set; } = string.Empty;
    public int? Id { get; set; }
    public string Title { get; set; } = string.Empty;
}
