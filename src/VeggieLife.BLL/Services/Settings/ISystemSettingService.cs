using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using VeggieLife.BLL.DTOs.Admin.Settings;

namespace VeggieLife.BLL.Services.Settings;

/// <summary>
/// Dịch vụ quản lý các tham số cấu hình động toàn hệ thống (SystemSettings) kết hợp Redis Cache-Aside.
/// Cung cấp dữ liệu hạn mức thời gian thực cho BLL và API quản trị cho Admin Dashboard.
/// </summary>
public interface ISystemSettingService
{
    /// <summary>
    /// Đọc giá trị cấu hình số nguyên (INT) với Redis Cache-Aside và giá trị dự phòng fallback.
    /// </summary>
    Task<int> GetIntSettingAsync(string key, int defaultValue, CancellationToken ct = default);

    /// <summary>
    /// Đọc giá trị cấu hình số thực (DECIMAL) với Redis Cache-Aside và giá trị dự phòng fallback.
    /// </summary>
    Task<decimal> GetDecimalSettingAsync(string key, decimal defaultValue, CancellationToken ct = default);

    /// <summary>
    /// Đọc giá trị cấu hình chuỗi (STRING) với Redis Cache-Aside và giá trị dự phòng fallback.
    /// </summary>
    Task<string> GetStringSettingAsync(string key, string defaultValue, CancellationToken ct = default);

    /// <summary>
    /// Đọc giá trị cấu hình logic (BOOLEAN) với Redis Cache-Aside và giá trị dự phòng fallback.
    /// </summary>
    Task<bool> GetBoolSettingAsync(string key, bool defaultValue, CancellationToken ct = default);

    /// <summary>
    /// Lấy toàn bộ danh sách cấu hình hệ thống (có thể lọc theo danh mục Category).
    /// </summary>
    Task<IReadOnlyList<SystemSettingDto>> GetAllSettingsAsync(string? category = null, CancellationToken ct = default);

    /// <summary>
    /// Lấy chi tiết một cấu hình hệ thống theo khóa SettingKey.
    /// </summary>
    Task<SystemSettingDto> GetSettingByKeyAsync(string key, CancellationToken ct = default);

    /// <summary>
    /// Cập nhật giá trị cấu hình hệ thống từ Admin Dashboard, ghi vết AuditLog và đồng bộ Redis Cache.
    /// </summary>
    Task<SystemSettingDto> UpdateSettingAsync(string key, UpdateSystemSettingRequest request, int adminId, CancellationToken ct = default);

    /// <summary>
    /// Khôi phục giá trị cấu hình về mặc định ban đầu (DefaultValue).
    /// </summary>
    Task<SystemSettingDto> ResetToDefaultAsync(string key, int adminId, CancellationToken ct = default);

    /// <summary>
    /// Lấy danh sách liên kết các tài liệu pháp lý công khai của hệ thống với Redis Cache-Aside (FN-AUTH-04).
    /// </summary>
    Task<VeggieLife.BLL.DTOs.Auth.LegalDocumentsResponseData> GetLegalDocumentsAsync(CancellationToken ct = default);

    /// <summary>
    /// Lấy toàn bộ các chỉ số cấu hình công khai phục vụ Frontend (MEAL_PLAN, AI_QUOTA, LEGAL).
    /// </summary>
    Task<VeggieLife.BLL.DTOs.Settings.PublicSystemSettingsDto> GetPublicSettingsAsync(CancellationToken ct = default);
}
