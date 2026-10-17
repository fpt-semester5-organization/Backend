using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using VeggieLife.BLL.DTOs.Media.Responses;

namespace VeggieLife.BLL.Services.Media;

/// <summary>
/// Triển khai bản rỗng (Stub/Mock) của IMediaService để phục vụ kiểm thử hoặc trường hợp cần mock.
/// </summary>
public class MediaServiceStub : IMediaService
{
    private readonly ILogger<MediaServiceStub> _logger;

    public MediaServiceStub(ILogger<MediaServiceStub> logger)
    {
        _logger = logger;
    }

    public Task<MediaUploadResponse> UploadImageAsync(Stream fileStream, string fileName, string folder = "veggielife/images", CancellationToken ct = default)
    {
        _logger.LogInformation("[MediaServiceStub] Tải ảnh '{FileName}' lên thư mục '{Folder}'", fileName, folder);
        var dummyPublicId = $"{folder}/{Guid.NewGuid()}_{fileName}";
        var dummyUrl = $"https://res.cloudinary.com/veggielife/image/upload/v1/{dummyPublicId}";
        return Task.FromResult(new MediaUploadResponse
        {
            PublicId = dummyPublicId,
            SecureUrl = dummyUrl,
            Format = "webp",
            Bytes = 1024,
            Width = 800,
            Height = 600
        });
    }

    public Task<MediaUploadResponse> UploadVideoAsync(Stream fileStream, string fileName, string folder = "veggielife/videos", CancellationToken ct = default)
    {
        _logger.LogInformation("[MediaServiceStub] Tải video '{FileName}' lên thư mục '{Folder}'", fileName, folder);
        var dummyPublicId = $"{folder}/{Guid.NewGuid()}_{fileName}";
        var dummyUrl = $"https://res.cloudinary.com/veggielife/video/upload/v1/{dummyPublicId}";
        return Task.FromResult(new MediaUploadResponse
        {
            PublicId = dummyPublicId,
            SecureUrl = dummyUrl,
            Format = "mp4",
            Bytes = 2048,
            Width = 1920,
            Height = 1080
        });
    }

    public Task<MediaDeleteResponse> DeleteResourceAsync(string publicId, string resourceType = "image", CancellationToken ct = default)
    {
        _logger.LogInformation("[MediaServiceStub] Xóa tài nguyên Cloudinary publicId: {PublicId}", publicId);
        return Task.FromResult(new MediaDeleteResponse
        {
            PublicId = publicId,
            Success = true,
            Message = "Xóa tài nguyên trên Cloudinary thành công (Stub)."
        });
    }

    public Task DeleteFileAsync(string publicId, CancellationToken ct = default)
    {
        _logger.LogInformation("[MediaServiceStub] Xóa file Cloudinary publicId: {PublicId}", publicId);
        return Task.CompletedTask;
    }

    public string GetImageUrl(string publicId, int? width = null, int? height = null, string crop = "fill", string format = "webp")
    {
        return $"https://res.cloudinary.com/veggielife/image/upload/w_{width ?? 800},h_{height ?? 600},c_{crop}/{publicId}.{format}";
    }

    public string GetVideoUrl(string publicId, string? format = null)
    {
        var ext = string.IsNullOrWhiteSpace(format) ? "mp4" : format;
        return $"https://res.cloudinary.com/veggielife/video/upload/{publicId}.{ext}";
    }
}
