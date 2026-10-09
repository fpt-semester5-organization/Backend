namespace VeggieLife.BLL.DTOs.Media.Responses;

/// <summary>
/// DTO phản hồi thông tin tệp đa phương tiện sau khi tải lên Cloudinary thành công.
/// </summary>
public class MediaUploadResponse
{
    /// <summary>
    /// Mã định danh công khai của tài nguyên trên Cloudinary.
    /// </summary>
    public string PublicId { get; set; } = string.Empty;

    /// <summary>
    /// Đường dẫn HTTP tiêu chuẩn của tệp.
    /// </summary>
    public string Url { get; set; } = string.Empty;

    /// <summary>
    /// Đường dẫn HTTPS bảo mật của tệp (khuyên dùng để hiển thị và lưu CSDL).
    /// </summary>
    public string SecureUrl { get; set; } = string.Empty;

    /// <summary>
    /// Loại tài nguyên: "image" hoặc "video".
    /// </summary>
    public string ResourceType { get; set; } = string.Empty;

    /// <summary>
    /// Định dạng tệp (jpg, png, webp, mp4, mov...).
    /// </summary>
    public string Format { get; set; } = string.Empty;

    /// <summary>
    /// Kích thước tệp tính bằng byte.
    /// </summary>
    public long Bytes { get; set; }

    /// <summary>
    /// Chiều rộng hình ảnh hoặc khung hình video (pixel).
    /// </summary>
    public int? Width { get; set; }

    /// <summary>
    /// Chiều cao hình ảnh hoặc khung hình video (pixel).
    /// </summary>
    public int? Height { get; set; }

    /// <summary>
    /// Thời lượng video tính bằng giây (chỉ có khi là video).
    /// </summary>
    public double? DurationSeconds { get; set; }
}
