using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using VeggieLife.BLL.DTOs.Admin.Knowledge;

namespace VeggieLife.BLL.Services.Knowledge;

/// <summary>
/// Dịch vụ quản lý các tài liệu dinh dưỡng PDF nạp vào Qdrant Vector DB (FN-CHAT-07).
/// </summary>
public interface INutritionDocumentService
{
    /// <summary>
    /// Lấy danh sách toàn bộ tài liệu dinh dưỡng trong kho Vector DB.
    /// </summary>
    Task<List<AdminNutritionDocumentDto>> GetDocumentsAsync(CancellationToken ct = default);

    /// <summary>
    /// Tải lên tệp tài liệu PDF/văn bản mới, trích xuất nội dung và vector hoá vào Qdrant.
    /// </summary>
    Task<AdminNutritionDocumentDto> UploadDocumentAsync(UploadNutritionDocumentRequest request, CancellationToken ct = default);

    /// <summary>
    /// Chuyển đổi trạng thái sử dụng (IsActive) của tài liệu cho Chatbot RAG.
    /// </summary>
    Task<AdminNutritionDocumentDto> UpdateStatusAsync(string id, bool isActive, CancellationToken ct = default);

    /// <summary>
    /// Xóa hoàn toàn tài liệu và các vector embeddings tương ứng khỏi Qdrant Vector DB.
    /// </summary>
    Task<bool> DeleteDocumentAsync(string id, CancellationToken ct = default);

    /// <summary>
    /// Lấy luồng tệp tài liệu nguyên bản và thông tin Content-Type để phục vụ xem trực tiếp trong iframe hoặc tải về.
    /// </summary>
    Task<(Stream FileStream, string ContentType, string FileName)?> GetDocumentFileAsync(string id, CancellationToken ct = default);
}
