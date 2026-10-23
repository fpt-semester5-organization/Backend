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
using VeggieLife.BLL.DTOs.Admin.DietarySchools;
using VeggieLife.BLL.Services.Admin;

namespace VeggieLife.API.Controllers.Admin;

/// <summary>
/// API Controller quản lý 07 trường phái ăn chay dành cho Quản trị viên (FN-ADM-10, BR-ADM-MAS-001, D109).
/// Hệ thống cố định 07 trường phái ăn chay, không cho phép thêm mới hoặc xóa bỏ.
/// </summary>
[Route("api/v1/admin/dietary-schools")]
[Authorize(Policy = PolicyNames.RequireAdmin)]
public class AdminDietarySchoolsController : BaseApiController
{
    private readonly IAdminDietarySchoolService _dietarySchoolService;

    public AdminDietarySchoolsController(IAdminDietarySchoolService dietarySchoolService)
    {
        _dietarySchoolService = dietarySchoolService;
    }

    /// <summary>
    /// FN-ADM-10: Xem danh sách 07 trường phái ăn chay cố định kèm quy tắc thực phẩm cấm.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<AdminDietarySchoolItemDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> ListDietarySchools(CancellationToken ct)
    {
        EnsureAdmin();
        var result = await _dietarySchoolService.GetDietarySchoolsAsync(ct);
        return OkResponse(result, "OK");
    }

    /// <summary>
    /// FN-ADM-10: Cập nhật tên hiển thị và quy tắc thực phẩm cấm của trường phái (không đổi SchoolCode, không thêm/xóa).
    /// </summary>
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(ApiResponse<UpdateAdminDietarySchoolResponseData>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateDietarySchool(
        [FromRoute] int id,
        [FromBody] UpdateAdminDietarySchoolRequest request,
        CancellationToken ct)
    {
        var adminId = EnsureAdmin();
        var result = await _dietarySchoolService.UpdateDietarySchoolAsync(id, request, adminId, ct);
        return OkResponse(result, "Đã cập nhật.");
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
