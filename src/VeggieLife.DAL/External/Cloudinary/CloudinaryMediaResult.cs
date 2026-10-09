using System;

namespace VeggieLife.DAL.External.Cloudinary;

/// <summary>
/// Kết quả phản hồi từ Cloudinary sau khi tải lên tệp đa phương tiện thành công.
/// </summary>
public class CloudinaryMediaResult
{
    /// <summary>
    /// Mã định danh công khai duy nhất của tệp trên Cloudinary (dùng để xóa hoặc chuyển đổi).
    /// </summary>
    public string PublicId { get; set; } = string.Empty;

    /// <summary>
    /// Đường dẫn HTTP tiêu chuẩn của tệp.
    /// </summary>
    public string Url { get; set; } = string.Empty;

    /// <summary>
    /// Đường dẫn HTTPS bảo mật của tệp (khuyên dùng lưu vào cơ sở dữ liệu).
    /// </summary>
    public string SecureUrl { get; set; } = string.Empty;

    /// <summary>
    /// Loại tài nguyên: "image" hoặc "video".
    /// </summary>
    public string ResourceType { get; set; } = string.Empty;

    /// <summary>
    /// Định dạng tệp sau khi lưu trữ trên Cloudinary (jpg, png, webp, mp4...).
    /// </summary>
    public string Format { get; set; } = string.Empty;

    /// <summary>
    /// Dung lượng tệp tính bằng byte.
    /// </summary>
    public long Bytes { get; set; }

    /// <summary>
    /// Chiều rộng (pixels).
    /// </summary>
    public int? Width { get; set; }

    /// <summary>
    /// Chiều cao (pixels).
    /// </summary>
    public int? Height { get; set; }

    /// <summary>
    /// Thời lượng video tính bằng giây (nếu là video).
    /// </summary>
    public double? Duration { get; set; }

    /// <summary>
    /// Thời điểm khởi tạo trên hệ thống Cloudinary.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
