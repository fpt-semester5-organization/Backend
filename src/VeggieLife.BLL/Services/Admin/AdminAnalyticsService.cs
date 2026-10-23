using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using VeggieLife.BLL.DTOs.Admin.Analytics;
using VeggieLife.BLL.Services.Cache;
using VeggieLife.BLL.Validators.Admin;
using VeggieLife.DAL.Context;

namespace VeggieLife.BLL.Services.Admin;

/// <summary>
/// Triển khai dịch vụ thống kê tổng quan nền tảng cho Quản trị viên (FN-ADM-18).
/// Tuân thủ:
/// - BR-ADM-REP-01: Chỉ Admin xem thống kê tăng trưởng, tổng hợp theo ngày, không lộ PII.
/// - BR-ADM-AI-001: Chi phí tiêu thụ Token AI từ AIQuotaLogs.
/// - Caching: Lưu tạm Redis Cache key 'admin_overview' với thời gian sống (TTL) 5 phút.
/// </summary>
public class AdminAnalyticsService : IAdminAnalyticsService
{
    private readonly AppDbContext _db;
    private readonly IRedisService _redis;
    private readonly ILogger<AdminAnalyticsService> _logger;
    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(5);

    public AdminAnalyticsService(
        AppDbContext db,
        IRedisService redis,
        ILogger<AdminAnalyticsService> logger)
    {
        _db = db;
        _redis = redis;
        _logger = logger;
    }

    public async Task<AdminAnalyticsOverviewResponseData> GetOverviewAsync(
        AdminAnalyticsOverviewFilterRequest filter,
        CancellationToken ct = default)
    {
        var (fromDate, toDate) = AdminAnalyticsValidator.ValidateFilter(filter);

        var cacheKey = string.IsNullOrWhiteSpace(filter.From) && string.IsNullOrWhiteSpace(filter.To)
            ? "admin_overview"
            : $"admin_overview:{filter.From ?? "all"}:{filter.To ?? "all"}";

        try
        {
            var cached = await _redis.GetAsync<AdminAnalyticsOverviewResponseData>(cacheKey, ct);
            if (cached != null)
            {
                return cached;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Lỗi khi đọc cache '{CacheKey}' từ Redis. Chuyển sang truy vấn trực tiếp cơ sở dữ liệu.", cacheKey);
        }

        // 1. Thống kê Users: COUNT Users theo ngày tạo và Account_Status
        var totalUsers = await _db.Users.AsNoTracking().CountAsync(ct);

        var usersQuery = _db.Users.AsNoTracking();
        if (fromDate.HasValue)
        {
            var fromUtc = fromDate.Value.Date;
            usersQuery = usersQuery.Where(u => u.CreatedAt >= fromUtc);
        }
        if (toDate.HasValue)
        {
            var toUtc = toDate.Value.Date.AddDays(1);
            usersQuery = usersQuery.Where(u => u.CreatedAt < toUtc);
        }
        if (!fromDate.HasValue && !toDate.HasValue)
        {
            var thirtyDaysAgo = DateTime.UtcNow.AddDays(-30);
            usersQuery = usersQuery.Where(u => u.CreatedAt >= thirtyDaysAgo);
        }
        var newInPeriod = await usersQuery.CountAsync(ct);

        var byStatusList = await _db.Users
            .AsNoTracking()
            .GroupBy(u => u.AccountStatus)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync(ct);
        var byStatusDict = byStatusList.ToDictionary(x => x.Status, x => x.Count);

        // 2. Thống kê Content: COUNT Recipes/Posts/Videos theo trạng thái
        var recipesCount = await _db.Recipes.AsNoTracking().CountAsync(ct);
        var postsCount = await _db.Posts.AsNoTracking().CountAsync(ct);
        var videosCount = await _db.Videos.AsNoTracking().CountAsync(ct);

        // 3. Thống kê VIP: VIP đang hoạt động (Starts_At <= hiện tại < Ends_At) & doanh thu theo kỳ
        var now = DateTime.UtcNow;
        var activeVip = await _db.PaymentTransactions
            .AsNoTracking()
            .Where(t => t.TransactionStatus == "SUCCESS" && t.StartsAt <= now && t.EndsAt > now)
            .Select(t => t.UserId)
            .Distinct()
            .CountAsync(ct);

        var revenueQuery = _db.PaymentTransactions
            .AsNoTracking()
            .Where(t => t.TransactionStatus == "SUCCESS");

        if (fromDate.HasValue)
        {
            var fromUtc = fromDate.Value.Date;
            revenueQuery = revenueQuery.Where(t => (t.CompletedAt ?? t.CreatedAt) >= fromUtc);
        }
        if (toDate.HasValue)
        {
            var toUtc = toDate.Value.Date.AddDays(1);
            revenueQuery = revenueQuery.Where(t => (t.CompletedAt ?? t.CreatedAt) < toUtc);
        }

        var revenueVnd = (long)await revenueQuery.SumAsync(t => (decimal?)t.AmountVnd ?? 0m, ct);

        // 4. Hàng đợi kiểm duyệt: ViolationReports PENDING
        var moderationQueue = await _db.ViolationReports
            .AsNoTracking()
            .CountAsync(r => r.Status == "PENDING", ct);

        // 5. Chi phí AI từ AiquotaLogs
        var aiQuery = _db.AiquotaLogs.AsNoTracking();
        if (fromDate.HasValue)
        {
            var fromUtc = fromDate.Value.Date;
            aiQuery = aiQuery.Where(a => a.CreatedAt >= fromUtc);
        }
        if (toDate.HasValue)
        {
            var toUtc = toDate.Value.Date.AddDays(1);
            aiQuery = aiQuery.Where(a => a.CreatedAt < toUtc);
        }

        var rawAiCost = await aiQuery.SumAsync(a => (decimal?)a.CostEstimateUsd ?? 0m, ct);
        var aiCostUsd = (double)Math.Round(rawAiCost, 2);

        var responseData = new AdminAnalyticsOverviewResponseData
        {
            Users = new UsersOverviewDto
            {
                Total = totalUsers,
                NewInPeriod = newInPeriod,
                ByStatus = byStatusDict
            },
            Content = new ContentOverviewDto
            {
                Recipes = recipesCount,
                Posts = postsCount,
                Videos = videosCount
            },
            Vip = new VipOverviewDto
            {
                Active = activeVip,
                RevenueVnd = revenueVnd
            },
            ModerationQueue = moderationQueue,
            AiCostUsd = aiCostUsd
        };

        try
        {
            await _redis.SetAsync(cacheKey, responseData, CacheTtl, ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Lỗi khi ghi dữ liệu thống kê vào cache Redis '{CacheKey}'.", cacheKey);
        }

        return responseData;
    }
}
