using System;

namespace VeggieLife.BLL.DTOs.Auth;

/// <summary>
/// Cấu trúc lưu trữ phiên Refresh Token trên bộ nhớ đệm Redis (refresh:{tokenHash}).
/// Phục vụ cơ chế xoay vòng token (Token Rotation) và phát hiện dùng lại token (BR-AUTH-004).
/// </summary>
public class RefreshTokenSession
{
    public int UserId { get; set; }

    public string TokenHash { get; set; } = string.Empty;

    public bool IsRevoked { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime ExpiresAt { get; set; }

    public string? ReplacedByTokenHash { get; set; }

    public string? ClientIp { get; set; }

    public string? UserAgent { get; set; }
}
