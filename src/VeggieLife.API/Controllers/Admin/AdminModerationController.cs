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
using VeggieLife.BLL.DTOs.Admin.Moderation;
using VeggieLife.BLL.Services.Admin;

namespace VeggieLife.API.Controllers.Admin;

/// <summary>
/// API Controller quản trị hàng đợi kiểm duyệt nội dung và quyết định xử lý báo cáo (FN-ADM-04, FN-ADM-05).
/// </summary>
[Route("api/v1/admin/moderation")]
[Authorize(Policy = PolicyNames.RequireAdmin)]
public class AdminModerationController : BaseApiController
{
    private readonly IAdminModerationService _moderationService;

    public AdminModerationController(IAdminModerationService moderationService)
    {
        _moderationService = moderationService;
    }

    /// <summary>
    /// FN-ADM-04: Lấy danh sách nội dung trong hàng đợi chờ duyệt (từ AI_SCAN hoặc USER_REPORT).
    /// </summary>
    [HttpGet("queue")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<AdminModerationQueueItemDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> ListModerationQueue(
        [FromQuery] AdminModerationQueueFilterRequest filter,
        CancellationToken ct)
    {
        EnsureAdmin();
        var (items, meta) = await _moderationService.GetModerationQueueAsync(filter, ct);
        return PagedResponse(items, meta, "OK");
    }

    /// <summary>
    /// FN-ADM-05: Quyết định xử lý báo cáo vi phạm nội dung (UPHOLD: gỡ nội dung; DISMISS: khôi phục).
    /// </summary>
    [HttpPut("reports/{id:int}/decision")]
    [ProducesResponseType(typeof(ApiResponse<DecideAdminModerationReportResponseData>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> DecideReport(
        [FromRoute] int id,
        [FromBody] DecideAdminModerationReportRequest request,
        CancellationToken ct)
    {
        var adminId = EnsureAdmin();
        var result = await _moderationService.DecideReportAsync(id, request, adminId, ct);
        return OkResponse(result, "Đã xử lý báo cáo.");
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
