using System;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using VeggieLife.BLL.Common.Constants;
using VeggieLife.BLL.Common.Exceptions;
using VeggieLife.BLL.DTOs.Guest.Responses;
using VeggieLife.BLL.Services.Guest;

namespace VeggieLife.API.Controllers;

/// <summary>
/// Controller xử lý các yêu cầu phiên Khách vãng lai và hạn mức dùng thử (FN-AUTH-01, FN-AUTH-02).
/// </summary>
[ApiController]
[Route("api/v1/guest")]
public class GuestController : BaseApiController
{
    private readonly IGuestService _guestService;

    public GuestController(IGuestService guestService)
    {
        _guestService = guestService;
    }

    /// <summary>
    /// FN-AUTH-02: Tra cứu hạn mức dùng thử còn lại của phiên khách (AI, Quét mã vạch, Bản đồ).
    /// </summary>
    /// <param name="ct">CancellationToken.</param>
    /// <returns>Thông tin hạn mức dùng thử còn lại.</returns>
    [HttpGet("quota")]
    public async Task<IActionResult> GetQuota(CancellationToken ct)
    {
        // 1. Xác thực Anonymous JWT; role khác GUEST thì từ chối (FN-AUTH-02)
        if (User.Identity?.IsAuthenticated == true)
        {
            var isGuest = User.IsInRole(UserRoles.Guest) ||
                          User.Claims.Any(c =>
                              (c.Type == ClaimTypes.Role || c.Type == "role" || c.Type.EndsWith("/role")) &&
                              string.Equals(c.Value, UserRoles.Guest, StringComparison.OrdinalIgnoreCase));

            if (!isGuest)
            {
                throw AppException.Forbidden(ErrorCodes.ErrAdmForbidden, "Chỉ tài khoản Khách vãng lai mới có quyền tra cứu hạn mức dùng thử (FN-AUTH-02).");
            }
        }
        else
        {
            // Nếu có Authorization header nhưng không xác thực được (sai chữ ký hoặc hết hạn) -> 401
            if (Request.Headers.ContainsKey("Authorization"))
            {
                throw AppException.Unauthorized(ErrorCodes.ErrAuthInvalidToken, "Thiếu, sai chữ ký hoặc hết hạn JWT (FN-AUTH-02).");
            }

            // Nếu không có Bearer token, kiểm tra định danh phiên khách (Cookie guest_uuid hoặc header X-Guest-UUID)
            if (!Request.Headers.ContainsKey("X-Guest-UUID") && !Request.Cookies.ContainsKey("guest_uuid"))
            {
                throw AppException.Unauthorized(ErrorCodes.ErrAuthInvalidToken, "Yêu cầu cần Anonymous JWT hoặc định danh phiên khách hợp lệ (FN-AUTH-02).");
            }
        }

        var guestUuid = ResolveGuestId();
        if (string.IsNullOrEmpty(guestUuid) || !Guid.TryParse(guestUuid, out _))
        {
            throw AppException.Unauthorized(ErrorCodes.ErrAuthInvalidToken, "Định danh phiên khách (Guest_UUID) không hợp lệ (FN-AUTH-02).");
        }

        var result = await _guestService.GetGuestQuotaAsync(guestUuid, ct);
        return OkResponse(result, "Lấy hạn mức dùng thử thành công.");
    }

    /// <summary>
    /// Tiêu hao 01 lượt dùng thử tính năng nguyên tử (Atomic Quota Consumption).
    /// </summary>
    /// <param name="trialType">Loại tính năng dùng thử: Chatbot/Ai (1), BarcodeDaily (2), MapSearch (3), FridgeVision (4), VideoStt (5).</param>
    /// <param name="ct">CancellationToken.</param>
    /// <returns>Kết quả tiêu hao và số lượt còn lại.</returns>
    [HttpPost("trial/{trialType}")]
    public async Task<IActionResult> ConsumeTrial([FromRoute] GuestTrialType trialType, CancellationToken ct)
    {
        var guestUuid = ResolveGuestId();
        if (string.IsNullOrEmpty(guestUuid) || !Guid.TryParse(guestUuid, out _))
        {
            throw AppException.BadRequest(ErrorCodes.ErrValidation, "Không xác định được định danh phiên khách hợp lệ (thiếu hoặc sai Guest_UUID).");
        }

        var result = await _guestService.ConsumeTrialQuotaAsync(guestUuid, trialType, ct);
        return OkResponse(result, result.Message);
    }

    private string? ResolveGuestId()
    {
        if (!string.IsNullOrEmpty(CurrentGuestUuid))
        {
            return CurrentGuestUuid;
        }

        if (Request.Headers.TryGetValue("X-Guest-UUID", out var headerValue) && !string.IsNullOrWhiteSpace(headerValue))
        {
            return headerValue.ToString().Trim();
        }

        if (Request.Cookies.TryGetValue("guest_uuid", out var cookieValue) && !string.IsNullOrWhiteSpace(cookieValue))
        {
            return cookieValue.Trim();
        }

        return null;
    }
}
