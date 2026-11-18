using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using VeggieLife.BLL.Common.Constants;
using VeggieLife.BLL.Common.Exceptions;
using VeggieLife.BLL.DTOs.Admin.Knowledge;
using VeggieLife.BLL.DTOs.Ai;

namespace VeggieLife.BLL.Services.AI;

/// <summary>
/// Triển khai client giao tiếp HTTP REST với AI Microservice qua các endpoint /chat/* và /rag/*.
/// Xử lý phân tích lỗi Qdrant (503), lỗi ngắt mạch (BR-ADM-AI-002), và lọc nội dung nguy hại (400 - D119).
/// </summary>
public class AiServiceClient : IAiServiceClient
{
    private static readonly JsonSerializerOptions CamelCaseOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };
    private readonly HttpClient _httpClient;
    private readonly IAiCircuitBreaker _circuitBreaker;
    private readonly ILogger<AiServiceClient> _logger;
    private readonly IConfiguration _configuration;

    public AiServiceClient(
        HttpClient httpClient,
        IAiCircuitBreaker circuitBreaker,
        ILogger<AiServiceClient> logger,
        IConfiguration configuration)
    {
        _httpClient = httpClient;
        _circuitBreaker = circuitBreaker;
        _logger = logger;
        _configuration = configuration;

        // Cấu hình BaseAddress và Header nếu chưa được thiết lập từ DI
        if (_httpClient.BaseAddress == null)
        {
            var baseUrl = _configuration["AiService:BaseUrl"] ?? "http://localhost:8080";
            _httpClient.BaseAddress = new Uri(baseUrl);
        }

        if (!_httpClient.DefaultRequestHeaders.Contains("X-Internal-Api-Key"))
        {
            var apiKey = _configuration["AiService:InternalApiKey"] ?? string.Empty;
            _httpClient.DefaultRequestHeaders.Add("X-Internal-Api-Key", apiKey);
        }
    }

    public async Task<AiChatAnswerResponse> GetChatAnswerAsync(
        AiChatAnswerRequest request,
        CancellationToken ct = default)
    {
        // 1. Kiểm tra mạch ngắt AI (BR-ADM-AI-002)
        _circuitBreaker.ThrowIfOpen();

        try
        {
            var response = await _httpClient.PostAsJsonAsync("/chat/answer", request, ct);

            if (!response.IsSuccessStatusCode)
            {
                var statusCode = (int)response.StatusCode;
                var errorBody = await response.Content.ReadAsStringAsync(ct);
                _logger.LogWarning("[AiServiceClient] AI Service trả về HTTP {StatusCode}: {ErrorBody}", statusCode, errorBody);

                if (statusCode == 400)
                {
                    if (errorBody.Contains("UNSAFE_CONTENT", StringComparison.OrdinalIgnoreCase) ||
                        errorBody.Contains("vi phạm tiêu chuẩn an toàn", StringComparison.OrdinalIgnoreCase))
                    {
                        // Lọc an toàn nội dung câu hỏi (thô tục, nguy hiểm y tế - D119, FN-CHAT-02)
                        throw AppException.BadRequest(
                            ErrorCodes.ErrChatUnsafeContent,
                            "Nội dung vi phạm tiêu chuẩn an toàn (vẫn tính lượt).");
                    }

                    _logger.LogError("[AiServiceClient] AI Service trả về lỗi dữ liệu đầu vào (HTTP 400): {ErrorBody}", errorBody);
                    throw AppException.BadRequest(
                        ErrorCodes.ErrValidation,
                        $"Dữ liệu gửi sang AI Service không hợp lệ: {errorBody}");
                }

                if (statusCode == 503)
                {
                    _circuitBreaker.RecordFailure();

                    if (errorBody.Contains("Qdrant", StringComparison.OrdinalIgnoreCase) ||
                        errorBody.Contains(ErrorCodes.ErrExtQdrantUnavailable, StringComparison.OrdinalIgnoreCase))
                    {
                        throw AppException.ServiceUnavailable(
                            ErrorCodes.ErrExtQdrantUnavailable,
                            "Qdrant không phản hồi hoặc quá 3 giây (AI service báo).");
                    }

                    if (errorBody.Contains(ErrorCodes.ErrAiCircuitOpen, StringComparison.OrdinalIgnoreCase))
                    {
                        throw AppException.ServiceUnavailable(
                            ErrorCodes.ErrAiCircuitOpen,
                            "Mạch AI đang ngắt tạm thời (BR-ADM-AI-002).");
                    }

                    throw AppException.ServiceUnavailable(
                        ErrorCodes.ErrAiUnavailable,
                        "Dịch vụ AI tạm thời không khả dụng.");
                }

                _circuitBreaker.RecordFailure();
                throw AppException.ServiceUnavailable(
                    ErrorCodes.ErrAiUnavailable,
                    $"Dịch vụ AI gặp lỗi nội bộ (HTTP {statusCode}).");
            }

            var result = await response.Content.ReadFromJsonAsync<AiChatAnswerResponse>(cancellationToken: ct);
            if (result == null)
            {
                _circuitBreaker.RecordFailure();
                throw AppException.ServiceUnavailable(
                    ErrorCodes.ErrAiUnavailable,
                    "Phản hồi từ AI Service rỗng.");
            }

            // Ghi nhận thành công vào bộ ngắt mạch
            _circuitBreaker.RecordSuccess();
            return result;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "[AiServiceClient] Lỗi kết nối mạng tới AI Service ({BaseAddress})", _httpClient.BaseAddress);
            _circuitBreaker.RecordFailure();
            throw AppException.ServiceUnavailable(
                ErrorCodes.ErrAiUnavailable,
                "Không thể kết nối đến máy chủ AI service.");
        }
        catch (TaskCanceledException ex) when (!ct.IsCancellationRequested)
        {
            _logger.LogWarning(ex, "[AiServiceClient] Timeout khi gọi AI Service (> 10s)");
            _circuitBreaker.RecordFailure();
            throw AppException.ServiceUnavailable(
                ErrorCodes.ErrAiUnavailable,
                "Thời gian chờ phản hồi từ AI service quá hạn (Timeout > 10s).");
        }
    }

    /// <summary>
    /// FN-CHAT-07: Lấy trạng thái hiện tại của kho vector Qdrant từ AI Service.
    /// </summary>
    public async Task<AdminKnowledgeStatusResponseData> GetKnowledgeStatusAsync(CancellationToken ct = default)
    {
        _circuitBreaker.ThrowIfOpen();

        try
        {
            var response = await _httpClient.GetAsync("/rag/status", ct);
            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(ct);
                _logger.LogWarning("[AiServiceClient] GET /rag/status trả về HTTP {StatusCode}: {ErrorBody}", (int)response.StatusCode, errorBody);

                if (response.StatusCode == System.Net.HttpStatusCode.ServiceUnavailable)
                {
                    _circuitBreaker.RecordFailure();
                    throw AppException.ServiceUnavailable(
                        ErrorCodes.ErrExtQdrantUnavailable,
                        "Qdrant không phản hồi hoặc quá 3 giây (AI service báo).");
                }

                _circuitBreaker.RecordFailure();
                throw AppException.ServiceUnavailable(
                    ErrorCodes.ErrAiUnavailable,
                    "Không thể lấy trạng thái kho tri thức từ AI Service.");
            }

            var result = await response.Content.ReadFromJsonAsync<AdminKnowledgeStatusResponseData>(CamelCaseOptions, ct);
            if (result == null)
            {
                throw AppException.ServiceUnavailable(
                    ErrorCodes.ErrAiUnavailable,
                    "Phản hồi trạng thái kho tri thức từ AI Service bị rỗng.");
            }

            _circuitBreaker.RecordSuccess();
            return result;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "[AiServiceClient] Lỗi kết nối mạng khi lấy trạng thái Qdrant");
            _circuitBreaker.RecordFailure();
            throw AppException.ServiceUnavailable(
                ErrorCodes.ErrAiUnavailable,
                "Không thể kết nối đến máy chủ AI service.");
        }
    }

    /// <summary>
    /// FN-CHAT-07: Nạp hoặc cập nhật vector embedding của một mẩu tri thức vào Qdrant.
    /// </summary>
    public async Task<bool> UpsertKnowledgeAsync(
        string sourceType,
        string sourceId,
        string title,
        string text,
        DateTime? updatedAt = null,
        CancellationToken ct = default)
    {
        _circuitBreaker.ThrowIfOpen();

        try
        {
            var payload = new
            {
                sourceType,
                sourceId,
                title,
                text,
                updatedAt = updatedAt ?? DateTime.UtcNow
            };

            var response = await _httpClient.PostAsJsonAsync("/rag/upsert", payload, CamelCaseOptions, ct);
            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(ct);
                _logger.LogWarning("[AiServiceClient] POST /rag/upsert [{Type} #{Id}] lỗi HTTP {Code}: {Body}",
                    sourceType, sourceId, (int)response.StatusCode, errorBody);

                if (response.StatusCode == System.Net.HttpStatusCode.ServiceUnavailable)
                {
                    _circuitBreaker.RecordFailure();
                    throw AppException.ServiceUnavailable(
                        ErrorCodes.ErrExtQdrantUnavailable,
                        "Qdrant không phản hồi hoặc quá 3 giây.");
                }

                return false;
            }

            _circuitBreaker.RecordSuccess();
            _logger.LogInformation("[AiServiceClient] Đã nạp thành công tri thức [{Type} #{Id}] vào Qdrant.", sourceType, sourceId);
            return true;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "[AiServiceClient] Lỗi mạng khi gọi /rag/upsert [{Type} #{Id}]", sourceType, sourceId);
            _circuitBreaker.RecordFailure();
            return false;
        }
    }

    /// <summary>
    /// FN-CHAT-07: Xóa toàn bộ vector embedding của một nguồn tri thức khỏi Qdrant.
    /// </summary>
    public async Task<int> DeleteKnowledgeAsync(
        string sourceType,
        string sourceId,
        CancellationToken ct = default)
    {
        _circuitBreaker.ThrowIfOpen();

        try
        {
            var payload = new
            {
                sourceType,
                sourceId
            };

            var response = await _httpClient.PostAsJsonAsync("/rag/delete", payload, CamelCaseOptions, ct);
            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(ct);
                _logger.LogWarning("[AiServiceClient] POST /rag/delete [{Type} #{Id}] lỗi HTTP {Code}: {Body}",
                    sourceType, sourceId, (int)response.StatusCode, errorBody);
                return 0;
            }

            var jsonDoc = await response.Content.ReadFromJsonAsync<JsonDocument>(CamelCaseOptions, ct);
            var deleted = jsonDoc?.RootElement.TryGetProperty("deleted", out var dProp) == true ? dProp.GetInt32() : 0;
            _circuitBreaker.RecordSuccess();
            _logger.LogInformation("[AiServiceClient] Đã xóa {Count} vector points của [{Type} #{Id}] khỏi Qdrant.", deleted, sourceType, sourceId);
            return deleted;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[AiServiceClient] Lỗi khi gọi /rag/delete [{Type} #{Id}]", sourceType, sourceId);
            return 0;
        }
    }

    public async Task<int> UpsertKnowledgeChunksAsync(
        string sourceType,
        string sourceId,
        string title,
        string text,
        DateTime? updatedAt = null,
        CancellationToken ct = default)
    {
        _circuitBreaker.ThrowIfOpen();
        HttpResponseMessage response;
        try
        {
            var payload = new { sourceType, sourceId, title, text, updatedAt = updatedAt ?? DateTime.UtcNow };
            response = await _httpClient.PostAsJsonAsync("/rag/upsert", payload, CamelCaseOptions, ct);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "[AiServiceClient] Lỗi mạng khi gọi /rag/upsert [{Type} #{Id}]", sourceType, sourceId);
            _circuitBreaker.RecordFailure();
            throw AppException.ServiceUnavailable(ErrorCodes.ErrExtQdrantUnavailable, "Không kết nối được AI service để vector hoá tài liệu.");
        }

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(ct);
            _logger.LogWarning("[AiServiceClient] POST /rag/upsert [{Type} #{Id}] lỗi HTTP {Code}: {Body}",
                sourceType, sourceId, (int)response.StatusCode, errorBody);
            if ((int)response.StatusCode >= 500) _circuitBreaker.RecordFailure();
            throw AppException.ServiceUnavailable(ErrorCodes.ErrExtQdrantUnavailable,
                $"AI service không vector hoá được tài liệu (HTTP {(int)response.StatusCode}).");
        }

        _circuitBreaker.RecordSuccess();
        var jsonDoc = await response.Content.ReadFromJsonAsync<JsonDocument>(CamelCaseOptions, ct);
        return jsonDoc?.RootElement.TryGetProperty("chunks", out var cProp) == true ? cProp.GetInt32() : 0;
    }

    public async Task<AiOcrPdfResult> OcrPdfAsync(byte[] pdf, string fileName, CancellationToken ct = default)
    {
        _circuitBreaker.ThrowIfOpen();
        HttpResponseMessage response;
        try
        {
            using var content = new MultipartFormDataContent();
            var file = new ByteArrayContent(pdf);
            file.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/pdf");
            content.Add(file, "file", string.IsNullOrWhiteSpace(fileName) ? "document.pdf" : fileName);
            // OCR sách dài mất vài phút (AI service chạy song song từng lô trang) — không áp timeout 10 giây của chat
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(TimeSpan.FromMinutes(15));
            response = await _httpClient.PostAsync("/rag/ocr-pdf", content, cts.Token);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "[AiServiceClient] Lỗi mạng khi gọi /rag/ocr-pdf ({File})", fileName);
            _circuitBreaker.RecordFailure();
            throw AppException.ServiceUnavailable(ErrorCodes.ErrExtSourceUnavailable, "Không kết nối được AI service để nhận dạng chữ trong PDF.");
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            throw AppException.ServiceUnavailable(ErrorCodes.ErrExtSourceUnavailable, "Nhận dạng chữ PDF quá thời gian (15 phút).");
        }

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(ct);
            _logger.LogWarning("[AiServiceClient] POST /rag/ocr-pdf ({File}) lỗi HTTP {Code}: {Body}", fileName, (int)response.StatusCode, errorBody);
            string message = $"AI service không nhận dạng được chữ trong PDF (HTTP {(int)response.StatusCode}).";
            try
            {
                using var err = JsonDocument.Parse(errorBody);
                if (err.RootElement.TryGetProperty("message", out var m) && m.ValueKind == JsonValueKind.String) message = m.GetString()!;
            }
            catch (JsonException) { }
            if ((int)response.StatusCode >= 500)
            {
                _circuitBreaker.RecordFailure();
                throw AppException.ServiceUnavailable(ErrorCodes.ErrExtSourceUnavailable, message);
            }
            throw AppException.UnprocessableEntity(ErrorCodes.ErrMediaInvalidFile, message);
        }

        _circuitBreaker.RecordSuccess();
        return await response.Content.ReadFromJsonAsync<AiOcrPdfResult>(CamelCaseOptions, ct)
               ?? throw AppException.ServiceUnavailable(ErrorCodes.ErrExtSourceUnavailable, "AI service trả kết quả OCR rỗng.");
    }

    public async Task<int> SetKnowledgeVisibilityAsync(
        string sourceType,
        IReadOnlyCollection<string> sourceIds,
        int? bookNo,
        string? sourceIdPrefix,
        bool active,
        CancellationToken ct = default)
    {
        _circuitBreaker.ThrowIfOpen();
        HttpResponseMessage response;
        try
        {
            var payload = new { sourceType, sourceIds, bookNo, sourceIdPrefix, active };
            response = await _httpClient.PostAsJsonAsync("/rag/visibility", payload, CamelCaseOptions, ct);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "[AiServiceClient] Lỗi mạng khi gọi /rag/visibility [{Type}]", sourceType);
            _circuitBreaker.RecordFailure();
            throw AppException.ServiceUnavailable(ErrorCodes.ErrExtQdrantUnavailable, "Không kết nối được AI service để cập nhật trạng thái tài liệu.");
        }

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(ct);
            _logger.LogWarning("[AiServiceClient] POST /rag/visibility [{Type}] lỗi HTTP {Code}: {Body}",
                sourceType, (int)response.StatusCode, errorBody);
            if ((int)response.StatusCode >= 500) _circuitBreaker.RecordFailure();
            throw AppException.ServiceUnavailable(ErrorCodes.ErrExtQdrantUnavailable,
                $"AI service không cập nhật được trạng thái tài liệu (HTTP {(int)response.StatusCode}).");
        }

        _circuitBreaker.RecordSuccess();
        var jsonDoc = await response.Content.ReadFromJsonAsync<JsonDocument>(CamelCaseOptions, ct);
        var updated = jsonDoc?.RootElement.TryGetProperty("updated", out var uProp) == true ? uProp.GetInt32() : 0;
        _logger.LogInformation("[AiServiceClient] {Action} {Count} điểm [{Type}] trên Qdrant.", active ? "Bật" : "Ẩn", updated, sourceType);
        return updated;
    }

    /// <summary>
    /// FN-CHAT-07: Kích hoạt nạp lại toàn bộ tài liệu dinh dưỡng chuẩn từ thư mục knowledge/nutrition/ của AI Service.
    /// </summary>
    public async Task<bool> ReindexNutritionAsync(CancellationToken ct = default)
    {
        _circuitBreaker.ThrowIfOpen();

        try
        {
            var response = await _httpClient.PostAsync("/rag/reindex/nutrition", null, ct);
            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(ct);
                _logger.LogWarning("[AiServiceClient] POST /rag/reindex/nutrition lỗi HTTP {Code}: {Body}",
                    (int)response.StatusCode, errorBody);
                return false;
            }

            _circuitBreaker.RecordSuccess();
            _logger.LogInformation("[AiServiceClient] Đồng bộ tài liệu dinh dưỡng (NUTRITION_DOC) thành công.");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[AiServiceClient] Lỗi khi gọi /rag/reindex/nutrition");
            _circuitBreaker.RecordFailure();
            return false;
        }
    }
}
