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
    /// Thành viên: bán kính 1..10 km, không giới hạn lượt (BR-USR-11).
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

    /// <summary>
    /// FN-MAP-02: Xem chi tiết một quán chay (không có thực đơn quán).
    /// Hỗ trợ cả Khách (GUEST) và Thành viên (MEMBER/VIP).
    /// Quán bị ẩn trả về 404 ERR_VENUE_NOT_FOUND (BR-ADM-MAP-001).
    /// </summary>
    /// <param name="id">Mã định danh quán ăn (> 0).</param>
    /// <param name="lat">Vĩ độ hiện tại của người dùng (tùy chọn, để tính khoảng cách).</param>
    /// <param name="lng">Kinh độ hiện tại của người dùng (tùy chọn, để tính khoảng cách).</param>
    /// <param name="ct">CancellationToken.</param>
    [HttpGet("{id:int}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<VenueDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetVenue(
        [FromRoute] int id,
        [FromQuery] double? lat = null,
        [FromQuery] double? lng = null,
        CancellationToken ct = default)
    {
        var result = await _venueService.GetVenueDetailAsync(id, lat, lng, ct);
        return OkResponse(result, "OK");
    }
}
