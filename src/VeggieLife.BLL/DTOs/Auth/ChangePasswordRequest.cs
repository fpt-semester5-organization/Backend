using System.ComponentModel.DataAnnotations;

namespace VeggieLife.BLL.DTOs.Auth;

/// <summary>
/// DTO yêu cầu đổi mật khẩu cho người dùng đang đăng nhập (FN-AUTH-13).
/// </summary>
public class ChangePasswordRequest
{
    [Required(ErrorMessage = "Mật khẩu hiện tại không được để trống.")]
    [StringLength(64, MinimumLength = 1, ErrorMessage = "Mật khẩu hiện tại phải từ 1 đến 64 ký tự.")]
    public string CurrentPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "Mật khẩu mới không được để trống.")]
    public string NewPassword { get; set; } = string.Empty;
}
