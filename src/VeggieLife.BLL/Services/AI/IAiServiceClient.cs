using System;
using System.Threading;
using System.Threading.Tasks;
using VeggieLife.BLL.DTOs.Admin.Knowledge;
using VeggieLife.BLL.DTOs.Ai;

namespace VeggieLife.BLL.Services.AI;

/// <summary>
/// Client giao tiếp HTTP REST với microservice AI (POST /chat/answer, /rag/*) theo Decision D118, S5-N1.
/// </summary>
public interface IAiServiceClient
{
    /// <summary>
    /// Gửi câu hỏi kèm lịch sử, hồ sơ và danh sách công thức ứng viên sang AI Service (RAG + Gemini 2.5 Flash).
    /// </summary>
    Task<AiChatAnswerResponse> GetChatAnswerAsync(AiChatAnswerRequest request, CancellationToken ct = default);

    /// <summary>
    /// Lấy trạng thái hiện tại của kho vector Qdrant từ AI Service (FN-CHAT-07).
    /// </summary>
    Task<AdminKnowledgeStatusResponseData> GetKnowledgeStatusAsync(CancellationToken ct = default);

    /// <summary>
    /// Nạp hoặc cập nhật vector embedding của một mẩu tri thức vào Qdrant (FN-CHAT-07).
    /// </summary>
    Task<bool> UpsertKnowledgeAsync(string sourceType, string sourceId, string title, string text, DateTime? updatedAt = null, CancellationToken ct = default);

    /// <summary>
    /// Xóa toàn bộ vector embedding của một nguồn tri thức khỏi Qdrant (FN-CHAT-07).
    /// </summary>
    Task<int> DeleteKnowledgeAsync(string sourceType, string sourceId, CancellationToken ct = default);

    /// <summary>
    /// Kích hoạt nạp lại toàn bộ tài liệu dinh dưỡng chuẩn từ thư mục knowledge/nutrition/ của AI Service (FN-CHAT-07).
    /// </summary>
    Task<bool> ReindexNutritionAsync(CancellationToken ct = default);
}
