using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using VeggieLife.BLL.Common.Responses;
using VeggieLife.BLL.DTOs.MealPlans.Requests;
using VeggieLife.BLL.DTOs.MealPlans.Responses;

namespace VeggieLife.BLL.Services.MealPlans;

/// <summary>
/// Interface dịch vụ quản lý và tạo thực đơn 7 ngày bằng AI (FN-MEAL-01..08).
/// </summary>
public interface IMealPlanService
{
    /// <summary>
    /// FN-MEAL-01: Sinh thực đơn 7 ngày x 3 bữa (21 bữa) bằng AI phù hợp hồ sơ sức khỏe và dị ứng.
    /// Kiểm tra hạn mức tuần, lọc trước công thức hợp lệ, kiểm tra calo lệch tối đa 10%,
    /// hỗ trợ tự động chuyển sang Fallback Engine khi AI quá 4 giây hoặc mạch AI mở.
    /// </summary>
    Task<CreateMealPlanResponseData> CreateMealPlanAsync(
        int userId,
        string userRole,
        CreateMealPlanRequest request,
        CancellationToken ct = default);

    /// <summary>
    /// FN-MEAL-02: Xem danh sách các thực đơn của người dùng (mới nhất trước, có phân trang).
    /// </summary>
    Task<(IReadOnlyList<MealPlanSummaryDto> Items, PagedMeta Meta)> ListMealPlansAsync(
        int userId,
        int page,
        int size,
        CancellationToken ct = default);

    /// <summary>
    /// FN-MEAL-02: Xem chi tiết 21 bữa ăn trong một thực đơn và số lần đổi món còn lại.
    /// </summary>
    Task<CreateMealPlanResponseData> GetMealPlanByIdAsync(
        int id,
        int userId,
        CancellationToken ct = default);

    /// <summary>
    /// FN-MEAL-02: Kích hoạt (triển khai) thực đơn từ DRAFT sang ACTIVE. Thực đơn ACTIVE cũ cùng tuần sẽ chuyển sang ARCHIVED.
    /// </summary>
    Task<ActivateMealPlanResponseData> ActivateMealPlanAsync(
        int id,
        int userId,
        string userRole,
        CancellationToken ct = default);

    /// <summary>
    /// FN-MEAL-03: Đổi một bữa sang món thay thế tương đương (cùng trường phái, không dị ứng, calo chênh lệch &lt;= 15%).
    /// Hạn mức: tối đa 21 lần đổi ở giai đoạn DRAFT và 21 lần đổi ở giai đoạn ACTIVE.
    /// </summary>
    Task<UpdateMealPlanSlotResponseData> UpdateMealPlanSlotAsync(
        int id,
        int slotId,
        int userId,
        string userRole,
        UpdateMealPlanSlotRequest request,
        CancellationToken ct = default);

    /// <summary>
    /// FN-MEAL-04: Đánh dấu một bữa trong thực đơn ACTIVE là đã ăn / chưa ăn để theo dõi tiến độ tuần.
    /// </summary>
    Task<PatchMealPlanSlotResponseData> PatchMealPlanSlotAsync(
        int id,
        int slotId,
        int userId,
        PatchMealPlanSlotRequest request,
        CancellationToken ct = default);
}
