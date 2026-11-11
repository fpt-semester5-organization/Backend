using System.Collections.Generic;

namespace VeggieLife.BLL.Common.Settings;

/// <summary>
/// Cấu hình kiểm soát vùng địa lý dành cho Khách vãng lai (BR-GUEST-06).
/// </summary>
public class GuestSecuritySettings
{
    public const string SectionName = "GuestSecurity";

    /// <summary>
    /// Công tắc bật/tắt ràng buộc vị trí địa lý chỉ cho phép truy cập từ Việt Nam (mặc định false để linh hoạt khi dev/test).
    /// </summary>
    public bool EnforceVietnamOnly { get; set; } = false;

    /// <summary>
    /// Danh sách mã quốc gia ISO được phép truy cập (mặc định: ["VN"]).
    /// </summary>
    public List<string> AllowedCountryCodes { get; set; } = new() { "VN" };
}
