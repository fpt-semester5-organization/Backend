namespace VeggieLife.BLL.DTOs.Admin.DietarySchools;

/// <summary>
/// Dữ liệu từng trường phái ăn chay trả về cho Quản trị viên (FN-ADM-10).
/// </summary>
public class AdminDietarySchoolItemDto
{
    public int DietarySchoolId { get; set; }
    public string SchoolCode { get; set; } = string.Empty;
    public string SchoolName { get; set; } = string.Empty;
    public ForbiddenFoodRulesDto ForbiddenFoodRules { get; set; } = new();
}
