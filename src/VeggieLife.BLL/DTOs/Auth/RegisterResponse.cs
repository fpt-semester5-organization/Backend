namespace VeggieLife.BLL.DTOs.Auth;

/// <summary>
/// DTO phản hồi sau khi đăng ký tài khoản thành công (FN-AUTH-04).
/// </summary>
public class RegisterResponse
{
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Thời gian hiệu lực của mã OTP tính bằng giây (mặc định 300s = 5 phút).
    /// </summary>
    public int OtpExpiresIn { get; set; } = 300;
}
