namespace VeggieLife.BLL.DTOs.Admin.Categories;

/// <summary>
/// Dữ liệu phản hồi sau khi tạo mới danh mục món ăn (FN-ADM-07).
/// </summary>
public class CreateAdminCategoryResponseData
{
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = null!;
}
