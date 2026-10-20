using System.ComponentModel.DataAnnotations;

namespace VeggieLife.BLL.DTOs.Admin.Categories;

/// <summary>
/// Yêu cầu cập nhật danh mục món ăn (FN-ADM-07).
/// </summary>
public class UpdateAdminCategoryRequest
{
    /// <summary>
    /// Tên danh mục mới (tùy chọn, 2–100 ký tự, duy nhất).
    /// </summary>
    [StringLength(100, MinimumLength = 2, ErrorMessage = "Tên danh mục phải từ 2 đến 100 ký tự.")]
    public string? CategoryName { get; set; }

    /// <summary>
    /// Trạng thái kích hoạt (tùy chọn).
    /// </summary>
    public bool? IsActive { get; set; }

    /// <summary>
    /// Mô tả danh mục (tùy chọn, tối đa 500 ký tự).
    /// </summary>
    [StringLength(500, ErrorMessage = "Mô tả danh mục tối đa 500 ký tự.")]
    public string? Description { get; set; }
}
