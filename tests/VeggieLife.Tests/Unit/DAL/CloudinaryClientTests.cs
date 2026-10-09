using System;
using System.IO;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using VeggieLife.DAL.External.Cloudinary;
using Xunit;

namespace VeggieLife.Tests.Unit.DAL;

/// <summary>
/// Kiểm thử đơn vị cho tầng DAL CloudinaryClient.
/// </summary>
public class CloudinaryClientTests
{
    private readonly Mock<ILogger<CloudinaryClient>> _mockLogger;

    public CloudinaryClientTests()
    {
        _mockLogger = new Mock<ILogger<CloudinaryClient>>();
    }

    [Fact]
    public async Task UploadImageAsync_MissingConfiguration_ThrowsCloudinaryException()
    {
        // Arrange: Cấu hình rỗng
        var emptyOptions = Options.Create(new CloudinarySettings
        {
            CloudName = "",
            ApiKey = "",
            ApiSecret = ""
        });

        var client = new CloudinaryClient(emptyOptions, _mockLogger.Object);
        using var stream = new MemoryStream(new byte[] { 0x89, 0x50, 0x4E, 0x47 });

        // Act
        Func<Task> act = async () => await client.UploadImageAsync(stream, "test.png");

        // Assert
        var exception = await act.Should().ThrowAsync<CloudinaryException>();
        exception.Which.Message.Should().Contain("Cấu hình tài khoản Cloudinary chưa hoàn tất");
    }

    [Fact]
    public async Task UploadVideoAsync_MissingConfiguration_ThrowsCloudinaryException()
    {
        // Arrange: Cấu hình rỗng
        var emptyOptions = Options.Create(new CloudinarySettings
        {
            CloudName = "",
            ApiKey = "",
            ApiSecret = ""
        });

        var client = new CloudinaryClient(emptyOptions, _mockLogger.Object);
        using var stream = new MemoryStream(new byte[] { 0x00, 0x00, 0x00, 0x18 });

        // Act
        Func<Task> act = async () => await client.UploadVideoAsync(stream, "test.mp4");

        // Assert
        var exception = await act.Should().ThrowAsync<CloudinaryException>();
        exception.Which.Message.Should().Contain("Cấu hình tài khoản Cloudinary chưa hoàn tất");
    }

    [Fact]
    public void GetImageUrl_FullUrl_ReturnsUrlDirectly()
    {
        // Arrange
        var options = Options.Create(new CloudinarySettings
        {
            CloudName = "test_cloud",
            ApiKey = "123456789",
            ApiSecret = "secret"
        });
        var client = new CloudinaryClient(options, _mockLogger.Object);
        const string fullUrl = "https://res.cloudinary.com/demo/image/upload/sample.jpg";

        // Act
        var result = client.GetImageUrl(fullUrl);

        // Assert
        result.Should().Be(fullUrl);
    }

    [Fact]
    public void GetImageUrl_EmptyPublicId_ReturnsEmptyString()
    {
        // Arrange
        var options = Options.Create(new CloudinarySettings());
        var client = new CloudinaryClient(options, _mockLogger.Object);

        // Act
        var result = client.GetImageUrl("");

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void GetVideoUrl_FullUrl_ReturnsUrlDirectly()
    {
        // Arrange
        var options = Options.Create(new CloudinarySettings
        {
            CloudName = "test_cloud",
            ApiKey = "123456789",
            ApiSecret = "secret"
        });
        var client = new CloudinaryClient(options, _mockLogger.Object);
        const string fullUrl = "https://res.cloudinary.com/demo/video/upload/sample.mp4";

        // Act
        var result = client.GetVideoUrl(fullUrl);

        // Assert
        result.Should().Be(fullUrl);
    }
}
