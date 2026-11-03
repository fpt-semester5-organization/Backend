using System.ComponentModel.DataAnnotations;

namespace VeggieLife.BLL.DTOs.Auth;

/// <summary>
/// DTO yêu cầu thiết lập mật khẩu lần đầu cho tài khoản OAuth chưa có mật khẩu (FN-AUTH-14).
/// </summary>
public class SetPasswordRequest
{
    /// <summary>
    /// Mật khẩu mới cần thiết lập.
    /// </summary>
    [Required(ErrorMessage = "Mật khẩu mới không được để trống.")]
    public string NewPassword { get; set; } = string.Empty;

    /// <summary>
    /// Mật khẩu xác nhận để đảm bảo người dùng nhập chính xác.
    /// </summary>
    [Required(ErrorMessage = "Mật khẩu xác nhận không được để trống.")]
    public string ConfirmPassword { get; set; } = string.Empty;
}

