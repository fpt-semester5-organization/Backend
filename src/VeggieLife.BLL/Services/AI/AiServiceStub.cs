using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace VeggieLife.BLL.Services.AI;

/// <summary>
/// Triển khai bản rỗng (Stub/Mock) của IAiService để thành viên khác hoàn thiện tích hợp Gemini API.
/// </summary>
public class AiServiceStub : IAiService
{
    private readonly ILogger<AiServiceStub> _logger;

    public AiServiceStub(ILogger<AiServiceStub> logger)
    {
        _logger = logger;
    }

    public Task<string> GenerateTextAsync(string prompt, CancellationToken ct = default)
    {
        _logger.LogInformation("[AiServiceStub] Sinh văn bản mẫu cho prompt: {Prompt}", prompt);
        // TODO: Tích hợp Gemini 1.5 Flash API thực tế
        return Task.FromResult("Đây là câu trả lời mẫu từ AiServiceStub (chờ tích hợp Gemini API).");
    }

    public Task<string> ConsultNutritionAsync(long sessionId, string userQuestion, CancellationToken ct = default)
    {
        _logger.LogInformation("[AiServiceStub] Tư vấn dinh dưỡng cho phiên #{SessionId}: {Question}", sessionId, userQuestion);
        // TODO: Tích hợp Gemini Flash + RAG Vector DB + Hồ sơ thể trạng
        return Task.FromResult($"[Tư vấn mẫu] Chào bạn, đối với câu hỏi \"{userQuestion}\", một chế độ ăn chay cân bằng cần đảm bảo đầy đủ chất đạm thực vật từ đậu hũ, các loại hạt và rau lá xanh đậm.");
    }

    public Task<IReadOnlyList<float>> GenerateEmbeddingAsync(string text, CancellationToken ct = default)
    {
        _logger.LogInformation("[AiServiceStub] Sinh vector embedding cho văn bản: {Text}", text);
        // TODO: Gọi mô hình text-embedding-004 của Google Gemini (768 chiều)
        var dummyVector = new float[768];
        return Task.FromResult<IReadOnlyList<float>>(dummyVector);
    }

    public Task<string> AnalyzeFridgeImageAsync(Stream imageStream, string mimeType, CancellationToken ct = default)
    {
        _logger.LogInformation("[AiServiceStub] Phân tích hình ảnh tủ lạnh với mimeType: {MimeType}", mimeType);
        // TODO: Tích hợp Gemini Vision để trích xuất JSON danh sách nguyên liệu
        return Task.FromResult("[{\"ingredientName\": \"Đậu hũ non\", \"quantity\": 2, \"unit\": \"miếng\"}, {\"ingredientName\": \"Cà chua\", \"quantity\": 3, \"unit\": \"quả\"}]");
    }
}
