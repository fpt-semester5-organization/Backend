using System.Collections.Generic;

namespace VeggieLife.BLL.Common.Settings;

/// <summary>
/// Cấu hình bảo mật và kiểm soát phiên dành cho Khách vãng lai (BR-GUEST-SEC-01, BR-GUEST-06).
/// </summary>
public class GuestSecuritySettings
{
    public const string SectionName = "GuestSecurity";

    /// <summary>
    /// Giới hạn số lượng yêu cầu tối đa mỗi phút cho mỗi địa chỉ IP (mặc định: 10).
    /// </summary>
    public int RateLimitPerMinute { get; set; } = 10;

    /// <summary>
    /// Thời gian chặn tạm thời địa chỉ IP khi vượt ngưỡng rate limit (phút, mặc định: 10).
    /// </summary>
    public int BanDurationMinutes { get; set; } = 10;

    /// <summary>
    /// Áp dụng Rate Limit cho cả Localhost/127.0.0.1 (mặc định false để không chặn Test runner).
    /// </summary>
    public bool RateLimitLocalhost { get; set; } = false;

    /// <summary>
    /// Công tắc bật/tắt ràng buộc vị trí địa lý chỉ cho phép truy cập từ Việt Nam (mặc định false để linh hoạt khi dev/test).
    /// </summary>
    public bool EnforceVietnamOnly { get; set; } = false;

    /// <summary>
    /// Danh sách mã quốc gia ISO được phép truy cập (mặc định: ["VN"]).
    /// </summary>
    public List<string> AllowedCountryCodes { get; set; } = new() { "VN" };
}
