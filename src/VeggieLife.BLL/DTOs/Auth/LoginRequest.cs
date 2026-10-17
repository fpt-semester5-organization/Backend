using System.ComponentModel.DataAnnotations;

namespace VeggieLife.BLL.DTOs.Auth;

/// <summary>
/// DTO yêu cầu đăng nhập bằng Email và Mật khẩu (FN-AUTH-08).
/// </summary>
public class LoginRequest
{
    /// <summary>
    /// Địa chỉ email đăng nhập (BẮT BUỘC, định dạng email, tối đa 150 ký tự).
    /// </summary>
    [Required(ErrorMessage = "Email không được để trống.")]
    [EmailAddress(ErrorMessage = "Định dạng email không hợp lệ.")]
    [StringLength(150, ErrorMessage = "Email không được vượt quá 150 ký tự.")]
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Mật khẩu người dùng (BẮT BUỘC, 8 - 64 ký tự).
    /// </summary>
    [Required(ErrorMessage = "Mật khẩu không được để trống.")]
    [StringLength(64, MinimumLength = 8, ErrorMessage = "Mật khẩu phải có độ dài từ 8 đến 64 ký tự.")]
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// Khôi phục tài khoản nếu đang trong 30 ngày ân hạn xóa (mặc định false).
    /// </summary>
    public bool RestoreAccount { get; set; } = false;

    /// <summary>
    /// Mã định danh khách vãng lai để hợp nhất ngữ cảnh đàm thoại AI (BR-GUEST-AUTH-01).
    /// </summary>
    public string? GuestUuid { get; set; }
}
