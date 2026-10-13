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
/// Controller quản lý và cá nhân hóa thực đơn dinh dưỡng 7 ngày (FN-MEAL-01..08).
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
    /// <param name="request">Thông tin cấu hình tạo thực đơn.</param>
    /// <param name="ct">CancellationToken.</param>
    /// <returns>Dữ liệu thực đơn nháp gồm 21 bữa ăn và hạn mức chỉnh sửa.</returns>
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

        var result = await _mealPlanService.CreateMealPlanAsync((int)CurrentUserId.Value, userRole, request, ct);
        return CreatedResponse(result, "Đã tạo thực đơn nháp.");
    }
}
