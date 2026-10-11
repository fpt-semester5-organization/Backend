using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using VeggieLife.BLL.Common.Constants;
using VeggieLife.BLL.Common.Exceptions;
using VeggieLife.BLL.DTOs.Ai;

namespace VeggieLife.BLL.Services.Moderation;

/// <summary>
/// Triển khai dịch vụ kiểm duyệt nội dung tự động giao tiếp HTTP REST với veggielife-ai-service.
/// Tuân thủ nghiêm ngặt tài liệu đặc tả VL-SPEC-AI-MOD-01, Decision D118, BR-ADM-CNT-001..003.
/// </summary>
public class ContentModerationService : IContentModerationService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ContentModerationService> _logger;
    private readonly SimulatedContentModerationService _fallbackService;

    public ContentModerationService(
        HttpClient httpClient,
        ILogger<ContentModerationService> logger,
        SimulatedContentModerationService? fallbackService = null)
    {
        _httpClient = httpClient;
        _logger = logger;
        _fallbackService = fallbackService ?? new SimulatedContentModerationService(
            new Microsoft.Extensions.Logging.Abstractions.NullLogger<SimulatedContentModerationService>());
    }

    /// <summary>
    /// Gửi yêu cầu kiểm duyệt sang endpoint POST /moderation/check của AI Service (gemini-2.5-flash).
    /// </summary>
    public async Task<ModerationCheckResponse> CheckContentAsync(
        ModerationCheckRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("/moderation/check", request, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var statusCode = (int)response.StatusCode;
                var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogWarning("AI Moderation Service phản hồi mã lỗi HTTP {StatusCode}: {Body}", statusCode, errorBody);

                if (statusCode == 400)
                {
                    throw AppException.BadRequest(ErrorCodes.ErrValidation, "Dữ liệu kiểm duyệt không hợp lệ.");
                }

                if (statusCode == 401 || statusCode == 403)
                {
                    _logger.LogError("Xác thực nội bộ với AI Service thất bại (X-Internal-Api-Key).");
                    throw AppException.Forbidden(ErrorCodes.ErrForbidden, "Lỗi xác thực dịch vụ kiểm duyệt nội bộ.");
                }

                // Trường hợp 503 hoặc quá tải: Kích hoạt cơ chế Fallback (Table 7)
                _logger.LogWarning("AI Service gián đoạn ({StatusCode}). Kích hoạt cơ chế Fallback kiểm duyệt nội bộ.", statusCode);
                return await _fallbackService.CheckContentAsync(request, cancellationToken);
            }

            var result = await response.Content.ReadFromJsonAsync<ModerationCheckResponse>(cancellationToken: cancellationToken);
            return result ?? new ModerationCheckResponse();
        }
        catch (HttpRequestException ex)
        {
            // Trường hợp máy chủ AI Service chưa bật hoặc rớt mạng kết nối: Kích hoạt cơ chế Fallback
            _logger.LogWarning(ex, "Không thể kết nối đến máy chủ AI Service ({BaseAddress}). Kích hoạt cơ chế Fallback.", _httpClient.BaseAddress);
            return await _fallbackService.CheckContentAsync(request, cancellationToken);
        }
        catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            // Timeout quá 10 giây (NFR SI-3)
            _logger.LogWarning(ex, "Timeout khi gọi AI Moderation Service. Kích hoạt cơ chế Fallback.");
            return await _fallbackService.CheckContentAsync(request, cancellationToken);
        }
    }

    /// <summary>
    /// Kiểm duyệt nội dung bài viết và chuyển đổi sang ContentModerationResult.
    /// </summary>
    public async Task<ContentModerationResult> ModeratePostAsync(
        string title,
        string content,
        string? imageUrl = null,
        CancellationToken ct = default)
    {
        var req = new ModerationCheckRequest
        {
            Title = title,
            Description = content,
            ImageUrls = !string.IsNullOrWhiteSpace(imageUrl) ? new List<string> { imageUrl } : new List<string>()
        };

        var response = await CheckContentAsync(req, ct);
        var isQuarantined = response.ViolationScore >= 0.85 || response.ViolationType != "NONE";

        return new ContentModerationResult
        {
            IsApproved = !isQuarantined,
            ViolationScore = response.ViolationScore * 100.0,
            ModerationStatus = isQuarantined ? "QUARANTINED_HIDDEN" : "APPROVED_ACTIVE",
            ViolationType = isQuarantined ? response.ViolationType : null,
            Note = response.Reasons.Count > 0 ? string.Join("; ", response.Reasons) : "Nội dung an toàn, đạt chuẩn kiểm duyệt.",
            TokensConsumed = response.Usage.PromptTokens + response.Usage.CompletionTokens,
            CostEstimateUsd = 0.0001m,
            ExecutionTimeMs = response.Usage.LatencyMs,
            Usage = response.Usage,
            Audit = response.Audit
        };
    }
}
