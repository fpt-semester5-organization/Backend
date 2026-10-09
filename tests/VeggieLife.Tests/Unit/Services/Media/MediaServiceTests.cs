using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using VeggieLife.BLL.Common.Constants;
using VeggieLife.BLL.Common.Exceptions;
using VeggieLife.BLL.Services.Media;
using VeggieLife.DAL.External.Cloudinary;
using Xunit;

namespace VeggieLife.Tests.Unit.Services.Media;

/// <summary>
/// Bộ kiểm thử đơn vị cho lớp MediaService dùng chung.
/// Kiểm chứng các ràng buộc BR-USR-MEDIA-01, BR-AI-STT-01 và tiêu chí nghiệm thu:
/// Giả lập lỗi mạng Cloudinary trả đúng mã lỗi 502 (ERR_EXT_CLOUDINARY_FAILED).
/// </summary>
public class MediaServiceTests
{
    private readonly Mock<ICloudinaryClient> _mockCloudinaryClient;
    private readonly Mock<ILogger<MediaService>> _mockLogger;
    private readonly MediaService _mediaService;

    public MediaServiceTests()
    {
        _mockCloudinaryClient = new Mock<ICloudinaryClient>();
        _mockLogger = new Mock<ILogger<MediaService>>();
        _mediaService = new MediaService(_mockCloudinaryClient.Object, _mockLogger.Object);
    }

    #region Helper Methods tạo dữ liệu tệp nhị phân hợp lệ

    private static MemoryStream CreateValidPngStream(int sizeInBytes = 1024)
    {
        var ms = new MemoryStream();
        // PNG Header magic bytes: 89 50 4E 47 0D 0A 1A 0A
        byte[] pngHeader = { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00, 0x00, 0x00, 0x0D };
        ms.Write(pngHeader, 0, pngHeader.Length);
        if (sizeInBytes > pngHeader.Length)
        {
            var remaining = new byte[sizeInBytes - pngHeader.Length];
            ms.Write(remaining, 0, remaining.Length);
        }
        ms.Position = 0;
        return ms;
    }

    private static MemoryStream CreateValidJpegStream(int sizeInBytes = 1024)
    {
        var ms = new MemoryStream();
        // JPEG Header magic bytes: FF D8 FF E0
        byte[] jpegHeader = { 0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10, 0x4A, 0x46, 0x49, 0x46, 0x00, 0x01 };
        ms.Write(jpegHeader, 0, jpegHeader.Length);
        if (sizeInBytes > jpegHeader.Length)
        {
            var remaining = new byte[sizeInBytes - jpegHeader.Length];
            ms.Write(remaining, 0, remaining.Length);
        }
        ms.Position = 0;
        return ms;
    }

    private static MemoryStream CreateValidWebpStream(int sizeInBytes = 1024)
    {
        var ms = new MemoryStream();
        // WEBP Header: "RIFF" .... "WEBP"
        byte[] webpHeader = { 0x52, 0x49, 0x46, 0x46, 0x00, 0x00, 0x00, 0x00, 0x57, 0x45, 0x42, 0x50 };
        ms.Write(webpHeader, 0, webpHeader.Length);
        if (sizeInBytes > webpHeader.Length)
        {
            var remaining = new byte[sizeInBytes - webpHeader.Length];
            ms.Write(remaining, 0, remaining.Length);
        }
        ms.Position = 0;
        return ms;
    }

    private static MemoryStream CreateValidMp4Stream(int sizeInBytes = 2048)
    {
        var ms = new MemoryStream();
        // MP4 Header: 4 bytes length + "ftyp"
        byte[] mp4Header = { 0x00, 0x00, 0x00, 0x18, 0x66, 0x74, 0x79, 0x70, 0x6D, 0x70, 0x34, 0x32 };
        ms.Write(mp4Header, 0, mp4Header.Length);
        if (sizeInBytes > mp4Header.Length)
        {
            var remaining = new byte[sizeInBytes - mp4Header.Length];
            ms.Write(remaining, 0, remaining.Length);
        }
        ms.Position = 0;
        return ms;
    }

    #endregion

    #region Upload Image Tests

    [Fact]
    public async Task UploadImageAsync_ValidPngImage_ReturnsMediaUploadResponse()
    {
        // Arrange
        using var stream = CreateValidPngStream(2048);
        var expectedResult = new CloudinaryMediaResult
        {
            PublicId = "veggielife/images/sample_avatar",
            Url = "http://res.cloudinary.com/veggielife/image/upload/sample_avatar.png",
            SecureUrl = "https://res.cloudinary.com/veggielife/image/upload/sample_avatar.png",
            ResourceType = "image",
            Format = "png",
            Bytes = 2048,
            Width = 500,
            Height = 500
        };

        _mockCloudinaryClient
            .Setup(c => c.UploadImageAsync(It.IsAny<Stream>(), "avatar.png", "veggielife/images", It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _mediaService.UploadImageAsync(stream, "avatar.png", "veggielife/images");

        // Assert
        result.Should().NotBeNull();
        result.PublicId.Should().Be("veggielife/images/sample_avatar");
        result.SecureUrl.Should().Be("https://res.cloudinary.com/veggielife/image/upload/sample_avatar.png");
        result.ResourceType.Should().Be("image");
        result.Format.Should().Be("png");
        result.Width.Should().Be(500);
        result.Height.Should().Be(500);
    }

    [Fact]
    public async Task UploadImageAsync_ValidJpegImage_ReturnsMediaUploadResponse()
    {
        // Arrange
        using var stream = CreateValidJpegStream(3000);
        var expectedResult = new CloudinaryMediaResult
        {
            PublicId = "veggielife/images/recipe_1",
            Url = "http://res.cloudinary.com/veggielife/image/upload/recipe_1.jpg",
            SecureUrl = "https://res.cloudinary.com/veggielife/image/upload/recipe_1.jpg",
            ResourceType = "image",
            Format = "jpg",
            Bytes = 3000,
            Width = 1080,
            Height = 720
        };

        _mockCloudinaryClient
            .Setup(c => c.UploadImageAsync(It.IsAny<Stream>(), "recipe.jpg", "veggielife/images", It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _mediaService.UploadImageAsync(stream, "recipe.jpg", "veggielife/images");

        // Assert
        result.Should().NotBeNull();
        result.PublicId.Should().Be("veggielife/images/recipe_1");
        result.Format.Should().Be("jpg");
    }

    [Fact]
    public async Task UploadImageAsync_ValidWebpImage_ReturnsMediaUploadResponse()
    {
        // Arrange
        using var stream = CreateValidWebpStream(1500);
        var expectedResult = new CloudinaryMediaResult
        {
            PublicId = "veggielife/images/dish_1",
            Url = "http://res.cloudinary.com/veggielife/image/upload/dish_1.webp",
            SecureUrl = "https://res.cloudinary.com/veggielife/image/upload/dish_1.webp",
            ResourceType = "image",
            Format = "webp",
            Bytes = 1500,
            Width = 800,
            Height = 600
        };

        _mockCloudinaryClient
            .Setup(c => c.UploadImageAsync(It.IsAny<Stream>(), "dish.webp", "veggielife/images", It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _mediaService.UploadImageAsync(stream, "dish.webp", "veggielife/images");

        // Assert
        result.Should().NotBeNull();
        result.Format.Should().Be("webp");
    }

    [Fact]
    public async Task UploadImageAsync_Exceeds5Mb_Throws413PayloadTooLarge()
    {
        // Arrange: BR-USR-MEDIA-01: Dung lượng tối đa 5 MB
        const int over5MbSize = 6 * 1024 * 1024; // 6 MB
        using var stream = CreateValidPngStream(over5MbSize);

        // Act
        Func<Task> act = async () => await _mediaService.UploadImageAsync(stream, "large_image.png");

        // Assert
        var exception = await act.Should().ThrowAsync<AppException>();
        exception.Which.StatusCode.Should().Be(StatusCodes.Status413PayloadTooLarge);
        exception.Which.ErrorCode.Should().Be(ErrorCodes.ErrMediaFileTooLarge);
    }

    [Fact]
    public async Task UploadImageAsync_InvalidExtension_Throws400BadRequest()
    {
        // Arrange: Đuôi tệp không thuộc jpeg, png, webp
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes("fake file content"));

        // Act
        Func<Task> act = async () => await _mediaService.UploadImageAsync(stream, "document.pdf");

        // Assert
        var exception = await act.Should().ThrowAsync<AppException>();
        exception.Which.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
        exception.Which.ErrorCode.Should().Be(ErrorCodes.ErrMediaInvalidFile);
    }

    [Fact]
    public async Task UploadImageAsync_FakeExtensionWithInvalidMagicBytes_Throws400BadRequest()
    {
        // Arrange: BR-USR-MEDIA-01: Chống giả mạo đuôi tệp
        // Đặt tên là avatar.png nhưng dữ liệu thực tế là chuỗi văn bản không có magic bytes PNG
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes("THIS_IS_NOT_A_PNG_FILE_CONTENT_AT_ALL"));

        // Act
        Func<Task> act = async () => await _mediaService.UploadImageAsync(stream, "avatar.png");

        // Assert
        var exception = await act.Should().ThrowAsync<AppException>();
        exception.Which.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
        exception.Which.ErrorCode.Should().Be(ErrorCodes.ErrMediaInvalidFile);
    }

    #endregion

    #region Upload Video Tests

    [Fact]
    public async Task UploadVideoAsync_ValidMp4Video_ReturnsMediaUploadResponse()
    {
        // Arrange
        using var stream = CreateValidMp4Stream(10240);
        var expectedResult = new CloudinaryMediaResult
        {
            PublicId = "veggielife/videos/cooking_1",
            Url = "http://res.cloudinary.com/veggielife/video/upload/cooking_1.mp4",
            SecureUrl = "https://res.cloudinary.com/veggielife/video/upload/cooking_1.mp4",
            ResourceType = "video",
            Format = "mp4",
            Bytes = 10240,
            Width = 1920,
            Height = 1080,
            Duration = 45.5
        };

        _mockCloudinaryClient
            .Setup(c => c.UploadVideoAsync(It.IsAny<Stream>(), "cooking.mp4", "veggielife/videos", It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _mediaService.UploadVideoAsync(stream, "cooking.mp4", "veggielife/videos");

        // Assert
        result.Should().NotBeNull();
        result.PublicId.Should().Be("veggielife/videos/cooking_1");
        result.ResourceType.Should().Be("video");
        result.DurationSeconds.Should().Be(45.5);
    }

    [Fact]
    public async Task UploadVideoAsync_Exceeds100Mb_Throws413PayloadTooLarge()
    {
        // Arrange: BR-AI-STT-01: Dung lượng tối đa 100 MB
        const long over100Mb = 105L * 1024 * 1024;
        // Giả lập stream kích thước lớn hơn 100MB
        var mockStream = new Mock<Stream>();
        mockStream.Setup(s => s.CanRead).Returns(true);
        mockStream.Setup(s => s.CanSeek).Returns(true);
        mockStream.Setup(s => s.Length).Returns(over100Mb);
        mockStream.Setup(s => s.Position).Returns(0);

        // Tạo custom stream để test kích thước vượt quá
        using var memoryStream = new MemoryStream();
        // Viết header MP4
        byte[] mp4Header = { 0x00, 0x00, 0x00, 0x18, 0x66, 0x74, 0x79, 0x70, 0x6D, 0x70, 0x34, 0x32 };
        memoryStream.Write(mp4Header);
        memoryStream.SetLength(over100Mb); // Đặt độ dài ảo vượt 100MB

        // Act
        Func<Task> act = async () => await _mediaService.UploadVideoAsync(memoryStream, "video_large.mp4");

        // Assert
        var exception = await act.Should().ThrowAsync<AppException>();
        exception.Which.StatusCode.Should().Be(StatusCodes.Status413PayloadTooLarge);
        exception.Which.ErrorCode.Should().Be(ErrorCodes.ErrMediaFileTooLarge);
    }

    [Fact]
    public async Task UploadVideoAsync_InvalidExtension_Throws400BadRequest()
    {
        // Arrange: Đuôi tệp không thuộc mp4, mov
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes("video content"));

        // Act
        Func<Task> act = async () => await _mediaService.UploadVideoAsync(stream, "video.avi");

        // Assert
        var exception = await act.Should().ThrowAsync<AppException>();
        exception.Which.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
        exception.Which.ErrorCode.Should().Be(ErrorCodes.ErrMediaInvalidFile);
    }

    #endregion

    #region Delete Resource Tests

    [Fact]
    public async Task DeleteResourceAsync_ValidPublicId_ReturnsMediaDeleteResponse()
    {
        // Arrange
        const string publicId = "veggielife/images/old_avatar";
        _mockCloudinaryClient
            .Setup(c => c.DeleteResourceAsync(publicId, "image", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CloudinaryDeleteResult
            {
                PublicId = publicId,
                Success = true,
                Result = "ok"
            });

        // Act
        var result = await _mediaService.DeleteResourceAsync(publicId, "image");

        // Assert
        result.Should().NotBeNull();
        result.PublicId.Should().Be(publicId);
        result.Success.Should().BeTrue();
    }

    [Fact]
    public async Task DeleteResourceAsync_EmptyPublicId_Throws400BadRequest()
    {
        // Act
        Func<Task> act = async () => await _mediaService.DeleteResourceAsync(string.Empty);

        // Assert
        var exception = await act.Should().ThrowAsync<AppException>();
        exception.Which.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
        exception.Which.ErrorCode.Should().Be(ErrorCodes.ErrValidation);
    }

    [Fact]
    public async Task DeleteFileAsync_CallsDeleteResourceAsync()
    {
        // Arrange
        const string publicId = "veggielife/images/avatar_to_delete";
        _mockCloudinaryClient
            .Setup(c => c.DeleteResourceAsync(publicId, "image", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CloudinaryDeleteResult { PublicId = publicId, Success = true, Result = "ok" });

        // Act
        await _mediaService.DeleteFileAsync(publicId);

        // Assert
        _mockCloudinaryClient.Verify(c => c.DeleteResourceAsync(publicId, "image", It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region URL Generation Tests

    [Fact]
    public void GetImageUrl_ValidParameters_ReturnsOptimizedUrl()
    {
        // Arrange
        const string publicId = "veggielife/images/sample";
        const string expectedUrl = "https://res.cloudinary.com/veggielife/image/upload/w_500,h_500,c_fill,f_webp/sample.webp";

        _mockCloudinaryClient
            .Setup(c => c.GetImageUrl(publicId, 500, 500, "fill", "webp"))
            .Returns(expectedUrl);

        // Act
        var url = _mediaService.GetImageUrl(publicId, 500, 500, "fill", "webp");

        // Assert
        url.Should().Be(expectedUrl);
    }

    [Fact]
    public void GetVideoUrl_ValidParameters_ReturnsVideoUrl()
    {
        // Arrange
        const string publicId = "veggielife/videos/sample";
        const string expectedUrl = "https://res.cloudinary.com/veggielife/video/upload/sample.mp4";

        _mockCloudinaryClient
            .Setup(c => c.GetVideoUrl(publicId, "mp4"))
            .Returns(expectedUrl);

        // Act
        var url = _mediaService.GetVideoUrl(publicId, "mp4");

        // Assert
        url.Should().Be(expectedUrl);
    }

    #endregion

    #region CRITICAL TEST: Giả lập lỗi mạng Cloudinary trả đúng mã lỗi 502 (ERR_EXT_CLOUDINARY_FAILED)

    [Fact]
    public async Task UploadImageAsync_CloudinaryNetworkFailure_Throws502BadGatewayWithProperErrorCode()
    {
        // Arrange: Tiêu chí nghiệm thu cốt lõi - Khi Cloudinary lỗi mạng thì báo đúng mã lỗi 502
        using var stream = CreateValidPngStream();

        _mockCloudinaryClient
            .Setup(c => c.UploadImageAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new CloudinaryException("Không thể kết nối đến máy chủ lưu trữ Cloudinary do sự cố mạng.", new HttpRequestException("Network connection failed.")));

        // Act
        Func<Task> act = async () => await _mediaService.UploadImageAsync(stream, "avatar.png");

        // Assert
        var exception = await act.Should().ThrowAsync<AppException>();
        exception.Which.StatusCode.Should().Be(StatusCodes.Status502BadGateway);
        exception.Which.ErrorCode.Should().Be(ErrorCodes.ErrExtCloudinaryFailed);
        exception.Which.Message.Should().Contain("Cloudinary");
    }

    [Fact]
    public async Task UploadImageAsync_HttpRequestException_Throws502BadGateway()
    {
        // Arrange: Mô phỏng ngoại lệ HttpRequestException trực tiếp
        using var stream = CreateValidJpegStream();

        _mockCloudinaryClient
            .Setup(c => c.UploadImageAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("Connection refused by Cloudinary host: api.cloudinary.com"));

        // Act
        Func<Task> act = async () => await _mediaService.UploadImageAsync(stream, "recipe.jpg");

        // Assert
        var exception = await act.Should().ThrowAsync<AppException>();
        exception.Which.StatusCode.Should().Be(StatusCodes.Status502BadGateway);
        exception.Which.ErrorCode.Should().Be(ErrorCodes.ErrExtCloudinaryFailed);
    }

    [Fact]
    public async Task UploadImageAsync_TimeoutException_Throws502BadGateway()
    {
        // Arrange: Mô phỏng lỗi Timeout khi tải ảnh
        using var stream = CreateValidWebpStream();

        _mockCloudinaryClient
            .Setup(c => c.UploadImageAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new TimeoutException("Yêu cầu gửi lên Cloudinary bị hết hạn (Timeout 30s)."));

        // Act
        Func<Task> act = async () => await _mediaService.UploadImageAsync(stream, "dish.webp");

        // Assert
        var exception = await act.Should().ThrowAsync<AppException>();
        exception.Which.StatusCode.Should().Be(StatusCodes.Status502BadGateway);
        exception.Which.ErrorCode.Should().Be(ErrorCodes.ErrExtCloudinaryFailed);
    }

    [Fact]
    public async Task UploadVideoAsync_CloudinaryNetworkTimeout_Throws502BadGateway()
    {
        // Arrange: Mô phỏng lỗi mạng khi tải video
        using var stream = CreateValidMp4Stream();

        _mockCloudinaryClient
            .Setup(c => c.UploadVideoAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new CloudinaryException("Kết nối đến Cloudinary bị quá thời gian chờ (Timeout).", new TimeoutException()));

        // Act
        Func<Task> act = async () => await _mediaService.UploadVideoAsync(stream, "video.mp4");

        // Assert
        var exception = await act.Should().ThrowAsync<AppException>();
        exception.Which.StatusCode.Should().Be(StatusCodes.Status502BadGateway);
        exception.Which.ErrorCode.Should().Be(ErrorCodes.ErrExtCloudinaryFailed);
    }

    [Fact]
    public async Task DeleteResourceAsync_CloudinaryNetworkError_Throws502BadGateway()
    {
        // Arrange: Mô phỏng lỗi mạng khi xóa tài nguyên
        _mockCloudinaryClient
            .Setup(c => c.DeleteResourceAsync("veggielife/images/item_1", "image", It.IsAny<CancellationToken>()))
            .ThrowsAsync(new CloudinaryException("Không thể kết nối đến máy chủ lưu trữ Cloudinary khi xóa tài nguyên."));

        // Act
        Func<Task> act = async () => await _mediaService.DeleteResourceAsync("veggielife/images/item_1");

        // Assert
        var exception = await act.Should().ThrowAsync<AppException>();
        exception.Which.StatusCode.Should().Be(StatusCodes.Status502BadGateway);
        exception.Which.ErrorCode.Should().Be(ErrorCodes.ErrExtCloudinaryFailed);
    }

    #endregion
}
