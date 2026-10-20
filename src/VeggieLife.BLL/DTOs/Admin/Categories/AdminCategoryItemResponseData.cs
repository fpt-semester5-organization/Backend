using System;

namespace VeggieLife.BLL.DTOs.Admin.Categories;

/// <summary>
/// Dữ liệu chi tiết từng danh mục món ăn trả về cho Quản trị viên.
/// </summary>
public class AdminCategoryItemResponseData
{
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsCore { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
