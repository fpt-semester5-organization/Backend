using System.IO;
using System.Threading;
using System.Threading.Tasks;
using VeggieLife.BLL.DTOs.Media.Responses;

namespace VeggieLife.BLL.Services.Media;

/// <summary>
/// Interface dịch vụ lưu trữ đa phương tiện dùng chung qua Cloudinary CDN cho toàn bộ hệ thống VeggieLife.
/// Cung cấp các thao tác tải lên, xóa và lấy đường dẫn tối ưu hóa/biến đổi hình ảnh và video.
/// </summary>
public interface IMediaService
{
    /// <summary>
    /// Tải ảnh lên Cloudinary CDN (avatar người dùng, ảnh bài viết, ảnh công thức, ảnh bao bì sản phẩm).
    /// Áp dụng quy chuẩn nghiệp vụ BR-USR-MEDIA-01: Tối đa 5 MB, định dạng JPEG/PNG/WEBP, kiểm tra magic bytes.
    /// </summary>
    Task<MediaUploadResponse> UploadImageAsync(Stream fileStream, string fileName, string folder = "veggielife/images", CancellationToken ct = default);

    /// <summary>
    /// Tải video nấu ăn lên Cloudinary CDN.
    /// Áp dụng quy chuẩn nghiệp vụ BR-AI-STT-01: Tối đa 100 MB, định dạng MP4/MOV, lấy thông số thời lượng.
    /// </summary>
    Task<MediaUploadResponse> UploadVideoAsync(Stream fileStream, string fileName, string folder = "veggielife/videos", CancellationToken ct = default);

    /// <summary>
    /// Xóa tài nguyên đa phương tiện khỏi Cloudinary CDN theo publicId và loại tài nguyên ("image" hoặc "video").
    /// </summary>
    Task<MediaDeleteResponse> DeleteResourceAsync(string publicId, string resourceType = "image", CancellationToken ct = default);

    /// <summary>
    /// Xóa tài nguyên tệp khỏi CDN theo publicId (phương thức tương thích ngược mặc định cho ảnh).
    /// </summary>
    Task DeleteFileAsync(string publicId, CancellationToken ct = default);

    /// <summary>
    /// Tạo đường dẫn URL ảnh CDN tối ưu hóa theo kích thước và định dạng (mặc định WebP).
    /// </summary>
    string GetImageUrl(string publicId, int? width = null, int? height = null, string crop = "fill", string format = "webp");

    /// <summary>
    /// Tạo đường dẫn URL video phát trực tuyến từ Cloudinary CDN.
    /// </summary>
    string GetVideoUrl(string publicId, string? format = null);
}
