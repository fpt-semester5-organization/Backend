using System.Collections.Generic;

namespace VeggieLife.BLL.DTOs.Admin.DietarySchools;

/// <summary>
/// Cấu trúc quy tắc thực phẩm cấm dạng JSON của trường phái ăn chay (D109).
/// </summary>
public class ForbiddenFoodRulesDto
{
    /// <summary>
    /// Danh sách các nhóm thực phẩm cấm (mảng con của [EGG, DAIRY, POULTRY, SEAFOOD]).
    /// </summary>
    public List<string> ForbiddenFoodGroups { get; set; } = new();

    /// <summary>
    /// Cờ cấm ngũ vị tân (hành, tỏi, kiệu, hẹ, nén).
    /// </summary>
    public bool ForbidAllium { get; set; }

    /// <summary>
    /// Mô tả chi tiết quy tắc (tùy chọn).
    /// </summary>
    public string? Description { get; set; }
}
