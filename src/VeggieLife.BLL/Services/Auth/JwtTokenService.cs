using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using VeggieLife.BLL.Common.Constants;
using VeggieLife.DAL.Entities;

namespace VeggieLife.BLL.Services.Auth;

/// <summary>
/// Triển khai dịch vụ cấp phát và giải mã JWT Token cho hệ thống VeggieLife.
/// </summary>
public class JwtTokenService : IJwtTokenService
{
    private readonly IConfiguration _configuration;

    public JwtTokenService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public TokenResult GenerateUserToken(User user, bool isVip = false)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.UserId.ToString()),
            new(JwtRegisteredClaimNames.Sub, user.UserId.ToString()),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Name, user.FullName),
            new(ClaimTypes.Role, user.Role),
            new("isVip", isVip.ToString().ToLowerInvariant()),
            new("AccountStatus", user.AccountStatus)
        };

        return BuildToken(claims);
    }

    public TokenResult GenerateGuestToken(string guestUuid)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, guestUuid),
            new(ClaimTypes.Role, UserRoles.Guest),
            new("Guest_UUID", guestUuid)
        };

        return BuildToken(claims);
    }

    public ClaimsPrincipal? GetPrincipalFromExpiredToken(string token)
    {
        var secretKey = _configuration["Jwt:SecretKey"]
            ?? throw new InvalidOperationException("Cấu hình 'Jwt:SecretKey' chưa được thiết lập trong appsettings.json.");
        var tokenValidationParameters = new TokenValidationParameters
        {
            ValidateAudience = false,
            ValidateIssuer = false,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
            ValidateLifetime = false // Không kiểm tra hết hạn ở đây để lấy claims làm mới token
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        try
        {
            var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out var securityToken);
            if (securityToken is not JwtSecurityToken jwtSecurityToken ||
                !jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
            {
                return null;
            }

            return principal;
        }
        catch
        {
            return null;
        }
    }

    private TokenResult BuildToken(IEnumerable<Claim> claims)
    {
        var secretKey = _configuration["Jwt:SecretKey"]
            ?? throw new InvalidOperationException("Cấu hình 'Jwt:SecretKey' chưa được thiết lập trong appsettings.json.");
        var issuer = _configuration["Jwt:Issuer"] ?? "VeggieLife.API";
        var audience = _configuration["Jwt:Audience"] ?? "VeggieLife.Client";
        var expiryMinutesStr = _configuration["Jwt:AccessTokenExpiryMinutes"];
        var expiryMinutes = int.TryParse(expiryMinutesStr, out var mins) && mins > 0 ? mins : 15;

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expires = DateTime.UtcNow.AddMinutes(expiryMinutes);

        var tokenDescriptor = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: expires,
            signingCredentials: creds
        );

        var accessToken = new JwtSecurityTokenHandler().WriteToken(tokenDescriptor);
        var refreshToken = GenerateRefreshToken();

        return new TokenResult
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            ExpiresInSeconds = expiryMinutes * 60
        };
    }

    private static string GenerateRefreshToken()
    {
        var randomNumber = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomNumber);
        return Convert.ToBase64String(randomNumber);
    }
}
