using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace VeggieLife.BLL.Services.AI;

/// <summary>
/// Triển khai bản rỗng (Stub/Mock) của IVectorDbService để thành viên khác hoàn thiện tích hợp Qdrant.
/// </summary>
public class VectorDbServiceStub : IVectorDbService
{
    private readonly ILogger<VectorDbServiceStub> _logger;

    public VectorDbServiceStub(ILogger<VectorDbServiceStub> logger)
    {
        _logger = logger;
    }

    public Task<IReadOnlyList<VectorSearchResult>> SearchSimilarAsync(
        IReadOnlyList<float> vector, 
        int topK = 5, 
        float minScore = 0.55f, 
        CancellationToken ct = default)
    {
        _logger.LogInformation("[VectorDbServiceStub] Tìm kiếm tương đồng vector top {TopK}, minScore {MinScore}", topK, minScore);
        // TODO: Tích hợp gRPC/REST Qdrant client
        IReadOnlyList<VectorSearchResult> sampleResults = new List<VectorSearchResult>
        {
            new()
            {
                Id = Guid.NewGuid().ToString(),
                Score = 0.88f,
                Title = "Kiến thức ăn chay thuần chuẩn khoa học",
                Content = "Người ăn chay cần bổ sung Vitamin B12, Kẽm và Sắt qua nấm men dinh dưỡng và hạt lanh.",
                SourceType = "KNOWLEDGE_BASE"
            }
        };

        return Task.FromResult(sampleResults);
    }

    public Task UpsertPointAsync(
        string id, 
        IReadOnlyList<float> vector, 
        IDictionary<string, object> payload, 
        CancellationToken ct = default)
    {
        _logger.LogInformation("[VectorDbServiceStub] Lưu point id: {Id} vào Qdrant", id);
        // TODO: Tích hợp Qdrant Upsert
        return Task.CompletedTask;
    }

    public Task DeletePointAsync(string id, CancellationToken ct = default)
    {
        _logger.LogInformation("[VectorDbServiceStub] Xóa point id: {Id} khỏi Qdrant", id);
        // TODO: Tích hợp Qdrant Delete
        return Task.CompletedTask;
    }
}
