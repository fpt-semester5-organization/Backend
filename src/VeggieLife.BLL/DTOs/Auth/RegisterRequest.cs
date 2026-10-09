using System.ComponentModel.DataAnnotations;

namespace VeggieLife.BLL.DTOs.Auth;

/// <summary>
/// DTO yêu cầu đăng ký tài khoản mới (FN-AUTH-04).
/// </summary>
public class RegisterRequest
{
    /// <summary>
    /// Họ và tên hiển thị người dùng (2 - 100 ký tự).
    /// </summary>
    [Required(ErrorMessage = "Họ và tên không được để trống.")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "Họ và tên phải có độ dài từ 2 đến 100 ký tự.")]
    public string FullName { get; set; } = string.Empty;

    /// <summary>
    /// Địa chỉ email đăng nhập (tối đa 150 ký tự).
    /// </summary>
    [Required(ErrorMessage = "Email không được để trống.")]
    [EmailAddress(ErrorMessage = "Định dạng email không hợp lệ.")]
    [StringLength(150, ErrorMessage = "Email không được vượt quá 150 ký tự.")]
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Mật khẩu người dùng (phải thỏa mãn chuẩn BR-AUTH-001).
    /// </summary>
    [Required(ErrorMessage = "Mật khẩu không được để trống.")]
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// Nhập lại mật khẩu (phải trùng khớp với Password).
    /// </summary>
    [Required(ErrorMessage = "Vui lòng xác nhận lại mật khẩu.")]
    public string ConfirmPassword { get; set; } = string.Empty;
}
