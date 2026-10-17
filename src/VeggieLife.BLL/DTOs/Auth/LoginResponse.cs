using System.Text.Json.Serialization;

namespace VeggieLife.BLL.DTOs.Auth;

/// <summary>
/// DTO phản hồi sau khi đăng nhập thành công (FN-AUTH-08).
/// </summary>
public class LoginResponse
{
    public string TokenType { get; set; } = "Bearer";

    public string AccessToken { get; set; } = string.Empty;

    public int ExpiresIn { get; set; } = 900;

    public AuthUserDto User { get; set; } = new();

    /// <summary>
    /// Refresh Token được lưu trong Cookie HttpOnly; không serialize ra JSON payload.
    /// </summary>
    [JsonIgnore]
    public string RefreshToken { get; set; } = string.Empty;
}
