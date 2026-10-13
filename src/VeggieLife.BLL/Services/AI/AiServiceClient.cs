using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using VeggieLife.BLL.Common.Constants;
using VeggieLife.BLL.Common.Exceptions;
using VeggieLife.BLL.DTOs.Ai;

namespace VeggieLife.BLL.Services.AI;

/// <summary>
/// Triển khai client giao tiếp HTTP REST với AI Microservice qua endpoint POST /chat/answer.
/// Xử lý phân tích lỗi Qdrant (503), lỗi ngắt mạch (BR-ADM-AI-002), và lọc nội dung nguy hại (400 - D119).
/// </summary>
public class AiServiceClient : IAiServiceClient
{
    private readonly HttpClient _httpClient;
    private readonly IAiCircuitBreaker _circuitBreaker;
    private readonly ILogger<AiServiceClient> _logger;

    public AiServiceClient(
        HttpClient httpClient,
        IAiCircuitBreaker circuitBreaker,
        ILogger<AiServiceClient> logger)
    {
        _httpClient = httpClient;
        _circuitBreaker = circuitBreaker;
        _logger = logger;
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
                    // Lọc an toàn nội dung câu hỏi (thô tục, nguy hiểm y tế - D119, FN-CHAT-02)
                    throw AppException.BadRequest(
                        ErrorCodes.ErrChatUnsafeContent,
                        "Nội dung vi phạm tiêu chuẩn an toàn (vẫn tính lượt).");
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
}
