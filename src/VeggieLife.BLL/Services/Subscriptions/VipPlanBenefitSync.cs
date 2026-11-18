using System;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using VeggieLife.BLL.Common.Constants;
using VeggieLife.DAL.Context;
using VeggieLife.DAL.Entities;

namespace VeggieLife.BLL.Services.Subscriptions;

/// <summary>
/// Đồng bộ ba hạn mức VIP từ SystemSettings vào Features_JSON của các gói (D121 Q1-B).
/// SystemSettings là nguồn sự thật; gói đổi hạn mức thì tăng Version, người mua trước giữ snapshot trên giao dịch.
/// </summary>
public static class VipPlanBenefitSync
{
    /// <summary>
    /// Ba hạn mức VIP đang cấu hình, đọc thẳng bảng SystemSettings (trong cùng DbContext, thấy cả giá trị vừa sửa chưa lưu).
    /// </summary>
    public static async Task<(int Chat, int Meal, int Fridge, int Barcode)> ReadVipQuotasAsync(AppDbContext db, CancellationToken ct)
    {
        var keys = new[] { VipBenefitJson.ChatSettingKey, VipBenefitJson.MealSettingKey, VipBenefitJson.FridgeSettingKey, VipBenefitJson.BarcodeSettingKey };
        var tracked = db.SystemSettings.Local.Where(s => keys.Contains(s.SettingKey)).ToList();
        var stored = await db.SystemSettings
            .AsNoTracking()
            .Where(s => keys.Contains(s.SettingKey))
            .ToListAsync(ct);

        int Read(string key, int fallback)
        {
            var value = tracked.FirstOrDefault(s => s.SettingKey == key)?.SettingValue
                ?? stored.FirstOrDefault(s => s.SettingKey == key)?.SettingValue;
            return int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed) ? parsed : fallback;
        }

        return (
            Read(VipBenefitJson.ChatSettingKey, VipBenefitJson.DefaultChat),
            Read(VipBenefitJson.MealSettingKey, VipBenefitJson.DefaultMeal),
            Read(VipBenefitJson.FridgeSettingKey, VipBenefitJson.DefaultFridge),
            Read(VipBenefitJson.BarcodeSettingKey, VipBenefitJson.DefaultBarcode));
    }

    /// <summary>
    /// Ghi giá trị mới của một khóa SystemSettings VIP vào mọi gói (cả gói đang ẩn) có hạn mức khác giá trị này:
    /// cập nhật Features_JSON, tăng Version và thêm AuditLog UPDATE_VIP_PLAN. Chỉ thêm thay đổi vào DbContext,
    /// người gọi lưu cùng bản ghi SystemSettings trong một lần SaveChanges. Trả số gói đã lên phiên bản mới.
    /// </summary>
    public static async Task<int> SyncSettingToPlansAsync(
        AppDbContext db,
        string settingKey,
        string settingValue,
        int adminId,
        DateTime now,
        CancellationToken ct)
    {
        var quotaKey = VipBenefitJson.QuotaKeyForSetting(settingKey);
        if (quotaKey == null ||
            !int.TryParse(settingValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out var newValue))
        {
            return 0;
        }

        var plans = await db.SubscriptionPlans
            .IgnoreQueryFilters()
            .ToListAsync(ct);

        var synced = 0;
        foreach (var plan in plans)
        {
            var oldQuota = VipBenefitJson.ReadQuota(plan.FeaturesJson, quotaKey);
            if (oldQuota == newValue) continue;

            var oldValue = SerializePlan(plan);
            plan.FeaturesJson = VipBenefitJson.SetQuota(plan.FeaturesJson, quotaKey, newValue);
            plan.Version += 1;
            plan.UpdatedAt = now;

            db.AuditLogs.Add(new AuditLog
            {
                ActorUserId = adminId,
                ActorRole = UserRoles.Admin,
                Action = "UPDATE_VIP_PLAN",
                TargetTable = "SubscriptionPlans",
                TargetId = plan.PlanId.ToString(CultureInfo.InvariantCulture),
                OldValue = oldValue,
                NewValue = SerializePlan(plan),
                Note = $"Đồng bộ từ SystemSettings {settingKey}: {oldQuota?.ToString(CultureInfo.InvariantCulture) ?? "(chưa có)"} → {newValue}. Gói {plan.PlanName} lên Version {plan.Version}.",
                CreatedAt = now
            });
            synced++;
        }

        return synced;
    }

    private static string SerializePlan(SubscriptionPlan plan) => JsonSerializer.Serialize(new
    {
        version = plan.Version,
        planName = plan.PlanName,
        durationDays = plan.DurationDays,
        price = (long)plan.Price,
        features = plan.FeaturesJson,
        isActive = plan.IsActive
    });
}
