using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using VeggieLife.BLL.Common.Constants;
using VeggieLife.BLL.Common.Exceptions;
using VeggieLife.BLL.DTOs.Guest.Responses;
using VeggieLife.BLL.Services.Cache;

namespace VeggieLife.API.Authorization;

/// <summary>
/// Bộ lọc dùng chung chặn mọi thao tác ghi cộng đồng và tính năng cá nhân hóa
/// khi request không mang Access Token của Authorized User (FN-AUTH-03, BR-AUTH-03, BR-GUEST-04, BR-GUEST-05, BR-GUEST-AUTH-01).
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
public class MemberOnlyAttribute : Attribute, IAsyncAuthorizationFilter
{
    public MemberFeatureType FeatureType { get; set; }

    public MemberOnlyAttribute(MemberFeatureType featureType = MemberFeatureType.General)
    {
        FeatureType = featureType;
    }

    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        var user = context.HttpContext.User;

        // 1. Không có token hoặc token không hợp lệ: trả 401 kèm đường dẫn đăng ký/đăng nhập (BR-AUTH-03)
        if (user?.Identity == null || !user.Identity.IsAuthenticated)
        {
            throw AppException.Unauthorized(
                ErrorCodes.ErrAuthInvalidToken,
                "Yêu cầu chưa được xác thực hoặc phiên làm việc đã hết hạn. Vui lòng đăng nhập hoặc đăng ký tài khoản.",
                new { registerUrl = "/register", loginUrl = "/login" });
        }

        // 2. Token role MEMBER/VIP/ADMIN: chuyển tiếp tới Controller đích
        var isGuest = user.IsInRole(UserRoles.Guest) ||
                      user.Claims.Any(c => 
                          (c.Type == ClaimTypes.Role || c.Type == "role" || c.Type.EndsWith("/role")) &&
                          string.Equals(c.Value, UserRoles.Guest, StringComparison.OrdinalIgnoreCase));

        if (!isGuest)
        {
            return;
        }

        // 3. Token role GUEST:
        var httpContext = context.HttpContext;
        string? returnUrl = null;
        if (httpContext.Request.Headers.TryGetValue("X-Return-Url", out var returnUrlHeader))
        {
            var rawUrl = returnUrlHeader.ToString().Trim();
            // Điều kiện an toàn: Bắt đầu bằng '/' và độ dài <= 500 ký tự (chống Open Redirect)
            if (rawUrl.StartsWith('/') && rawUrl.Length <= 500)
            {
                returnUrl = rawUrl;
            }
        }

        string? pendingAction = null;
        if (httpContext.Request.Headers.TryGetValue("X-Pending-Action", out var pendingActionHeader))
        {
            var rawAction = pendingActionHeader.ToString().Trim().ToUpperInvariant();
            if (PendingActions.IsValid(rawAction))
            {
                pendingAction = rawAction;
            }
        }

        // 4. Lưu { returnUrl, pendingAction } vào Redis guest_return:{guestId} TTL 15 phút (BR-GUEST-AUTH-01)
        var guestId = user.FindFirstValue("Guest_UUID")
                      ?? user.FindFirstValue(ClaimTypes.NameIdentifier)
                      ?? (httpContext.Request.Cookies.TryGetValue("guest_uuid", out var cookieId) ? cookieId : null);

        if (!string.IsNullOrEmpty(guestId) && (!string.IsNullOrEmpty(returnUrl) || !string.IsNullOrEmpty(pendingAction)))
        {
            var redis = httpContext.RequestServices.GetService<IRedisService>();
            if (redis != null)
            {
                var returnContext = new GuestReturnContextDto
                {
                    ReturnUrl = returnUrl,
                    PendingAction = pendingAction,
                    CreatedAt = DateTime.UtcNow
                };
                await redis.SetAsync($"guest_return:{guestId}", returnContext, TimeSpan.FromMinutes(15));
            }
        }

        var authUrls = new { registerUrl = "/register", loginUrl = "/login" };

        switch (FeatureType)
        {
            case MemberFeatureType.Community:
                throw AppException.Forbidden(
                    ErrorCodes.ErrGuestCommunityActionDenied,
                    "Khách vãng lai thực hiện thao tác ghi cộng đồng (FN-AUTH-03). Vui lòng đăng ký tài khoản để thích, bình luận và chia sẻ bài viết.",
                    authUrls);

            case MemberFeatureType.Personalization:
                throw AppException.Forbidden(
                    ErrorCodes.ErrGuestPersonalizationRestricted,
                    "Khách vãng lai gọi tính năng cá nhân hóa (FN-AUTH-03). Vui lòng đăng ký tài khoản để lưu trữ thông tin và theo dõi sức khỏe cá nhân.",
                    authUrls);

            default:
                throw AppException.Forbidden(
                    ErrorCodes.ErrGuestPersonalizationRestricted,
                    "Tính năng chỉ dành cho thành viên chính thức (FN-AUTH-03). Vui lòng đăng ký tài khoản để tiếp tục.",
                    authUrls);
        }
    }
}
