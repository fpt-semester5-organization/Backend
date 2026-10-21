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
using VeggieLife.BLL.DTOs.Admin.Ingredients;
using VeggieLife.BLL.Services.Admin;

namespace VeggieLife.API.Controllers.Admin;

/// <summary>
/// API Controller quản lý nguyên liệu thực phẩm và nhóm dị ứng dành cho Quản trị viên (FN-ADM-12).
/// </summary>
[Route("api/v1/admin/ingredients")]
[Authorize(Policy = PolicyNames.RequireAdmin)]
public class AdminIngredientsController : BaseApiController
{
    private readonly IAdminIngredientService _ingredientService;

    public AdminIngredientsController(IAdminIngredientService ingredientService)
    {
        _ingredientService = ingredientService;
    }

    /// <summary>
    /// FN-ADM-12: Lấy danh sách nguyên liệu theo bộ lọc và phân trang dành cho Quản trị viên.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<AdminIngredientListItemDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> ListIngredients(
        [FromQuery] AdminIngredientFilterRequest filter,
        CancellationToken ct)
    {
        EnsureAdmin();
        var (items, meta) = await _ingredientService.GetIngredientsAsync(filter, ct);
        return PagedResponse(items, meta, "OK");
    }

    /// <summary>
    /// FN-ADM-12: Thêm mới nguyên liệu thực phẩm (calo/100 g, cờ ngũ vị tân, quầy mặc định).
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<CreateAdminIngredientResponseData>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateIngredient(
        [FromBody] CreateAdminIngredientRequest request,
        CancellationToken ct)
    {
        var adminId = EnsureAdmin();
        var result = await _ingredientService.CreateIngredientAsync(request, adminId, ct);
        return CreatedResponse(result, "Đã thêm.");
    }

    /// <summary>
    /// FN-ADM-12: Cập nhật thông tin nguyên liệu thực phẩm.
    /// </summary>
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(ApiResponse<UpdateAdminIngredientResponseData>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateIngredient(
        [FromRoute] int id,
        [FromBody] UpdateAdminIngredientRequest request,
        CancellationToken ct)
    {
        var adminId = EnsureAdmin();
        var result = await _ingredientService.UpdateIngredientAsync(id, request, adminId, ct);
        return OkResponse(result, "Đã cập nhật.");
    }

    /// <summary>
    /// FN-ADM-12: Gán/thay thế toàn bộ nhóm dị ứng cho nguyên liệu (IngredientAllergens).
    /// </summary>
    [HttpPut("{id:int}/allergens")]
    [ProducesResponseType(typeof(ApiResponse<ReplaceAdminIngredientAllergensResponseData>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ReplaceIngredientAllergens(
        [FromRoute] int id,
        [FromBody] ReplaceAdminIngredientAllergensRequest request,
        CancellationToken ct)
    {
        var adminId = EnsureAdmin();
        var result = await _ingredientService.ReplaceIngredientAllergensAsync(id, request, adminId, ct);
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
