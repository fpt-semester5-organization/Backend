using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using VeggieLife.BLL.Common.Constants;
using VeggieLife.BLL.Common.Exceptions;
using VeggieLife.BLL.Common.Responses;
using VeggieLife.BLL.DTOs.Admin.Recipes;
using VeggieLife.BLL.Services.Admin;

namespace VeggieLife.API.Controllers.Admin;

/// <summary>
/// API Controller quản lý công thức món ăn dành cho Quản trị viên (FN-ADM-09: Xác minh công thức chuẩn chay).
/// </summary>
[Route("api/v1/admin/recipes")]
[Authorize(Policy = PolicyNames.RequireAdmin)]
public class AdminRecipesController : BaseApiController
{
    private readonly IAdminRecipeService _recipeService;

    public AdminRecipesController(IAdminRecipeService recipeService)
    {
        _recipeService = recipeService;
    }

    /// <summary>
    /// FN-ADM-09: Gắn huy hiệu "Công thức chay chuẩn" khi đủ 4 tiêu chí của BR-ADM-MAS-003.
    /// </summary>
    [HttpPost("{id:int}/verify")]
    [ProducesResponseType(typeof(ApiResponse<VerifyAdminRecipeResponseData>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> VerifyRecipe(
        [FromRoute] int id,
        [FromBody] VerifyAdminRecipeRequest? request,
        CancellationToken ct)
    {
        var adminId = EnsureAdmin();
        var result = await _recipeService.VerifyRecipeAsync(id, request, adminId, ct);
        return OkResponse(result, "Đã cấp huy hiệu.");
    }

    /// <summary>
    /// FN-ADM-09: Gỡ huy hiệu "Công thức chay chuẩn" khỏi công thức.
    /// </summary>
    [HttpDelete("{id:int}/verify")]
    [ProducesResponseType(typeof(ApiResponse<UnverifyAdminRecipeResponseData>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UnverifyRecipe(
        [FromRoute] int id,
        CancellationToken ct)
    {
        var adminId = EnsureAdmin();
        var result = await _recipeService.UnverifyRecipeAsync(id, adminId, ct);
        return OkResponse(result, "Đã gỡ huy hiệu.");
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
