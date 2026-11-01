using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using VeggieLife.BLL.Common.Constants;
using VeggieLife.BLL.Common.Exceptions;
using VeggieLife.BLL.Common.Responses;
using VeggieLife.BLL.DTOs.Admin.Llmops;
using VeggieLife.BLL.Services.Admin;

namespace VeggieLife.API.Controllers.Admin;

/// <summary>
/// API Controller theo dõi giám sát mức tiêu thụ Token và chi phí AI (LLMOps) dành cho Quản trị viên (FN-ADM-16).
/// Tuân thủ:
/// - BR-ADM-AI-001: Chi phí tiêu thụ Token tính theo thời gian thực; cảnh báo khẩn cấp khi đạt 80% ngân sách ngày ($10) và tự động kích hoạt hạ tải (Throttling) ở 100% ($12.5).
/// - RESTful URL: GET /api/v1/admin/llmops/dashboard
/// </summary>
[Route("api/v1/admin/llmops")]
[Authorize(Policy = PolicyNames.RequireAdmin)]
public class AdminLlmopsController : BaseApiController
{
    private readonly IAdminLlmopsService _llmopsService;

    public AdminLlmopsController(IAdminLlmopsService llmopsService)
    {
        _llmopsService = llmopsService;
    }

    /// <summary>
    /// FN-ADM-16: Lấy dữ liệu bảng điều khiển LLMOps, chi phí AI theo ngày/tính năng và trạng thái ngân sách.
    /// </summary>
    [HttpGet("dashboard")]
    [HttpGet("")]
    [ProducesResponseType(typeof(ApiResponse<AdminLlmopsDashboardResponseData>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetDashboard(
        [FromQuery] AdminLlmopsFilterRequest filter,
        CancellationToken ct)
    {
        EnsureAdmin();
        var data = await _llmopsService.GetDashboardAsync(filter, ct);
        return OkResponse(data, "OK");
    }

    /// <summary>
    /// FN-ADM-16: Tra cứu chi tiết danh sách nhật ký gọi AI phân trang kèm bộ lọc tính năng và trạng thái.
    /// </summary>
    [HttpGet("logs")]
    [ProducesResponseType(typeof(ApiResponse<GetAdminLlmopsLogsResponseData>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetLogs(
        [FromQuery] AdminLlmopsLogFilterRequest filter,
        CancellationToken ct)
    {
        EnsureAdmin();
        var data = await _llmopsService.GetLogsAsync(filter, ct);
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
            throw AppException.Unauthorized(ErrorCodes.ErrAuthInvalidToken, "JWT thiếu claim sub / user_id.");
        }

        return (int)CurrentUserId.Value;
    }
}
