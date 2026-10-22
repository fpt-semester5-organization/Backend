using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using VeggieLife.BLL.Common.Constants;
using VeggieLife.BLL.Common.Exceptions;
using VeggieLife.BLL.Common.Responses;
using VeggieLife.BLL.DTOs.Admin.Subscriptions;
using VeggieLife.BLL.Services.Admin;

namespace VeggieLife.API.Controllers.Admin;

/// <summary>
/// API Controller quản trị cấu hình các gói VIP và giá dành cho Quản trị viên (FN-ADM-13, BR-ADM-PAY-001).
/// </summary>
[Route("api/v1/admin/subscription-plans")]
[Authorize(Policy = PolicyNames.RequireAdmin)]
public class AdminSubscriptionPlansController : BaseApiController
{
    private readonly IAdminSubscriptionService _subscriptionService;

    public AdminSubscriptionPlansController(IAdminSubscriptionService subscriptionService)
    {
        _subscriptionService = subscriptionService;
    }

    /// <summary>
    /// FN-ADM-13: Lấy toàn bộ danh sách gói VIP dành cho Quản trị viên (bao gồm gói bật/tắt).
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<AdminSubscriptionPlanItemDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> ListSubscriptionPlans(CancellationToken ct)
    {
        EnsureAdmin();
        var result = await _subscriptionService.GetAllSubscriptionPlansAsync(ct);
        return OkResponse(result, "OK");
    }

    /// <summary>
    /// FN-ADM-13: Tạo mới gói VIP (tên gói duy nhất, thời hạn, giá).
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<CreateAdminSubscriptionPlanResponseData>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateSubscriptionPlan(
        [FromBody] CreateAdminSubscriptionPlanRequest request,
        CancellationToken ct)
    {
        var adminId = EnsureAdmin();
        var result = await _subscriptionService.CreateSubscriptionPlanAsync(request, adminId, ct);
        return CreatedResponse(result, "Đã tạo gói.");
    }

    /// <summary>
    /// FN-ADM-13: Cập nhật giá bán hoặc trạng thái bật/tắt của gói VIP. Giá mới không áp hồi tố.
    /// </summary>
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(ApiResponse<UpdateAdminSubscriptionPlanResponseData>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateSubscriptionPlan(
        [FromRoute] int id,
        [FromBody] UpdateAdminSubscriptionPlanRequest request,
        CancellationToken ct)
    {
        var adminId = EnsureAdmin();
        var result = await _subscriptionService.UpdateSubscriptionPlanAsync(id, request, adminId, ct);
        return OkResponse(result, "Đã cập nhật gói.");
    }

    private int EnsureAdmin()
    {
        if (User.Identity == null || !User.Identity.IsAuthenticated)
        {
            throw AppException.Unauthorized(ErrorCodes.ErrAuthInvalidToken, "Thiếu, sai chữ ký hoặc hết hạn JWT.");
        }

        if (!string.Equals(CurrentUserRole, UserRoles.Admin, StringComparison.OrdinalIgnoreCase))
        {
            throw AppException.Forbidden(ErrorCodes.ErrAdmForbidden, "Claim role khác ADMIN.");
        }

        if (CurrentUserId == null)
        {
            throw AppException.Unauthorized(ErrorCodes.ErrAuthInvalidToken, "Thiếu, sai chữ ký hoặc hết hạn JWT.");
        }

        return (int)CurrentUserId.Value;
    }
}
