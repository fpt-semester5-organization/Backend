namespace VeggieLife.BLL.DTOs.Guest.Responses;

/// <summary>
/// DTO phản hồi khởi tạo phiên Khách vãng lai (FN-AUTH-01).
/// </summary>
public class CreateAnonymousSessionResponse
{
    public string TokenType { get; set; } = "Bearer";
    public string AccessToken { get; set; } = string.Empty;
    public int ExpiresIn { get; set; } = 86400;
    public string Role { get; set; } = "GUEST";
    public string GuestId { get; set; } = string.Empty;
    public string CountryCode { get; set; } = "VN";
}
