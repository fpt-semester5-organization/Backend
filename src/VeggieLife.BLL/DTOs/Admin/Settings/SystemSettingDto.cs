using System;

namespace VeggieLife.BLL.DTOs.Admin.Settings;

/// <summary>
/// DTO đại diện cho một tham số cấu hình hệ thống trả về cho Admin.
/// </summary>
public class SystemSettingDto
{
    public string SettingKey { get; set; } = null!;

    public string SettingValue { get; set; } = null!;

    public string DefaultValue { get; set; } = null!;

    public string DataType { get; set; } = null!;

    public string Category { get; set; } = null!;

    public string? Description { get; set; }

    public DateTime UpdatedAt { get; set; }

    public int? UpdatedBy { get; set; }
}
