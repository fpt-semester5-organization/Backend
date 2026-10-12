using System.ComponentModel.DataAnnotations;

namespace VeggieLife.BLL.DTOs.Auth;

/// <summary>
/// DTO yêu cầu gửi OTP quên mật khẩu (FN-AUTH-11).
/// </summary>
public class ForgotPasswordRequest
{
    [Required(ErrorMessage = "Email không được để trống.")]
    [EmailAddress(ErrorMessage = "Định dạng email không hợp lệ.")]
    [StringLength(150, ErrorMessage = "Email không được vượt quá 150 ký tự.")]
    public string Email { get; set; } = string.Empty;
}

/// <summary>
/// DTO yêu cầu đặt lại mật khẩu bằng OTP (FN-AUTH-12).
/// </summary>
public class ResetPasswordRequest
{
    [Required(ErrorMessage = "Email không được để trống.")]
    [EmailAddress(ErrorMessage = "Định dạng email không hợp lệ.")]
    [StringLength(150, ErrorMessage = "Email không được vượt quá 150 ký tự.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Mã OTP không được để trống.")]
    [RegularExpression(@"^\d{6}$", ErrorMessage = "Mã OTP phải bao gồm chính xác 6 chữ số.")]
    public string Otp { get; set; } = string.Empty;

    [Required(ErrorMessage = "Mật khẩu mới không được để trống.")]
    public string NewPassword { get; set; } = string.Empty;
}
