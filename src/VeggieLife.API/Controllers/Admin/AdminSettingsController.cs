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
using VeggieLife.BLL.DTOs.Admin.Settings;
using VeggieLife.BLL.Services.Settings;

namespace VeggieLife.API.Controllers.Admin;

/// <summary>
/// API Controller quản trị cấu hình hệ thống động (SystemSettings) dành cho Quản trị viên (Admin Dashboard).
/// Cho phép điều chỉnh linh hoạt các hạn mức đổi món, hạn mức AI, ngân sách LLMOps theo chuẩn Enterprise.
/// </summary>
[Route("api/v1/admin/settings")]
[Authorize(Policy = PolicyNames.RequireAdmin)]
public class AdminSettingsController : BaseApiController
{
    private readonly ISystemSettingService _settingService;

    public AdminSettingsController(ISystemSettingService settingService)
    {
        _settingService = settingService;
    }

    /// <summary>
    /// Lấy toàn bộ danh sách tham số cấu hình hệ thống (có thể lọc theo ?category=MEAL_PLAN, AI_QUOTA, LLMOPS, COMMUNITY).
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<SystemSettingDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> ListSettings(
        [FromQuery] string? category,
        CancellationToken ct)
    {
        EnsureAdmin();
        var result = await _settingService.GetAllSettingsAsync(category, ct);
        return OkResponse(result, "OK");
    }

    /// <summary>
    /// Lấy thông tin chi tiết một tham số cấu hình theo khóa SettingKey.
    /// </summary>
    [HttpGet("{key}")]
    [ProducesResponseType(typeof(ApiResponse<SystemSettingDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetSetting(
        [FromRoute] string key,
        CancellationToken ct)
    {
        EnsureAdmin();
        var result = await _settingService.GetSettingByKeyAsync(key, ct);
        return OkResponse(result, "OK");
    }

    /// <summary>
    /// Cập nhật giá trị một tham số cấu hình từ Admin Dashboard (ghi nhận AuditLog và xóa/cập nhật Redis Cache).
    /// </summary>
    [HttpPut("{key}")]
    [ProducesResponseType(typeof(ApiResponse<SystemSettingDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateSetting(
        [FromRoute] string key,
        [FromBody] UpdateSystemSettingRequest request,
        CancellationToken ct)
    {
        var adminId = EnsureAdmin();
        var result = await _settingService.UpdateSettingAsync(key, request, adminId, ct);
        return OkResponse(result, "Đã cập nhật cấu hình hệ thống.");
    }

    /// <summary>
    /// Khôi phục một tham số cấu hình về giá trị mặc định ban đầu (DefaultValue).
    /// </summary>
    [HttpPost("{key}/reset")]
    [ProducesResponseType(typeof(ApiResponse<SystemSettingDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ResetSetting(
        [FromRoute] string key,
        CancellationToken ct)
    {
        var adminId = EnsureAdmin();
        var result = await _settingService.ResetToDefaultAsync(key, adminId, ct);
        return OkResponse(result, "Đã khôi phục cấu hình về giá trị mặc định.");
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
