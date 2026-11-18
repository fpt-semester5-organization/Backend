using System;
using System.Collections.Generic;
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
    /// Nạp một nguồn tri thức và trả số đoạn (chunk) thực tế đã nhúng; lỗi → ném AppException (không nuốt lỗi).
    /// </summary>
    Task<int> UpsertKnowledgeChunksAsync(string sourceType, string sourceId, string title, string text, DateTime? updatedAt = null, CancellationToken ct = default);

    /// <summary>
    /// Bật/ẩn nguồn tri thức trên Qdrant (payload active) qua POST /rag/visibility: theo danh sách sourceId,
    /// theo số sách (sourceId = bookNo*1000 + k) hoặc theo tiền tố sourceId (tài liệu tải lên nhiều phần).
    /// Trả số điểm bị ảnh hưởng; lỗi → ném AppException để không lệch trạng thái giữa Redis và Qdrant.
    /// </summary>
    /// <summary>
    /// Nhận dạng chữ (OCR) PDF scan qua POST /rag/ocr-pdf (Gemini, D118); lỗi → ném AppException.
    /// </summary>
    Task<AiOcrPdfResult> OcrPdfAsync(byte[] pdf, string fileName, CancellationToken ct = default);

    Task<int> SetKnowledgeVisibilityAsync(string sourceType, IReadOnlyCollection<string> sourceIds, int? bookNo, string? sourceIdPrefix, bool active, CancellationToken ct = default);

    /// <summary>
    /// Kích hoạt nạp lại toàn bộ tài liệu dinh dưỡng chuẩn từ thư mục knowledge/nutrition/ của AI Service (FN-CHAT-07).
    /// </summary>
    Task<bool> ReindexNutritionAsync(CancellationToken ct = default);
}
