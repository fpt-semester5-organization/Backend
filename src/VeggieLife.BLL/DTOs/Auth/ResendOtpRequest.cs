using System.ComponentModel.DataAnnotations;
using VeggieLife.BLL.Common.Constants;

namespace VeggieLife.BLL.DTOs.Auth;

/// <summary>
/// DTO yêu cầu gửi lại mã OTP (FN-AUTH-06).
/// </summary>
public class ResendOtpRequest
{
    /// <summary>
    /// Địa chỉ email nhận mã OTP.
    /// </summary>
    [Required(ErrorMessage = "Email không được để trống.")]
    [EmailAddress(ErrorMessage = "Định dạng email không hợp lệ.")]
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Mục đích gửi mã (EMAIL_VERIFY | PASSWORD_RESET).
    /// </summary>
    public string Purpose { get; set; } = OtpPurposes.EmailVerify;
}
