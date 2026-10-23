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
using VeggieLife.BLL.DTOs.Admin.ENumbers;
using VeggieLife.BLL.Services.Admin;

namespace VeggieLife.API.Controllers.Admin;

/// <summary>
/// API Controller quản lý từ điển phụ gia thực phẩm E-number dành cho Quản trị viên (FN-ADM-11, BR-ADM-MAS-002, BR-ADM-AI-005).
/// </summary>
[Route("api/v1/admin/e-numbers")]
[Authorize(Policy = PolicyNames.RequireAdmin)]
public class AdminENumbersController : BaseApiController
{
    private readonly IAdminENumberService _eNumberService;

    public AdminENumbersController(IAdminENumberService eNumberService)
    {
        _eNumberService = eNumberService;
    }

    /// <summary>
    /// FN-ADM-11: Tra cứu danh sách từ điển mã phụ gia E-number có phân trang và bộ lọc.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<AdminENumberListItemDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> ListENumbers(
        [FromQuery] AdminENumberFilterRequest filter,
        CancellationToken ct)
    {
        EnsureAdmin();
        var (items, meta) = await _eNumberService.GetENumbersAsync(filter, ct);
        return PagedResponse(items, meta, "OK");
    }

    /// <summary>
    /// FN-ADM-11: Lấy thông tin chi tiết một mã phụ gia E-number theo mã Code.
    /// </summary>
    [HttpGet("{code}")]
    [ProducesResponseType(typeof(ApiResponse<AdminENumberListItemDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetENumber(
        [FromRoute] string code,
        CancellationToken ct)
    {
        EnsureAdmin();
        var result = await _eNumberService.GetENumberByCodeAsync(code, ct);
        return OkResponse(result, "OK");
    }

    /// <summary>
    /// FN-ADM-11: Thêm mới mã phụ gia E-number vào từ điển.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<CreateAdminENumberResponseData>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateENumber(
        [FromBody] CreateAdminENumberRequest request,
        CancellationToken ct)
    {
        var adminId = EnsureAdmin();
        var result = await _eNumberService.CreateENumberAsync(request, adminId, ct);
        return CreatedResponse(result, "Đã thêm.");
    }

    /// <summary>
    /// FN-ADM-11: Cập nhật thông tin mã phụ gia E-number. Nếu đổi trạng thái chay, tính lại kết luận sản phẩm liên quan (bỏ qua Admin_Verified = 1).
    /// </summary>
    [HttpPut("{code}")]
    [ProducesResponseType(typeof(ApiResponse<UpdateAdminENumberResponseData>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateENumber(
        [FromRoute] string code,
        [FromBody] UpdateAdminENumberRequest request,
        CancellationToken ct)
    {
        var adminId = EnsureAdmin();
        var result = await _eNumberService.UpdateENumberAsync(code, request, adminId, ct);
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
