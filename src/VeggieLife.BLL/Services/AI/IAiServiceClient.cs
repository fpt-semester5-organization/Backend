using System.Threading;
using System.Threading.Tasks;
using VeggieLife.BLL.DTOs.Ai;

namespace VeggieLife.BLL.Services.AI;

/// <summary>
/// Client giao tiếp HTTP REST với microservice AI (POST /chat/answer) theo Decision D118, S5-N1.
/// </summary>
public interface IAiServiceClient
{
    /// <summary>
    /// Gửi câu hỏi kèm lịch sử, hồ sơ và danh sách công thức ứng viên sang AI Service (RAG + Gemini 2.5 Flash).
    /// </summary>
    Task<AiChatAnswerResponse> GetChatAnswerAsync(AiChatAnswerRequest request, CancellationToken ct = default);
}
