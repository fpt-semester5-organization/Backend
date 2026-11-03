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

        // Múi giờ Việt Nam (UTC+7) chuẩn cho toàn bộ người dùng và hệ thống VeggieLife
        var vietnamOffset = TimeSpan.FromHours(7);
        DateTime? fromUtc = fromDate.HasValue ? fromDate.Value.Date - vietnamOffset : null;
        DateTime? toUtc = null;
        if (toDate.HasValue)
        {
            var toUtcLocal = toDate.Value.Date.AddDays(1) - vietnamOffset;
            var toUtcUtc = toDate.Value.Date.AddDays(1);
            toUtc = toUtcLocal > toUtcUtc ? toUtcLocal : toUtcUtc;
        }

        // 1. Thống kê Users: COUNT Users theo ngày tạo và Account_Status
        var totalUsers = await _db.Users.AsNoTracking().CountAsync(ct);

        var usersQuery = _db.Users.AsNoTracking();
        if (fromUtc.HasValue)
        {
            usersQuery = usersQuery.Where(u => u.CreatedAt >= fromUtc.Value);
        }
        if (toUtc.HasValue)
        {
            usersQuery = usersQuery.Where(u => u.CreatedAt < toUtc.Value);
        }
        if (!fromUtc.HasValue && !toUtc.HasValue)
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

        // 2. Thống kê Content: COUNT Recipes/Posts/Videos tổng và theo khoảng thời gian
        var recipesTotal = await _db.Recipes.AsNoTracking().CountAsync(ct);
        var postsTotal = await _db.Posts.AsNoTracking().CountAsync(ct);
        var videosTotal = await _db.Videos.AsNoTracking().CountAsync(ct);

        var recipesQuery = _db.Recipes.AsNoTracking();
        var postsQuery = _db.Posts.AsNoTracking();
        var videosQuery = _db.Videos.AsNoTracking();

        if (fromUtc.HasValue)
        {
            recipesQuery = recipesQuery.Where(r => r.CreatedAt >= fromUtc.Value);
            postsQuery = postsQuery.Where(p => p.CreatedAt >= fromUtc.Value);
            videosQuery = videosQuery.Where(v => v.CreatedAt >= fromUtc.Value);
        }
        if (toUtc.HasValue)
        {
            recipesQuery = recipesQuery.Where(r => r.CreatedAt < toUtc.Value);
            postsQuery = postsQuery.Where(p => p.CreatedAt < toUtc.Value);
            videosQuery = videosQuery.Where(v => v.CreatedAt < toUtc.Value);
        }
        if (!fromUtc.HasValue && !toUtc.HasValue)
        {
            var thirtyDaysAgo = DateTime.UtcNow.AddDays(-30);
            recipesQuery = recipesQuery.Where(r => r.CreatedAt >= thirtyDaysAgo);
            postsQuery = postsQuery.Where(p => p.CreatedAt >= thirtyDaysAgo);
            videosQuery = videosQuery.Where(v => v.CreatedAt >= thirtyDaysAgo);
        }

        var recipesNew = await recipesQuery.CountAsync(ct);
        var postsNew = await postsQuery.CountAsync(ct);
        var videosNew = await videosQuery.CountAsync(ct);

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

        if (fromUtc.HasValue)
        {
            revenueQuery = revenueQuery.Where(t => (t.CompletedAt ?? t.CreatedAt) >= fromUtc.Value);
        }
        if (toUtc.HasValue)
        {
            revenueQuery = revenueQuery.Where(t => (t.CompletedAt ?? t.CreatedAt) < toUtc.Value);
        }

        var revenueVnd = (long)await revenueQuery.SumAsync(t => (decimal?)t.AmountVnd ?? 0m, ct);

        // 4. Hàng đợi kiểm duyệt: ViolationReports PENDING
        var moderationQueue = await _db.ViolationReports
            .AsNoTracking()
            .CountAsync(r => r.Status == "PENDING", ct);

        // 5. Chi phí AI từ AiquotaLogs
        var aiQuery = _db.AiquotaLogs.AsNoTracking();
        if (fromUtc.HasValue)
        {
            aiQuery = aiQuery.Where(a => a.CreatedAt >= fromUtc.Value);
        }
        if (toUtc.HasValue)
        {
            aiQuery = aiQuery.Where(a => a.CreatedAt < toUtc.Value);
        }

        var rawAiCost = await aiQuery.SumAsync(a => (decimal?)a.CostEstimateUsd ?? 0m, ct);
        var aiCostUsd = (double)Math.Round(rawAiCost, 4);

        // Phân rã nhóm người dùng phục vụ quản trị vận hành: Thành viên thường, VIP và Tài khoản bị đình chỉ
        var suspendedMembers = (byStatusDict.TryGetValue("SUSPENDED", out var susp) ? susp : 0)
            + (byStatusDict.TryGetValue("LOCKED", out var locked) ? locked : 0)
            + (byStatusDict.TryGetValue("BANNED", out var banned) ? banned : 0);
        var activeUsers = byStatusDict.TryGetValue("ACTIVE", out var act) ? act : 0;
        var standardMembers = Math.Max(0, activeUsers - activeVip);

        var responseData = new AdminAnalyticsOverviewResponseData
        {
            Users = new UsersOverviewDto
            {
                Total = totalUsers,
                NewInPeriod = newInPeriod,
                ByStatus = byStatusDict,
                StandardMembers = standardMembers,
                VipMembers = activeVip,
                SuspendedMembers = suspendedMembers
            },
            Content = new ContentOverviewDto
            {
                Recipes = recipesTotal,
                RecipesNewInPeriod = recipesNew,
                Posts = postsTotal,
                PostsNewInPeriod = postsNew,
                Videos = videosTotal,
                VideosNewInPeriod = videosNew
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
