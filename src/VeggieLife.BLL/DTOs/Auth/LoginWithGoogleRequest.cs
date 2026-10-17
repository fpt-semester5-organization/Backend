using System.ComponentModel.DataAnnotations;

namespace VeggieLife.BLL.DTOs.Auth;

/// <summary>
/// DTO yêu cầu đăng nhập hoặc đăng ký nhanh bằng Google OAuth (FN-AUTH-07).
/// Khớp với schema OpenAPI LoginWithGoogleRequest.
/// </summary>
public class LoginWithGoogleRequest
{
    /// <summary>
    /// Google ID Token nhận từ client sau khi người dùng đăng nhập Google. Bắt buộc.
    /// </summary>
    [Required(ErrorMessage = "Dữ liệu đầu vào sai định dạng hoặc thiếu trường bắt buộc.")]
    public string IdToken { get; set; } = string.Empty;

    /// <summary>
    /// Mật khẩu hiện tại khi tài khoản email này đã được tạo mật khẩu trước đó.
    /// Bắt buộc khi tài khoản đã tồn tại dưới hình thức đăng ký mật khẩu (BR-USR-AUTH-02).
    /// </summary>
    public string? CurrentPassword { get; set; }

    /// <summary>
    /// Guest UUID để hợp nhất ngữ cảnh khách vãng lai nếu có (BR-GUEST-AUTH-01).
    /// </summary>
    public string? GuestUuid { get; set; }
}
