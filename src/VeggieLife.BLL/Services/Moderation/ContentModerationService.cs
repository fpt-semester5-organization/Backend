using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using VeggieLife.BLL.Common.Constants;
using VeggieLife.BLL.Common.Exceptions;
using VeggieLife.BLL.DTOs.Ai;
using VeggieLife.BLL.Services.AI;

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
            var sanitizedRequest = SanitizeRequest(request);
            var response = await _httpClient.PostAsJsonAsync("/moderation/check", sanitizedRequest, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var statusCode = (int)response.StatusCode;
                var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogWarning("AI Moderation Service phản hồi mã lỗi HTTP {StatusCode}: {Body}", statusCode, errorBody);

                if (statusCode == 400)
                {
                    var detailMsg = "Dữ liệu kiểm duyệt không hợp lệ.";
                    try
                    {
                        using var doc = JsonDocument.Parse(errorBody);
                        if (doc.RootElement.TryGetProperty("message", out var msgProp) && !string.IsNullOrWhiteSpace(msgProp.GetString()))
                        {
                            detailMsg = msgProp.GetString()!;
                        }
                    }
                    catch
                    {
                        // Giữ thông điệp mặc định
                    }

                    throw AppException.BadRequest(ErrorCodes.ErrValidation, detailMsg);
                }

                if (statusCode == 401 || statusCode == 403)
                {
                    _logger.LogError("Xác thực nội bộ với AI Service thất bại (X-Internal-Api-Key).");
                    throw AppException.Forbidden(ErrorCodes.ErrForbidden, "Lỗi xác thực dịch vụ kiểm duyệt nội bộ.");
                }

                // Trường hợp 503 hoặc quá tải: Kích hoạt cơ chế Fallback (Table 7)
                _logger.LogWarning("AI Service gián đoạn ({StatusCode}). Kích hoạt cơ chế Fallback kiểm duyệt nội bộ.", statusCode);
                return await _fallbackService.CheckContentAsync(sanitizedRequest, cancellationToken);
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
    /// Chuẩn hóa DTO trước khi gọi AI Service theo đặc tả VL-SPEC-AI-MOD-01 (Table 3.1 & 4).
    /// </summary>
    private static ModerationCheckRequest SanitizeRequest(ModerationCheckRequest request)
    {
        if (request == null)
        {
            return new ModerationCheckRequest { Title = "Nội dung" };
        }

        var sanitized = new ModerationCheckRequest
        {
            Title = string.IsNullOrWhiteSpace(request.Title) ? "Nội dung" : request.Title.Trim(),
            Description = request.Description?.Trim(),
            Steps = request.Steps != null
                ? request.Steps.Where(s => !string.IsNullOrWhiteSpace(s)).Take(100).ToList()
                : new List<string>(),
            IngredientNames = request.IngredientNames != null
                ? request.IngredientNames.Where(i => !string.IsNullOrWhiteSpace(i)).Take(200).ToList()
                : new List<string>(),
            DietarySchoolCode = request.DietarySchoolCode?.Trim(),
            ImageUrls = new List<string>()
        };

        if (sanitized.Title.Length > 300)
        {
            sanitized.Title = sanitized.Title[..300];
        }

        if (sanitized.Description != null && sanitized.Description.Length > 10000)
        {
            sanitized.Description = sanitized.Description[..10000];
        }

        if (request.ImageUrls != null)
        {
            foreach (var url in request.ImageUrls)
            {
                if (string.IsNullOrWhiteSpace(url)) continue;
                var trimmed = url.Trim();

                // Theo VL-SPEC-AI-MOD-01 mục 3.1 & cơ chế bảo mật chống SSRF của AI-Service:
                // AI-Service chỉ chấp nhận tải ảnh HTTPS từ Cloudinary (res.cloudinary.com) và tối đa 4 ảnh.
                // Các liên kết ảnh từ host khác (như ảnh demo Unsplash từ seed data) không gửi sang AI
                // để tránh lỗi 400 VALIDATION_ERROR làm gián đoạn kiểm duyệt nội dung bài viết/công thức.
                if (Uri.TryCreate(trimmed, UriKind.Absolute, out var uri) &&
                    uri.Scheme.Equals(Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase) &&
                    uri.Host.EndsWith("cloudinary.com", StringComparison.OrdinalIgnoreCase))
                {
                    sanitized.ImageUrls.Add(trimmed);
                }

                if (sanitized.ImageUrls.Count >= 4) break;
            }
        }

        return sanitized;
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
        var isQuarantined = response.ViolationScore >= 0.85 || (!string.IsNullOrWhiteSpace(response.ViolationType) && response.ViolationType != "NONE");

        return new ContentModerationResult
        {
            IsApproved = !isQuarantined,
            ViolationScore = response.ViolationScore * 100.0,
            ModerationStatus = isQuarantined ? "QUARANTINED_HIDDEN" : "APPROVED_ACTIVE",
            ViolationType = isQuarantined ? response.ViolationType : null,
            Note = response.Reasons.Count > 0 ? string.Join("; ", response.Reasons) : "Nội dung an toàn, đạt chuẩn kiểm duyệt.",
            TokensConsumed = response.Usage.PromptTokens + response.Usage.CompletionTokens,
            CostEstimateUsd = AiCostCalculator.Calculate(response.Usage.PromptTokens, response.Usage.CompletionTokens),
            ExecutionTimeMs = response.Usage.LatencyMs,
            Usage = response.Usage,
            Audit = response.Audit
        };
    }
}
