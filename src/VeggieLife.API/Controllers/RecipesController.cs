using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using VeggieLife.BLL.Common.Responses;
using VeggieLife.BLL.DTOs.Recipes.Requests;
using VeggieLife.BLL.DTOs.Recipes.Responses;
using VeggieLife.BLL.Services.Recipes;

namespace VeggieLife.API.Controllers;

/// <summary>
/// Controller cung cấp các endpoint tìm kiếm, lọc và xem chi tiết công thức, video nấu ăn công khai (FN-POST-01, FN-POST-02).
/// Hỗ trợ cả Khách vãng lai (Guest) và Thành viên đã đăng nhập (Member, VIP, Admin).
/// </summary>
[ApiController]
[Route("api/v1/recipes")]
public class RecipesController : BaseApiController
{
    private readonly IRecipeService _recipeService;

    public RecipesController(IRecipeService recipeService)
    {
        _recipeService = recipeService;
    }

    /// <summary>
    /// FN-POST-01: Tìm kiếm & lọc công thức, video công khai.
    /// Tra cứu công thức/video đã được duyệt (APPROVED_ACTIVE) theo từ khóa, danh mục, trường phái, nguyên liệu, thời gian nấu; có phân trang.
    /// Dùng chung cho Khách và Thành viên (D18). Không trừ hạn mức.
    /// </summary>
    /// <param name="filter">Bộ lọc tìm kiếm và phân trang.</param>
    /// <param name="ct">CancellationToken.</param>
    /// <returns>Danh sách tóm tắt công thức nấu ăn kèm metadata phân trang.</returns>
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<RecipeSummaryDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> ListRecipes([FromQuery] RecipeFilterRequest filter, CancellationToken ct)
    {
        var isGuest = IsGuestUser();
        var guestId = ResolveGuestId();

        var (items, meta) = await _recipeService.ListRecipesAsync(filter, isGuest, guestId, ct);
        return PagedResponse(items, meta, "OK");
    }

    /// <summary>
    /// FN-POST-02: Xem chi tiết công thức.
    /// Trả chi tiết công thức đã duyệt: nguyên liệu có định lượng, các bước nấu, dinh dưỡng, video liên quan; hiển thị đầy đủ cho Khách (không làm mờ). Tăng View_Count.
    /// </summary>
    /// <param name="id">Mã định danh công thức (> 0).</param>
    /// <param name="ct">CancellationToken.</param>
    /// <returns>Chi tiết công thức nấu ăn.</returns>
    [HttpGet("{id:int}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<RecipeDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetRecipe([FromRoute] int id, CancellationToken ct)
    {
        var isGuest = IsGuestUser();
        var currentUserId = CurrentUserId.HasValue ? (int?)CurrentUserId.Value : null;

        var recipe = await _recipeService.GetRecipeByIdAsync(id, isGuest, currentUserId, ct);
        return OkResponse(recipe, "OK");
    }

    /// <summary>
    /// Xác định xem người dùng hiện tại là khách vãng lai hay thành viên đã đăng nhập.
    /// </summary>
    private bool IsGuestUser()
    {
        if (User?.Identity == null || !User.Identity.IsAuthenticated)
        {
            return true;
        }

        var role = CurrentUserRole;
        return string.IsNullOrEmpty(role) || string.Equals(role, "GUEST", System.StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Phân giải mã định danh của khách vãng lai từ Claim, Header hoặc Cookie.
    /// </summary>
    private string? ResolveGuestId()
    {
        // 1. Từ JWT claim Guest_UUID
        if (!string.IsNullOrEmpty(CurrentGuestUuid))
        {
            return CurrentGuestUuid;
        }

        // 2. Từ HTTP Header: X-Guest-UUID
        if (Request.Headers.TryGetValue("X-Guest-UUID", out var headerValue) && !string.IsNullOrEmpty(headerValue.FirstOrDefault()))
        {
            return headerValue.FirstOrDefault();
        }

        // 3. Từ HTTP Cookie: guest_uuid
        if (Request.Cookies.TryGetValue("guest_uuid", out var cookieValue) && !string.IsNullOrEmpty(cookieValue))
        {
            return cookieValue;
        }

        return null;
    }
}
