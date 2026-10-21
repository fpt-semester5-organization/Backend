namespace VeggieLife.BLL.DTOs.Admin.Ingredients;

/// <summary>
/// DTO yêu cầu thêm nhóm dị ứng mới (FN-ADM-12).
/// </summary>
public class CreateAdminAllergenRequest
{
    public string AllergenName { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string? Description { get; set; }
}
