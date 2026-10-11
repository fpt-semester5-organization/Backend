using System;

namespace VeggieLife.BLL.DTOs.Guest;

/// <summary>
/// Dữ liệu phiên lưu trên Redis cho Khách vãng lai với TTL 24 giờ (FN-AUTH-01, BR-TECH-GUEST-02).
/// Không ghi vào cơ sở dữ liệu SQL.
/// </summary>
public class GuestSessionData
{
    public string GuestId { get; set; } = string.Empty;
    public string CountryCode { get; set; } = "VN";
    public string? ClientIp { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
