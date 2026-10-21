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
using VeggieLife.BLL.DTOs.Admin.Venues;
using VeggieLife.BLL.Services.Admin;

namespace VeggieLife.API.Controllers.Admin;

/// <summary>
/// API Controller quản trị quán chay & đồng bộ nguồn bên ngoài (FN-ADM-14).
/// </summary>
[Route("api/v1/admin/venues")]
[Authorize(Policy = PolicyNames.RequireAdmin)]
public class AdminVenuesController : BaseApiController
{
    private readonly IAdminVenueService _venueService;

    public AdminVenuesController(IAdminVenueService venueService)
    {
        _venueService = venueService;
    }

    /// <summary>
    /// FN-ADM-14: Lấy danh sách quán chay theo bộ lọc và phân trang dành cho Quản trị viên.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<AdminVenueListItemDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> ListVenues(
        [FromQuery] AdminVenueFilterRequest filter,
        CancellationToken ct)
    {
        EnsureAdmin();
        var (items, meta) = await _venueService.GetVenuesAsync(filter, ct);
        return PagedResponse(items, meta, "OK");
    }

    /// <summary>
    /// FN-ADM-14: Ẩn hoặc hiện lại quán chay không phù hợp (BR-ADM-MAP-001).
    /// </summary>
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(ApiResponse<UpdateAdminVenueResponseData>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateVenue(
        [FromRoute] int id,
        [FromBody] UpdateAdminVenueRequest request,
        CancellationToken ct)
    {
        var adminId = EnsureAdmin();
        var result = await _venueService.UpdateVenueStatusAsync(id, request, adminId, ct);
        return OkResponse(result, "Đã cập nhật.");
    }

    /// <summary>
    /// FN-ADM-14: Xếp job đồng bộ quán chay từ Google Places / HappyCow theo tỉnh/thành.
    /// </summary>
    [HttpPost("sync")]
    [ProducesResponseType(typeof(ApiResponse<SyncAdminVenuesResponseData>), StatusCodes.Status202Accepted)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> SyncVenues(
        [FromBody] SyncAdminVenuesRequest request,
        CancellationToken ct)
    {
        var adminId = EnsureAdmin();
        var result = await _venueService.SyncVenuesAsync(request, adminId, ct);
        return AcceptedResponse(result, "Đã xếp job đồng bộ.");
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
