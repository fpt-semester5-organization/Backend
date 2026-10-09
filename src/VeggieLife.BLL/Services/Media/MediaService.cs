using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using VeggieLife.BLL.Common.Constants;
using VeggieLife.BLL.Common.Exceptions;
using VeggieLife.BLL.DTOs.Media.Responses;
using VeggieLife.DAL.External.Cloudinary;

namespace VeggieLife.BLL.Services.Media;

/// <summary>
/// Triển khai dịch vụ lưu trữ đa phương tiện dùng chung qua Cloudinary CDN.
/// Hiện thực đầy đủ các ràng buộc nghiệp vụ BR-USR-MEDIA-01, BR-AI-STT-01 và chuẩn mã lỗi 502 khi có sự cố mạng.
/// </summary>
public class MediaService : IMediaService
{
    private readonly ICloudinaryClient _cloudinaryClient;
    private readonly ILogger<MediaService> _logger;

    // Giới hạn dung lượng tệp theo đặc tả nghiệp vụ
    private const long MaxImageSizeBytes = 5 * 1024 * 1024;   // 5 MB (BR-USR-MEDIA-01)
    private const long MaxVideoSizeBytes = 100 * 1024 * 1024; // 100 MB (BR-AI-STT-01)

    private static readonly string[] AllowedImageExtensions = { ".jpg", ".jpeg", ".png", ".webp" };
    private static readonly string[] AllowedVideoExtensions = { ".mp4", ".mov" };

    public MediaService(
        ICloudinaryClient cloudinaryClient,
        ILogger<MediaService> logger)
    {
        _cloudinaryClient = cloudinaryClient;
        _logger = logger;
    }

    /// <summary>
    /// Tải ảnh lên Cloudinary CDN với kiểm tra magic bytes và giới hạn 5 MB (BR-USR-MEDIA-01).
    /// </summary>
    public async Task<MediaUploadResponse> UploadImageAsync(
        Stream fileStream, 
        string fileName, 
        string folder = "veggielife/images", 
        CancellationToken ct = default)
    {
        if (fileStream == null || string.IsNullOrWhiteSpace(fileName))
        {
            throw AppException.BadRequest(ErrorCodes.ErrValidation, "Tệp ảnh hoặc tên tệp không được để trống.");
        }

        // 1. Kiểm tra phần mở rộng tệp
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        if (!AllowedImageExtensions.Contains(extension))
        {
            // BR-USR-MEDIA-01
            throw AppException.BadRequest(ErrorCodes.ErrMediaInvalidFile, 
                $"Định dạng ảnh '{extension}' không được hỗ trợ. Hệ thống chỉ chấp nhận định dạng JPEG, PNG hoặc WEBP.");
        }

        // 2. Chuyển đổi sang MemoryStream để kiểm tra kích thước và magic bytes
        using var memoryStream = new MemoryStream();
        await fileStream.CopyToAsync(memoryStream, ct);

        // BR-USR-MEDIA-01: Kiểm tra dung lượng tối đa 5 MB
        if (memoryStream.Length > MaxImageSizeBytes)
        {
            throw new AppException(413, ErrorCodes.ErrMediaFileTooLarge, 
                $"Dung lượng ảnh vượt quá giới hạn cho phép (Tối đa 5 MB; kích thước nhận được: {memoryStream.Length / (1024.0 * 1024.0):F2} MB).");
        }

        if (memoryStream.Length < 12)
        {
            throw AppException.BadRequest(ErrorCodes.ErrMediaInvalidFile, "Tệp ảnh bị rỗng hoặc không có dữ liệu hợp lệ.");
        }

        // BR-USR-MEDIA-01: Kiểm tra chữ ký tệp (magic bytes) để chống giả mạo đuôi tệp
        memoryStream.Position = 0;
        var headerBytes = new byte[12];
        _ = await memoryStream.ReadAsync(headerBytes.AsMemory(0, 12), ct);
        memoryStream.Position = 0;

        if (!IsValidImageSignature(headerBytes, extension))
        {
            _logger.LogWarning("Phát hiện tệp ảnh giả mạo chữ ký (Magic bytes không khớp với đuôi {Extension}): {FileName}", extension, fileName);
            throw AppException.BadRequest(ErrorCodes.ErrMediaInvalidFile, 
                "Chữ ký tệp (magic bytes) không khớp với định dạng ảnh yêu cầu. Tệp có thể đã bị sửa đuôi hoặc bị hỏng.");
        }

        // 3. Giao tiếp với Cloudinary CDN
        try
        {
            var result = await _cloudinaryClient.UploadImageAsync(memoryStream, fileName, folder, ct);

            return new MediaUploadResponse
            {
                PublicId = result.PublicId,
                Url = result.Url,
                SecureUrl = result.SecureUrl,
                ResourceType = result.ResourceType,
                Format = result.Format,
                Bytes = result.Bytes,
                Width = result.Width,
                Height = result.Height,
                DurationSeconds = null
            };
        }
        catch (CloudinaryException ex)
        {
            _logger.LogError(ex, "Lỗi từ dịch vụ Cloudinary khi tải ảnh '{FileName}': {Message}", fileName, ex.Message);
            // Báo đúng mã lỗi HTTP 502 ERR_EXT_CLOUDINARY_FAILED
            throw AppException.BadGateway(ErrorCodes.ErrExtCloudinaryFailed, 
                "Không thể kết nối đến máy chủ lưu trữ Cloudinary.", ex.Message);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Lỗi mạng HttpRequestException khi tải ảnh lên Cloudinary: {Message}", ex.Message);
            throw AppException.BadGateway(ErrorCodes.ErrExtCloudinaryFailed, 
                "Không thể kết nối đến máy chủ lưu trữ Cloudinary do sự cố mạng.", ex.Message);
        }
        catch (TimeoutException ex)
        {
            _logger.LogError(ex, "Quá thời gian chờ Timeout khi kết nối Cloudinary: {Message}", ex.Message);
            throw AppException.BadGateway(ErrorCodes.ErrExtCloudinaryFailed, 
                "Kết nối đến máy chủ Cloudinary bị quá thời gian chờ (Timeout).", ex.Message);
        }
    }

    /// <summary>
    /// Tải video nấu ăn lên Cloudinary CDN với kiểm tra giới hạn 100 MB (BR-AI-STT-01).
    /// </summary>
    public async Task<MediaUploadResponse> UploadVideoAsync(
        Stream fileStream, 
        string fileName, 
        string folder = "veggielife/videos", 
        CancellationToken ct = default)
    {
        if (fileStream == null || string.IsNullOrWhiteSpace(fileName))
        {
            throw AppException.BadRequest(ErrorCodes.ErrValidation, "Tệp video hoặc tên tệp không được để trống.");
        }

        // 1. Kiểm tra phần mở rộng tệp video
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        if (!AllowedVideoExtensions.Contains(extension))
        {
            // BR-AI-STT-01
            throw AppException.BadRequest(ErrorCodes.ErrMediaInvalidFile, 
                $"Định dạng video '{extension}' không được hỗ trợ. Hệ thống chỉ chấp nhận video MP4 hoặc MOV.");
        }

        // 2. Chuyển đổi sang MemoryStream để kiểm tra kích thước
        using var memoryStream = new MemoryStream();
        await fileStream.CopyToAsync(memoryStream, ct);

        // BR-AI-STT-01: Kiểm tra dung lượng tối đa 100 MB
        if (memoryStream.Length > MaxVideoSizeBytes)
        {
            throw new AppException(413, ErrorCodes.ErrMediaFileTooLarge, 
                $"Dung lượng video vượt quá giới hạn cho phép (Tối đa 100 MB; kích thước nhận được: {memoryStream.Length / (1024.0 * 1024.0):F2} MB).");
        }

        if (memoryStream.Length < 12)
        {
            throw AppException.BadRequest(ErrorCodes.ErrMediaInvalidFile, "Tệp video bị rỗng hoặc không có dữ liệu hợp lệ.");
        }

        // Kiểm tra chữ ký video cơ bản (ISO Base Media File / ftyp)
        memoryStream.Position = 0;
        var headerBytes = new byte[12];
        _ = await memoryStream.ReadAsync(headerBytes.AsMemory(0, 12), ct);
        memoryStream.Position = 0;

        if (!IsValidVideoSignature(headerBytes, extension))
        {
            _logger.LogWarning("Phát hiện tệp video giả mạo chữ ký: {FileName}", fileName);
            throw AppException.BadRequest(ErrorCodes.ErrMediaInvalidFile, 
                "Chữ ký tệp (magic bytes) không khớp với định dạng video MP4/MOV hợp lệ.");
        }

        // 3. Giao tiếp với Cloudinary CDN
        try
        {
            var result = await _cloudinaryClient.UploadVideoAsync(memoryStream, fileName, folder, ct);

            return new MediaUploadResponse
            {
                PublicId = result.PublicId,
                Url = result.Url,
                SecureUrl = result.SecureUrl,
                ResourceType = result.ResourceType,
                Format = result.Format,
                Bytes = result.Bytes,
                Width = result.Width,
                Height = result.Height,
                DurationSeconds = result.Duration
            };
        }
        catch (CloudinaryException ex)
        {
            _logger.LogError(ex, "Lỗi từ dịch vụ Cloudinary khi tải video '{FileName}': {Message}", fileName, ex.Message);
            // Báo đúng mã lỗi HTTP 502 ERR_EXT_CLOUDINARY_FAILED
            throw AppException.BadGateway(ErrorCodes.ErrExtCloudinaryFailed, 
                "Không thể kết nối đến máy chủ lưu trữ Cloudinary.", ex.Message);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Lỗi mạng HttpRequestException khi tải video lên Cloudinary: {Message}", ex.Message);
            throw AppException.BadGateway(ErrorCodes.ErrExtCloudinaryFailed, 
                "Không thể kết nối đến máy chủ lưu trữ Cloudinary do sự cố mạng.", ex.Message);
        }
        catch (TimeoutException ex)
        {
            _logger.LogError(ex, "Quá thời gian chờ Timeout khi kết nối Cloudinary: {Message}", ex.Message);
            throw AppException.BadGateway(ErrorCodes.ErrExtCloudinaryFailed, 
                "Kết nối đến máy chủ Cloudinary bị quá thời gian chờ (Timeout).", ex.Message);
        }
    }

    /// <summary>
    /// Xóa tài nguyên đa phương tiện khỏi Cloudinary theo publicId và loại tài nguyên.
    /// </summary>
    public async Task<MediaDeleteResponse> DeleteResourceAsync(
        string publicId, 
        string resourceType = "image", 
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(publicId))
        {
            throw AppException.BadRequest(ErrorCodes.ErrValidation, "Mã publicId của tài nguyên cần xóa không được để trống.");
        }

        try
        {
            var result = await _cloudinaryClient.DeleteResourceAsync(publicId, resourceType, ct);

            return new MediaDeleteResponse
            {
                PublicId = result.PublicId,
                Success = result.Success,
                Message = result.Success ? "Xóa tài nguyên trên Cloudinary thành công." : $"Không tìm thấy hoặc không thể xóa: {result.Result}"
            };
        }
        catch (CloudinaryException ex)
        {
            _logger.LogError(ex, "Lỗi từ Cloudinary khi xóa tài nguyên '{PublicId}': {Message}", publicId, ex.Message);
            throw AppException.BadGateway(ErrorCodes.ErrExtCloudinaryFailed, 
                "Không thể kết nối đến máy chủ lưu trữ Cloudinary khi xóa tài nguyên.", ex.Message);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Lỗi mạng khi xóa tài nguyên Cloudinary '{PublicId}': {Message}", publicId, ex.Message);
            throw AppException.BadGateway(ErrorCodes.ErrExtCloudinaryFailed, 
                "Không thể kết nối đến máy chủ lưu trữ Cloudinary do sự cố mạng.", ex.Message);
        }
        catch (TimeoutException ex)
        {
            _logger.LogError(ex, "Quá thời gian chờ Timeout khi xóa tài nguyên '{PublicId}': {Message}", publicId, ex.Message);
            throw AppException.BadGateway(ErrorCodes.ErrExtCloudinaryFailed, 
                "Kết nối đến máy chủ Cloudinary bị quá thời gian chờ (Timeout).", ex.Message);
        }
    }

    /// <summary>
    /// Phương thức xóa tương thích ngược với interface IMediaService cũ.
    /// </summary>
    public async Task DeleteFileAsync(string publicId, CancellationToken ct = default)
    {
        await DeleteResourceAsync(publicId, "image", ct);
    }

    /// <summary>
    /// Lấy URL ảnh tối ưu hóa theo kích thước và định dạng WebP.
    /// </summary>
    public string GetImageUrl(
        string publicId, 
        int? width = null, 
        int? height = null, 
        string crop = "fill", 
        string format = "webp")
    {
        return _cloudinaryClient.GetImageUrl(publicId, width, height, crop, format);
    }

    /// <summary>
    /// Lấy URL video phát trực tuyến.
    /// </summary>
    public string GetVideoUrl(string publicId, string? format = null)
    {
        return _cloudinaryClient.GetVideoUrl(publicId, format);
    }

    /// <summary>
    /// Kiểm tra chữ ký magic bytes của tệp ảnh (JPEG, PNG, WEBP).
    /// </summary>
    private static bool IsValidImageSignature(byte[] bytes, string extension)
    {
        // JPEG: FF D8 FF
        if (bytes[0] == 0xFF && bytes[1] == 0xD8 && bytes[2] == 0xFF)
        {
            return extension == ".jpg" || extension == ".jpeg";
        }

        // PNG: 89 50 4E 47 (0x89 'P' 'N' 'G')
        if (bytes[0] == 0x89 && bytes[1] == 0x50 && bytes[2] == 0x4E && bytes[3] == 0x47)
        {
            return extension == ".png";
        }

        // WEBP: "RIFF" ở bytes 0..3 và "WEBP" ở bytes 8..11
        if (bytes[0] == 0x52 && bytes[1] == 0x49 && bytes[2] == 0x46 && bytes[3] == 0x46 &&
            bytes[8] == 0x57 && bytes[9] == 0x45 && bytes[10] == 0x42 && bytes[11] == 0x50)
        {
            return extension == ".webp";
        }

        return false;
    }

    /// <summary>
    /// Kiểm tra chữ ký magic bytes của tệp video (MP4, MOV / QuickTime).
    /// </summary>
    private static bool IsValidVideoSignature(byte[] bytes, string extension)
    {
        // MP4 / MOV thông thường chứa "ftyp", "moov", "mdat" hoặc "wide" ở bytes 4..7
        var signature = System.Text.Encoding.ASCII.GetString(bytes, 4, 4);
        if (signature == "ftyp" || signature == "moov" || signature == "mdat" || signature == "wide")
        {
            return true;
        }

        // Một số định dạng QuickTime (.mov) bắt đầu bằng moov hoặc wide ngay từ đầu
        var startSignature = System.Text.Encoding.ASCII.GetString(bytes, 0, 4);
        if (startSignature == "moov" || startSignature == "mdat" || startSignature == "wide")
        {
            return true;
        }

        return false;
    }
}
