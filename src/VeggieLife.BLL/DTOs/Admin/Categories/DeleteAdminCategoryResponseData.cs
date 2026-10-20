namespace VeggieLife.BLL.DTOs.Admin.Categories;

/// <summary>
/// Dữ liệu phản hồi sau khi xóa danh mục món ăn kèm chuyển dữ liệu (FN-ADM-08).
/// </summary>
public class DeleteAdminCategoryResponseData
{
    /// <summary>
    /// Số lượng công thức đã di chuyển sang danh mục đích.
    /// </summary>
    public int MigratedRecipes { get; set; }

    /// <summary>
    /// Số lượng địa điểm quán ăn đã di chuyển sang danh mục đích.
    /// </summary>
    public int MigratedVenues { get; set; }
}
