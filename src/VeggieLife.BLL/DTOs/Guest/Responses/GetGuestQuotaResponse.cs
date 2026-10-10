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
/// </summary>
public class GetGuestQuotaResponse
{
    public GuestQuotaItemDto Ai { get; set; } = new();
    public GuestQuotaItemDto BarcodeDaily { get; set; } = new();
    public GuestQuotaItemDto MapSearch { get; set; } = new();
    public string ResetAt { get; set; } = string.Empty;
}
