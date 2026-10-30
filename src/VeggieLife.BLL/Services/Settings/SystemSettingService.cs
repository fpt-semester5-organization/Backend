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
}
