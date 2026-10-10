using System.IO;

namespace VeggieLife.BLL.DTOs.Videos.Requests;

/// <summary>
/// DTO tiếp nhận dữ liệu tải lên video hoặc hình ảnh cho bài viết hoặc công thức tại tầng nghiệp vụ (FN-POST-08).
/// Không phụ thuộc vào IFormFile của tầng Web API để đảm bảo tính độc lập và sạch sẽ của kiến trúc.
/// </summary>
public class CreateVideoDto
{
    /// <summary>
    /// Luồng dữ liệu của tệp tải lên.
    /// </summary>
    public Stream FileStream { get; set; } = null!;

    /// <summary>
    /// Tên tệp gốc kèm phần mở rộng.
    /// </summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// Dung lượng tệp tính bằng byte.
    /// </summary>
    public long FileLength { get; set; }

    /// <summary>
    /// Mã định danh bài viết đích (cung cấp đúng 1 trong postId hoặc recipeId).
    /// </summary>
    public int? PostId { get; set; }

    /// <summary>
    /// Mã định danh công thức đích (cung cấp đúng 1 trong postId hoặc recipeId, > 0).
    /// </summary>
    public int? RecipeId { get; set; }

    /// <summary>
    /// Tiêu đề video (tùy chọn, tối đa 150 ký tự).
    /// </summary>
    public string? Title { get; set; }
}
