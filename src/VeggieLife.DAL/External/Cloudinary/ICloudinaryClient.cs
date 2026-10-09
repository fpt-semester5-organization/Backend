using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace VeggieLife.DAL.External.Cloudinary;

/// <summary>
/// Interface giao tiếp trực tiếp với Cloudinary DotNet SDK tại tầng Data Access Layer.
/// Xử lý tải lên, xóa và sinh URL biến đổi/tối ưu hóa cho hình ảnh và video.
/// </summary>
public interface ICloudinaryClient
{
    /// <summary>
    /// Tải luồng ảnh lên Cloudinary.
    /// </summary>
    Task<CloudinaryMediaResult> UploadImageAsync(Stream stream, string fileName, string folder = "veggielife/images", CancellationToken ct = default);

    /// <summary>
    /// Tải luồng video nấu ăn lên Cloudinary.
    /// </summary>
    Task<CloudinaryMediaResult> UploadVideoAsync(Stream stream, string fileName, string folder = "veggielife/videos", CancellationToken ct = default);

    /// <summary>
    /// Xóa tài nguyên khỏi Cloudinary theo publicId và loại tài nguyên ("image" hoặc "video").
    /// </summary>
    Task<CloudinaryDeleteResult> DeleteResourceAsync(string publicId, string resourceType = "image", CancellationToken ct = default);

    /// <summary>
    /// Tạo đường dẫn URL ảnh tối ưu hóa (định dạng WebP, thay đổi kích thước theo yêu cầu).
    /// </summary>
    string GetImageUrl(string publicId, int? width = null, int? height = null, string crop = "fill", string format = "webp");

    /// <summary>
    /// Tạo đường dẫn URL video phát trực tuyến.
    /// </summary>
    string GetVideoUrl(string publicId, string? format = null);
}
