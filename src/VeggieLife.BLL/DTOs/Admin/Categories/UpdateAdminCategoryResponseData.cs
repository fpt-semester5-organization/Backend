namespace VeggieLife.BLL.DTOs.Admin.Categories;

/// <summary>
/// Dữ liệu phản hồi sau khi cập nhật danh mục món ăn (FN-ADM-07).
/// </summary>
public class UpdateAdminCategoryResponseData
{
    public int CategoryId { get; set; }
    public bool IsActive { get; set; }
    public string? CategoryName { get; set; }
}
