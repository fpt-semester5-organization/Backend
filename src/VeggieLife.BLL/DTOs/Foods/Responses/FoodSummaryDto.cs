namespace VeggieLife.BLL.DTOs.Foods.Responses;

/// <summary>
/// DTO tóm tắt thông tin một món ăn, dùng cho autocomplete combobox khi tạo công thức.
/// </summary>
public class FoodSummaryDto
{
    /// <summary>Định danh duy nhất của món ăn.</summary>
    public int FoodId { get; set; }

    /// <summary>Tên món ăn.</summary>
    public string FoodName { get; set; } = string.Empty;

    /// <summary>Định danh danh mục mà món ăn thuộc về.</summary>
    public int CategoryId { get; set; }

    /// <summary>URL ảnh minh họa của món ăn.</summary>
    public string? ImageUrl { get; set; }
}
