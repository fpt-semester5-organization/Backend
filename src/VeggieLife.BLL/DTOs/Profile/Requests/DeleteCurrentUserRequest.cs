namespace VeggieLife.BLL.DTOs.Profile.Requests;

/// <summary>
/// DTO yêu cầu vô hiệu hóa tài khoản cá nhân có thời gian chờ xóa 30 ngày (có thể khôi phục) (FN-PROF-07).
/// </summary>
public class DeleteCurrentUserRequest
{
    /// <summary>
    /// Mã xác thực OTP 6 số gửi tới email của tài khoản để xác nhận vô hiệu hóa.
    /// </summary>
    public string? Otp { get; set; }

    /// <summary>
    /// Mật khẩu hiện tại để xác thực lại danh tính (dành cho tài khoản sử dụng mật khẩu).
    /// </summary>
    public string? ConfirmPassword { get; set; }

    /// <summary>
    /// Google ID Token để xác thực lại danh tính (dành cho tài khoản chỉ dùng Google OAuth).
    /// </summary>
    public string? GoogleIdToken { get; set; }

    /// <summary>
    /// Lý do người dùng muốn vô hiệu hóa / xóa tài khoản (tối đa 255 ký tự).
    /// </summary>
    public string? Reason { get; set; }
}
