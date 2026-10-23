using System.ComponentModel.DataAnnotations;

namespace VeggieLife.BLL.DTOs.Admin.DietarySchools;

/// <summary>
/// Yêu cầu cập nhật trường phái ăn chay dành cho Quản trị viên (FN-ADM-10).
/// </summary>
public class UpdateAdminDietarySchoolRequest
{
    /// <summary>
    /// Tên trường phái hiển thị (tùy chọn, tối đa 100 ký tự).
    /// </summary>
    [StringLength(100, ErrorMessage = "Tên trường phái tối đa 100 ký tự.")]
    public string? SchoolName { get; set; }

    /// <summary>
    /// Cấu trúc quy tắc thực phẩm cấm dạng JSON {forbiddenFoodGroups, forbidAllium, description} (tùy chọn).
    /// </summary>
    public ForbiddenFoodRulesDto? ForbiddenFoodRules { get; set; }
}
