using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using VeggieLife.BLL.Common.Constants;
using VeggieLife.BLL.Common.Exceptions;
using VeggieLife.BLL.Common.Responses;
using VeggieLife.BLL.DTOs.Venues.Requests;
using VeggieLife.BLL.DTOs.Venues.Responses;
using VeggieLife.BLL.Services.Venues;

namespace VeggieLife.API.Controllers;

/// <summary>
/// Controller quản lý bản đồ quán ăn thuần chay (FN-MAP-01).
/// </summary>
[ApiController]
[Route("api/v1/venues")]
public class VenuesController : BaseApiController
{
    private readonly IVenueService _venueService;

    public VenuesController(IVenueService venueService)
    {
        _venueService = venueService;
    }

    /// <summary>
    /// FN-MAP-01: Tìm quán chay lân cận quanh tọa độ GPS hoặc theo tỉnh/thành.
    /// Khách vãng lai: bán kính tối đa 5 km, tối đa 03 lượt (BR-GUEST-03).
    /// Thành viên: bán kính 1..10 km, không giới hạn lượt, loại bỏ quán &lt; 2.5 sao (BR-USR-11).
    /// </summary>
    [HttpGet("nearby")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<List<NearbyVenueItemDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> ListNearbyVenues(
        [FromQuery] ListNearbyVenuesRequest request,
        CancellationToken ct = default)
    {
        if (User?.Identity?.IsAuthenticated != true)
        {
            throw AppException.Unauthorized(
                ErrorCodes.ErrAuthInvalidToken,
                "Thiếu, sai chữ ký hoặc hết hạn JWT.");
        }

        var userId = CurrentUserId.HasValue ? (int)CurrentUserId.Value : (int?)null;
        var userRole = CurrentUserRole ?? UserRoles.Guest;

        string? guestUuid = Request.Headers["X-Guest-UUID"].ToString();
        if (string.IsNullOrWhiteSpace(guestUuid))
        {
            guestUuid = CurrentGuestUuid;
        }

        var result = await _venueService.ListNearbyVenuesAsync(
            request,
            userId,
            userRole,
            guestUuid,
            ct);

        return PagedResponse(result.Venues, result.Meta, "Tìm thấy quán chay quanh bạn.");
    }
}
