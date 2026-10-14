using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using VeggieLife.BLL.Common.Constants;
using VeggieLife.BLL.Common.Exceptions;
using VeggieLife.BLL.Common.Responses;
using VeggieLife.BLL.DTOs.MealPlans.Requests;
using VeggieLife.BLL.DTOs.MealPlans.Responses;
using VeggieLife.BLL.Services.MealPlans;

namespace VeggieLife.API.Controllers;

/// <summary>
/// Controller quản lý và cá nhân hóa thực đơn dinh dưỡng 7 ngày (FN-MEAL-01, FN-MEAL-02, FN-MEAL-03, FN-MEAL-04).
/// </summary>
[ApiController]
[Route("api/v1/meal-plans")]
public class MealPlansController : BaseApiController
{
    private readonly IMealPlanService _mealPlanService;

    public MealPlansController(IMealPlanService mealPlanService)
    {
        _mealPlanService = mealPlanService;
    }

    /// <summary>
    /// FN-MEAL-01: Sinh thực đơn tuần bằng AI.
    /// Sinh thực đơn 7 ngày × 3 bữa (21 bữa) từ hồ sơ sức khỏe, trường phái, dị ứng, mức ngân sách.
    /// Thực đơn ở trạng thái DRAFT để người dùng xem và tùy chỉnh.
    /// Hạn mức tuần: Free 1 thực đơn/tuần, VIP 3 thực đơn/tuần.
    /// </summary>
    [HttpPost]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<CreateMealPlanResponseData>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> CreateMealPlan(
        [FromBody] CreateMealPlanRequest request,
        CancellationToken ct)
    {
        var (userId, userRole) = EnsureAuthorizedUser();
        var result = await _mealPlanService.CreateMealPlanAsync(userId, userRole, request, ct);
        return CreatedResponse(result, "Đã tạo thực đơn nháp.");
    }

    /// <summary>
    /// FN-MEAL-02: Xem danh sách các thực đơn của người dùng.
    /// Lấy danh sách thực đơn (Plan_Status != TEMPLATE) sắp xếp mới nhất trước, có phân trang.
    /// </summary>
    [HttpGet]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<MealPlanSummaryDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> ListMealPlans(
        [FromQuery] int page = 1,
        [FromQuery] int size = 10,
        CancellationToken ct = default)
    {
        var (userId, _) = EnsureAuthorizedUser();
        var (items, meta) = await _mealPlanService.ListMealPlansAsync(userId, page, size, ct);
        return PagedResponse(items, meta, "OK");
    }

    /// <summary>
    /// FN-MEAL-02: Xem chi tiết một thực đơn 21 bữa và số lượt đổi món còn lại.
    /// </summary>
    [HttpGet("{id:int}")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<CreateMealPlanResponseData>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetMealPlan(
        [FromRoute] int id,
        CancellationToken ct = default)
    {
        var (userId, _) = EnsureAuthorizedUser();
        var plan = await _mealPlanService.GetMealPlanByIdAsync(id, userId, ct);
        return OkResponse(plan, "OK");
    }

    /// <summary>
    /// FN-MEAL-02: Kích hoạt (triển khai) thực đơn từ DRAFT sang ACTIVE.
    /// Thực đơn ACTIVE cũ trùng tuần sẽ tự động chuyển sang ARCHIVED.
    /// </summary>
    [HttpPost("{id:int}/activate")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<ActivateMealPlanResponseData>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> ActivateMealPlan(
        [FromRoute] int id,
        CancellationToken ct = default)
    {
        var (userId, userRole) = EnsureAuthorizedUser();
        var result = await _mealPlanService.ActivateMealPlanAsync(id, userId, userRole, ct);
        return OkResponse(result, "Đã kích hoạt thực đơn.");
    }

    /// <summary>
    /// FN-MEAL-03: Đổi một bữa sang món thay thế tương đương.
    /// Món thay thế bảo đảm cùng trường phái, không dị ứng, calo chênh lệch &lt;= 15%.
    /// Hạn mức: tối đa 21 lần đổi ở giai đoạn DRAFT và 21 lần đổi ở giai đoạn ACTIVE.
    /// </summary>
    [HttpPut("{id:int}/slots/{slotId:int}")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<UpdateMealPlanSlotResponseData>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> UpdateMealPlanSlot(
        [FromRoute] int id,
        [FromRoute] int slotId,
        [FromBody] UpdateMealPlanSlotRequest? request,
        CancellationToken ct = default)
    {
        var (userId, userRole) = EnsureAuthorizedUser();
        var result = await _mealPlanService.UpdateMealPlanSlotAsync(id, slotId, userId, userRole, request ?? new UpdateMealPlanSlotRequest(), ct);
        return OkResponse(result, "Đã đổi món.");
    }

    /// <summary>
    /// FN-MEAL-04: Đánh dấu bữa ăn trong thực đơn ACTIVE là đã ăn / chưa ăn.
    /// </summary>
    [HttpPatch("{id:int}/slots/{slotId:int}")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<PatchMealPlanSlotResponseData>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> PatchMealPlanSlot(
        [FromRoute] int id,
        [FromRoute] int slotId,
        [FromBody] PatchMealPlanSlotRequest? request,
        CancellationToken ct = default)
    {
        if (request == null)
        {
            throw AppException.BadRequest(
                ErrorCodes.ErrValidation,
                "Dữ liệu đầu vào không hợp lệ.");
        }

        var (userId, _) = EnsureAuthorizedUser();
        var result = await _mealPlanService.PatchMealPlanSlotAsync(id, slotId, userId, request, ct);
        return OkResponse(result, "OK");
    }

    /// <summary>
    /// FN-MEAL-07: Xem calo thực đơn (Free: tổng calo mỗi ngày; VIP: chi tiết từng món, bữa, ngày so với mục tiêu).
    /// </summary>
    [HttpGet("{id:int}/nutrition")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<GetMealPlanNutritionResponseData>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetMealPlanNutrition(
        [FromRoute] int id,
        [FromQuery] string level = "BASIC",
        CancellationToken ct = default)
    {
        var (userId, userRole) = EnsureAuthorizedUser();
        var result = await _mealPlanService.GetMealPlanNutritionAsync(id, userId, userRole, level, ct);
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
