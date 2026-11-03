using System;

namespace VeggieLife.BLL.Services.Knowledge;

/// <summary>
/// Cấu trúc công việc đồng bộ kho tri thức vector Qdrant đẩy vào hàng đợi Redis (FN-CHAT-07).
/// </summary>
public class KnowledgeSyncJobPayload
{
    /// <summary>
    /// Hành động: UPSERT hoặc DELETE.
    /// </summary>
    public string Action { get; set; } = "UPSERT";

    /// <summary>
    /// Loại nguồn: RECIPE, POST, E_NUMBER, INGREDIENT, ALLERGEN, VIDEO_TRANSCRIPT.
    /// </summary>
    public string ContentType { get; set; } = string.Empty;

    /// <summary>
    /// Mã định danh của nội dung (số ID hoặc mã chuỗi như E100).
    /// </summary>
    public string SourceId { get; set; } = string.Empty;

    /// <summary>
    /// Thời điểm đưa vào hàng đợi.
    /// </summary>
    public DateTime EnqueuedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Số lần đã thử lại khi gặp lỗi.
    /// </summary>
    public int RetryCount { get; set; } = 0;
}
