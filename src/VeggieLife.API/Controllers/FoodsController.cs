using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using VeggieLife.BLL.Common.Constants;
using VeggieLife.BLL.Common.Exceptions;
using VeggieLife.BLL.Common.Responses;
using VeggieLife.BLL.DTOs.Foods.Requests;
using VeggieLife.BLL.DTOs.Foods.Responses;
using VeggieLife.BLL.DTOs.Recipes.Requests;
using VeggieLife.BLL.DTOs.Recipes.Responses;
using VeggieLife.BLL.Services.Foods;
using VeggieLife.BLL.Services.Recipes;

namespace VeggieLife.API.Controllers;

/// <summary>
/// Controller quản lý và tra cứu món ăn khái niệm (Food entity).
/// Một Food có thể có nhiều công thức nấu (Recipes) theo các mục tiêu khác nhau (tăng cân, giảm cân, duy trì).
/// </summary>
[ApiController]
[Route("api/v1/foods")]
public class FoodsController : BaseApiController
{
    private readonly IFoodService _foodService;
    private readonly IRecipeService _recipeService;

    public FoodsController(IFoodService foodService, IRecipeService recipeService)
    {
        _foodService = foodService;
        _recipeService = recipeService;
    }

    /// <summary>
    /// Lấy danh sách tất cả các món ăn khái niệm trong hệ thống (KHÔNG bắt buộc truyền ID, có phân trang và lọc theo tên).
    /// </summary>
    /// <param name="q">Từ khóa tìm kiếm tên món ăn (tùy chọn).</param>
    /// <param name="page">Số trang (mặc định: 1).</param>
    /// <param name="size">Số lượng bản ghi mỗi trang (mặc định: 20, tối đa: 100).</param>
    /// <param name="ct">CancellationToken.</param>
    /// <returns>Danh sách món ăn kèm metadata phân trang.</returns>
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<FoodSummaryDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListFoods(
        [FromQuery] string? q = null,
        [FromQuery] int page = 1,
        [FromQuery] int size = 20,
        CancellationToken ct = default)
    {
        var (items, meta) = await _foodService.ListFoodsAsync(q, page, size, ct);
        return PagedResponse(items, meta, "OK");
    }

    /// <summary>
    /// Tìm kiếm món ăn theo từ khóa, hỗ trợ gợi ý autocomplete / combobox khi người dùng tạo công thức.
    /// Trả về tối đa 10 kết quả phù hợp nhất.
    /// </summary>
    /// <param name="q">Từ khóa tìm kiếm tên món ăn.</param>
    /// <param name="ct">CancellationToken.</param>
    /// <returns>Danh sách món ăn gợi ý.</returns>
    [HttpGet("search")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<FoodSummaryDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> SearchFoods([FromQuery] string? q, CancellationToken ct)
    {
        var items = await _foodService.SearchFoodsAsync(q ?? string.Empty, ct);
        return OkResponse(items, "OK");
    }

    /// <summary>
    /// Xem thông tin chi tiết một món ăn khái niệm theo mã ID.
    /// </summary>
    /// <param name="id">Mã định danh món ăn (Food_ID > 0).</param>
    /// <param name="ct">CancellationToken.</param>
    /// <returns>Thông tin chi tiết món ăn.</returns>
    [HttpGet("{id:int}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<FoodSummaryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetFoodById([FromRoute] int id, CancellationToken ct)
    {
        if (id <= 0)
        {
            throw AppException.BadRequest(ErrorCodes.ErrValidation, "Mã món ăn phải lớn hơn 0.");
        }

        var food = await _foodService.GetFoodByIdAsync(id, ct);
        if (food == null)
        {
            throw AppException.NotFound(ErrorCodes.ErrContentNotFound, $"Không tìm thấy món ăn với mã {id}.");
        }

        return OkResponse(food, "OK");
    }

    /// <summary>
    /// Xem danh sách tất cả công thức nấu ăn thuộc về một món ăn cụ thể (VD: các công thức khác nhau của món 'Bún Huế').
    /// BẮT BUỘC truyền {id} của món ăn để hệ thống biết đang xem công thức của món nào.
    /// Nếu muốn xem toàn bộ công thức mọi món (không cần id), hãy dùng GET /api/v1/recipes.
    /// </summary>
    /// <param name="id">Mã định danh món ăn (Food_ID).</param>
    /// <param name="filter">Bộ lọc và phân trang bổ sung.</param>
    /// <param name="ct">CancellationToken.</param>
    /// <returns>Danh sách công thức của món ăn kèm phân trang.</returns>
    [HttpGet("{id:int}/recipes")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<RecipeSummaryDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ListRecipesByFood([FromRoute] int id, [FromQuery] RecipeFilterRequest filter, CancellationToken ct)
    {
        if (id <= 0)
        {
            throw AppException.BadRequest(ErrorCodes.ErrValidation, "Mã món ăn phải lớn hơn 0.");
        }

        var food = await _foodService.GetFoodByIdAsync(id, ct);
        if (food == null)
        {
            throw AppException.NotFound(ErrorCodes.ErrContentNotFound, $"Không tìm thấy món ăn với mã {id}.");
        }

        filter.FoodId = id;
        var isGuest = IsGuestUser();
        var guestId = ResolveGuestId();

        var (items, meta) = await _recipeService.ListRecipesAsync(filter, isGuest, guestId, ct);
        return PagedResponse(items, meta, "OK");
    }

    /// <summary>
    /// Tạo mới một món ăn khái niệm (nếu chưa có trong hệ thống).
    /// Thường được gọi tự động khi đăng công thức, hoặc gọi trực tiếp từ giao diện.
    /// </summary>
    /// <param name="request">Thông tin món ăn cần tạo (tên và danh mục).</param>
    /// <param name="ct">CancellationToken.</param>
    /// <returns>Món ăn đã tạo hoặc đã có sẵn.</returns>
    [HttpPost]
    [Authorize(Policy = PolicyNames.RequireMember)]
    [ProducesResponseType(typeof(ApiResponse<FoodSummaryDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> CreateOrGetFood([FromBody] CreateFoodRequest request, CancellationToken ct)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.FoodName))
        {
            throw AppException.BadRequest(ErrorCodes.ErrValidation, "Tên món ăn không được để trống.");
        }

        if (request.CategoryId <= 0)
        {
            throw AppException.BadRequest(ErrorCodes.ErrValidation, "Mã danh mục không hợp lệ.");
        }

        var food = await _foodService.GetOrCreateFoodAsync(request.FoodName, request.CategoryId, request.ImageUrl, ct);

        var dto = new FoodSummaryDto
        {
            FoodId = food.FoodId,
            FoodName = food.FoodName,
            CategoryId = food.CategoryId,
            ImageUrl = food.ImageUrl
        };

        return CreatedResponse(dto, "Xử lý thông tin món ăn thành công.");
    }

    private bool IsGuestUser()
    {
        if (User?.Identity == null || !User.Identity.IsAuthenticated)
        {
            return true;
        }

        var role = CurrentUserRole;
        return string.IsNullOrEmpty(role) || string.Equals(role, "GUEST", System.StringComparison.OrdinalIgnoreCase);
    }

    private string? ResolveGuestId()
    {
        if (User == null)
        {
            return null;
        }

        try
        {
            return CurrentGuestUuid;
        }
        catch
        {
            return null;
        }
    }
}
