namespace VeggieLife.BLL.DTOs.Auth;

/// <summary>
/// DTO thông tin cơ bản của người dùng sau khi xác thực thành công.
/// </summary>
public class AuthUserDto
{
    public int UserId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public bool IsVip { get; set; }
    public string AccountStatus { get; set; } = string.Empty;
    public bool IsProfileCompleted { get; set; }
    public string? AvatarUrl { get; set; }

    /// <summary>
    /// Cho biết tài khoản đã có mật khẩu đăng nhập hay chưa (false đối với tài khoản chỉ dùng Google OAuth).
    /// </summary>
    public bool HasPassword { get; set; }

    /// <summary>
    /// Cho biết tài khoản đã liên kết với Google OAuth hay chưa.
    /// </summary>
    public bool IsGoogleLinked { get; set; }
}

/// <summary>
/// DTO phản hồi sau khi xác thực OTP kích hoạt tài khoản thành công (FN-AUTH-05).
/// </summary>
public class VerifyOtpResponse
{
    public string TokenType { get; set; } = "Bearer";
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public int ExpiresIn { get; set; } = 900;
    public AuthUserDto User { get; set; } = new();
    public string RedirectUrl { get; set; } = "/onboarding/health-profile";
}
