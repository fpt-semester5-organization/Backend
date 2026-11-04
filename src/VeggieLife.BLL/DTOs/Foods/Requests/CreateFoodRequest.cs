namespace VeggieLife.BLL.DTOs.Foods.Requests;

/// <summary>
/// Yêu cầu tạo mới hoặc kiểm tra món ăn khái niệm (Food).
/// </summary>
public class CreateFoodRequest
{
    /// <summary>
    /// Tên món ăn (VD: "Bún Huế", "Phở chay").
    /// </summary>
    public string FoodName { get; set; } = string.Empty;

    /// <summary>
    /// Mã danh mục món ăn (Category_ID).
    /// </summary>
    public int CategoryId { get; set; }

    /// <summary>
    /// URL ảnh đại diện của món ăn (tùy chọn).
    /// </summary>
    public string? ImageUrl { get; set; }
}
