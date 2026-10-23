using System;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using VeggieLife.BLL.Common.Constants;
using VeggieLife.BLL.Common.Exceptions;
using VeggieLife.BLL.Common.Responses;
using VeggieLife.BLL.DTOs.Admin.Analytics;
using VeggieLife.BLL.Services.Admin;

namespace VeggieLife.API.Controllers.Admin;

/// <summary>
/// API Controller thống kê tổng quan và phân tích dữ liệu nền tảng dành riêng cho Quản trị viên (FN-ADM-18).
/// Tuân thủ BR-ADM-REP-01 (Chỉ Admin xem báo cáo tăng trưởng, không lộ PII), BR-ADM-AI-001 (Theo dõi chi phí AI),
/// và cơ chế lưu tạm Redis Cache 5 phút.
/// </summary>
[Route("api/v1/admin/analytics")]
[Authorize(Policy = PolicyNames.RequireAdmin)]
public class AdminAnalyticsController : BaseApiController
{
    private readonly IAdminAnalyticsService _analyticsService;

    public AdminAnalyticsController(IAdminAnalyticsService analyticsService)
    {
        _analyticsService = analyticsService;
    }

    /// <summary>
    /// FN-ADM-18: Thống kê tổng quan nền tảng (người dùng, nội dung, VIP và doanh thu, hàng đợi duyệt, chi phí AI).
    /// </summary>
    [HttpGet("overview")]
    [ProducesResponseType(typeof(ApiResponse<AdminAnalyticsOverviewResponseData>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetOverview(
        [FromQuery] AdminAnalyticsOverviewFilterRequest filter,
        CancellationToken ct)
    {
        EnsureAdmin();
        var data = await _analyticsService.GetOverviewAsync(filter, ct);
        return OkResponse(data, "OK");
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
