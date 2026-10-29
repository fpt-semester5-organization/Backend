using System.ComponentModel.DataAnnotations;

namespace VeggieLife.BLL.DTOs.Admin.Settings;

/// <summary>
/// Request cập nhật giá trị cấu hình hệ thống từ Admin Dashboard.
/// </summary>
public class UpdateSystemSettingRequest
{
    [Required(ErrorMessage = "Giá trị cấu hình không được để trống.")]
    [MaxLength(500, ErrorMessage = "Giá trị cấu hình không được vượt quá 500 ký tự.")]
    public string SettingValue { get; set; } = null!;
}
