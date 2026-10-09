using System.Collections.Generic;

namespace VeggieLife.BLL.DTOs.MasterData.Responses;

/// <summary>
/// DTO danh mục trường phái ăn chay kèm quy tắc cấm thực phẩm (FN-PROF-08, D109).
/// </summary>
public class DietarySchoolDto
{
    public int DietarySchoolId { get; set; }
    public string SchoolCode { get; set; } = string.Empty;
    public string SchoolName { get; set; } = string.Empty;
    public List<string> ForbiddenFoodGroups { get; set; } = new();
    public bool ForbidAllium { get; set; }
}
