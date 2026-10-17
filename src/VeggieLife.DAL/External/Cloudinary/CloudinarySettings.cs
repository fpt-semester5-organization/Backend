namespace VeggieLife.DAL.External.Cloudinary;

/// <summary>
/// Cấu hình thông tin xác thực và kết nối đến dịch vụ lưu trữ đám mây Cloudinary CDN.
/// Được ánh xạ từ section "Cloudinary" trong appsettings.json.
/// </summary>
public class CloudinarySettings
{
    public const string SectionName = "Cloudinary";

    /// <summary>
    /// Tên tài khoản lưu trữ đám mây (Cloud Name).
    /// </summary>
    public string CloudName { get; set; } = string.Empty;

    /// <summary>
    /// Khóa API công khai (API Key).
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>
    /// Khóa bí mật (API Secret) - Tuyệt đối không commit giá trị thật lên Git.
    /// </summary>
    public string ApiSecret { get; set; } = string.Empty;
}
