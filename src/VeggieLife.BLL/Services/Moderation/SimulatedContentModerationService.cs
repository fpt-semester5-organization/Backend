using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace VeggieLife.BLL.Services.Moderation;

/// <summary>
/// Bản giả lập kiểm duyệt nội dung AI (BR-USR-03, BR-ADM-CNT-002) dùng trong giai đoạn phát triển
/// cho đến khi dịch vụ AI chính thức hoàn thiện.
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
        "chửi", "hate", "xúc phạm", "đồ ngu", "phản động", "thù địch", "đánh chết", "lăng mạ"
    };

    private static readonly string[] SpamKeywords = new[]
    {
        "spam", "mua ngay", "quảng cáo", "khuyến mãi sốc", "kiếm tiền online", "đầu tư sinh lời"
    };

    public SimulatedContentModerationService(ILogger<SimulatedContentModerationService> logger)
    {
        _logger = logger;
    }

    public Task<ContentModerationResult> ModeratePostAsync(
        string title,
        string content,
        string? imageUrl = null,
        CancellationToken ct = default)
    {
        var sw = Stopwatch.StartNew();
        var fullText = $"{title} {content}".ToLowerInvariant();

        // 1. Kiểm tra kích hoạt giả lập cưỡng bức
        if (fullText.Contains("[simulate_violation]") || fullText.Contains("[violation]"))
        {
            sw.Stop();
            _logger.LogWarning("[SimulatedContentModerationService] Gắn cờ vi phạm cưỡng bức từ marker kiểm thử.");
            return Task.FromResult(new ContentModerationResult
            {
                IsApproved = false,
                ViolationScore = 95.0,
                ModerationStatus = "QUARANTINED_HIDDEN",
                ViolationType = "VIO_HATE",
                Note = "Phát hiện nội dung không phù hợp chuẩn mực cộng đồng (Marker kiểm thử).",
                TokensConsumed = 120,
                CostEstimateUsd = 0.0001m,
                ExecutionTimeMs = (int)sw.ElapsedMilliseconds
            });
        }

        // 2. Kiểm tra từ khóa động vật / thịt mặn (VIO_MEAT)
        if (MeatKeywords.Any(k => fullText.Contains(k)))
        {
            sw.Stop();
            var detected = MeatKeywords.First(k => fullText.Contains(k));
            _logger.LogWarning("[SimulatedContentModerationService] Phát hiện từ khóa động vật '{Keyword}'. Gắn cờ VIO_MEAT.", detected);
            return Task.FromResult(new ContentModerationResult
            {
                IsApproved = false,
                ViolationScore = 92.0,
                ModerationStatus = "QUARANTINED_HIDDEN",
                ViolationType = "VIO_MEAT",
                Note = $"Phát hiện nội dung có chứa từ ngữ liên quan đến nguyên liệu động vật ({detected}).",
                TokensConsumed = 150,
                CostEstimateUsd = 0.00015m,
                ExecutionTimeMs = (int)sw.ElapsedMilliseconds
            });
        }

        // 3. Kiểm tra từ ngữ thù địch, xúc phạm (VIO_HATE)
        if (HateKeywords.Any(k => fullText.Contains(k)))
        {
            sw.Stop();
            var detected = HateKeywords.First(k => fullText.Contains(k));
            _logger.LogWarning("[SimulatedContentModerationService] Phát hiện từ ngữ thù địch '{Keyword}'. Gắn cờ VIO_HATE.", detected);
            return Task.FromResult(new ContentModerationResult
            {
                IsApproved = false,
                ViolationScore = 90.0,
                ModerationStatus = "QUARANTINED_HIDDEN",
                ViolationType = "VIO_HATE",
                Note = $"Phát hiện ngôn từ thù địch hoặc không phù hợp ({detected}).",
                TokensConsumed = 130,
                CostEstimateUsd = 0.00012m,
                ExecutionTimeMs = (int)sw.ElapsedMilliseconds
            });
        }

        // 4. Kiểm tra spam quảng cáo (VIO_SPAM)
        if (SpamKeywords.Any(k => fullText.Contains(k)))
        {
            sw.Stop();
            var detected = SpamKeywords.First(k => fullText.Contains(k));
            _logger.LogWarning("[SimulatedContentModerationService] Phát hiện từ ngữ spam '{Keyword}'. Gắn cờ VIO_SPAM.", detected);
            return Task.FromResult(new ContentModerationResult
            {
                IsApproved = false,
                ViolationScore = 88.0,
                ModerationStatus = "QUARANTINED_HIDDEN",
                ViolationType = "VIO_SPAM",
                Note = $"Phát hiện nội dung có dấu hiệu quảng cáo spam ({detected}).",
                TokensConsumed = 110,
                CostEstimateUsd = 0.0001m,
                ExecutionTimeMs = (int)sw.ElapsedMilliseconds
            });
        }

        // 5. Nội dung an toàn và hợp lệ (< 85%) -> Tự động duyệt APPROVED_ACTIVE
        sw.Stop();
        _logger.LogInformation("[SimulatedContentModerationService] Nội dung đạt chuẩn kiểm duyệt an toàn (Điểm vi phạm: 10%).");
        return Task.FromResult(new ContentModerationResult
        {
            IsApproved = true,
            ViolationScore = 10.0,
            ModerationStatus = "APPROVED_ACTIVE",
            ViolationType = null,
            Note = "Nội dung an toàn, đạt chuẩn kiểm duyệt cộng đồng.",
            TokensConsumed = 90,
            CostEstimateUsd = 0.00008m,
            ExecutionTimeMs = Math.Max(1, (int)sw.ElapsedMilliseconds)
        });
    }
}
