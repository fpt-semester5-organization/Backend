using System.Security.Claims;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using VeggieLife.BLL.Common.Constants;
using VeggieLife.BLL.Common.Responses;
using VeggieLife.BLL.Services.Cache;

namespace VeggieLife.API.Middlewares;

/// <summary>
/// Middleware kiểm soát danh sách đen tài khoản (User Blacklist Gatekeeper).
/// Chặn đứng mọi request từ các tài khoản đã bị Quản trị viên áp dụng chế tài cấm hoặc tạm khóa (BAN/SUSPEND),
/// đảm bảo thu hồi quyền truy cập tức thì với mã HTTP 403 Forbidden.
/// </summary>
public class UserBlacklistMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<UserBlacklistMiddleware> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        WriteIndented = false
    };

    public UserBlacklistMiddleware(RequestDelegate next, ILogger<UserBlacklistMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, IRedisService redisService)
    {
        if (context.User.Identity?.IsAuthenticated == true)
        {
            var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                         ?? context.User.FindFirst("sub")?.Value;

            if (!string.IsNullOrEmpty(userId))
            {
                var isBlacklisted = await redisService.ExistsAsync($"user_blacklist:{userId}", context.RequestAborted);
                if (isBlacklisted)
                {
                    _logger.LogWarning("Chặn request người dùng bị cấm ID {UserId} với HTTP 403 Forbidden.", userId);

                    context.Response.StatusCode = StatusCodes.Status403Forbidden;
                    context.Response.ContentType = "application/json; charset=utf-8";

                    var response = new ApiErrorResponse(
                        StatusCodes.Status403Forbidden,
                        ErrorCodes.ErrAuthAccountBanned,
                        "Tài khoản của bạn đã bị đình chỉ hoặc cấm do vi phạm điều khoản dịch vụ. Vui lòng liên hệ quản trị viên để biết thêm chi tiết.");

                    await context.Response.WriteAsync(JsonSerializer.Serialize(response, JsonOptions));
                    return;
                }
            }
        }

        await _next(context);
    }
}

