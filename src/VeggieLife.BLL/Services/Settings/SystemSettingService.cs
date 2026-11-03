using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using VeggieLife.BLL.Common.Constants;
using VeggieLife.BLL.Common.Exceptions;
using VeggieLife.BLL.DTOs.Admin.Settings;
using VeggieLife.BLL.Services.Cache;
using VeggieLife.DAL.Context;
using VeggieLife.DAL.Entities;

namespace VeggieLife.BLL.Services.Settings;

/// <summary>
/// Triển khai dịch vụ quản lý cấu hình hệ thống động SystemSettings theo mô hình Redis Cache-Aside.
/// Đảm bảo hiệu năng tra cứu cao nhất cho các nghiệp vụ kiểm tra hạn mức (O(1)) và khả năng tùy biến linh hoạt cho Quản trị viên.
/// </summary>
public class SystemSettingService : ISystemSettingService
{
    private const string CacheKeyPrefix = "sys_setting:";
    private const string CacheKeyLegalDocuments = "sys_setting:legal_documents:all";
    private const string CacheKeyPublicSettings = "sys_setting:public_cache";
    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(30);

    private readonly AppDbContext _db;
    private readonly IRedisService _redis;
    private readonly ILogger<SystemSettingService> _logger;

    public SystemSettingService(
        AppDbContext db,
        IRedisService redis,
        ILogger<SystemSettingService> logger)
    {
        _db = db;
        _redis = redis;
        _logger = logger;
    }

    public async Task<int> GetIntSettingAsync(string key, int defaultValue, CancellationToken ct = default)
    {
        var rawValue = await GetRawSettingValueAsync(key, defaultValue.ToString(CultureInfo.InvariantCulture), ct);
        if (int.TryParse(rawValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed))
        {
            return parsed;
        }

        _logger.LogWarning("Cấu hình '{Key}' có giá trị '{Value}' không thể chuyển đổi sang số nguyên. Dùng mặc định: {Default}", key, rawValue, defaultValue);
        return defaultValue;
    }

    public async Task<decimal> GetDecimalSettingAsync(string key, decimal defaultValue, CancellationToken ct = default)
    {
        var rawValue = await GetRawSettingValueAsync(key, defaultValue.ToString(CultureInfo.InvariantCulture), ct);
        if (decimal.TryParse(rawValue, NumberStyles.Number, CultureInfo.InvariantCulture, out var parsed))
        {
            return parsed;
        }

        _logger.LogWarning("Cấu hình '{Key}' có giá trị '{Value}' không thể chuyển đổi sang số thực. Dùng mặc định: {Default}", key, rawValue, defaultValue);
        return defaultValue;
    }

    public async Task<string> GetStringSettingAsync(string key, string defaultValue, CancellationToken ct = default)
    {
        return await GetRawSettingValueAsync(key, defaultValue, ct);
    }

    public async Task<bool> GetBoolSettingAsync(string key, bool defaultValue, CancellationToken ct = default)
    {
        var rawValue = await GetRawSettingValueAsync(key, defaultValue.ToString(), ct);
        if (bool.TryParse(rawValue, out var parsed))
        {
            return parsed;
        }

        if (string.Equals(rawValue, "1", StringComparison.OrdinalIgnoreCase)) return true;
        if (string.Equals(rawValue, "0", StringComparison.OrdinalIgnoreCase)) return false;

        _logger.LogWarning("Cấu hình '{Key}' có giá trị '{Value}' không thể chuyển đổi sang boolean. Dùng mặc định: {Default}", key, rawValue, defaultValue);
        return defaultValue;
    }

    public async Task<IReadOnlyList<SystemSettingDto>> GetAllSettingsAsync(string? category = null, CancellationToken ct = default)
    {
        var query = _db.SystemSettings.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(category))
        {
            var normalizedCategory = category.Trim().ToUpperInvariant();
            query = query.Where(s => s.Category == normalizedCategory);
        }

        var settings = await query
            .OrderBy(s => s.Category)
            .ThenBy(s => s.SettingKey)
            .Select(s => MapToDto(s))
            .ToListAsync(ct);

        return settings;
    }

    public async Task<SystemSettingDto> GetSettingByKeyAsync(string key, CancellationToken ct = default)
    {
        var setting = await _db.SystemSettings
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.SettingKey == key, ct);

        if (setting == null)
        {
            throw AppException.NotFound(
                ErrorCodes.ErrAdmNotFound,
                $"Không tìm thấy cấu hình hệ thống với khóa '{key}'.");
        }

        return MapToDto(setting);
    }

    public async Task<SystemSettingDto> UpdateSettingAsync(
        string key,
        UpdateSystemSettingRequest request,
        int adminId,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request?.SettingValue))
        {
            throw AppException.BadRequest(
                ErrorCodes.ErrValidation,
                "Giá trị cấu hình không được để trống.");
        }

        var setting = await _db.SystemSettings
            .FirstOrDefaultAsync(s => s.SettingKey == key, ct);

        if (setting == null)
        {
            throw AppException.NotFound(
                ErrorCodes.ErrAdmNotFound,
                $"Không tìm thấy cấu hình hệ thống với khóa '{key}'.");
        }

        var trimmedNewValue = request.SettingValue.Trim();
        ValidateSettingValue(setting.SettingKey, trimmedNewValue, setting.DataType);

        var oldValue = setting.SettingValue;
        setting.SettingValue = trimmedNewValue;
        if (request.LegalDocumentsUrl != null)
        {
            setting.LegalDocumentsUrl = request.LegalDocumentsUrl.Trim();
        }
        setting.UpdatedAt = DateTime.UtcNow;
        setting.UpdatedBy = adminId;

        // Ghi vết nhật ký kiểm toán quản trị (AuditLog)
        _db.AuditLogs.Add(new AuditLog
        {
            ActorUserId = adminId,
            ActorRole = "ADMIN",
            Action = "UPDATE_SYSTEM_SETTING",
            TargetTable = "SystemSettings",
            TargetId = key,
            OldValue = JsonSerializer.Serialize(new { value = oldValue }),
            NewValue = JsonSerializer.Serialize(new { value = trimmedNewValue }),
            Note = $"Cập nhật tham số {key} từ '{oldValue}' sang '{trimmedNewValue}'",
            CreatedAt = DateTime.UtcNow
        });

        await _db.SaveChangesAsync(ct);

        // Đồng bộ cập nhật Redis Cache
        var cacheKey = CacheKeyPrefix + key;
        try
        {
            await _redis.SetAsync(cacheKey, trimmedNewValue, CacheTtl, ct);
            await _redis.RemoveAsync(CacheKeyPublicSettings, ct);
            await _redis.RemoveAsync("plans_cache", ct);
            if (key.StartsWith("LEGAL_", StringComparison.OrdinalIgnoreCase))
            {
                await _redis.RemoveAsync(CacheKeyLegalDocuments, ct);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Không thể cập nhật Redis cache cho khóa '{CacheKey}'", cacheKey);
        }

        return MapToDto(setting);
    }

    public async Task<SystemSettingDto> ResetToDefaultAsync(string key, int adminId, CancellationToken ct = default)
    {
        var setting = await _db.SystemSettings
            .FirstOrDefaultAsync(s => s.SettingKey == key, ct);

        if (setting == null)
        {
            throw AppException.NotFound(
                ErrorCodes.ErrAdmNotFound,
                $"Không tìm thấy cấu hình hệ thống với khóa '{key}'.");
        }

        var oldValue = setting.SettingValue;
        setting.SettingValue = setting.DefaultValue;
        setting.UpdatedAt = DateTime.UtcNow;
        setting.UpdatedBy = adminId;

        _db.AuditLogs.Add(new AuditLog
        {
            ActorUserId = adminId,
            ActorRole = "ADMIN",
            Action = "RESET_SYSTEM_SETTING",
            TargetTable = "SystemSettings",
            TargetId = key,
            OldValue = JsonSerializer.Serialize(new { value = oldValue }),
            NewValue = JsonSerializer.Serialize(new { value = setting.DefaultValue }),
            Note = $"Khôi phục tham số {key} về giá trị mặc định '{setting.DefaultValue}'",
            CreatedAt = DateTime.UtcNow
        });

        await _db.SaveChangesAsync(ct);

        var cacheKey = CacheKeyPrefix + key;
        try
        {
            await _redis.SetAsync(cacheKey, setting.DefaultValue, CacheTtl, ct);
            await _redis.RemoveAsync(CacheKeyPublicSettings, ct);
            await _redis.RemoveAsync("plans_cache", ct);
            if (key.StartsWith("LEGAL_", StringComparison.OrdinalIgnoreCase))
            {
                await _redis.RemoveAsync(CacheKeyLegalDocuments, ct);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Không thể cập nhật Redis cache cho khóa '{CacheKey}'", cacheKey);
        }

        return MapToDto(setting);
    }

    private async Task<string> GetRawSettingValueAsync(string key, string fallbackValue, CancellationToken ct)
    {
        var cacheKey = CacheKeyPrefix + key;

        // 1. Kiểm tra bộ nhớ đệm Redis (Cache-Aside)
        try
        {
            var cached = await _redis.GetAsync<string>(cacheKey, ct);
            if (!string.IsNullOrEmpty(cached))
            {
                return cached;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Lỗi đọc Redis cache cho cấu hình '{Key}'. Tiến hành đọc từ Database.", key);
        }

        // 2. Truy vấn Cơ sở dữ liệu nếu Cache miss
        try
        {
            var setting = await _db.SystemSettings
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.SettingKey == key, ct);

            if (setting != null)
            {
                // Nạp lại vào Redis Cache
                try
                {
                    await _redis.SetAsync(cacheKey, setting.SettingValue, CacheTtl, ct);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Không thể lưu Redis cache cho cấu hình '{Key}'", key);
                }

                return setting.SettingValue;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi truy vấn Database cho cấu hình '{Key}'. Trả về giá trị mặc định fallback.", key);
        }

        return fallbackValue;
    }

    private static void ValidateSettingValue(string key, string value, string dataType)
    {
        switch (dataType.ToUpperInvariant())
        {
            case "INT":
                if (!int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var intVal) || intVal < 0)
                {
                    throw AppException.BadRequest(
                        ErrorCodes.ErrValidation,
                        $"Giá trị tham số '{key}' phải là số nguyên không âm hợp lệ (INT).");
                }

                if (key.EndsWith("_PERCENT", StringComparison.OrdinalIgnoreCase) && (intVal < 0 || intVal > 100))
                {
                    throw AppException.BadRequest(
                        ErrorCodes.ErrValidation,
                        $"Tham số phần trăm '{key}' phải nằm trong khoảng từ 0% đến 100% (nhận: {intVal}%).");
                }
                break;

            case "DECIMAL":
                if (!decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var decVal) || decVal < 0)
                {
                    throw AppException.BadRequest(
                        ErrorCodes.ErrValidation,
                        $"Giá trị tham số '{key}' phải là số thực không âm hợp lệ (DECIMAL).");
                }

                if (key.EndsWith("_PERCENT", StringComparison.OrdinalIgnoreCase) && (decVal < 0 || decVal > 100))
                {
                    throw AppException.BadRequest(
                        ErrorCodes.ErrValidation,
                        $"Tham số phần trăm '{key}' phải nằm trong khoảng từ 0% đến 100% (nhận: {decVal}%).");
                }
                break;

            case "BOOLEAN":
                if (!bool.TryParse(value, out _) && value != "0" && value != "1")
                {
                    throw AppException.BadRequest(
                        ErrorCodes.ErrValidation,
                        $"Giá trị tham số '{key}' phải là kiểu logic (true/false hoặc 1/0).");
                }
                break;

            case "STRING":
                if (string.IsNullOrWhiteSpace(value))
                {
                    throw AppException.BadRequest(
                        ErrorCodes.ErrValidation,
                        $"Giá trị tham số '{key}' không được là chuỗi rỗng.");
                }
                break;
        }
    }

    private static SystemSettingDto MapToDto(SystemSetting setting)
    {
        return new SystemSettingDto
        {
            SettingKey = setting.SettingKey,
            SettingValue = setting.SettingValue,
            DefaultValue = setting.DefaultValue,
            DataType = setting.DataType,
            Category = setting.Category,
            Description = setting.Description,
            LegalDocumentsUrl = setting.LegalDocumentsUrl,
            UpdatedAt = setting.UpdatedAt,
            UpdatedBy = setting.UpdatedBy
        };
    }

    public async Task<VeggieLife.BLL.DTOs.Auth.LegalDocumentsResponseData> GetLegalDocumentsAsync(CancellationToken ct = default)
    {
        // 1. Kiểm tra Redis Cache
        try
        {
            var cached = await _redis.GetAsync<VeggieLife.BLL.DTOs.Auth.LegalDocumentsResponseData>(CacheKeyLegalDocuments, ct);
            if (cached != null)
            {
                return cached;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Lỗi đọc Redis cache cho danh mục tài liệu pháp lý. Chuyển sang đọc Database.");
        }

        const string defaultUserAgreement = "https://res.cloudinary.com/swrsuenp/image/upload/v1793718600/veggielife/legal/116_2025_QH15_666020.jpg";
        const string defaultPrivacyPolicy = "https://res.cloudinary.com/swrsuenp/image/upload/v1793718345/veggielife/legal/13_2023_ND-CP_465185.jpg";
        const string defaultTermsOfService = "https://res.cloudinary.com/swrsuenp/image/upload/v1793718645/veggielife/legal/19_2023_QH15_500102.jpg";
        const string defaultVeganDisclaimer = "https://res.cloudinary.com/swrsuenp/image/upload/v1793718667/veggielife/legal/142_2026_ND-CP_696080.jpg";

        var result = new VeggieLife.BLL.DTOs.Auth.LegalDocumentsResponseData
        {
            UserAgreementUrl = defaultUserAgreement,
            PrivacyPolicyUrl = defaultPrivacyPolicy,
            TermsOfServiceUrl = defaultTermsOfService,
            VeganDisclaimerUrl = defaultVeganDisclaimer,
            MedicalDisclaimer = "Khuyến nghị mang tính chất tham khảo dinh dưỡng, vui lòng tham vấn ý kiến bác sĩ chuyên khoa đối với các bệnh lý nền.",
            UpdatedAt = DateTime.UtcNow
        };

        try
        {
            var legalSettings = await _db.SystemSettings
                .AsNoTracking()
                .Where(s => s.SettingKey == "LEGAL_DOCUMENTS" || s.SettingKey.StartsWith("LEGAL_"))
                .ToListAsync(ct);

            var mainSetting = legalSettings.FirstOrDefault(s => s.SettingKey == "LEGAL_DOCUMENTS");
            if (mainSetting != null)
            {
                result.UpdatedAt = mainSetting.UpdatedAt;
                var rawJson = !string.IsNullOrWhiteSpace(mainSetting.LegalDocumentsUrl)
                    ? mainSetting.LegalDocumentsUrl
                    : mainSetting.SettingValue;

                if (!string.IsNullOrWhiteSpace(rawJson) && rawJson.TrimStart().StartsWith("{"))
                {
                    try
                    {
                        using var doc = JsonDocument.Parse(rawJson);
                        var root = doc.RootElement;
                        if (root.TryGetProperty("user_agreement", out var ua) || root.TryGetProperty("userAgreement", out ua))
                            result.UserAgreementUrl = ua.GetString() ?? result.UserAgreementUrl;
                        if (root.TryGetProperty("privacy_policy", out var pp) || root.TryGetProperty("privacyPolicy", out pp))
                            result.PrivacyPolicyUrl = pp.GetString() ?? result.PrivacyPolicyUrl;
                        if (root.TryGetProperty("terms_of_service", out var tos) || root.TryGetProperty("termsOfService", out tos))
                            result.TermsOfServiceUrl = tos.GetString() ?? result.TermsOfServiceUrl;
                        if (root.TryGetProperty("vegan_disclaimer", out var vd) || root.TryGetProperty("veganDisclaimer", out vd))
                            result.VeganDisclaimerUrl = vd.GetString() ?? result.VeganDisclaimerUrl;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Lỗi giải mã JSON cấu hình LEGAL_DOCUMENTS.");
                    }
                }
            }

            foreach (var item in legalSettings)
            {
                var url = !string.IsNullOrWhiteSpace(item.LegalDocumentsUrl) ? item.LegalDocumentsUrl : item.SettingValue;
                if (string.IsNullOrWhiteSpace(url)) continue;

                if (string.Equals(item.SettingKey, "LEGAL_USER_AGREEMENT_URL", StringComparison.OrdinalIgnoreCase))
                    result.UserAgreementUrl = url;
                else if (string.Equals(item.SettingKey, "LEGAL_PRIVACY_POLICY_URL", StringComparison.OrdinalIgnoreCase))
                    result.PrivacyPolicyUrl = url;
                else if (string.Equals(item.SettingKey, "LEGAL_TERMS_OF_SERVICE_URL", StringComparison.OrdinalIgnoreCase))
                    result.TermsOfServiceUrl = url;
                else if (string.Equals(item.SettingKey, "LEGAL_VEGAN_DISCLAIMER_URL", StringComparison.OrdinalIgnoreCase))
                    result.VeganDisclaimerUrl = url;

                if (item.UpdatedAt > (result.UpdatedAt ?? DateTime.MinValue))
                {
                    result.UpdatedAt = item.UpdatedAt;
                }
            }

            // Lưu cache Redis
            try
            {
                await _redis.SetAsync(CacheKeyLegalDocuments, result, CacheTtl, ct);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Không thể lưu Redis cache cho danh mục tài liệu pháp lý.");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi truy vấn Database cho danh mục tài liệu pháp lý. Sử dụng giá trị fallback.");
        }

        return result;
    }

    /// <summary>
    /// Lấy toàn bộ các chỉ số cấu hình công khai phục vụ Frontend (MEAL_PLAN, AI_QUOTA, LEGAL) với Redis Cache-Aside.
    /// </summary>
    public async Task<VeggieLife.BLL.DTOs.Settings.PublicSystemSettingsDto> GetPublicSettingsAsync(CancellationToken ct = default)
    {
        try
        {
            var cached = await _redis.GetAsync<VeggieLife.BLL.DTOs.Settings.PublicSystemSettingsDto>(CacheKeyPublicSettings, ct);
            if (cached != null)
            {
                return cached;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Lỗi đọc Redis cache cho cấu hình công khai. Chuyển sang đọc trực tiếp.");
        }

        var result = new VeggieLife.BLL.DTOs.Settings.PublicSystemSettingsDto();

        result.MealPlanWeeklyLimitFree = await GetIntSettingAsync("MEAL_PLAN_WEEKLY_LIMIT_FREE", 1, ct);
        result.MealPlanWeeklyLimitVip = await GetIntSettingAsync("MEAL_PLAN_WEEKLY_LIMIT_VIP", 3, ct);
        result.MealSwapLimitDraft = await GetIntSettingAsync("MEAL_SWAP_LIMIT_DRAFT", 21, ct);
        result.MealSwapLimitActive = await GetIntSettingAsync("MEAL_SWAP_LIMIT_ACTIVE", 21, ct);
        result.MealSwapCalorieTolerancePercent = await GetIntSettingAsync("MEAL_SWAP_CALORIE_TOLERANCE_PERCENT", 15, ct);

        result.ChatLimitDailyGuest = await GetIntSettingAsync("CHAT_LIMIT_DAILY_GUEST", 3, ct);
        result.ChatLimitDailyFree = await GetIntSettingAsync("CHAT_LIMIT_DAILY_FREE", 20, ct);
        result.ChatLimitDailyVip = await GetIntSettingAsync("CHAT_LIMIT_DAILY_VIP", 200, ct);
        result.FridgeScanLimitDailyFree = await GetIntSettingAsync("FRIDGE_SCAN_LIMIT_DAILY_FREE", 10, ct);
        result.FridgeScanLimitDailyVip = await GetIntSettingAsync("FRIDGE_SCAN_LIMIT_DAILY_VIP", 30, ct);

        var legalDocs = await GetLegalDocumentsAsync(ct);
        result.LegalUserAgreementUrl = legalDocs.UserAgreementUrl;
        result.LegalPrivacyPolicyUrl = legalDocs.PrivacyPolicyUrl;
        result.LegalTermsOfServiceUrl = legalDocs.TermsOfServiceUrl;
        result.LegalVeganDisclaimerUrl = legalDocs.VeganDisclaimerUrl;

        try
        {
            await _redis.SetAsync(CacheKeyPublicSettings, result, TimeSpan.FromMinutes(10), ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Không thể lưu Redis cache cho cấu hình công khai.");
        }

        return result;
    }
}
