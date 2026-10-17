using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VeggieLife.BLL.Common.Constants;
using VeggieLife.BLL.Common.Exceptions;
using VeggieLife.BLL.DTOs.Subscriptions.Requests;
using VeggieLife.BLL.Services.Subscriptions;

namespace VeggieLife.API.Controllers;

/// <summary>
/// Controller quản lý danh mục gói VIP và quy trình mua gói (FN-SUB-01, FN-SUB-02, FN-SUB-04).
/// </summary>
[ApiController]
public class SubscriptionsController : BaseApiController
{
    private readonly ISubscriptionService _subscriptionService;

    public SubscriptionsController(ISubscriptionService subscriptionService)
    {
        _subscriptionService = subscriptionService;
    }

    /// <summary>
    /// FN-SUB-01: Xem danh sách các gói VIP đang mở bán và bảng so sánh quyền lợi Free/VIP.
    /// Auth: Public / Bearer (GUEST | MEMBER | VIP).
    /// </summary>
    [HttpGet("/api/v1/subscription-plans")]
    [HttpGet("/subscription-plans")]
    [AllowAnonymous]
    public async Task<IActionResult> ListSubscriptionPlans(CancellationToken ct)
    {
        var result = await _subscriptionService.GetSubscriptionPlansAsync(ct);
        return OkResponse(result, "Lấy danh sách gói VIP thành công.");
    }

    /// <summary>
    /// FN-SUB-02: Thanh toán gói VIP (MoMo / VietQR).
    /// Auth: Bearer (MEMBER | VIP).
    /// </summary>
    [HttpPost("/api/v1/subscriptions/checkout")]
    [HttpPost("/subscriptions/checkout")]
    [Authorize(Policy = PolicyNames.RequireMember)]
    public async Task<IActionResult> CheckoutSubscription(
        [FromBody] CheckoutSubscriptionRequest request,
        CancellationToken ct)
    {
        EnsureAuthorizedMember();
        var result = await _subscriptionService.CheckoutSubscriptionAsync(
            (int)CurrentUserId!.Value,
            CurrentUserRole ?? UserRoles.Member,
            request,
            ct);

        return CreatedResponse(result, "Đã tạo đơn thanh toán.");
    }

    /// <summary>
    /// FN-SUB-04: Xem trạng thái VIP của tôi và lịch sử giao dịch.
    /// Auth: Bearer (MEMBER | VIP).
    /// </summary>
    [HttpGet("/api/v1/users/me/subscription")]
    [HttpGet("/users/me/subscription")]
    [HttpGet("/api/v1/subscriptions/me")]
    [Authorize(Policy = PolicyNames.RequireMember)]
    public async Task<IActionResult> GetCurrentUserSubscription(CancellationToken ct)
    {
        EnsureAuthorizedMember();
        var result = await _subscriptionService.GetCurrentUserSubscriptionAsync((int)CurrentUserId!.Value, ct);
        return OkResponse(result, "Lấy trạng thái gói VIP thành công.");
    }

    /// <summary>
    /// Kiểm tra tính hợp lệ của tài khoản đăng nhập và từ chối khách vãng lai gọi tính năng cá nhân hóa (FN-AUTH-03).
    /// </summary>
    private void EnsureAuthorizedMember()
    {
        if (string.Equals(CurrentUserRole, UserRoles.Guest, System.StringComparison.OrdinalIgnoreCase))
        {
            throw AppException.Forbidden(
                ErrorCodes.ErrGuestPersonalizationRestricted,
                "Khách vãng lai gọi tính năng cá nhân hóa (FN-AUTH-03).");
        }

        if (!CurrentUserId.HasValue)
        {
            throw AppException.Unauthorized(
                ErrorCodes.ErrAuthInvalidToken,
                "Phiên đăng nhập không hợp lệ hoặc thiếu thông tin định danh.");
        }
    }
}
