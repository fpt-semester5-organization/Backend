using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace VeggieLife.BLL.Services.AI;

/// <summary>
/// Kết quả tìm kiếm tương đồng ngữ nghĩa từ Qdrant.
/// </summary>
public class VectorSearchResult
{
    public string Id { get; set; } = string.Empty;
    public float Score { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string SourceType { get; set; } = string.Empty;
}

/// <summary>
/// Interface lưu trữ và tìm kiếm vector ngữ nghĩa (Qdrant Vector Database).
/// </summary>
public interface IVectorDbService
{
    /// <summary>
    /// Tìm kiếm top-K đoạn tri thức tương đồng theo vector embedding (ngưỡng tương đồng minScore >= 0.55).
    /// </summary>
    Task<IReadOnlyList<VectorSearchResult>> SearchSimilarAsync(
        IReadOnlyList<float> vector,
        int topK = 5,
        float minScore = 0.55f,
        CancellationToken ct = default);

    /// <summary>
    /// Lưu trữ hoặc cập nhật vector embedding của tri thức vào collection veggielife_knowledge.
    /// </summary>
    Task UpsertPointAsync(
        string id,
        IReadOnlyList<float> vector,
        IDictionary<string, object> payload,
        CancellationToken ct = default);

    /// <summary>
    /// Xóa vector tri thức theo mã định danh.
    /// </summary>
    Task DeletePointAsync(string id, CancellationToken ct = default);
}
