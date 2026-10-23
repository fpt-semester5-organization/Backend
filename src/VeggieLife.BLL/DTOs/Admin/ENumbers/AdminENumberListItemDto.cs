using System;

namespace VeggieLife.BLL.DTOs.Admin.ENumbers;

/// <summary>
/// Dữ liệu chi tiết mã E-Number trả về cho Quản trị viên (FN-ADM-11).
/// </summary>
public class AdminENumberListItemDto
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? CategoryType { get; set; }
    public string OriginSource { get; set; } = string.Empty;
    public string VeganStatus { get; set; } = string.Empty;
    public string? HealthRiskNotes { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
