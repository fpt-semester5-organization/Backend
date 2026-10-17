using System;

namespace VeggieLife.BLL.DTOs.Auth;

/// <summary>
/// Cấu trúc lưu trữ dữ liệu OTP tạm thời trên Redis Cache.
/// </summary>
public class OtpCacheItem
{
    /// <summary>
    /// Chuỗi băm SHA-256 của mã OTP 6 chữ số để bảo mật theo quy định BR-USR-AUTH-01.
    /// </summary>
    public string OtpHash { get; set; } = string.Empty;

    /// <summary>
    /// Số lần nhập sai mã OTP liên tiếp (tối đa 5 lần trước khi bị khóa 15 phút).
    /// </summary>
    public int FailedAttempts { get; set; }

    /// <summary>
    /// Thời điểm sinh mã OTP (UTC).
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Mục đích của mã OTP (EMAIL_VERIFY, PASSWORD_RESET).
    /// </summary>
    public string Purpose { get; set; } = "EMAIL_VERIFY";
}
