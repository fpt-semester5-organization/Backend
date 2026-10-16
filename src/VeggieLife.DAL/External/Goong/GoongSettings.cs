namespace VeggieLife.DAL.External.Goong;

/// <summary>
/// Cấu hình kết nối tới dịch vụ bản đồ và địa điểm Goong Map/Places API (FN-MAP-01).
/// Được cấu hình trong appsettings.json theo quy tắc bảo mật không hardcode API key.
/// </summary>
public class GoongSettings
{
    public const string SectionName = "Goong";

    /// <summary>
    /// Khóa REST API dùng cho Backend gọi Goong Places (Autocomplete, Place Detail, Geocode).
    /// Đăng ký và lấy key tại https://account.goong.io/keys
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>
    /// Khóa Map Tiles dùng cho Frontend (Web/Mobile) render bản đồ Goong.
    /// </summary>
    public string MapKey { get; set; } = string.Empty;

    /// <summary>
    /// Base URL REST API của Goong (mặc định https://rsapi.goong.io).
    /// </summary>
    public string BaseUrl { get; set; } = "https://rsapi.goong.io";

    /// <summary>
    /// Thời gian chờ gọi API ngoại vi (tối đa 4 giây để bảo đảm SLA P95).
    /// </summary>
    public int TimeoutSeconds { get; set; } = 4;
}
