using System.Collections.Generic;

namespace VeggieLife.BLL.DTOs.Ai;

/// <summary>
/// DTO gửi yêu cầu kiểm duyệt nội dung sang AI Service (POST /moderation/check).
/// Tuân thủ tài liệu đặc tả VL-SPEC-AI-MOD-01.
/// </summary>
public class ModerationCheckRequest
{
    /// <summary>
    /// Tiêu đề bài viết hoặc tên công thức (Posts.Title / Recipes.Title). Bắt buộc (1-300 ký tự).
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Nội dung văn bản chi tiết bài đăng hoặc mô tả món ăn (Tối đa 10.000 ký tự).
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Danh sách tên nguyên liệu chế biến (Tối đa 200 phần tử).
    /// </summary>
    public List<string> IngredientNames { get; set; } = new();

    /// <summary>
    /// Các bước hướng dẫn thực hiện món ăn (Tối đa 100 phần tử).
    /// </summary>
    public List<string> Steps { get; set; } = new();

    /// <summary>
    /// Mã trường phái chay đã khai báo (VEGAN, OVO, LACTO, LACTO_OVO, BUDDHIST...).
    /// </summary>
    public string? DietarySchoolCode { get; set; }

    /// <summary>
    /// Danh sách liên kết ảnh công khai (Cloudinary HTTPS, tối đa 4 ảnh).
    /// </summary>
    public List<string> ImageUrls { get; set; } = new();
}
