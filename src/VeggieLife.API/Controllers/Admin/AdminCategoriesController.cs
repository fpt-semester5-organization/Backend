using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using VeggieLife.BLL.Common.Constants;
using VeggieLife.BLL.Common.Exceptions;
using VeggieLife.BLL.Common.Responses;
using VeggieLife.BLL.DTOs.Admin.Categories;
using VeggieLife.BLL.Services.Admin;

namespace VeggieLife.API.Controllers.Admin;

/// <summary>
/// API Controller quản lý danh mục món ăn dành cho Quản trị viên (FN-ADM-07, FN-ADM-08).
/// </summary>
[Route("api/v1/admin/categories")]
[Authorize(Policy = PolicyNames.RequireAdmin)]
public class AdminCategoriesController : BaseApiController
{
    private readonly IAdminCategoryService _categoryService;

    public AdminCategoriesController(IAdminCategoryService categoryService)
    {
        _categoryService = categoryService;
    }

    /// <summary>
    /// FN-ADM-07: Lấy toàn bộ danh mục món ăn dành cho Quản trị viên (bao gồm cả danh mục ẩn, danh mục cốt lõi).
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<System.Collections.Generic.IReadOnlyList<AdminCategoryItemResponseData>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetAllCategories(CancellationToken ct)
    {
        EnsureAdmin();
        var result = await _categoryService.GetAllCategoriesAsync(ct);
        return OkResponse(result, "Danh sách danh mục món ăn.");
    }

    /// <summary>
    /// FN-ADM-07: Tạo mới danh mục món ăn (CategoryName duy nhất, 2-100 ký tự).
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<CreateAdminCategoryResponseData>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateCategory(
        [FromBody] CreateAdminCategoryRequest request,
        CancellationToken ct)
    {
        var adminId = EnsureAdmin();
        var result = await _categoryService.CreateCategoryAsync(request, adminId, ct);
        return CreatedResponse(result, "Đã tạo danh mục.");
    }

    /// <summary>
    /// FN-ADM-07: Cập nhật danh mục món ăn (CategoryName, IsActive, Description). IsCore không đổi được.
    /// </summary>
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(ApiResponse<UpdateAdminCategoryResponseData>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateCategory(
        [FromRoute] int id,
        [FromBody] UpdateAdminCategoryRequest request,
        CancellationToken ct)
    {
        var adminId = EnsureAdmin();
        var result = await _categoryService.UpdateCategoryAsync(id, request, adminId, ct);
        return OkResponse(result, "Đã cập nhật.");
    }

    /// <summary>
    /// FN-ADM-08: Xóa mềm danh mục kèm chuyển dữ liệu. Danh mục cốt lõi không được xóa.
    /// </summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(typeof(ApiResponse<DeleteAdminCategoryResponseData>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> DeleteCategory(
        [FromRoute] int id,
        [FromQuery] int? targetCategoryId,
        CancellationToken ct)
    {
        var adminId = EnsureAdmin();
        var result = await _categoryService.DeleteCategoryAsync(id, targetCategoryId, adminId, ct);
        return OkResponse(result, "Đã xóa danh mục.");
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
