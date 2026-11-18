using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using VeggieLife.BLL.Common.Responses;
using VeggieLife.BLL.Common.Utils;
using VeggieLife.BLL.DTOs.Admin.AuditLogs;
using VeggieLife.BLL.Validators.Admin;
using VeggieLife.DAL.Context;
using VeggieLife.DAL.Entities;

namespace VeggieLife.BLL.Services.Admin;

/// <summary>
/// Triển khai dịch vụ tra cứu nhật ký kiểm toán cho Quản trị viên (FN-ADM-17).
/// Tuân thủ:
/// - BR-ADM-SYS-002: Nhật ký chỉ ghi (Append-only); cấm sửa/xóa; dữ liệu nhạy cảm được che giấu.
/// - Hiệu năng: Đạt &lt; 800 ms với 100.000 bản ghi nhờ AsNoTracking, chỉ số index và phân trang tối ưu.
/// </summary>
public class AdminAuditLogService : IAdminAuditLogService
{
    private readonly AppDbContext _db;
    private readonly ILogger<AdminAuditLogService> _logger;

    public AdminAuditLogService(AppDbContext db, ILogger<AdminAuditLogService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<(IReadOnlyList<AdminAuditLogListItemDto> Logs, PagedMeta Meta)> GetAuditLogsAsync(
        AdminAuditLogFilterRequest filter,
        CancellationToken ct = default)
    {
        var (fromDate, toDate) = AdminAuditLogValidator.ValidateFilter(filter);

        IQueryable<AuditLog> query = _db.AuditLogs.AsNoTracking();

        if (filter.ActorUserId.HasValue)
        {
            query = query.Where(a => a.ActorUserId == filter.ActorUserId.Value);
        }

        if (!string.IsNullOrWhiteSpace(filter.Action))
        {
            var actionTrimmed = filter.Action.Trim();
            query = query.Where(a => a.Action == actionTrimmed);
        }

        if (!string.IsNullOrWhiteSpace(filter.TargetTable))
        {
            var targetTableTrimmed = filter.TargetTable.Trim();
            query = query.Where(a => a.TargetTable == targetTableTrimmed);
        }

        if (!string.IsNullOrWhiteSpace(filter.TargetId))
        {
            var targetIdTrimmed = filter.TargetId.Trim();
            query = query.Where(a => a.TargetId == targetIdTrimmed);
        }

        var vietnamOffset = TimeSpan.FromHours(7);

        if (fromDate.HasValue)
        {
            var fromUtc = fromDate.Value.Date - vietnamOffset;
            query = query.Where(a => a.CreatedAt >= fromUtc);
        }

        if (toDate.HasValue)
        {
            var toUtc = toDate.Value.Date.AddDays(1) - vietnamOffset;
            query = query.Where(a => a.CreatedAt < toUtc);
        }

        var total = await query.CountAsync(ct);

        if (total == 0)
        {
            return (Array.Empty<AdminAuditLogListItemDto>(), new PagedMeta
            {
                Page = filter.Page,
                Size = filter.Size,
                Total = 0
            });
        }

        var skip = (filter.Page - 1) * filter.Size;
        var pagedEntities = await query
            .Include(a => a.ActorUser)
            .OrderByDescending(a => a.CreatedAt)
            .ThenByDescending(a => a.AuditId)
            .Skip(skip)
            .Take(filter.Size)
            .ToListAsync(ct);

        var targetMap = await ResolveTargetsAsync(pagedEntities, ct);

        var items = pagedEntities.Select(a =>
        {
            var targetKey = $"{a.TargetTable}:{a.TargetId}";
            targetMap.TryGetValue(targetKey, out var targetInfo);

            return new AdminAuditLogListItemDto
            {
                AuditId = a.AuditId,
                ActorUserId = a.ActorUserId,
                ActorName = a.ActorUser?.FullName,
                ActorAvatarUrl = a.ActorUser?.AvatarUrl,
                ActorRole = a.ActorRole,
                Action = a.Action,
                TargetTable = a.TargetTable,
                TargetId = a.TargetId,
                TargetName = targetInfo.Name,
                TargetAvatarUrl = targetInfo.AvatarUrl,
                Note = a.Note,
                CreatedAt = a.CreatedAt,
                OldValue = AuditLogSanitizer.MaskSensitiveData(a.OldValue),
                NewValue = AuditLogSanitizer.MaskSensitiveData(a.NewValue)
            };
        }).ToList();

        return (items, new PagedMeta
        {
            Page = filter.Page,
            Size = filter.Size,
            Total = total
        });
    }

    private async Task<Dictionary<string, (string? Name, string? AvatarUrl)>> ResolveTargetsAsync(
        IReadOnlyList<AuditLog> logs,
        CancellationToken ct)
    {
        var targetMap = new Dictionary<string, (string? Name, string? AvatarUrl)>(StringComparer.OrdinalIgnoreCase);

        // 1. Users
        var userIds = logs
            .Where(a => string.Equals(a.TargetTable, "Users", StringComparison.OrdinalIgnoreCase) && int.TryParse(a.TargetId, out _))
            .Select(a => int.Parse(a.TargetId))
            .Distinct()
            .ToList();

        if (userIds.Count > 0)
        {
            var users = await _db.Users.AsNoTracking()
                .Where(u => userIds.Contains(u.UserId))
                .Select(u => new { u.UserId, u.FullName, u.AvatarUrl })
                .ToListAsync(ct);

            foreach (var u in users)
            {
                targetMap[$"Users:{u.UserId}"] = (u.FullName, u.AvatarUrl);
            }
        }

        // 2. Posts
        var postIds = logs
            .Where(a => string.Equals(a.TargetTable, "Posts", StringComparison.OrdinalIgnoreCase) && int.TryParse(a.TargetId, out _))
            .Select(a => int.Parse(a.TargetId))
            .Distinct()
            .ToList();

        if (postIds.Count > 0)
        {
            var posts = await _db.Posts.AsNoTracking()
                .Where(p => postIds.Contains(p.PostId))
                .Select(p => new { p.PostId, p.Title, p.ImageUrl })
                .ToListAsync(ct);

            foreach (var p in posts)
            {
                targetMap[$"Posts:{p.PostId}"] = (p.Title, p.ImageUrl);
            }
        }

        // 3. Recipes
        var recipeIds = logs
            .Where(a => string.Equals(a.TargetTable, "Recipes", StringComparison.OrdinalIgnoreCase) && int.TryParse(a.TargetId, out _))
            .Select(a => int.Parse(a.TargetId))
            .Distinct()
            .ToList();

        if (recipeIds.Count > 0)
        {
            var recipes = await _db.Recipes.AsNoTracking()
                .Where(r => recipeIds.Contains(r.RecipeId))
                .Select(r => new { r.RecipeId, r.Title, r.ImageUrl })
                .ToListAsync(ct);

            foreach (var r in recipes)
            {
                targetMap[$"Recipes:{r.RecipeId}"] = (r.Title, r.ImageUrl);
            }
        }

        // 4. Products
        var productIds = logs
            .Where(a => string.Equals(a.TargetTable, "Products", StringComparison.OrdinalIgnoreCase) && int.TryParse(a.TargetId, out _))
            .Select(a => int.Parse(a.TargetId))
            .Distinct()
            .ToList();

        if (productIds.Count > 0)
        {
            var products = await _db.Products.AsNoTracking()
                .Where(p => productIds.Contains(p.ProductId))
                .Select(p => new { p.ProductId, p.ProductName, p.ImageUrl })
                .ToListAsync(ct);

            foreach (var p in products)
            {
                targetMap[$"Products:{p.ProductId}"] = (p.ProductName, p.ImageUrl);
            }
        }

        // 5. SubscriptionPlans
        var planIds = logs
            .Where(a => string.Equals(a.TargetTable, "SubscriptionPlans", StringComparison.OrdinalIgnoreCase) && int.TryParse(a.TargetId, out _))
            .Select(a => int.Parse(a.TargetId))
            .Distinct()
            .ToList();

        if (planIds.Count > 0)
        {
            var plans = await _db.SubscriptionPlans.AsNoTracking()
                .Where(p => planIds.Contains(p.PlanId))
                .Select(p => new { p.PlanId, p.PlanName })
                .ToListAsync(ct);

            foreach (var p in plans)
            {
                targetMap[$"SubscriptionPlans:{p.PlanId}"] = (p.PlanName, null);
            }
        }

        // 6. FoodCategories
        var categoryIds = logs
            .Where(a => string.Equals(a.TargetTable, "FoodCategories", StringComparison.OrdinalIgnoreCase) && int.TryParse(a.TargetId, out _))
            .Select(a => int.Parse(a.TargetId))
            .Distinct()
            .ToList();

        if (categoryIds.Count > 0)
        {
            var categories = await _db.FoodCategories.AsNoTracking()
                .Where(c => categoryIds.Contains(c.CategoryId))
                .Select(c => new { c.CategoryId, c.CategoryName })
                .ToListAsync(ct);

            foreach (var c in categories)
            {
                targetMap[$"FoodCategories:{c.CategoryId}"] = (c.CategoryName, null);
            }
        }

        // 7. SystemSettings
        var settingKeys = logs
            .Where(a => string.Equals(a.TargetTable, "SystemSettings", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(a.TargetId))
            .Select(a => a.TargetId.Trim())
            .Distinct()
            .ToList();

        if (settingKeys.Count > 0)
        {
            var settings = await _db.SystemSettings.AsNoTracking()
                .Where(s => settingKeys.Contains(s.SettingKey))
                .Select(s => new { s.SettingKey, s.Description })
                .ToListAsync(ct);

            foreach (var s in settings)
            {
                var displayName = string.IsNullOrWhiteSpace(s.Description) ? s.SettingKey : s.Description;
                targetMap[$"SystemSettings:{s.SettingKey}"] = (displayName, null);
            }
        }

        // 8. PaymentTransactions
        var transactionIds = logs
            .Where(a => string.Equals(a.TargetTable, "PaymentTransactions", StringComparison.OrdinalIgnoreCase) && long.TryParse(a.TargetId, out _))
            .Select(a => long.Parse(a.TargetId))
            .Distinct()
            .ToList();

        if (transactionIds.Count > 0)
        {
            var transactions = await _db.PaymentTransactions.AsNoTracking()
                .Where(t => transactionIds.Contains(t.TransactionId))
                .Select(t => new { t.TransactionId, t.AmountVnd })
                .ToListAsync(ct);

            foreach (var t in transactions)
            {
                targetMap[$"PaymentTransactions:{t.TransactionId}"] = ($"Giao dịch #{t.TransactionId} ({t.AmountVnd:N0} đ)", null);
            }
        }

        return targetMap;
    }
}
