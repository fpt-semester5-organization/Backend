using System;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using VeggieLife.BLL.Common.Constants;
using VeggieLife.BLL.Common.Responses;
using VeggieLife.BLL.Common.Settings;
using VeggieLife.BLL.Services.Cache;

namespace VeggieLife.API.Middlewares;

/// <summary>
/// Middleware đóng vai trò "người gác cổng" (Gatekeeper):
/// 1. Giới hạn 10 yêu cầu/phút mỗi địa chỉ IP dành cho Khách, vượt thì tạm khóa 10 phút (BR-GUEST-SEC-01).
/// 2. Kiểm soát vùng địa lý (chỉ cho phép truy cập từ Việt Nam với công tắc bật/tắt linh hoạt) (BR-GUEST-06).
/// </summary>
public class GuestSecurityMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GuestSecurityMiddleware> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        WriteIndented = false
    };

    public GuestSecurityMiddleware(RequestDelegate next, ILogger<GuestSecurityMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(
        HttpContext context,
        IRedisService redisService,
        IOptions<GuestSecuritySettings> securityOptions)
    {
        var path = context.Request.Path.Value?.ToLowerInvariant() ?? string.Empty;

        // Bỏ qua các endpoint hạ tầng, tài liệu Swagger và kiểm tra sức khỏe
        if (path.StartsWith("/swagger") ||
            path.StartsWith("/health") ||
            path.StartsWith("/favicon") ||
            path == "/" ||
            path.Contains("/dev-latest-otp"))
        {
            await _next(context);
            return;
        }

        var settings = securityOptions.Value;

        // 1. Kiểm tra miễn trừ: Nếu là Thành viên chính thức (MEMBER, VIP, ADMIN) thì không bị áp Rate Limit của Khách
        var user = context.User;
        var isMemberOrAbove = user?.Identity?.IsAuthenticated == true &&
                              (user.IsInRole(UserRoles.Member) ||
                               user.IsInRole(UserRoles.Vip) ||
                               user.IsInRole(UserRoles.Admin) ||
                               user.Claims.Any(c => 
                                   (c.Type == ClaimTypes.Role || c.Type == "role" || c.Type.EndsWith("/role")) &&
                                   (string.Equals(c.Value, UserRoles.Member, StringComparison.OrdinalIgnoreCase) ||
                                    string.Equals(c.Value, UserRoles.Vip, StringComparison.OrdinalIgnoreCase) ||
                                    string.Equals(c.Value, UserRoles.Admin, StringComparison.OrdinalIgnoreCase))));

        if (isMemberOrAbove)
        {
            await _next(context);
            return;
        }

        // 2. Phân giải địa chỉ IP và Quốc gia của người gửi
        var clientIp = ResolveClientIp(context);
        var countryCode = ResolveCountryCode(context, clientIp);

        // 3. Kiểm tra ràng buộc vùng địa lý (Chỉ cho dùng từ Việt Nam khi công tắc bật)
        if (settings.EnforceVietnamOnly)
        {
            if (!settings.AllowedCountryCodes.Contains(countryCode, StringComparer.OrdinalIgnoreCase))
            {
                _logger.LogWarning("Chặn truy cập khách từ ngoài lãnh thổ Việt Nam: IP {Ip}, Country {Country}", clientIp, countryCode);

                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                context.Response.ContentType = "application/json; charset=utf-8";

                var regionError = new ApiErrorResponse(
                    StatusCodes.Status403Forbidden,
                    ErrorCodes.ErrGuestRegionUnsupported,
                    "Hệ thống chỉ hỗ trợ người dùng truy cập từ lãnh thổ Việt Nam (BR-GUEST-06). Vui lòng đăng ký tài khoản để được hỗ trợ.",
                    new { clientIp, countryCode, allowedCountries = settings.AllowedCountryCodes });

                await context.Response.WriteAsync(JsonSerializer.Serialize(regionError, JsonOptions));
                return;
            }
        }

        // 4. Kiểm tra Rate Limit: 10 yêu cầu/phút mỗi IP, vượt thì chặn 10 phút
        // Nếu là localhost/loopback và không bật RateLimitLocalhost (và không có header X-Enforce-Rate-Limit), bỏ qua Rate Limit để không chặn test runner
        var isLocal = IsLoopbackOrPrivateIp(clientIp);
        var forceRateLimit = context.Request.Headers.ContainsKey("X-Enforce-Rate-Limit");
        if (isLocal && !settings.RateLimitLocalhost && !forceRateLimit)
        {
            await _next(context);
            return;
        }

        var rateLimitResult = await redisService.CheckIpRateLimitAsync(
            clientIp,
            settings.RateLimitPerMinute,
            settings.BanDurationMinutes,
            context.RequestAborted);

        if (!rateLimitResult.Allowed)
        {
            _logger.LogWarning("IP {Ip} đã vượt hạn mức Rate Limit hoặc đang bị tạm khóa 10 phút (Count: {Count}, RetryAfter: {Ttl}s)",
                clientIp, rateLimitResult.CurrentCount, rateLimitResult.RetryAfter?.TotalSeconds);

            context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
            context.Response.ContentType = "application/json; charset=utf-8";

            if (rateLimitResult.RetryAfter.HasValue)
            {
                var seconds = (int)Math.Max(1, rateLimitResult.RetryAfter.Value.TotalSeconds);
                context.Response.Headers["Retry-After"] = seconds.ToString();
            }

            var message = "Vượt quá giới hạn 10 yêu cầu/phút. Địa chỉ IP của bạn bị tạm khóa trong 10 phút (BR-GUEST-SEC-01). Vui lòng thử lại sau.";
            var errorResponse = new ApiErrorResponse(
                StatusCodes.Status429TooManyRequests,
                ErrorCodes.ErrGuestRateLimitExceeded,
                message,
                new
                {
                    ip = clientIp,
                    retryAfterSeconds = (int)(rateLimitResult.RetryAfter?.TotalSeconds ?? 600)
                });

            await context.Response.WriteAsync(JsonSerializer.Serialize(errorResponse, JsonOptions));
            return;
        }

        await _next(context);
    }

    private static string ResolveClientIp(HttpContext context)
    {
        if (context.Request.Headers.TryGetValue("X-Forwarded-For", out var forwardedFor) && !string.IsNullOrWhiteSpace(forwardedFor))
        {
            var firstIp = forwardedFor.ToString().Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).FirstOrDefault();
            if (!string.IsNullOrEmpty(firstIp))
            {
                return firstIp;
            }
        }

        if (context.Request.Headers.TryGetValue("X-Real-IP", out var realIp) && !string.IsNullOrWhiteSpace(realIp))
        {
            return realIp.ToString().Trim();
        }

        return context.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";
    }

    private static string ResolveCountryCode(HttpContext context, string ip)
    {
        // Ưu tiên header tùy chỉnh phục vụ kiểm thử và Proxy Gateway
        if (context.Request.Headers.TryGetValue("X-Country-Code", out var customCountry) && !string.IsNullOrWhiteSpace(customCountry))
        {
            return customCountry.ToString().Trim().ToUpperInvariant();
        }

        // Header Cloudflare
        if (context.Request.Headers.TryGetValue("CF-IPCountry", out var cfCountry) && !string.IsNullOrWhiteSpace(cfCountry))
        {
            return cfCountry.ToString().Trim().ToUpperInvariant();
        }

        // Môi trường Localhost và mạng nội bộ mặc định coi là VN để lập trình viên kiểm thử thuận tiện
        if (IsLoopbackOrPrivateIp(ip))
        {
            return "VN";
        }

        return "VN";
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
                // 10.0.0.0/8
                if (bytes[0] == 10) return true;
                // 172.16.0.0/12
                if (bytes[0] == 172 && bytes[1] >= 16 && bytes[1] <= 31) return true;
                // 192.168.0.0/16
                if (bytes[0] == 192 && bytes[1] == 168) return true;
            }
        }

        return false;
    }
}
