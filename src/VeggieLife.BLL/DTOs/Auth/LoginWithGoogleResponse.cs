using System.Text.Json.Serialization;

namespace VeggieLife.BLL.DTOs.Auth;

/// <summary>
/// DTO phản hồi sau khi đăng nhập / đăng ký bằng Google OAuth thành công (FN-AUTH-07).
/// Khớp với schema OpenAPI LoginWithGoogleResponse.
/// </summary>
public class LoginWithGoogleResponse
{
    public string TokenType { get; set; } = "Bearer";

    public string AccessToken { get; set; } = string.Empty;

    public int ExpiresIn { get; set; } = 900;

    public AuthUserDto User { get; set; } = new();

    /// <summary>
    /// Đánh dấu tài khoản mới tạo (true) hay đăng nhập tài khoản đã có (false).
    /// </summary>
    public bool IsNewAccount { get; set; }

    /// <summary>
    /// Refresh Token được lưu trong Cookie HttpOnly; không serialize ra JSON payload.
    /// </summary>
    [JsonIgnore]
    public string RefreshToken { get; set; } = string.Empty;
}
