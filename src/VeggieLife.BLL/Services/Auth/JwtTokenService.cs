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
        var jti = Guid.NewGuid().ToString("N");
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.UserId.ToString()),
            new(JwtRegisteredClaimNames.Sub, user.UserId.ToString()),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Name, user.FullName),
            new(ClaimTypes.Role, user.Role),
            new("role", user.Role),
            new("AccountStatus", user.AccountStatus),
            new(JwtRegisteredClaimNames.Jti, jti),
            new("isVip", isVip ? "true" : "false")
        };

        return BuildToken(claims, TimeSpan.FromMinutes(15));
    }

    public TokenResult GenerateGuestToken(string guestUuid)
    {
        return GenerateGuestToken(guestUuid, "VN");
    }

    public TokenResult GenerateGuestToken(string guestUuid, string country)
    {
        var jti = Guid.NewGuid().ToString("N");
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, guestUuid),
            new(JwtRegisteredClaimNames.Sub, guestUuid),
            new(ClaimTypes.Role, UserRoles.Guest),
            new("role", UserRoles.Guest),
            new("Guest_UUID", guestUuid),
            new("guestId", guestUuid),
            new("country", string.IsNullOrWhiteSpace(country) ? "VN" : country.Trim().ToUpperInvariant()),
            new(JwtRegisteredClaimNames.Jti, jti)
        };

        return BuildToken(claims, TimeSpan.FromHours(24));
    }

    public ClaimsPrincipal? GetPrincipalFromExpiredToken(string token)
    {
        var secretKey = _configuration["Jwt:SecretKey"]
            ?? _configuration["JwtSettings:SecretKey"]
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

    private TokenResult BuildToken(IEnumerable<Claim> claims, TimeSpan? customExpiry = null)
    {
        var secretKey = _configuration["Jwt:SecretKey"]
            ?? _configuration["JwtSettings:SecretKey"]
            ?? throw new InvalidOperationException("Cấu hình 'Jwt:SecretKey' chưa được thiết lập trong appsettings.json.");
        var issuer = _configuration["Jwt:Issuer"] ?? _configuration["JwtSettings:Issuer"] ?? "VeggieLife.API";
        var audience = _configuration["Jwt:Audience"] ?? _configuration["JwtSettings:Audience"] ?? "VeggieLife.Client";

        TimeSpan expiry;
        if (customExpiry.HasValue)
        {
            expiry = customExpiry.Value;
        }
        else
        {
            var expiryMinutesStr = _configuration["Jwt:AccessTokenExpiryMinutes"];
            var expiryMinutes = int.TryParse(expiryMinutesStr, out var mins) ? mins : 15;
            expiry = TimeSpan.FromMinutes(expiryMinutes);
        }

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expires = DateTime.UtcNow.Add(expiry);

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
            ExpiresInSeconds = (int)expiry.TotalSeconds
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
