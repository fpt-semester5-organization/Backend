using System.Threading;
using System.Threading.Tasks;

namespace VeggieLife.DAL.External.AiService;

/// <summary>
/// Hợp đồng giao tiếp Client gọi sang microservice ai-service (Python/FastAPI).
/// Tuân thủ nguyên tắc D118–D119: Backend không giữ Gemini SDK trực tiếp.
/// </summary>
public interface IAiServiceClient
{
    /// <summary>
    /// Gửi danh sách món ứng viên sang AI Service xếp 21 bữa ăn trong tuần (FN-MEAL-01).
    /// </summary>
    Task<AiMealPlanResponse?> GenerateMealPlanAsync(AiMealPlanRequest request, CancellationToken ct = default);

    /// <summary>
    /// Kiểm tra xem trạng thái ngắt mạch (Circuit Breaker) có đang mở hay không (BR-ADM-AI-002).
    /// </summary>
    bool IsCircuitOpen { get; }
}
