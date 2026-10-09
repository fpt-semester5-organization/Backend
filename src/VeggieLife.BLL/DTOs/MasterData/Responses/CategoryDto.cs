namespace VeggieLife.BLL.DTOs.MasterData.Responses;

/// <summary>
/// DTO danh mục món ăn thuần chay (FN-PROF-08).
/// </summary>
public class CategoryDto
{
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public bool IsCore { get; set; }
}
