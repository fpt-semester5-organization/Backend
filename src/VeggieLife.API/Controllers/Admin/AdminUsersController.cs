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
using VeggieLife.BLL.DTOs.Admin.Users;
using VeggieLife.BLL.Services.Admin;

namespace VeggieLife.API.Controllers.Admin;

/// <summary>
/// API Controller quản lý tài khoản người dùng và xử lý kỷ luật dành cho Quản trị viên (FN-ADM-01, FN-ADM-02, FN-ADM-03).
/// Tuân thủ: BR-ADM-USR-001 (Bảo toàn Admin cuối cùng), BR-ADM-USR-002 (Thang kỷ luật lũy tiến), BR-ADM-SYS-002 (AuditLogs), BR-AUTH-004 (Thu hồi phiên).
/// </summary>
[Route("api/v1/admin/users")]
[Authorize(Policy = PolicyNames.RequireAdmin)]
public class AdminUsersController : BaseApiController
{
    private readonly IAdminUserService _adminUserService;

    public AdminUsersController(IAdminUserService adminUserService)
    {
        _adminUserService = adminUserService;
    }

    /// <summary>
    /// FN-ADM-01: Tra cứu, tìm kiếm, lọc và phân trang danh sách người dùng trong hệ thống.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<AdminUserListItemDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> ListUsers(
        [FromQuery] AdminUserListFilterRequest filter,
        CancellationToken ct)
    {
        EnsureAdmin();
        var (users, meta) = await _adminUserService.GetUsersAsync(filter, ct);
        return PagedResponse(users, meta, "OK");
    }

    /// <summary>
    /// FN-ADM-02: Cập nhật trạng thái và áp dụng chế tài kỷ luật tài khoản người dùng theo thang lũy tiến.
    /// </summary>
    [HttpPut("{id:int}/status")]
    [ProducesResponseType(typeof(ApiResponse<UpdateAdminUserStatusResponseData>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateUserStatus(
        [FromRoute] int id,
        [FromBody] UpdateAdminUserStatusRequest request,
        CancellationToken ct)
    {
        var adminId = EnsureAdmin();
        var result = await _adminUserService.UpdateUserStatusAsync(id, request, adminId, ct);
        return OkResponse(result, "Đã cập nhật trạng thái.");
    }

    /// <summary>
    /// FN-ADM-03: Xem tổng hợp các nội dung đã tạo và lịch sử báo cáo vi phạm của người dùng.
    /// </summary>
    [HttpGet("{id:int}/contents")]
    [ProducesResponseType(typeof(ApiResponse<AdminUserContentsResponseData>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetUserContents(
        [FromRoute] int id,
        [FromQuery] AdminUserContentsFilterRequest filter,
        CancellationToken ct)
    {
        EnsureAdmin();
        var (data, meta) = await _adminUserService.GetUserContentsAsync(id, filter, ct);
        return PagedResponse(data, meta, "OK");
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
