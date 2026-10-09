using System.ComponentModel.DataAnnotations;

namespace VeggieLife.BLL.DTOs.Auth;

/// <summary>
/// DTO yêu cầu xác thực mã OTP kích hoạt tài khoản (FN-AUTH-05).
/// </summary>
public class VerifyOtpRequest
{
    /// <summary>
    /// Email tài khoản cần kích hoạt.
    /// </summary>
    [Required(ErrorMessage = "Email không được để trống.")]
    [EmailAddress(ErrorMessage = "Định dạng email không hợp lệ.")]
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Mã OTP 6 chữ số nhận qua email.
    /// </summary>
    [Required(ErrorMessage = "Mã OTP không được để trống.")]
    [RegularExpression(@"^\d{6}$", ErrorMessage = "Mã OTP phải bao gồm chính xác 6 chữ số.")]
    public string Otp { get; set; } = string.Empty;

    /// <summary>
    /// Mã định danh khách vãng lai (Guest_UUID) của phiên hiện tại để hợp nhất dữ liệu (BR-GUEST-AUTH-01).
    /// </summary>
    public string? GuestUuid { get; set; }
}
