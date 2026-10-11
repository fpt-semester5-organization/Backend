using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using VeggieLife.BLL.Services.Cache;

namespace VeggieLife.BLL.Services.Guest;

/// <summary>
/// Triển khai dịch vụ xác định quốc gia theo IP với bộ nhớ đệm Redis 24 giờ (FN-AUTH-01, BR-GUEST-06).
/// </summary>
public class IpGeolocationService : IIpGeolocationService
{
    private readonly IRedisService _redisService;
    private readonly ILogger<IpGeolocationService> _logger;

    public IpGeolocationService(IRedisService redisService, ILogger<IpGeolocationService> logger)
    {
        _redisService = redisService;
        _logger = logger;
    }

    public async Task<string> ResolveCountryAsync(
        string? clientIp,
        string? headerCountry = null,
        CancellationToken ct = default)
    {
        // 1. Ưu tiên Header tùy chỉnh (X-Country-Code, CF-IPCountry)
        if (!string.IsNullOrWhiteSpace(headerCountry))
        {
            return headerCountry.Trim().ToUpperInvariant();
        }

        var ip = string.IsNullOrWhiteSpace(clientIp) ? "127.0.0.1" : clientIp.Trim();

        // 2. Môi trường Localhost và mạng nội bộ mặc định là VN để chạy dev/test thuận lợi
        if (IsLoopbackOrPrivateIp(ip))
        {
            return "VN";
        }

        // 3. Tra cứu từ bộ nhớ đệm Redis: ip_geo:{ip} (TTL 24 giờ - BR-GUEST-06, FN-AUTH-01)
        var cacheKey = $"ip_geo:{ip}";
        try
        {
            var cachedCountry = await _redisService.GetAsync<string>(cacheKey, ct);
            if (!string.IsNullOrWhiteSpace(cachedCountry))
            {
                return cachedCountry.Trim().ToUpperInvariant();
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning("Không thể đọc cache Geolocation từ Redis: {Message}", ex.Message);
        }

        // 4. Mặc định gán là VN cho hệ thống nội địa
        var resolvedCountry = "VN";

        // 5. Lưu vào bộ nhớ đệm Redis với thời hạn 24 giờ
        try
        {
            await _redisService.SetAsync(cacheKey, resolvedCountry, TimeSpan.FromHours(24), ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning("Không thể lưu cache Geolocation vào Redis: {Message}", ex.Message);
        }

        return resolvedCountry;
    }

    private static bool IsLoopbackOrPrivateIp(string ip)
    {
        if (string.Equals(ip, "127.0.0.1", StringComparison.Ordinal) ||
            string.Equals(ip, "::1", StringComparison.Ordinal) ||
            string.Equals(ip, "localhost", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (IPAddress.TryParse(ip, out var parsedIp))
        {
            if (IPAddress.IsLoopback(parsedIp))
            {
                return true;
            }

            var bytes = parsedIp.GetAddressBytes();
            if (bytes.Length == 4)
            {
                if (bytes[0] == 10) return true;
                if (bytes[0] == 172 && bytes[1] >= 16 && bytes[1] <= 31) return true;
                if (bytes[0] == 192 && bytes[1] == 168) return true;
            }
        }

        return false;
    }
}
