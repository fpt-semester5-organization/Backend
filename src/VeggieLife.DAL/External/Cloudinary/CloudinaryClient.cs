using System;
using System.IO;
using System.Net.Http;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace VeggieLife.DAL.External.Cloudinary;

/// <summary>
/// Triển khai thực tế của ICloudinaryClient sử dụng CloudinaryDotNet SDK.
/// Chịu trách nhiệm giao tiếp HTTP với hạ tầng CDN Cloudinary và bắt các sự cố mạng/timeout.
/// </summary>
public class CloudinaryClient : ICloudinaryClient
{
    private readonly CloudinarySettings _settings;
    private readonly ILogger<CloudinaryClient> _logger;
    private readonly CloudinaryDotNet.Cloudinary _cloudinary;

    public CloudinaryClient(
        IOptions<CloudinarySettings> options,
        ILogger<CloudinaryClient> logger)
    {
        _settings = options.Value ?? new CloudinarySettings();
        _logger = logger;

        if (!string.IsNullOrWhiteSpace(_settings.CloudName) &&
            !string.IsNullOrWhiteSpace(_settings.ApiKey) &&
            !string.IsNullOrWhiteSpace(_settings.ApiSecret))
        {
            var account = new Account(_settings.CloudName, _settings.ApiKey, _settings.ApiSecret);
            _cloudinary = new CloudinaryDotNet.Cloudinary(account);
            _cloudinary.Api.Secure = true;
        }
        else
        {
            // Thiết lập tài khoản fallback để khởi tạo thành công khi chưa điền secret
            var fallbackAccount = new Account(
                string.IsNullOrWhiteSpace(_settings.CloudName) ? "veggielife-placeholder" : _settings.CloudName,
                string.IsNullOrWhiteSpace(_settings.ApiKey) ? "000000000000000" : _settings.ApiKey,
                string.IsNullOrWhiteSpace(_settings.ApiSecret) ? "placeholder_secret" : _settings.ApiSecret);

            _cloudinary = new CloudinaryDotNet.Cloudinary(fallbackAccount);
            _cloudinary.Api.Secure = true;
        }
    }

    /// <summary>
    /// Constructor hỗ trợ tiêm đối tượng Cloudinary phục vụ kiểm thử đơn vị (Unit Test Mock).
    /// </summary>
    public CloudinaryClient(
        CloudinaryDotNet.Cloudinary cloudinary,
        CloudinarySettings settings,
        ILogger<CloudinaryClient> logger)
    {
        _cloudinary = cloudinary;
        _settings = settings;
        _logger = logger;
    }

    public async Task<CloudinaryMediaResult> UploadImageAsync(
        Stream stream, 
        string fileName, 
        string folder = "veggielife/images", 
        CancellationToken ct = default)
    {
        ValidateConfiguration();

        try
        {
            var uploadParams = new ImageUploadParams
            {
                File = new FileDescription(fileName, stream),
                Folder = folder,
                UseFilename = false,
                UniqueFilename = true,
                Overwrite = false
            };

            var uploadResult = await _cloudinary.UploadAsync(uploadParams, ct);

            if (uploadResult.Error != null)
            {
                _logger.LogError("Lỗi phản hồi từ Cloudinary API khi tải ảnh '{FileName}': {Message}", 
                    fileName, uploadResult.Error.Message);
                throw new CloudinaryException($"Máy chủ Cloudinary từ chối yêu cầu: {uploadResult.Error.Message}");
            }

            return new CloudinaryMediaResult
            {
                PublicId = uploadResult.PublicId,
                Url = uploadResult.Url?.ToString() ?? string.Empty,
                SecureUrl = uploadResult.SecureUrl?.ToString() ?? string.Empty,
                ResourceType = "image",
                Format = uploadResult.Format ?? Path.GetExtension(fileName).TrimStart('.'),
                Bytes = uploadResult.Bytes,
                Width = uploadResult.Width,
                Height = uploadResult.Height,
                CreatedAt = uploadResult.CreatedAt
            };
        }
        catch (CloudinaryException)
        {
            throw;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Lỗi kết nối mạng HttpRequestException khi tải ảnh lên Cloudinary: {Message}", ex.Message);
            throw new CloudinaryException("Không thể kết nối đến máy chủ lưu trữ Cloudinary do sự cố mạng.", ex);
        }
        catch (TimeoutException ex)
        {
            _logger.LogError(ex, "Kết nối Cloudinary bị quá thời gian chờ (Timeout): {Message}", ex.Message);
            throw new CloudinaryException("Kết nối đến Cloudinary bị quá thời gian chờ (Timeout).", ex);
        }
        catch (Exception ex) when (ex is SocketException || ex is IOException || (ex is TaskCanceledException && !ct.IsCancellationRequested))
        {
            _logger.LogError(ex, "Lỗi đường truyền socket/I/O khi tải ảnh lên Cloudinary: {Message}", ex.Message);
            throw new CloudinaryException("Không thể kết nối đến máy chủ lưu trữ Cloudinary do lỗi truyền tải mạng.", ex);
        }
    }

    public async Task<CloudinaryMediaResult> UploadVideoAsync(
        Stream stream, 
        string fileName, 
        string folder = "veggielife/videos", 
        CancellationToken ct = default)
    {
        ValidateConfiguration();

        try
        {
            var videoParams = new VideoUploadParams
            {
                File = new FileDescription(fileName, stream),
                Folder = folder,
                UseFilename = false,
                UniqueFilename = true
            };

            var uploadResult = await _cloudinary.UploadAsync(videoParams, ct);

            if (uploadResult.Error != null)
            {
                _logger.LogError("Lỗi phản hồi từ Cloudinary API khi tải video '{FileName}': {Message}", 
                    fileName, uploadResult.Error.Message);
                throw new CloudinaryException($"Máy chủ Cloudinary từ chối yêu cầu: {uploadResult.Error.Message}");
            }

            return new CloudinaryMediaResult
            {
                PublicId = uploadResult.PublicId,
                Url = uploadResult.Url?.ToString() ?? string.Empty,
                SecureUrl = uploadResult.SecureUrl?.ToString() ?? string.Empty,
                ResourceType = "video",
                Format = uploadResult.Format ?? Path.GetExtension(fileName).TrimStart('.'),
                Bytes = uploadResult.Bytes,
                Width = uploadResult.Width,
                Height = uploadResult.Height,
                Duration = uploadResult.Duration,
                CreatedAt = uploadResult.CreatedAt
            };
        }
        catch (CloudinaryException)
        {
            throw;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Lỗi kết nối mạng HttpRequestException khi tải video lên Cloudinary: {Message}", ex.Message);
            throw new CloudinaryException("Không thể kết nối đến máy chủ lưu trữ Cloudinary do sự cố mạng.", ex);
        }
        catch (TimeoutException ex)
        {
            _logger.LogError(ex, "Kết nối Cloudinary bị quá thời gian chờ (Timeout): {Message}", ex.Message);
            throw new CloudinaryException("Kết nối đến Cloudinary bị quá thời gian chờ (Timeout).", ex);
        }
        catch (Exception ex) when (ex is SocketException || ex is IOException || (ex is TaskCanceledException && !ct.IsCancellationRequested))
        {
            _logger.LogError(ex, "Lỗi đường truyền socket/I/O khi tải video lên Cloudinary: {Message}", ex.Message);
            throw new CloudinaryException("Không thể kết nối đến máy chủ lưu trữ Cloudinary do lỗi truyền tải mạng.", ex);
        }
    }

    public async Task<CloudinaryDeleteResult> DeleteResourceAsync(
        string publicId, 
        string resourceType = "image", 
        CancellationToken ct = default)
    {
        ValidateConfiguration();

        if (string.IsNullOrWhiteSpace(publicId))
        {
            return new CloudinaryDeleteResult { PublicId = publicId, Success = false, Result = "empty_public_id" };
        }

        try
        {
            var isVideo = resourceType.Equals("video", StringComparison.OrdinalIgnoreCase);
            var deletionParams = new DeletionParams(publicId)
            {
                ResourceType = isVideo ? ResourceType.Video : ResourceType.Image
            };

            var destroyResult = await _cloudinary.DestroyAsync(deletionParams);

            if (destroyResult.Error != null)
            {
                _logger.LogError("Lỗi phản hồi từ Cloudinary API khi xóa publicId '{PublicId}': {Message}", 
                    publicId, destroyResult.Error.Message);
                throw new CloudinaryException($"Máy chủ Cloudinary từ chối yêu cầu xóa: {destroyResult.Error.Message}");
            }

            var isOk = string.Equals(destroyResult.Result, "ok", StringComparison.OrdinalIgnoreCase);

            return new CloudinaryDeleteResult
            {
                PublicId = publicId,
                Success = isOk,
                Result = destroyResult.Result ?? string.Empty
            };
        }
        catch (CloudinaryException)
        {
            throw;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Lỗi kết nối mạng HttpRequestException khi xóa tài nguyên Cloudinary '{PublicId}': {Message}", 
                publicId, ex.Message);
            throw new CloudinaryException("Không thể kết nối đến máy chủ lưu trữ Cloudinary do sự cố mạng.", ex);
        }
        catch (TimeoutException ex)
        {
            _logger.LogError(ex, "Kết nối Cloudinary bị quá thời gian chờ khi xóa tài nguyên '{PublicId}': {Message}", 
                publicId, ex.Message);
            throw new CloudinaryException("Kết nối đến Cloudinary bị quá thời gian chờ (Timeout).", ex);
        }
        catch (Exception ex) when (ex is SocketException || ex is IOException || (ex is TaskCanceledException && !ct.IsCancellationRequested))
        {
            _logger.LogError(ex, "Lỗi truyền tải mạng khi xóa tài nguyên Cloudinary '{PublicId}': {Message}", 
                publicId, ex.Message);
            throw new CloudinaryException("Không thể kết nối đến máy chủ lưu trữ Cloudinary do lỗi truyền tải mạng.", ex);
        }
    }

    public string GetImageUrl(
        string publicId, 
        int? width = null, 
        int? height = null, 
        string crop = "fill", 
        string format = "webp")
    {
        if (string.IsNullOrWhiteSpace(publicId))
        {
            return string.Empty;
        }

        // Nếu publicId đã là đường dẫn URL hoàn chỉnh
        if (publicId.StartsWith("http://", StringComparison.OrdinalIgnoreCase) || 
            publicId.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            return publicId;
        }

        var transformation = new Transformation();
        if (width.HasValue && width.Value > 0) transformation.Width(width.Value);
        if (height.HasValue && height.Value > 0) transformation.Height(height.Value);
        if (!string.IsNullOrWhiteSpace(crop)) transformation.Crop(crop);
        if (!string.IsNullOrWhiteSpace(format)) transformation.FetchFormat(format);

        return _cloudinary.Api.UrlImgUp.Transform(transformation).BuildUrl(publicId);
    }

    public string GetVideoUrl(string publicId, string? format = null)
    {
        if (string.IsNullOrWhiteSpace(publicId))
        {
            return string.Empty;
        }

        if (publicId.StartsWith("http://", StringComparison.OrdinalIgnoreCase) || 
            publicId.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            return publicId;
        }

        var urlBuilder = _cloudinary.Api.UrlVideoUp;
        if (!string.IsNullOrWhiteSpace(format))
        {
            urlBuilder = urlBuilder.Transform(new Transformation().FetchFormat(format));
        }

        return urlBuilder.BuildUrl(publicId);
    }

    private void ValidateConfiguration()
    {
        if (string.IsNullOrWhiteSpace(_settings.CloudName) ||
            string.IsNullOrWhiteSpace(_settings.ApiKey) ||
            string.IsNullOrWhiteSpace(_settings.ApiSecret))
        {
            _logger.LogWarning("Chưa cấu hình tài khoản Cloudinary hợp lệ (CloudName, ApiKey hoặc ApiSecret đang trống).");
            throw new CloudinaryException("Cấu hình tài khoản Cloudinary chưa hoàn tất. Vui lòng kiểm tra biến môi trường hoặc file appsettings.");
        }
    }
}
