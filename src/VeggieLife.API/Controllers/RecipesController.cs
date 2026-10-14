using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using VeggieLife.BLL.Common.Constants;
using VeggieLife.BLL.Common.Exceptions;
using VeggieLife.BLL.Common.Responses;
using VeggieLife.BLL.DTOs.Recipes.Requests;
using VeggieLife.BLL.DTOs.Recipes.Responses;
using VeggieLife.BLL.Services.Recipes;

namespace VeggieLife.API.Controllers;

/// <summary>
/// Controller cung cấp các endpoint tìm kiếm, lọc, xem chi tiết, đăng, sửa, gỡ và xóa công thức nấu ăn (FN-POST-01, FN-POST-02, FN-POST-03, FN-POST-04).
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
    /// FN-POST-11: Gợi ý nội dung liên quan khi xem chi tiết công thức.
    /// Gợi ý công thức tương đồng (cùng trường phái, chung nguyên liệu chính, nhiều lượt thả tim) và bài viết/video liên quan.
    /// Dùng chung cho Khách vãng lai và Thành viên (GUEST | MEMBER | VIP).
    /// </summary>
    /// <param name="id">Mã định danh công thức (> 0).</param>
    /// <param name="limit">Số lượng gợi ý (1–10, mặc định 6).</param>
    /// <param name="ct">CancellationToken.</param>
    /// <returns>Danh sách công thức, bài viết và video liên quan.</returns>
    [HttpGet("{id:int}/related")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<RelatedRecipesResponseData>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> ListRelatedRecipes(
        [FromRoute] int id,
        [FromQuery] int limit = 6,
        CancellationToken ct = default)
    {
        if (id <= 0)
        {
            throw AppException.BadRequest(ErrorCodes.ErrValidation, "Mã công thức phải lớn hơn 0.");
        }

        if (limit < 1 || limit > 10)
        {
            throw AppException.BadRequest(ErrorCodes.ErrValidation, "Giới hạn số lượng gợi ý phải từ 1 đến 10.");
        }

        var currentUserId = CurrentUserId.HasValue ? (int?)CurrentUserId.Value : null;
        var relatedData = await _recipeService.GetRelatedRecipesAsync(id, limit, currentUserId, ct);
        return OkResponse(relatedData, "OK");
    }


    /// <summary>
    /// FN-POST-03: Đăng công thức nấu ăn mới (lưu nháp hoặc gửi kiểm duyệt tự động bằng AI).
    /// Hỗ trợ kiểm duyệt Red Alert thành phần cấm theo trường phái và quét nội dung món chay qua AI Service.
    /// </summary>
    /// <param name="request">Payload thông tin công thức, định lượng nguyên liệu và các bước nấu.</param>
    /// <param name="ct">CancellationToken.</param>
    /// <returns>Mã công thức vừa tạo và trạng thái duyệt ban đầu.</returns>
    [HttpPost]
    [Authorize(Policy = PolicyNames.RequireMember)]
    [ProducesResponseType(typeof(ApiResponse<CreateRecipeResponseDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> CreateRecipe([FromBody] CreateRecipeRequest request, CancellationToken ct)
    {
        EnsureAuthorizedMember();

        var userId = (int)CurrentUserId!.Value;
        var result = await _recipeService.CreateRecipeAsync(userId, request, ct);
        return CreatedResponse(result, "Tạo công thức thành công.");
    }

    /// <summary>
    /// FN-POST-04: Cập nhật công thức nấu ăn của mình.
    /// Đối với công thức đã duyệt (APPROVED_ACTIVE), nếu thay đổi nội dung sẽ được kiểm duyệt AI đồng bộ trước khi ghi (D107).
    /// Nếu không qua kiểm duyệt, bản đang đăng được giữ nguyên và trả mã lỗi 422.
    /// </summary>
    /// <param name="id">Mã định danh công thức (> 0).</param>
    /// <param name="request">Payload thông tin cập nhật.</param>
    /// <param name="ct">CancellationToken.</param>
    /// <returns>Mã công thức và trạng thái sau khi cập nhật.</returns>
    [HttpPut("{id:int}")]
    [Authorize(Policy = PolicyNames.RequireMember)]
    [ProducesResponseType(typeof(ApiResponse<UpdateRecipeResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> UpdateRecipe([FromRoute] int id, [FromBody] UpdateRecipeRequest request, CancellationToken ct)
    {
        EnsureAuthorizedMember();

        var userId = (int)CurrentUserId!.Value;
        var role = CurrentUserRole ?? "MEMBER";

        var result = await _recipeService.UpdateRecipeAsync(id, userId, role, request, ct);
        return OkResponse(result, "Cập nhật công thức thành công.");
    }

    /// <summary>
    /// FN-POST-04: Gỡ công thức khỏi trang cá nhân (mode=archive) hoặc xóa mềm toàn hệ thống (mode=all).
    /// </summary>
    /// <param name="id">Mã định danh công thức (> 0).</param>
    /// <param name="mode">Chế độ gỡ/xóa: 'archive' (mặc định) hoặc 'all'.</param>
    /// <param name="ct">CancellationToken.</param>
    /// <returns>Phản hồi mã công thức và trạng thái sau khi gỡ/xóa.</returns>
    [HttpDelete("{id:int}")]
    [Authorize(Policy = PolicyNames.RequireMember)]
    [ProducesResponseType(typeof(ApiResponse<DeleteRecipeResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteRecipe([FromRoute] int id, [FromQuery] string mode = "archive", CancellationToken ct = default)
    {
        EnsureAuthorizedMember();

        var userId = (int)CurrentUserId!.Value;
        var role = CurrentUserRole ?? "MEMBER";

        var result = await _recipeService.DeleteRecipeAsync(id, userId, role, mode, ct);
        return OkResponse(result, "Đã xử lý xóa/gỡ công thức.");
    }

    /// <summary>
    /// Kiểm tra và xác thực quyền thành viên chính thức cho các tác vụ ghi dữ liệu công thức (FN-AUTH-03).
    /// </summary>
    private void EnsureAuthorizedMember()
    {
        if (User?.Identity == null || !User.Identity.IsAuthenticated)
        {
            throw AppException.Unauthorized(ErrorCodes.ErrAuthInvalidToken, "Thiếu hoặc sai chữ ký, hết hạn JWT.");
        }

        if (IsGuestUser())
        {
            throw AppException.Forbidden(ErrorCodes.ErrGuestCommunityActionDenied, "Khách vãng lai thực hiện thao tác ghi cộng đồng (FN-AUTH-03).");
        }

        if (!CurrentUserId.HasValue)
        {
            throw AppException.Unauthorized(ErrorCodes.ErrAuthInvalidToken, "Thiếu thông tin người dùng trong token.");
        }
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

