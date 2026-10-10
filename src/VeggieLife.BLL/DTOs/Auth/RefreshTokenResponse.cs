using System.Text.Json.Serialization;

namespace VeggieLife.BLL.DTOs.Auth;

/// <summary>
/// DTO phản hồi khi làm mới Access Token thành công (FN-AUTH-09).
/// </summary>
public class RefreshTokenResponse
{
    public string TokenType { get; set; } = "Bearer";

    public string AccessToken { get; set; } = string.Empty;

    public int ExpiresIn { get; set; } = 900;

    /// <summary>
    /// Refresh Token mới sau khi xoay vòng, được lưu trong Cookie HttpOnly.
    /// </summary>
    [JsonIgnore]
    public string RefreshToken { get; set; } = string.Empty;
}
