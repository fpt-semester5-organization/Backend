namespace VeggieLife.BLL.DTOs.Guest.Responses;

/// <summary>
/// Chi tiết một mục hạn ngạch dùng thử.
/// </summary>
public class GuestQuotaItemDto
{
    public int Used { get; set; }
    public int Limit { get; set; }
    public int Remaining { get; set; }
}

/// <summary>
/// DTO phản hồi tra cứu hạn mức dùng thử của Khách (FN-AUTH-02).
/// Phân chia độc lập: Chatbot AI (3 lượt), Quét tủ lạnh (3 lượt), Bóc tách video STT (3 lượt), Quét mã vạch (3 lần/ngày), Tìm quán (3 lần).
/// </summary>
public class GetGuestQuotaResponse
{
    public GuestQuotaItemDto Chatbot { get; set; } = new();
    public GuestQuotaItemDto FridgeVision { get; set; } = new();
    public GuestQuotaItemDto VideoStt { get; set; } = new();
    public GuestQuotaItemDto BarcodeDaily { get; set; } = new();
    public GuestQuotaItemDto MapSearch { get; set; } = new();
    public string ResetAt { get; set; } = string.Empty;

    /// <summary>
    /// Thuộc tính tương thích ngược cho Frontend (đồng bộ giá trị với Chatbot).
    /// </summary>
    public GuestQuotaItemDto Ai
    {
        get => Chatbot;
        set => Chatbot = value;
    }
}
