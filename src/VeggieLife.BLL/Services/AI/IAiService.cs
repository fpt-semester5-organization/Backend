using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace VeggieLife.BLL.Services.AI;

/// <summary>
/// Interface dịch vụ AI (Gemini Flash) phục vụ tư vấn dinh dưỡng, gợi ý thực đơn, trích xuất hình ảnh tủ lạnh và tạo embedding.
/// </summary>
public interface IAiService
{
    /// <summary>
    /// Sinh phản hồi văn bản tự do từ lời nhắc.
    /// </summary>
    Task<string> GenerateTextAsync(string prompt, CancellationToken ct = default);

    /// <summary>
    /// Tư vấn dinh dưỡng thông minh kết hợp ngữ cảnh RAG và hồ sơ thể trạng người dùng.
    /// </summary>
    Task<string> ConsultNutritionAsync(long sessionId, string userQuestion, CancellationToken ct = default);

    /// <summary>
    /// Tạo vector embedding từ văn bản phục vụ truy vấn ngữ nghĩa trên Vector DB (Qdrant).
    /// </summary>
    Task<IReadOnlyList<float>> GenerateEmbeddingAsync(string text, CancellationToken ct = default);

    /// <summary>
    /// Nhận diện nguyên liệu trong hình ảnh chụp tủ lạnh bằng Gemini Vision.
    /// </summary>
    Task<string> AnalyzeFridgeImageAsync(Stream imageStream, string mimeType, CancellationToken ct = default);
}
