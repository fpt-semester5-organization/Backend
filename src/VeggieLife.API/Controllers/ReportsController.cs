using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using VeggieLife.BLL.Common.Constants;
using VeggieLife.BLL.Common.Exceptions;
using VeggieLife.BLL.Common.Responses;
using VeggieLife.BLL.DTOs.Community.Requests;
using VeggieLife.BLL.DTOs.Community.Responses;
using VeggieLife.BLL.Services.Community;

namespace VeggieLife.API.Controllers;

/// <summary>
/// Controller tiếp nhận báo cáo vi phạm nội dung cộng đồng.
/// Tác nhân: Thành viên (MEMBER, VIP, ADMIN). Khách vãng lai bị từ chối 403 (BR-AUTH-03).
/// </summary>
[ApiController]
[Route("api/v1/reports")]
public class ReportsController : BaseApiController
{
    private readonly ICommunityInteractionService _communityService;

    public ReportsController(ICommunityInteractionService communityService)
    {
        _communityService = communityService;
    }

    /// <summary>
    /// Gửi báo cáo vi phạm bài viết, công thức, video hoặc bình luận.
    /// Giới hạn tối đa 10 báo cáo/ngày/tài khoản (BR-USR-04), không báo trùng (ERR_REPORT_DUPLICATE).
    /// Nội dung bị báo cáo được ẩn ngay lập tức (BR-INT-REPORT-01, BR-ADM-CNT-001) chờ Quản trị viên xử lý.
    /// </summary>
    /// <param name="request">Payload thông tin báo cáo.</param>
    /// <param name="ct">CancellationToken.</param>
    /// <returns>Mã phiếu báo cáo vi phạm và trạng thái PENDING.</returns>
    [HttpPost]
    [Authorize(Policy = PolicyNames.RequireMember)]
    [ProducesResponseType(typeof(ApiResponse<ReportResponseData>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CreateReport([FromBody] CreateReportRequest request, CancellationToken ct)
    {
        EnsureAuthorizedMember();

        var userId = (int)CurrentUserId!.Value;
        var result = await _communityService.CreateReportAsync(request, userId, ct);

        return CreatedResponse(result, "Đã ghi nhận báo cáo. Nội dung đã được ẩn chờ Admin xử lý.");
    }

    /// <summary>
    /// Kiểm tra và xác thực quyền thành viên chính thức cho các tác vụ ghi dữ liệu cộng đồng.
    /// </summary>
    private void EnsureAuthorizedMember()
    {
        if (User?.Identity == null || !User.Identity.IsAuthenticated)
        {
            throw AppException.Unauthorized(ErrorCodes.ErrAuthInvalidToken, "Thiếu hoặc sai chữ ký, hết hạn JWT.");
        }

        if (IsGuestUser())
        {
            throw AppException.Forbidden(ErrorCodes.ErrGuestCommunityActionDenied, "Khách vãng lai thực hiện thao tác ghi cộng đồng (FN-AUTH-03).");
        }

        if (!CurrentUserId.HasValue)
        {
            throw AppException.Unauthorized(ErrorCodes.ErrAuthInvalidToken, "Thiếu thông tin người dùng trong token.");
        }
    }

    /// <summary>
    /// Xác định xem người dùng hiện tại là khách vãng lai hay thành viên đã đăng nhập.
    /// </summary>
    private bool IsGuestUser()
    {
        if (User?.Identity == null || !User.Identity.IsAuthenticated)
        {
            return true;
        }

        var role = CurrentUserRole;
        return string.IsNullOrEmpty(role) || string.Equals(role, "GUEST", System.StringComparison.OrdinalIgnoreCase);
    }
}
