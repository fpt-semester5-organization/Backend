using System;
using System.Linq;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace VeggieLife.BLL.Services.Subscriptions;

/// <summary>
/// Đọc/ghi ba hạn mức VIP trong Features_JSON của gói và Snapshot_Features_JSON của giao dịch (D121).
/// Khóa camelCase là chuẩn; khóa snake_case vẫn được nhận khi đọc (dữ liệu cũ).
/// </summary>
public static class VipBenefitJson
{
    public const string ChatLimitDaily = "chatLimitDaily";
    public const string MealPlanLimitWeekly = "mealPlanLimitWeekly";
    public const string FridgeScanLimitDaily = "fridgeScanLimitDaily";
    public const string BarcodeScanLimitDaily = "barcodeScanLimitDaily";

    public const string ChatSettingKey = "CHAT_LIMIT_DAILY_VIP";
    public const string MealSettingKey = "MEAL_PLAN_WEEKLY_LIMIT_VIP";
    public const string FridgeSettingKey = "FRIDGE_SCAN_LIMIT_DAILY_VIP";
    public const string BarcodeSettingKey = "BARCODE_SCAN_LIMIT_DAILY_VIP";

    public const int DefaultChat = 200;
    public const int DefaultMeal = 3;
    public const int DefaultFridge = 30;
    public const int DefaultBarcode = 50;

    private static readonly JsonSerializerOptions WriteOptions = new()
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    /// <summary>
    /// Bảo đảm JSON có đủ bốn hạn mức: khóa đã có giữ nguyên, khóa thiếu lấy giá trị truyền vào.
    /// Không thiếu khóa nào thì trả lại đúng chuỗi gốc.
    /// </summary>
    public static string EnsureQuotas(string? json, int chatLimitDaily, int mealPlanLimitWeekly, int fridgeScanLimitDaily, int barcodeScanLimitDaily)
    {
        var root = ParseObject(json);
        var changed = root == null;
        root ??= new JsonObject();

        changed |= AddIfMissing(root, ChatLimitDaily, chatLimitDaily);
        changed |= AddIfMissing(root, MealPlanLimitWeekly, mealPlanLimitWeekly);
        changed |= AddIfMissing(root, FridgeScanLimitDaily, fridgeScanLimitDaily);
        changed |= AddIfMissing(root, BarcodeScanLimitDaily, barcodeScanLimitDaily);

        return changed ? root.ToJsonString(WriteOptions) : json!;
    }

    /// <summary>Khóa camelCase của hạn mức ứng với một khóa SystemSettings VIP; null nếu không phải khóa hạn mức VIP.</summary>
    public static string? QuotaKeyForSetting(string settingKey) => settingKey switch
    {
        ChatSettingKey => ChatLimitDaily,
        MealSettingKey => MealPlanLimitWeekly,
        FridgeSettingKey => FridgeScanLimitDaily,
        BarcodeSettingKey => BarcodeScanLimitDaily,
        _ => null
    };

    /// <summary>Đọc một hạn mức (camelCase hoặc snake_case, không phân biệt hoa thường); null nếu thiếu.</summary>
    public static int? ReadQuota(string? json, string camelKey)
    {
        var root = ParseObject(json);
        if (root == null) return null;
        var snakeKey = ToSnake(camelKey);
        foreach (var p in root)
        {
            if ((string.Equals(p.Key, camelKey, StringComparison.OrdinalIgnoreCase) ||
                 string.Equals(p.Key, snakeKey, StringComparison.OrdinalIgnoreCase)) &&
                p.Value is JsonValue v && v.TryGetValue<int>(out var value))
            {
                return value;
            }
        }
        return null;
    }

    /// <summary>Ghi một hạn mức theo khóa camelCase, bỏ các biến thể khóa cũ để không còn hai giá trị song song.</summary>
    public static string SetQuota(string? json, string camelKey, int value)
    {
        var root = ParseObject(json) ?? new JsonObject();
        var snakeKey = ToSnake(camelKey);
        var variants = root
            .Where(p => string.Equals(p.Key, camelKey, StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(p.Key, snakeKey, StringComparison.OrdinalIgnoreCase))
            .Select(p => p.Key)
            .ToList();
        foreach (var key in variants)
        {
            root.Remove(key);
        }
        root[camelKey] = value;
        return root.ToJsonString(WriteOptions);
    }

    private static string ToSnake(string camelKey) => camelKey switch
    {
        ChatLimitDaily => "chat_limit_daily",
        MealPlanLimitWeekly => "meal_plan_limit_weekly",
        FridgeScanLimitDaily => "fridge_scan_limit_daily",
        BarcodeScanLimitDaily => "barcode_scan_limit_daily",
        _ => camelKey
    };

    private static JsonObject? ParseObject(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return null;
        try
        {
            return JsonNode.Parse(json) as JsonObject;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static bool AddIfMissing(JsonObject root, string camelKey, int value)
    {
        var snakeKey = ToSnake(camelKey);
        var present = root.Any(p =>
            (string.Equals(p.Key, camelKey, StringComparison.OrdinalIgnoreCase) ||
             string.Equals(p.Key, snakeKey, StringComparison.OrdinalIgnoreCase)) &&
            p.Value is JsonValue v && v.TryGetValue<int>(out _));
        if (present) return false;

        root[camelKey] = value;
        return true;
    }
}
