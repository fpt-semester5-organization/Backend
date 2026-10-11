using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using VeggieLife.BLL.DTOs.Ai;

namespace VeggieLife.BLL.Services.Moderation;

/// <summary>
/// Bản giả lập kiểm duyệt nội dung AI (BR-USR-03, BR-ADM-CNT-002, VL-SPEC-AI-MOD-01)
/// dùng trong môi trường phát triển (Development) và kiểm thử (Unit/Integration Tests).
/// </summary>
public class SimulatedContentModerationService : IContentModerationService
{
    private readonly ILogger<SimulatedContentModerationService> _logger;

    private static readonly string[] MeatKeywords = new[]
    {
        "thịt", "meat", "pork", "beef", "chicken", "gà", "bò", "heo", "cá", "tôm", "mực", "tiết canh", "mỡ lợn"
    };

    private static readonly string[] HateKeywords = new[]
    {
        "chửi", "hate", "xúc phạm", "đồ ngu", "phản động", "thù địch", "đánh chết", "lăng mạ", "lũ ngu", "phá nát"
    };

    private static readonly string[] SpamKeywords = new[]
    {
        "spam", "mua ngay", "quảng cáo", "khuyến mãi sốc", "kiếm tiền online", "đầu tư sinh lời"
    };

    public SimulatedContentModerationService(ILogger<SimulatedContentModerationService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Giả lập gọi API /moderation/check theo đặc tả VL-SPEC-AI-MOD-01.
    /// </summary>
    public Task<ModerationCheckResponse> CheckContentAsync(
        ModerationCheckRequest request,
        CancellationToken cancellationToken = default)
    {
        var sw = Stopwatch.StartNew();

        var textBuilder = new List<string>();
        if (!string.IsNullOrWhiteSpace(request.Title)) textBuilder.Add(request.Title);
        if (!string.IsNullOrWhiteSpace(request.Description)) textBuilder.Add(request.Description);
        if (request.IngredientNames != null && request.IngredientNames.Count > 0)
        {
            textBuilder.AddRange(request.IngredientNames);
        }
        if (request.Steps != null && request.Steps.Count > 0)
        {
            textBuilder.AddRange(request.Steps);
        }

        var fullText = string.Join(" ", textBuilder).ToLowerInvariant();

        // 1. Kích hoạt giả lập vi phạm kiểm thử
        if (fullText.Contains("[simulate_violation]") || fullText.Contains("[violation]"))
        {
            sw.Stop();
            _logger.LogWarning("[SimulatedModeration] Gắn cờ vi phạm cưỡng bức từ marker kiểm thử.");
            return Task.FromResult(new ModerationCheckResponse
            {
                ViolationScore = 0.95,
                ViolationType = "VIO_HATE",
                Reasons = new List<string> { "Phát hiện nội dung không phù hợp chuẩn mực cộng đồng (Marker kiểm thử)." },
                Usage = CreateUsage(sw.ElapsedMilliseconds),
                Audit = CreateAudit(request, 0.95, "VIO_HATE")
            });
        }

        // 2. Mâu thuẫn trường phái (VIO_LABEL) kiểm tra trước khi có nhãn trường phái
        if (!string.IsNullOrWhiteSpace(request.DietarySchoolCode))
        {
            var school = request.DietarySchoolCode.Trim().ToUpperInvariant();
            if (school == "VEGAN" && (ContainsWord(fullText, "trứng") || ContainsWord(fullText, "sữa") || ContainsWord(fullText, "mật ong")))
            {
                sw.Stop();
                _logger.LogWarning("[SimulatedModeration] Trường phái VEGAN chứa trứng/sữa/mật ong. Gắn cờ VIO_LABEL.");
                return Task.FromResult(new ModerationCheckResponse
                {
                    ViolationScore = 0.89,
                    ViolationType = "VIO_LABEL",
                    Reasons = new List<string> { "Công thức khai báo nhãn VEGAN nhưng nguyên liệu chứa trứng, sữa hoặc mật ong." },
                    Usage = CreateUsage(sw.ElapsedMilliseconds),
                    Audit = CreateAudit(request, 0.89, "VIO_LABEL")
                });
            }
        }

        // 3. Vi phạm ngôn từ thù ghét / bạo lực (VIO_HATE)
        var detectedHate = HateKeywords.FirstOrDefault(k => fullText.Contains(k));
        if (detectedHate != null)
        {
            sw.Stop();
            _logger.LogWarning("[SimulatedModeration] Phát hiện từ ngữ thù địch '{Keyword}'. Gắn cờ VIO_HATE.", detectedHate);
            return Task.FromResult(new ModerationCheckResponse
            {
                ViolationScore = 0.90,
                ViolationType = "VIO_HATE",
                Reasons = new List<string> { $"Phát hiện ngôn từ thù địch hoặc không phù hợp ({detectedHate})." },
                Usage = CreateUsage(sw.ElapsedMilliseconds),
                Audit = CreateAudit(request, 0.90, "VIO_HATE")
            });
        }

        // 4. Vi phạm spam quảng cáo (VIO_SPAM)
        var detectedSpam = SpamKeywords.FirstOrDefault(k => fullText.Contains(k));
        if (detectedSpam != null)
        {
            sw.Stop();
            _logger.LogWarning("[SimulatedModeration] Phát hiện từ ngữ spam '{Keyword}'. Gắn cờ VIO_SPAM.", detectedSpam);
            return Task.FromResult(new ModerationCheckResponse
            {
                ViolationScore = 0.88,
                ViolationType = "VIO_SPAM",
                Reasons = new List<string> { $"Phát hiện nội dung có dấu hiệu quảng cáo spam ({detectedSpam})." },
                Usage = CreateUsage(sw.ElapsedMilliseconds),
                Audit = CreateAudit(request, 0.88, "VIO_SPAM")
            });
        }

        // 5. Vi phạm thịt động vật / mặn (VIO_MEAT)
        // Chuẩn hóa cụm "trứng gà", "trứng vịt" để không bị nhận nhầm thành thịt gia cầm
        var textForMeatCheck = System.Text.RegularExpressions.Regex.Replace(
            fullText,
            @"(?<!\p{L})trứng\s+(gà|vịt|cút)(?!\p{L})",
            "trứng",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);

        var detectedMeat = MeatKeywords.FirstOrDefault(k => ContainsWord(textForMeatCheck, k));
        if (detectedMeat != null)
        {
            sw.Stop();
            _logger.LogWarning("[SimulatedModeration] Phát hiện từ khóa động vật '{Keyword}'. Gắn cờ VIO_MEAT.", detectedMeat);
            return Task.FromResult(new ModerationCheckResponse
            {
                ViolationScore = 0.92,
                ViolationType = "VIO_MEAT",
                Reasons = new List<string> { $"Nguyên liệu hoặc nội dung chứa '{detectedMeat}', vi phạm quy chuẩn món chay." },
                Usage = CreateUsage(sw.ElapsedMilliseconds),
                Audit = CreateAudit(request, 0.92, "VIO_MEAT")
            });
        }

        // 6. Nội dung an toàn (< 0.85, type = NONE)
        sw.Stop();
        _logger.LogInformation("[SimulatedModeration] Nội dung an toàn, đạt chuẩn kiểm duyệt cộng đồng.");
        return Task.FromResult(new ModerationCheckResponse
        {
            ViolationScore = 0.10,
            ViolationType = "NONE",
            Reasons = new List<string>(),
            Usage = CreateUsage(sw.ElapsedMilliseconds),
            Audit = CreateAudit(request, 0.10, "NONE")
        });
    }

    /// <summary>
    /// Triển khai ModeratePostAsync thông qua CheckContentAsync để đồng bộ kết quả.
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

    private static ModerationUsage CreateUsage(long latencyMs)
    {
        return new ModerationUsage
        {
            Model = "gemini-2.5-flash",
            PromptTokens = 120,
            CompletionTokens = 45,
            LatencyMs = Math.Max(1, (int)latencyMs),
            Temperature = 0.1
        };
    }

    private static ModerationAudit CreateAudit(ModerationCheckRequest request, double score, string type)
    {
        return new ModerationAudit
        {
            Prompt = $"=== KIỂM DUYỆT NỘI DUNG ===\nTiêu đề: {request.Title}\nTrường phái: {request.DietarySchoolCode ?? "Mặc định"}",
            RawResponse = $"{{\"violationScore\": {score:0.00}, \"violationType\": \"{type}\"}}"
        };
    }

    private static bool ContainsWord(string text, string keyword)
    {
        var pattern = $@"(?<!\p{{L}}){System.Text.RegularExpressions.Regex.Escape(keyword)}(?!\p{{L}})";
        return System.Text.RegularExpressions.Regex.IsMatch(text, pattern, System.Text.RegularExpressions.RegexOptions.IgnoreCase);
    }
}
