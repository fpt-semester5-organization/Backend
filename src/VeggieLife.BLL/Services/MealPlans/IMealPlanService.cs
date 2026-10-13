using System.Threading;
using System.Threading.Tasks;
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
}
