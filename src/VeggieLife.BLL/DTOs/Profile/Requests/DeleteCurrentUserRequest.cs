namespace VeggieLife.BLL.DTOs.Profile.Requests;

/// <summary>
/// DTO yêu cầu vô hiệu hóa tài khoản cá nhân có thời gian ân hạn 30 ngày (FN-PROF-07).
/// </summary>
public class DeleteCurrentUserRequest
{
    /// <summary>
    /// Mật khẩu hiện tại để xác thực lại danh tính (bắt buộc đối với tài khoản đăng ký bằng mật khẩu).
    /// </summary>
    public string? ConfirmPassword { get; set; }

    /// <summary>
    /// Google ID Token để xác thực lại danh tính (bắt buộc đối với tài khoản chỉ dùng Google OAuth).
    /// </summary>
    public string? GoogleIdToken { get; set; }

    /// <summary>
    /// Lý do người dùng muốn vô hiệu hóa / xóa tài khoản (tối đa 255 ký tự).
    /// </summary>
    public string? Reason { get; set; }
}
