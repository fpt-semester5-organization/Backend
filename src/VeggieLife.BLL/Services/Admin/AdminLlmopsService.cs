using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using VeggieLife.BLL.DTOs.Admin.Llmops;
using VeggieLife.BLL.Services.Cache;
using VeggieLife.BLL.Validators.Admin;
using VeggieLife.DAL.Context;

namespace VeggieLife.BLL.Services.Admin;

/// <summary>
/// Triển khai dịch vụ giám sát mức tiêu thụ Token và chi phí AI (LLMOps) cho Quản trị viên (FN-ADM-16).
/// Tuân thủ:
/// - BR-ADM-AI-001: Chi phí tiêu thụ Token tính theo thời gian thực; cảnh báo 80% ngân sách ngày ($10) và hạ tải khi chạm 100% ($12.5).
/// - Redis Caching: Lưu tạm key 'ai_cost_today' với TTL 1 phút.
/// </summary>
public class AdminLlmopsService : IAdminLlmopsService
{
    private readonly AppDbContext _db;
    private readonly IRedisService _redis;
    private readonly IConfiguration _config;
    private readonly ILogger<AdminLlmopsService> _logger;
    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(1);

    public AdminLlmopsService(
        AppDbContext db,
        IRedisService redis,
        IConfiguration config,
        ILogger<AdminLlmopsService> logger)
    {
        _db = db;
        _redis = redis;
        _config = config;
        _logger = logger;
    }

    public async Task<AdminLlmopsDashboardResponseData> GetDashboardAsync(
        AdminLlmopsFilterRequest filter,
        CancellationToken ct = default)
    {
        var (fromDate, toDate, groupBy) = AdminLlmopsValidator.ValidateFilter(filter);

        var cacheKey = $"ai_cost_today:{filter.From ?? "all"}:{filter.To ?? "all"}:{filter.GroupBy ?? "all"}";

        try
        {
            var cached = await _redis.GetAsync<AdminLlmopsDashboardResponseData>(cacheKey, ct);
            if (cached != null)
            {
                return cached;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Lỗi khi đọc cache '{CacheKey}' từ Redis. Chuyển sang truy vấn trực tiếp cơ sở dữ liệu.", cacheKey);
        }

        // 1. Tính toán chi phí ngày hôm nay và hạn mức ngân sách (BR-ADM-AI-001)
        var todayUtc = DateTime.UtcNow.Date;
        var todayCostRaw = await _db.AiquotaLogs
            .AsNoTracking()
            .Where(l => l.CreatedAt >= todayUtc && l.CreatedAt < todayUtc.AddDays(1))
            .SumAsync(l => (decimal?)l.CostEstimateUsd ?? 0m, ct);

        var todayCostUsd = Math.Round(todayCostRaw, 4);

        var dailyBudgetUsd = _config.GetValue<decimal?>("AiBudget:ThrottleUsd")
            ?? _config.GetValue<decimal?>("AiBudget__ThrottleUsd")
            ?? 12.5m;

        var usagePercent = dailyBudgetUsd > 0
            ? (double)Math.Round((todayCostUsd / dailyBudgetUsd) * 100m, 1)
            : 0;

        var alertLevel = usagePercent >= 100
            ? "THROTTLED"
            : (usagePercent >= 80 ? "WARNING" : "NONE");

        // 2. Truy vấn dữ liệu AiquotaLogs theo khoảng thời gian lọc
        var query = _db.AiquotaLogs.AsNoTracking();

        if (fromDate.HasValue)
        {
            var fromUtc = fromDate.Value.Date;
            query = query.Where(l => l.CreatedAt >= fromUtc);
        }

        if (toDate.HasValue)
        {
            var toUtc = toDate.Value.Date.AddDays(1);
            query = query.Where(l => l.CreatedAt < toUtc);
        }

        if (!fromDate.HasValue && !toDate.HasValue)
        {
            var defaultFrom = DateTime.UtcNow.Date.AddDays(-29);
            query = query.Where(l => l.CreatedAt >= defaultFrom);
        }

        var rawLogs = await query
            .Select(l => new
            {
                Date = l.CreatedAt.Date,
                l.FeatureCode,
                l.TokensConsumed,
                l.CostEstimateUsd,
                l.UserId
            })
            .ToListAsync(ct);

        // 3. Chuẩn hóa tính năng và gom nhóm
        List<CostPointDto> series;

        if (string.Equals(groupBy, "DAY", StringComparison.OrdinalIgnoreCase))
        {
            series = rawLogs
                .GroupBy(l => l.Date.ToString("yyyy-MM-dd"))
                .Select(g => new CostPointDto
                {
                    Date = g.Key,
                    Feature = null,
                    Tokens = g.Sum(x => x.TokensConsumed),
                    CostUsd = Math.Round(g.Sum(x => x.CostEstimateUsd), 4)
                })
                .OrderBy(x => x.Date)
                .ToList();
        }
        else if (string.Equals(groupBy, "FEATURE", StringComparison.OrdinalIgnoreCase))
        {
            series = rawLogs
                .GroupBy(l => NormalizeFeatureCode(l.FeatureCode))
                .Select(g => new CostPointDto
                {
                    Date = null,
                    Feature = g.Key,
                    Tokens = g.Sum(x => x.TokensConsumed),
                    CostUsd = Math.Round(g.Sum(x => x.CostEstimateUsd), 4)
                })
                .OrderBy(x => x.Feature)
                .ToList();
        }
        else
        {
            // Mặc định gom nhóm cả 2 chiều: Ngày và Tính năng (phục vụ biểu đồ cột chồng & bảng chi tiết)
            series = rawLogs
                .GroupBy(l => new
                {
                    Date = l.Date.ToString("yyyy-MM-dd"),
                    Feature = NormalizeFeatureCode(l.FeatureCode)
                })
                .Select(g => new CostPointDto
                {
                    Date = g.Key.Date,
                    Feature = g.Key.Feature,
                    Tokens = g.Sum(x => x.TokensConsumed),
                    CostUsd = Math.Round(g.Sum(x => x.CostEstimateUsd), 4)
                })
                .OrderBy(x => x.Date)
                .ThenBy(x => x.Feature)
                .ToList();
        }

        // 4. Xây dựng ma trận chi tiết: cells, dayTotals, featureTotals, total
        var cells = new Dictionary<string, Dictionary<string, CostCellDto>>();
        var dayTotals = new Dictionary<string, CostCellDto>();
        var featureTotals = new Dictionary<string, CostCellDto>();
        var total = new CostCellDto { CostUsd = 0m, Tokens = 0 };

        foreach (var log in rawLogs)
        {
            var dateKey = log.Date.ToString("yyyy-MM-dd");
            var featureKey = NormalizeFeatureCode(log.FeatureCode);
            var cost = Math.Round(log.CostEstimateUsd, 4);
            var tokens = log.TokensConsumed;

            if (!cells.TryGetValue(dateKey, out var dateCellDict))
            {
                dateCellDict = new Dictionary<string, CostCellDto>();
                cells[dateKey] = dateCellDict;
            }

            if (!dateCellDict.TryGetValue(featureKey, out var existingCell))
            {
                dateCellDict[featureKey] = new CostCellDto { CostUsd = cost, Tokens = tokens };
            }
            else
            {
                existingCell.CostUsd = Math.Round(existingCell.CostUsd + cost, 4);
                existingCell.Tokens += tokens;
            }

            if (!dayTotals.TryGetValue(dateKey, out var dayCell))
            {
                dayTotals[dateKey] = new CostCellDto { CostUsd = cost, Tokens = tokens };
            }
            else
            {
                dayCell.CostUsd = Math.Round(dayCell.CostUsd + cost, 4);
                dayCell.Tokens += tokens;
            }

            if (!featureTotals.TryGetValue(featureKey, out var featCell))
            {
                featureTotals[featureKey] = new CostCellDto { CostUsd = cost, Tokens = tokens };
            }
            else
            {
                featCell.CostUsd = Math.Round(featCell.CostUsd + cost, 4);
                featCell.Tokens += tokens;
            }

            total.CostUsd += cost;
            total.Tokens += tokens;
        }

        total.CostUsd = Math.Round(total.CostUsd, 4);
        var maxDayCost = dayTotals.Count > 0 ? dayTotals.Values.Max(c => c.CostUsd) : 0m;
        var dates = dayTotals.Keys.OrderBy(k => k).ToList();
        var features = featureTotals.Keys.ToList();

        // 5. Bóc tách Input/Output Tokens, Phân bổ Hạng hội viên & Top Spenders
        var totalPromptTokens = (long)Math.Round(total.Tokens * 0.68);
        var totalCompletionTokens = total.Tokens - totalPromptTokens;
        var promptCostUsd = Math.Round((totalPromptTokens * 0.075m) / 1_000_000m, 4);
        var completionCostUsd = Math.Round((totalCompletionTokens * 0.300m) / 1_000_000m, 4);

        var userIds = rawLogs.Select(l => l.UserId).Distinct().ToList();
        var users = await _db.Users
            .AsNoTracking()
            .Where(u => userIds.Contains(u.UserId))
            .ToDictionaryAsync(u => u.UserId, ct);

        var tierDist = new UserTierDistributionDto();
        foreach (var log in rawLogs)
        {
            users.TryGetValue(log.UserId, out var user);
            var role = (user?.Role ?? "MEMBER").ToUpperInvariant();
            var cost = Math.Round(log.CostEstimateUsd, 4);
            var tokens = log.TokensConsumed;

            if (role == "VIP")
            {
                tierDist.VipTokens += tokens;
                tierDist.VipCostUsd += cost;
            }
            else if (role == "ADMIN" || role == "SYSTEM")
            {
                tierDist.SystemTokens += tokens;
                tierDist.SystemCostUsd += cost;
            }
            else
            {
                tierDist.MemberTokens += tokens;
                tierDist.MemberCostUsd += cost;
            }
        }
        tierDist.VipCostUsd = Math.Round(tierDist.VipCostUsd, 4);
        tierDist.MemberCostUsd = Math.Round(tierDist.MemberCostUsd, 4);
        tierDist.SystemCostUsd = Math.Round(tierDist.SystemCostUsd, 4);

        var topSpenders = rawLogs
            .Where(l => l.UserId > 0)
            .GroupBy(l => l.UserId)
            .Select(g =>
            {
                users.TryGetValue(g.Key, out var user);
                return new TopSpenderDto
                {
                    UserId = g.Key,
                    FullName = user?.FullName ?? $"Người dùng #{g.Key}",
                    Email = user?.Email ?? "",
                    Role = user?.Role ?? "MEMBER",
                    TotalTokens = g.Sum(x => x.TokensConsumed),
                    TotalCostUsd = Math.Round(g.Sum(x => x.CostEstimateUsd), 4),
                    RequestCount = g.Count()
                };
            })
            .OrderByDescending(x => x.TotalTokens)
            .Take(5)
            .ToList();

        var responseData = new AdminLlmopsDashboardResponseData
        {
            TodayCostUsd = todayCostUsd,
            DailyBudgetUsd = dailyBudgetUsd,
            UsagePercent = usagePercent,
            AlertLevel = alertLevel,
            Series = series,
            Dates = dates,
            Features = features,
            Cells = cells,
            DayTotals = dayTotals,
            FeatureTotals = featureTotals,
            Total = total,
            TotalPromptTokens = totalPromptTokens,
            TotalCompletionTokens = totalCompletionTokens,
            PromptCostUsd = promptCostUsd,
            CompletionCostUsd = completionCostUsd,
            UserTierDistribution = tierDist,
            TopSpenders = topSpenders,
            MaxDayCost = maxDayCost
        };

        try
        {
            await _redis.SetAsync(cacheKey, responseData, CacheTtl, ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Lỗi khi ghi dữ liệu thống kê LLMOps vào Redis '{CacheKey}'.", cacheKey);
        }

        return responseData;
    }

    /// <summary>
    /// Tra cứu chi tiết danh sách nhật ký gọi AI phân trang kèm bộ lọc tính năng và trạng thái.
    /// </summary>
    public async Task<GetAdminLlmopsLogsResponseData> GetLogsAsync(
        AdminLlmopsLogFilterRequest filter,
        CancellationToken ct = default)
    {
        var query = _db.AiquotaLogs.AsNoTracking();

        if (DateTime.TryParse(filter.From, out var fromDate))
        {
            var fromUtc = fromDate.Date;
            query = query.Where(l => l.CreatedAt >= fromUtc);
        }

        if (DateTime.TryParse(filter.To, out var toDate))
        {
            var toUtc = toDate.Date.AddDays(1);
            query = query.Where(l => l.CreatedAt < toUtc);
        }

        if (!string.IsNullOrWhiteSpace(filter.Feature) && !string.Equals(filter.Feature, "ALL", StringComparison.OrdinalIgnoreCase))
        {
            var normFeature = NormalizeFeatureCode(filter.Feature);
            query = query.Where(l => l.FeatureCode == filter.Feature || l.FeatureCode == normFeature);
        }

        if (!string.IsNullOrWhiteSpace(filter.Status) && !string.Equals(filter.Status, "ALL", StringComparison.OrdinalIgnoreCase))
        {
            query = query.Where(l => l.RequestStatus == filter.Status);
        }

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var search = filter.Search.Trim().ToLower();
            var matchingUserIds = await _db.Users
                .AsNoTracking()
                .Where(u => u.FullName.ToLower().Contains(search) || u.Email.ToLower().Contains(search))
                .Select(u => u.UserId)
                .ToListAsync(ct);

            query = query.Where(l => matchingUserIds.Contains(l.UserId) ||
                                     l.FeatureCode.ToLower().Contains(search) ||
                                     (l.ErrorMessage != null && l.ErrorMessage.ToLower().Contains(search)));
        }

        var total = await query.CountAsync(ct);
        var page = Math.Max(1, filter.Page);
        var pageSize = Math.Clamp(filter.PageSize, 1, 100);
        var totalPages = (int)Math.Ceiling(total / (double)pageSize);

        var rawItems = await query
            .OrderByDescending(l => l.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(l => new
            {
                l.LogId,
                l.UserId,
                l.FeatureCode,
                l.TokensConsumed,
                l.CostEstimateUsd,
                l.RequestStatus,
                l.ErrorMessage,
                l.ExecutionTimeMs,
                l.CreatedAt
            })
            .ToListAsync(ct);

        var logUserIds = rawItems.Select(x => x.UserId).Distinct().ToList();
        var logUsers = await _db.Users
            .AsNoTracking()
            .Where(u => logUserIds.Contains(u.UserId))
            .ToDictionaryAsync(u => u.UserId, ct);

        var items = rawItems.Select(l =>
        {
            logUsers.TryGetValue(l.UserId, out var user);
            return new AdminLlmopsLogItemDto
            {
                LogId = l.LogId,
                UserId = l.UserId,
                UserFullName = user?.FullName ?? (l.UserId > 0 ? $"Người dùng #{l.UserId}" : "Khách vãng lai"),
                UserEmail = user?.Email ?? "",
                UserRole = user?.Role ?? "MEMBER",
                FeatureCode = NormalizeFeatureCode(l.FeatureCode),
                TokensConsumed = l.TokensConsumed,
                CostEstimateUsd = Math.Round(l.CostEstimateUsd, 4),
                RequestStatus = l.RequestStatus,
                ErrorMessage = l.ErrorMessage,
                ExecutionTimeMs = l.ExecutionTimeMs,
                CreatedAt = l.CreatedAt
            };
        }).ToList();

        return new GetAdminLlmopsLogsResponseData
        {
            Items = items,
            Total = total,
            Page = page,
            PageSize = pageSize,
            TotalPages = totalPages
        };
    }

    /// <summary>
    /// Chuẩn hóa mã tính năng từ CSDL về các mã định danh chuẩn của hệ thống và giao diện Frontend.
    /// </summary>
    public static string NormalizeFeatureCode(string? rawCode)
    {
        if (string.IsNullOrWhiteSpace(rawCode))
        {
            return "OTHER";
        }

        var upper = rawCode.Trim().ToUpperInvariant();
        return upper switch
        {
            "CHATBOT" or "AI_CHAT" => "AI_CHAT",
            "MEAL_PLAN" or "MEAL_PLAN_GEN" or "MEAL_PLANNER" => "MEAL_PLAN_GEN",
            "FRIDGE_VISION" or "AI_FRIDGE" or "FRIDGE" => "AI_FRIDGE",
            "BARCODE" or "AI_BARCODE" => "AI_BARCODE",
            "STT" or "VIDEO_TO_RECIPE" or "SUMMARIZE_VIDEO" => "SUMMARIZE_VIDEO",
            "MODERATION" or "CONTENT_MODERATION" => "CONTENT_MODERATION",
            "EMBEDDING" or "AI_EMBEDDING" => "AI_EMBEDDING",
            _ => upper
        };
    }
}
