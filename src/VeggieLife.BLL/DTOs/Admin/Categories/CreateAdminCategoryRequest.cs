using System.ComponentModel.DataAnnotations;

namespace VeggieLife.BLL.DTOs.Admin.Categories;

/// <summary>
/// Yêu cầu tạo mới danh mục món ăn (FN-ADM-07).
/// </summary>
public class CreateAdminCategoryRequest
{
    /// <summary>
    /// Tên danh mục (2–100 ký tự, duy nhất).
    /// </summary>
    [Required(ErrorMessage = "Tên danh mục không được để trống.")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "Tên danh mục phải từ 2 đến 100 ký tự.")]
    public string CategoryName { get; set; } = null!;

    /// <summary>
    /// Mô tả danh mục (tối đa 500 ký tự).
    /// </summary>
    [StringLength(500, ErrorMessage = "Mô tả danh mục tối đa 500 ký tự.")]
    public string? Description { get; set; }
}
