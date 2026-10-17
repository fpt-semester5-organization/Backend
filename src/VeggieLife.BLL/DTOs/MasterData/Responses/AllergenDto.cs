namespace VeggieLife.BLL.DTOs.MasterData.Responses;

/// <summary>
/// DTO danh mục chất dị ứng thực phẩm (FN-PROF-08).
/// </summary>
public class AllergenDto
{
    public int AllergenId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string AllergenName { get; set; } = string.Empty;
}
