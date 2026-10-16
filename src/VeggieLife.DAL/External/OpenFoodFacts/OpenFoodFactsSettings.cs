namespace VeggieLife.DAL.External.OpenFoodFacts;

/// <summary>
/// Cấu hình kết nối tới dịch vụ ngoại vi Open Food Facts API (FN-BAR-01).
/// Được cấu hình trong appsettings.json theo quy tắc không hardcode thông số bên thứ ba.
/// </summary>
public class OpenFoodFactsSettings
{
    public const string SectionName = "OpenFoodFacts";

    /// <summary>
    /// Base URL của Open Food Facts API (mặc định: https://world.openfoodfacts.org).
    /// </summary>
    public string BaseUrl { get; set; } = "https://world.openfoodfacts.org";

    /// <summary>
    /// Thời gian chờ gọi API ngoại vi (đặc tả FN-BAR-01 yêu cầu tối đa 3 giây).
    /// </summary>
    public int TimeoutSeconds { get; set; } = 3;

    /// <summary>
    /// Header User-Agent để nhận dạng ứng dụng gửi request tới Open Food Facts.
    /// </summary>
    public string UserAgent { get; set; } = "VeggieLife/1.0 (Web/Mobile; https://veggielife.vn; support@veggielife.vn)";
}
