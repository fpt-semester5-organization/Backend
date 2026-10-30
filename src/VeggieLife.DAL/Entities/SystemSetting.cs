using System;

namespace VeggieLife.DAL.Entities;

/// <summary>
/// Thực thể cấu hình động toàn hệ thống (SystemSettings).
/// Lưu trữ các hạn mức nghiệp vụ, ngưỡng cảnh báo, ngân sách AI cho phép Admin điều chỉnh linh hoạt.
/// </summary>
public partial class SystemSetting
{
    public string SettingKey { get; set; } = null!;

    public string SettingValue { get; set; } = null!;

    public string DefaultValue { get; set; } = null!;

    public string DataType { get; set; } = null!;

    public string Category { get; set; } = null!;

    public string? Description { get; set; }

    public DateTime UpdatedAt { get; set; }

    public int? UpdatedBy { get; set; }

    public virtual User? UpdatedByUser { get; set; }
}
