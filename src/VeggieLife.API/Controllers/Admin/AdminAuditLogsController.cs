using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using VeggieLife.BLL.Common.Constants;
using VeggieLife.BLL.Common.Exceptions;
using VeggieLife.BLL.Common.Responses;
using VeggieLife.BLL.DTOs.Admin.AuditLogs;
using VeggieLife.BLL.Services.Admin;

namespace VeggieLife.API.Controllers.Admin;

/// <summary>
/// API Controller tra cứu nhật ký kiểm toán hệ thống dành riêng cho Quản trị viên (FN-ADM-17).
/// Tuân thủ BR-ADM-SYS-002: Bảng chỉ ghi (Append-only); cấm tuyệt đối cung cấp API chỉnh sửa (PUT) hoặc xóa (DELETE).
/// Dữ liệu nhạy cảm được che giấu tự động trước khi phản hồi.
/// </summary>
[Route("api/v1/admin/audit-logs")]
[Authorize(Policy = PolicyNames.RequireAdmin)]
public class AdminAuditLogsController : BaseApiController
{
    private readonly IAdminAuditLogService _auditLogService;

    public AdminAuditLogsController(IAdminAuditLogService auditLogService)
    {
        _auditLogService = auditLogService;
    }

    /// <summary>
    /// FN-ADM-17: Tra cứu nhật ký kiểm toán theo tác nhân, hành động, bảng đối tượng và khoảng thời gian.
    /// </summary>
    [HttpGet]
    [EndpointSummary("Tra cứu nhật ký kiểm toán")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<AdminAuditLogListItemDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> ListAuditLogs(
        [FromQuery] AdminAuditLogFilterRequest filter,
        CancellationToken ct)
    {
        EnsureAdmin();
        var (logs, meta) = await _auditLogService.GetAuditLogsAsync(filter, ct);
        return PagedResponse(logs, meta, "OK");
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
