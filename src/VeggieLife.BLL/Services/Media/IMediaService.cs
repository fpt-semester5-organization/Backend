using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace VeggieLife.BLL.Services.Media;

/// <summary>
/// Interface dịch vụ lưu trữ đa phương tiện (Cloudinary CDN).
/// </summary>
public interface IMediaService
{
    /// <summary>
    /// Tải ảnh lên CDN (avatar, ảnh bài viết, ảnh công thức, ảnh bao bì sản phẩm).
    /// </summary>
    Task<string> UploadImageAsync(Stream fileStream, string fileName, string folder = "veggielife/images", CancellationToken ct = default);

    /// <summary>
    /// Tải video ngắn nấu ăn lên CDN.
    /// </summary>
    Task<string> UploadVideoAsync(Stream fileStream, string fileName, string folder = "veggielife/videos", CancellationToken ct = default);

    /// <summary>
    /// Xóa tài nguyên tệp khỏi CDN theo publicId.
    /// </summary>
    Task DeleteFileAsync(string publicId, CancellationToken ct = default);
}
