namespace VeggieLife.DAL.External.Google;

/// <summary>
/// Dữ liệu trích xuất từ Google ID Token sau khi giải mã và xác thực chữ ký số.
/// </summary>
public class GoogleTokenPayload
{
    /// <summary>
    /// Định danh duy nhất của người dùng trên hệ thống Google (claim "sub").
    /// </summary>
    public string Subject { get; set; } = string.Empty;

    /// <summary>
    /// Địa chỉ Email của người dùng Google.
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Trạng thái email đã được Google xác thực (claim "email_verified").
    /// </summary>
    public bool EmailVerified { get; set; }

    /// <summary>
    /// Họ và tên hiển thị của người dùng (claim "name").
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Đường dẫn ảnh đại diện Google (claim "picture").
    /// </summary>
    public string? Picture { get; set; }
}
