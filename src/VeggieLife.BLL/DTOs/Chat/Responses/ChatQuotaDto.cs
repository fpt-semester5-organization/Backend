namespace VeggieLife.BLL.DTOs.Chat.Responses;

/// <summary>
/// Thông tin hạn mức lượt hỏi AI còn lại (Khách 3 lượt, Free 20/ngày, VIP 200/ngày) (FN-CHAT-02).
/// </summary>
public class ChatQuotaDto
{
    public int Used { get; set; }
    public int Limit { get; set; }
    public int Remaining { get; set; }
    public string Scope { get; set; } = "DAILY";
}
