using System.IO;

namespace VeggieLife.BLL.DTOs.Fridge.Requests;

/// <summary>
/// DTO chứa luồng dữ liệu và thông số tệp ảnh tải lên để phân tích nhận diện tủ lạnh (FN-VIS-01).
/// Đảm bảo tính độc lập kiến trúc, không phụ thuộc vào IFormFile của ASP.NET Core.
/// </summary>
public class FridgeDetectionInputDto
{
    /// <summary>
    /// Luồng dữ liệu của tệp ảnh.
    /// </summary>
    public Stream ImageStream { get; set; } = null!;

    /// <summary>
    /// Tên tệp gốc kèm phần mở rộng.
    /// </summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// Dung lượng tệp tính bằng byte.
    /// </summary>
    public long FileLength { get; set; }

    /// <summary>
    /// Loại MIME của tệp (ví dụ image/jpeg, image/png, image/webp).
    /// </summary>
    public string? ContentType { get; set; }
}
