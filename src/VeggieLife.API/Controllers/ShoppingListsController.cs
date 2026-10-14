using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using VeggieLife.BLL.Common.Constants;
using VeggieLife.BLL.Common.Exceptions;
using VeggieLife.BLL.Common.Responses;
using VeggieLife.BLL.DTOs.ShoppingLists.Requests;
using VeggieLife.BLL.DTOs.ShoppingLists.Responses;
using VeggieLife.BLL.Services.ShoppingLists;

namespace VeggieLife.API.Controllers;

/// <summary>
/// Controller quản lý danh sách đi chợ thông minh (FN-MEAL-06).
/// </summary>
[ApiController]
[Route("api/v1/shopping-lists")]
public class ShoppingListsController : BaseApiController
{
    private readonly IShoppingListService _shoppingListService;

    public ShoppingListsController(IShoppingListService shoppingListService)
    {
        _shoppingListService = shoppingListService;
    }

    /// <summary>
    /// FN-MEAL-06: Xem danh sách đi chợ theo quầy kèm tiến độ mua sắm.
    /// </summary>
    [HttpGet("{id:int}")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<GetShoppingListResponseData>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetShoppingList(
        [FromRoute] int id,
        CancellationToken ct = default)
    {
        var (userId, _) = EnsureAuthorizedUser();
        var result = await _shoppingListService.GetShoppingListByIdAsync(id, userId, ct);
        return OkResponse(result, "OK");
    }

    /// <summary>
    /// FN-MEAL-06: Đánh dấu một món hàng trong danh sách đi chợ đã mua hoặc chưa mua.
    /// </summary>
    [HttpPatch("{id:int}/items/{itemId:int}")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<PatchShoppingListItemResponseData>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> PatchShoppingListItem(
        [FromRoute] int id,
        [FromRoute] int itemId,
        [FromBody] PatchShoppingListItemRequest? request,
        CancellationToken ct = default)
    {
        if (request == null)
        {
            throw AppException.BadRequest(
                ErrorCodes.ErrValidation,
                "Dữ liệu đầu vào không hợp lệ.");
        }

        var (userId, _) = EnsureAuthorizedUser();
        var result = await _shoppingListService.PatchShoppingListItemAsync(id, itemId, userId, request, ct);
        return OkResponse(result, "OK");
    }

    /// <summary>
    /// FN-MEAL-06: Cập nhật trạng thái danh sách đi chợ (COMPLETED hoặc CANCELLED).
    /// </summary>
    [HttpPatch("{id:int}")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<PatchShoppingListResponseData>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> PatchShoppingList(
        [FromRoute] int id,
        [FromBody] PatchShoppingListRequest? request,
        CancellationToken ct = default)
    {
        if (request == null)
        {
            throw AppException.BadRequest(
                ErrorCodes.ErrValidation,
                "Dữ liệu đầu vào không hợp lệ.");
        }

        var (userId, _) = EnsureAuthorizedUser();
        var result = await _shoppingListService.PatchShoppingListAsync(id, userId, request, ct);
        return OkResponse(result, "OK");
    }

    private (int UserId, string UserRole) EnsureAuthorizedUser()
    {
        if (CurrentUserId == null)
        {
            throw AppException.Unauthorized(
                ErrorCodes.ErrAuthInvalidToken,
                "Thiếu, sai chữ ký hoặc hết hạn JWT.");
        }

        var userRole = CurrentUserRole ?? UserRoles.Member;
        if (string.Equals(userRole, UserRoles.Guest, System.StringComparison.OrdinalIgnoreCase))
        {
            throw AppException.Forbidden(
                ErrorCodes.ErrGuestPersonalizationRestricted,
                "Khách vãng lai gọi tính năng cá nhân hóa (FN-AUTH-03).");
        }

        return ((int)CurrentUserId.Value, userRole);
    }
}
