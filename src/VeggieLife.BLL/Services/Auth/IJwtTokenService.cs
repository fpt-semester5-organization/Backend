using System.Collections.Generic;
using System.Security.Claims;
using VeggieLife.DAL.Entities;

namespace VeggieLife.BLL.Services.Auth;

/// <summary>
/// DTO chứa cặp mã xác thực Access Token và Refresh Token.
/// </summary>
public class TokenResult
{
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public int ExpiresInSeconds { get; set; }
}

/// <summary>
/// Interface dịch vụ cấp phát và xác thực JWT Token cho Khách vãng lai, Thành viên, VIP và Admin.
/// </summary>
public interface IJwtTokenService
{
    /// <summary>
    /// Sinh Access Token và Refresh Token cho người dùng đã đăng nhập (Member, VIP, Admin).
    /// </summary>
    TokenResult GenerateUserToken(User user, bool isVip = false);

    /// <summary>
    /// Sinh Anonymous JWT cho khách vãng lai mang claim Role=GUEST và Guest_UUID.
    /// </summary>
    TokenResult GenerateGuestToken(string guestUuid);

    /// <summary>
    /// Trích xuất danh sách Claims từ JWT Token mà không cần kiểm tra hạn dùng (phục vụ refresh).
    /// </summary>
    ClaimsPrincipal? GetPrincipalFromExpiredToken(string token);
}
