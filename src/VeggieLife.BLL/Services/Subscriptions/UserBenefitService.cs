using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using VeggieLife.BLL.Common.Constants;
using VeggieLife.BLL.DTOs.Subscriptions.Responses;
using VeggieLife.BLL.Services.Cache;
using VeggieLife.BLL.Services.Settings;
using VeggieLife.DAL.Context;

namespace VeggieLife.BLL.Services.Subscriptions;

/// <summary>
/// Dịch vụ phân giải quyền lợi và hạn mức người dùng (Dynamic Entitlement & Grandfathering Resolver).
/// Đảm bảo người dùng hưởng đúng cam kết biên lai tại thời điểm mua đến hết chu kỳ VIP (BR-PAY-SUB-01).
/// </summary>
public class UserBenefitService : IUserBenefitService
{
    private readonly AppDbContext _db;
    private readonly IRedisService _redis;
    private readonly ISystemSettingService _settings;
    private readonly ILogger<UserBenefitService> _logger;

    public UserBenefitService(
        AppDbContext db,
        IRedisService redis,
        ISystemSettingService settings,
        ILogger<UserBenefitService> logger)
    {
        _db = db;
        _redis = redis;
        _settings = settings;
        _logger = logger;
    }

    public async Task<VipBenefitFeaturesDto> GetUserBenefitsAsync(
        int userId,
        string? userRole = null,
        CancellationToken ct = default)
    {
        if (userId <= 0)
        {
            return await GetDefaultFreeBenefitsAsync(ct);
        }

        var cacheKey = $"user_benefits:{userId}";
        try
        {
            var cached = await _redis.GetAsync<VipBenefitFeaturesDto>(cacheKey, ct);
            if (cached != null)
            {
                return cached;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[GetUserBenefitsAsync] Không thể đọc cache Redis cho user {UserId}", userId);
        }

        // Xác định role người dùng nếu chưa có
        var role = userRole;
        if (string.IsNullOrWhiteSpace(role))
        {
            role = await _db.Users
                .AsNoTracking()
                .Where(u => u.UserId == userId)
                .Select(u => u.Role)
                .FirstOrDefaultAsync(ct);
        }

        var isVipRole = string.Equals(role, UserRoles.Vip, StringComparison.OrdinalIgnoreCase);

        VipBenefitFeaturesDto result;
        if (!isVipRole)
        {
            result = await GetDefaultFreeBenefitsAsync(ct);
        }
        else
        {
            result = await ResolveVipBenefitsFromSnapshotAsync(userId, ct);
        }

        try
        {
            // Cache 10 phút để tránh query DB liên tục khi chat hay đổi món
            await _redis.SetAsync(cacheKey, result, TimeSpan.FromMinutes(10), ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[GetUserBenefitsAsync] Không thể ghi cache Redis cho user {UserId}", userId);
        }

        return result;
    }

    public async Task InvalidateUserBenefitsCacheAsync(int userId, CancellationToken ct = default)
    {
        if (userId <= 0) return;
        try
        {
            await _redis.RemoveAsync($"user_benefits:{userId}", ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[InvalidateUserBenefitsCacheAsync] Không thể xóa cache Redis cho user {UserId}", userId);
        }
    }

    private async Task<VipBenefitFeaturesDto> GetDefaultFreeBenefitsAsync(CancellationToken ct)
    {
        var chatLimit = await _settings.GetIntSettingAsync("CHAT_LIMIT_DAILY_FREE", 20, ct);
        var mealLimit = await _settings.GetIntSettingAsync("MEAL_PLAN_WEEKLY_LIMIT_FREE", 1, ct);
        var fridgeLimit = await _settings.GetIntSettingAsync("FRIDGE_SCAN_LIMIT_DAILY_FREE", 10, ct);

        return new VipBenefitFeaturesDto
        {
            IsVip = false,
            PlanVersion = 0,
            PlanName = "Gói Cơ Bản (Free)",
            ChatLimitDaily = chatLimit,
            MealPlanLimitWeekly = mealLimit,
            FridgeScanLimitDaily = fridgeLimit,
            ExclusiveBadge = false,
            AdFree = false,
            PriorityAiResponse = false,
            VipDietitianAi = false,
            VideoToRecipeStt = false,
            Description = "Gói miễn phí dành cho thành viên VeggieLife."
        };
    }

    private async Task<VipBenefitFeaturesDto> ResolveVipBenefitsFromSnapshotAsync(int userId, CancellationToken ct)
    {
        var now = DateTime.UtcNow;

        // Truy vấn giao dịch VIP đang có hiệu lực gần nhất của người dùng
        var activeTx = await _db.PaymentTransactions
            .AsNoTracking()
            .Include(pt => pt.Plan)
            .Where(pt => pt.UserId == userId &&
                         (pt.TransactionStatus == "SUCCESS" || pt.TransactionStatus == "COMPLETED") &&
                         pt.StartsAt <= now && now < pt.EndsAt)
            .OrderByDescending(pt => pt.EndsAt)
            .FirstOrDefaultAsync(ct);

        // Giá trị dự phòng từ SystemSettings nếu thiếu thuộc tính
        var defaultVipChat = await _settings.GetIntSettingAsync("CHAT_LIMIT_DAILY_VIP", 200, ct);
        var defaultVipMeal = await _settings.GetIntSettingAsync("MEAL_PLAN_WEEKLY_LIMIT_VIP", 3, ct);
        var defaultVipFridge = await _settings.GetIntSettingAsync("FRIDGE_SCAN_LIMIT_DAILY_VIP", 30, ct);

        if (activeTx == null)
        {
            // Trường hợp tài khoản mang Role VIP nhưng không tìm thấy giao dịch active -> Fallback SystemSettings VIP
            return new VipBenefitFeaturesDto
            {
                IsVip = true,
                PlanVersion = 1,
                PlanName = "Gói VIP (Mặc định)",
                ChatLimitDaily = defaultVipChat,
                MealPlanLimitWeekly = defaultVipMeal,
                FridgeScanLimitDaily = defaultVipFridge,
                ExclusiveBadge = true,
                AdFree = true,
                PriorityAiResponse = true,
                VipDietitianAi = true,
                VideoToRecipeStt = true,
                Description = "Đặc quyền gói VIP VeggieLife."
            };
        }

        var planName = activeTx.SnapshotPlanName ?? activeTx.Plan?.PlanName ?? "Gói VIP";
        var planVersion = activeTx.PlanVersion > 0 ? activeTx.PlanVersion : (activeTx.Plan?.Version ?? 1);
        var featuresJson = activeTx.SnapshotFeaturesJson ?? activeTx.Plan?.FeaturesJson;

        var benefit = new VipBenefitFeaturesDto
        {
            IsVip = true,
            PlanVersion = planVersion,
            PlanName = planName,
            ChatLimitDaily = defaultVipChat,
            MealPlanLimitWeekly = defaultVipMeal,
            FridgeScanLimitDaily = defaultVipFridge,
            ExclusiveBadge = true,
            AdFree = true,
            PriorityAiResponse = true,
            VipDietitianAi = true,
            VideoToRecipeStt = false,
            Description = $"Gói {planName} (Version {planVersion})"
        };

        if (string.IsNullOrWhiteSpace(featuresJson))
        {
            return benefit;
        }

        try
        {
            using var doc = JsonDocument.Parse(featuresJson);
            var root = doc.RootElement;

            if (TryGetInt(root, "chatLimitDaily", out var chatVal) || TryGetInt(root, "chat_limit_daily", out chatVal))
            {
                benefit.ChatLimitDaily = chatVal;
            }

            if (TryGetInt(root, "mealPlanLimitWeekly", out var mealVal) || TryGetInt(root, "meal_plan_limit_weekly", out mealVal))
            {
                benefit.MealPlanLimitWeekly = mealVal;
            }

            if (TryGetInt(root, "fridgeScanLimitDaily", out var fridgeVal) || TryGetInt(root, "fridge_scan_limit_daily", out fridgeVal))
            {
                benefit.FridgeScanLimitDaily = fridgeVal;
            }

            if (TryGetBool(root, "exclusiveBadge", out var badgeVal) || TryGetBool(root, "exclusive_badge", out badgeVal))
            {
                benefit.ExclusiveBadge = badgeVal;
            }

            if (TryGetBool(root, "adFree", out var adVal) || TryGetBool(root, "ad_free", out adVal))
            {
                benefit.AdFree = adVal;
            }

            if (TryGetBool(root, "priorityAiResponse", out var priorityVal) || TryGetBool(root, "priority_ai_response", out priorityVal))
            {
                benefit.PriorityAiResponse = priorityVal;
            }

            if (TryGetBool(root, "vipDietitianAi", out var dietitianVal) || TryGetBool(root, "vip_dietitian_ai", out dietitianVal))
            {
                benefit.VipDietitianAi = dietitianVal;
            }

            if (TryGetBool(root, "videoToRecipeStt", out var sttVal) || TryGetBool(root, "video_to_recipe_stt", out sttVal))
            {
                benefit.VideoToRecipeStt = sttVal;
            }

            if (TryGetString(root, "description", out var descVal))
            {
                benefit.Description = descVal;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[ResolveVipBenefitsFromSnapshotAsync] Không thể parse Features_JSON cho giao dịch #{TransactionId}", activeTx.TransactionId);
        }

        return benefit;
    }

    private static bool TryGetInt(JsonElement root, string propName, out int value)
    {
        foreach (var prop in root.EnumerateObject())
        {
            if (string.Equals(prop.Name, propName, StringComparison.OrdinalIgnoreCase))
            {
                if (prop.Value.ValueKind == JsonValueKind.Number && prop.Value.TryGetInt32(out value))
                {
                    return true;
                }
            }
        }
        value = 0;
        return false;
    }

    private static bool TryGetBool(JsonElement root, string propName, out bool value)
    {
        foreach (var prop in root.EnumerateObject())
        {
            if (string.Equals(prop.Name, propName, StringComparison.OrdinalIgnoreCase))
            {
                if (prop.Value.ValueKind == JsonValueKind.True || prop.Value.ValueKind == JsonValueKind.False)
                {
                    value = prop.Value.GetBoolean();
                    return true;
                }
            }
        }
        value = false;
        return false;
    }

    private static bool TryGetString(JsonElement root, string propName, out string? value)
    {
        foreach (var prop in root.EnumerateObject())
        {
            if (string.Equals(prop.Name, propName, StringComparison.OrdinalIgnoreCase))
            {
                if (prop.Value.ValueKind == JsonValueKind.String)
                {
                    value = prop.Value.GetString();
                    return true;
                }
            }
        }
        value = null;
        return false;
    }
}
