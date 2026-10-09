using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace VeggieLife.BLL.Services.Media;

/// <summary>
/// Triển khai bản rỗng (Stub/Mock) của IMediaService để thành viên khác hoàn thiện tích hợp Cloudinary CDN.
/// </summary>
public class MediaServiceStub : IMediaService
{
    private readonly ILogger<MediaServiceStub> _logger;

    public MediaServiceStub(ILogger<MediaServiceStub> logger)
    {
        _logger = logger;
    }

    public Task<string> UploadImageAsync(Stream fileStream, string fileName, string folder = "veggielife/images", CancellationToken ct = default)
    {
        _logger.LogInformation("[MediaServiceStub] Tải ảnh '{FileName}' lên thư mục '{Folder}'", fileName, folder);
        // TODO: Tích hợp Cloudinary DotNet SDK
        var dummyUrl = $"https://res.cloudinary.com/veggielife/image/upload/v1/{folder}/{Guid.NewGuid()}_{fileName}";
        return Task.FromResult(dummyUrl);
    }

    public Task<string> UploadVideoAsync(Stream fileStream, string fileName, string folder = "veggielife/videos", CancellationToken ct = default)
    {
        _logger.LogInformation("[MediaServiceStub] Tải video '{FileName}' lên thư mục '{Folder}'", fileName, folder);
        // TODO: Tích hợp Cloudinary Video Upload
        var dummyUrl = $"https://res.cloudinary.com/veggielife/video/upload/v1/{folder}/{Guid.NewGuid()}_{fileName}";
        return Task.FromResult(dummyUrl);
    }

    public Task DeleteFileAsync(string publicId, CancellationToken ct = default)
    {
        _logger.LogInformation("[MediaServiceStub] Xóa tài nguyên Cloudinary publicId: {PublicId}", publicId);
        // TODO: Tích hợp Cloudinary Destroy
        return Task.CompletedTask;
    }
}
