using System;
using System.Buffers.Binary;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using VeggieLife.BLL.Common.Constants;
using VeggieLife.BLL.Common.Exceptions;
using VeggieLife.BLL.Services.Media;
using VeggieLife.DAL.External.Cloudinary;
using Xunit;

namespace VeggieLife.Tests.Unit.Services.Media;

public class MediaServiceDurationTests
{
    private readonly Mock<ICloudinaryClient> _mockCloudinaryClient = new();
    private readonly Mock<ILogger<MediaService>> _mockLogger = new();
    private readonly MediaService _mediaService;

    public MediaServiceDurationTests()
    {
        _mediaService = new MediaService(_mockCloudinaryClient.Object, _mockLogger.Object);
    }

    /// <summary>
    /// Tạo byte stream cho tệp MP4 tối thiểu với header ftyp hợp lệ.
    /// Có thể kèm atom moov->mvhd với duration cấu hình sẵn.
    /// </summary>
    private static byte[] CreateMockMp4Bytes(double? durationSeconds = null)
    {
        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);

        // ftyp box (16 bytes)
        Span<byte> ftypHeader = stackalloc byte[16];
        BinaryPrimitives.WriteUInt32BigEndian(ftypHeader[..4], 16);
        Encoding.ASCII.GetBytes("ftyp").CopyTo(ftypHeader.Slice(4, 4));
        Encoding.ASCII.GetBytes("isom").CopyTo(ftypHeader.Slice(8, 4));
        BinaryPrimitives.WriteUInt32BigEndian(ftypHeader.Slice(12, 4), 512);
        writer.Write(ftypHeader);

        if (durationSeconds.HasValue)
        {
            uint timescale = 1000;
            uint duration = (uint)(durationSeconds.Value * timescale);

            // mvhd box payload (20 bytes for version 0)
            var mvhdPayload = new byte[20];
            mvhdPayload[0] = 0; // version 0
            BinaryPrimitives.WriteUInt32BigEndian(mvhdPayload.AsSpan(12, 4), timescale);
            BinaryPrimitives.WriteUInt32BigEndian(mvhdPayload.AsSpan(16, 4), duration);

            // mvhd header (8 bytes) + payload = 28 bytes
            var mvhdBox = new byte[28];
            BinaryPrimitives.WriteUInt32BigEndian(mvhdBox.AsSpan(0, 4), 28);
            Encoding.ASCII.GetBytes("mvhd").CopyTo(mvhdBox.AsSpan(4, 4));
            mvhdPayload.CopyTo(mvhdBox, 8);

            // moov header (8 bytes) + mvhdBox (28 bytes) = 36 bytes
            var moovBox = new byte[36];
            BinaryPrimitives.WriteUInt32BigEndian(moovBox.AsSpan(0, 4), 36);
            Encoding.ASCII.GetBytes("moov").CopyTo(moovBox.AsSpan(4, 4));
            mvhdBox.CopyTo(moovBox, 8);

            writer.Write(moovBox);
        }

        return ms.ToArray();
    }

    [Fact]
    public async Task UploadVideoAsync_WhenDurationExceeds300SecondsFromCloudinary_RejectsWith422AndDeletesResource()
    {
        // Arrange: Video 6 phút (360 giây) trả về từ Cloudinary
        var mp4Bytes = CreateMockMp4Bytes(); // không nhúng mvhd để test nhánh Cloudinary response
        using var stream = new MemoryStream(mp4Bytes);

        _mockCloudinaryClient.Setup(c => c.UploadVideoAsync(
            It.IsAny<Stream>(),
            "cooking_6min.mp4",
            "veggielife/videos",
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CloudinaryMediaResult
            {
                PublicId = "vid_6min",
                Url = "http://res.cloudinary.com/demo/video/upload/vid_6min.mp4",
                SecureUrl = "https://res.cloudinary.com/demo/video/upload/vid_6min.mp4",
                Duration = 360.0, // 6 phút = 360 giây > 300s
                ResourceType = "video"
            });

        _mockCloudinaryClient.Setup(c => c.DeleteResourceAsync("vid_6min", "video", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CloudinaryDeleteResult { PublicId = "vid_6min", Success = true });

        // Act
        var act = () => _mediaService.UploadVideoAsync(stream, "cooking_6min.mp4");

        // Assert
        var ex = await act.Should().ThrowAsync<AppException>();
        ex.Which.StatusCode.Should().Be(422);
        ex.Which.ErrorCode.Should().Be(ErrorCodes.ErrSttDurationInvalid);
        ex.Which.Message.Should().Contain("10 giây đến tối đa 5 phút");

        // Đảm bảo đã gọi xóa tài nguyên thu hồi trên Cloudinary
        _mockCloudinaryClient.Verify(c => c.DeleteResourceAsync("vid_6min", "video", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UploadVideoAsync_WhenDurationLessThan10SecondsFromCloudinary_RejectsWith422AndDeletesResource()
    {
        // Arrange: Video ngắn 5 giây
        var mp4Bytes = CreateMockMp4Bytes();
        using var stream = new MemoryStream(mp4Bytes);

        _mockCloudinaryClient.Setup(c => c.UploadVideoAsync(
            It.IsAny<Stream>(),
            "cooking_short.mp4",
            "veggielife/videos",
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CloudinaryMediaResult
            {
                PublicId = "vid_short",
                Duration = 5.0, // 5s < 10s
                ResourceType = "video"
            });

        _mockCloudinaryClient.Setup(c => c.DeleteResourceAsync("vid_short", "video", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CloudinaryDeleteResult { PublicId = "vid_short", Success = true });

        // Act
        var act = () => _mediaService.UploadVideoAsync(stream, "cooking_short.mp4");

        // Assert
        var ex = await act.Should().ThrowAsync<AppException>();
        ex.Which.StatusCode.Should().Be(422);
        ex.Which.ErrorCode.Should().Be(ErrorCodes.ErrSttDurationInvalid);

        _mockCloudinaryClient.Verify(c => c.DeleteResourceAsync("vid_short", "video", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UploadVideoAsync_WhenPreCheckDetects6MinutesInHeader_RejectsImmediatelyWithoutUploading()
    {
        // Arrange: Video 360 giây (6 phút) nhúng ngay trong atom mvhd của MP4
        var mp4Bytes = CreateMockMp4Bytes(durationSeconds: 360.0);
        using var stream = new MemoryStream(mp4Bytes);

        // Act
        var act = () => _mediaService.UploadVideoAsync(stream, "sample_6min.mp4");

        // Assert
        var ex = await act.Should().ThrowAsync<AppException>();
        ex.Which.StatusCode.Should().Be(422);
        ex.Which.ErrorCode.Should().Be(ErrorCodes.ErrSttDurationInvalid);

        // Đảm bảo không gọi lên Cloudinary vì đã chặn sớm
        _mockCloudinaryClient.Verify(c => c.UploadVideoAsync(
            It.IsAny<Stream>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UploadVideoAsync_WhenPreCheckDetectsLessThan10SecondsInHeader_RejectsImmediatelyWithoutUploading()
    {
        // Arrange: Video 7 giây nhúng trong atom mvhd của MP4
        var mp4Bytes = CreateMockMp4Bytes(durationSeconds: 7.0);
        using var stream = new MemoryStream(mp4Bytes);

        // Act
        var act = () => _mediaService.UploadVideoAsync(stream, "sample_short.mp4");

        // Assert
        var ex = await act.Should().ThrowAsync<AppException>();
        ex.Which.StatusCode.Should().Be(422);
        ex.Which.ErrorCode.Should().Be(ErrorCodes.ErrSttDurationInvalid);

        _mockCloudinaryClient.Verify(c => c.UploadVideoAsync(
            It.IsAny<Stream>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData(10.0)]   // Biên dưới hợp lệ
    [InlineData(60.0)]   // 1 phút hợp lệ
    [InlineData(180.0)]  // 3 phút hợp lệ
    [InlineData(300.0)]  // Biên trên hợp lệ (5 phút)
    public async Task UploadVideoAsync_WhenDurationWithinAllowedRange_Succeeds(double durationSeconds)
    {
        // Arrange
        var mp4Bytes = CreateMockMp4Bytes();
        using var stream = new MemoryStream(mp4Bytes);

        _mockCloudinaryClient.Setup(c => c.UploadVideoAsync(
            It.IsAny<Stream>(),
            "valid_video.mp4",
            "veggielife/videos",
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CloudinaryMediaResult
            {
                PublicId = "vid_valid",
                SecureUrl = "https://res.cloudinary.com/demo/video/upload/vid_valid.mp4",
                Duration = durationSeconds,
                ResourceType = "video"
            });

        // Act
        var result = await _mediaService.UploadVideoAsync(stream, "valid_video.mp4");

        // Assert
        result.Should().NotBeNull();
        result.DurationSeconds.Should().Be(durationSeconds);
        result.PublicId.Should().Be("vid_valid");
    }
}
