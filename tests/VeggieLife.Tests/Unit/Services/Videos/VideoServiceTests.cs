using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using VeggieLife.BLL.Common.Constants;
using VeggieLife.BLL.Common.Exceptions;
using VeggieLife.BLL.DTOs.Media.Responses;
using VeggieLife.BLL.DTOs.Videos.Requests;
using VeggieLife.BLL.Services.Cache;
using VeggieLife.BLL.Services.Media;
using VeggieLife.BLL.Services.Videos;
using VeggieLife.DAL.Context;
using VeggieLife.DAL.Entities;
using Xunit;

namespace VeggieLife.Tests.Unit.Services.Videos;

public class VideoServiceTests : IDisposable
{
    private readonly AppDbContext _context;
    private readonly Mock<IMediaService> _mockMediaService = new();
    private readonly Mock<IRedisService> _mockRedisService = new();
    private readonly Mock<ILogger<VideoService>> _mockLogger = new();
    private readonly VideoService _videoService;

    public VideoServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: "VideoServiceTests_" + Guid.NewGuid())
            .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        _context = new AppDbContext(options);
        _videoService = new VideoService(_context, _mockMediaService.Object, _mockRedisService.Object, _mockLogger.Object);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    [Fact]
    public async Task CreateVideoAsync_WhenFileStreamIsNull_ThrowsBadRequest()
    {
        // Arrange
        var dto = new CreateVideoDto
        {
            FileStream = null!,
            FileName = "test.mp4",
            FileLength = 0,
            PostId = 1
        };

        // Act
        var act = () => _videoService.CreateVideoAsync(1, dto);

        // Assert
        var ex = await act.Should().ThrowAsync<AppException>();
        ex.Which.StatusCode.Should().Be(400);
        ex.Which.ErrorCode.Should().Be(ErrorCodes.ErrValidation);
    }

    [Fact]
    public async Task CreateVideoAsync_WhenBothPostIdAndRecipeIdAreNull_ThrowsBadRequest()
    {
        // Arrange
        using var stream = new MemoryStream(new byte[] { 1, 2, 3 });
        var dto = new CreateVideoDto
        {
            FileStream = stream,
            FileName = "test.mp4",
            FileLength = 3,
            PostId = null,
            RecipeId = null
        };

        // Act
        var act = () => _videoService.CreateVideoAsync(1, dto);

        // Assert
        var ex = await act.Should().ThrowAsync<AppException>();
        ex.Which.StatusCode.Should().Be(400);
        ex.Which.ErrorCode.Should().Be(ErrorCodes.ErrValidation);
    }

    [Fact]
    public async Task CreateVideoAsync_WhenBothPostIdAndRecipeIdAreProvided_ThrowsBadRequest()
    {
        // Arrange
        using var stream = new MemoryStream(new byte[] { 1, 2, 3 });
        var dto = new CreateVideoDto
        {
            FileStream = stream,
            FileName = "test.mp4",
            FileLength = 3,
            PostId = 1,
            RecipeId = 2
        };

        // Act
        var act = () => _videoService.CreateVideoAsync(1, dto);

        // Assert
        var ex = await act.Should().ThrowAsync<AppException>();
        ex.Which.StatusCode.Should().Be(400);
        ex.Which.ErrorCode.Should().Be(ErrorCodes.ErrValidation);
    }

    [Fact]
    public async Task CreateVideoAsync_WhenPostIdIsZeroOrNegative_ThrowsBadRequest()
    {
        // Arrange
        using var stream = new MemoryStream(new byte[] { 1, 2, 3 });
        var dto = new CreateVideoDto
        {
            FileStream = stream,
            FileName = "test.mp4",
            FileLength = 3,
            PostId = 0
        };

        // Act
        var act = () => _videoService.CreateVideoAsync(1, dto);

        // Assert
        var ex = await act.Should().ThrowAsync<AppException>();
        ex.Which.StatusCode.Should().Be(400);
        ex.Which.ErrorCode.Should().Be(ErrorCodes.ErrValidation);
    }

    [Fact]
    public async Task CreateVideoAsync_WhenTitleExceeds150Chars_ThrowsBadRequest()
    {
        // Arrange
        using var stream = new MemoryStream(new byte[] { 1, 2, 3 });
        var dto = new CreateVideoDto
        {
            FileStream = stream,
            FileName = "test.mp4",
            FileLength = 3,
            PostId = 1,
            Title = new string('A', 151)
        };

        // Act
        var act = () => _videoService.CreateVideoAsync(1, dto);

        // Assert
        var ex = await act.Should().ThrowAsync<AppException>();
        ex.Which.StatusCode.Should().Be(400);
        ex.Which.ErrorCode.Should().Be(ErrorCodes.ErrValidation);
    }

    [Fact]
    public async Task CreateVideoAsync_WhenTargetPostNotFound_ThrowsForbiddenNotOwner()
    {
        // Arrange
        using var stream = new MemoryStream(new byte[] { 1, 2, 3 });
        var dto = new CreateVideoDto
        {
            FileStream = stream,
            FileName = "test.mp4",
            FileLength = 3,
            PostId = 999
        };

        // Act
        var act = () => _videoService.CreateVideoAsync(1, dto);

        // Assert
        var ex = await act.Should().ThrowAsync<AppException>();
        ex.Which.StatusCode.Should().Be(403);
        ex.Which.ErrorCode.Should().Be(ErrorCodes.ErrContentNotOwner);
    }

    [Fact]
    public async Task CreateVideoAsync_WhenUserIsNotOwnerOfPost_ThrowsForbiddenNotOwner()
    {
        // Arrange
        var post = new Post
        {
            PostId = 10,
            UserId = 2, // Khác userId = 1
            Title = "Bài viết của người khác",
            Content = "Nội dung",
            Visibility = "PUBLIC",
            ModerationStatus = "APPROVED_ACTIVE"
        };
        _context.Posts.Add(post);
        await _context.SaveChangesAsync();

        using var stream = new MemoryStream(new byte[] { 1, 2, 3 });
        var dto = new CreateVideoDto
        {
            FileStream = stream,
            FileName = "test.mp4",
            FileLength = 3,
            PostId = 10
        };

        // Act
        var act = () => _videoService.CreateVideoAsync(1, dto);

        // Assert
        var ex = await act.Should().ThrowAsync<AppException>();
        ex.Which.StatusCode.Should().Be(403);
        ex.Which.ErrorCode.Should().Be(ErrorCodes.ErrContentNotOwner);
    }

    [Fact]
    public async Task CreateVideoAsync_WhenFileExtensionNotSupported_ThrowsBadRequest()
    {
        // Arrange
        var post = new Post
        {
            PostId = 20,
            UserId = 1,
            Title = "Bài viết của tôi",
            Content = "Nội dung",
            Visibility = "PUBLIC",
            ModerationStatus = "APPROVED_ACTIVE"
        };
        _context.Posts.Add(post);
        await _context.SaveChangesAsync();

        using var stream = new MemoryStream(new byte[] { 1, 2, 3 });
        var dto = new CreateVideoDto
        {
            FileStream = stream,
            FileName = "document.pdf",
            FileLength = 3,
            PostId = 20
        };

        // Act
        var act = () => _videoService.CreateVideoAsync(1, dto);

        // Assert
        var ex = await act.Should().ThrowAsync<AppException>();
        ex.Which.StatusCode.Should().Be(400);
        ex.Which.ErrorCode.Should().Be(ErrorCodes.ErrMediaInvalidFile);
    }

    [Fact]
    public async Task CreateVideoAsync_WhenImageExceeds5MB_ThrowsPayloadTooLarge()
    {
        // Arrange
        var post = new Post
        {
            PostId = 30,
            UserId = 1,
            Title = "Bài viết của tôi",
            Content = "Nội dung",
            Visibility = "PUBLIC",
            ModerationStatus = "APPROVED_ACTIVE"
        };
        _context.Posts.Add(post);
        await _context.SaveChangesAsync();

        using var stream = new MemoryStream(new byte[10]);
        var dto = new CreateVideoDto
        {
            FileStream = stream,
            FileName = "photo.jpg",
            FileLength = 6 * 1024 * 1024, // 6 MB
            PostId = 30
        };

        // Act
        var act = () => _videoService.CreateVideoAsync(1, dto);

        // Assert
        var ex = await act.Should().ThrowAsync<AppException>();
        ex.Which.StatusCode.Should().Be(413);
        ex.Which.ErrorCode.Should().Be(ErrorCodes.ErrMediaFileTooLarge);
    }

    [Fact]
    public async Task CreateVideoAsync_WhenVideoExceeds100MB_ThrowsPayloadTooLarge()
    {
        // Arrange
        var post = new Post
        {
            PostId = 40,
            UserId = 1,
            Title = "Bài viết của tôi",
            Content = "Nội dung",
            Visibility = "PUBLIC",
            ModerationStatus = "APPROVED_ACTIVE"
        };
        _context.Posts.Add(post);
        await _context.SaveChangesAsync();

        using var stream = new MemoryStream(new byte[10]);
        var dto = new CreateVideoDto
        {
            FileStream = stream,
            FileName = "video.mp4",
            FileLength = 101 * 1024 * 1024, // 101 MB
            PostId = 40
        };

        // Act
        var act = () => _videoService.CreateVideoAsync(1, dto);

        // Assert
        var ex = await act.Should().ThrowAsync<AppException>();
        ex.Which.StatusCode.Should().Be(413);
        ex.Which.ErrorCode.Should().Be(ErrorCodes.ErrMediaFileTooLarge);
    }

    [Fact]
    public async Task CreateVideoAsync_WhenImageUploadSucceeds_UpdatesPostImageUrlAndReturnsReady()
    {
        // Arrange
        var post = new Post
        {
            PostId = 50,
            UserId = 1,
            Title = "Bài viết chuẩn bị thêm ảnh",
            Content = "Nội dung bài viết",
            Visibility = "PUBLIC",
            ModerationStatus = "APPROVED_ACTIVE"
        };
        _context.Posts.Add(post);
        await _context.SaveChangesAsync();

        _mockMediaService.Setup(m => m.UploadImageAsync(
            It.IsAny<Stream>(),
            It.IsAny<string>(),
            "veggielife/images",
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MediaUploadResponse
            {
                PublicId = "img_50",
                SecureUrl = "https://res.cloudinary.com/demo/image/upload/img_50.jpg",
                ResourceType = "image"
            });

        using var stream = new MemoryStream(new byte[100]);
        var dto = new CreateVideoDto
        {
            FileStream = stream,
            FileName = "dish.png",
            FileLength = 100,
            PostId = 50
        };

        // Act
        var result = await _videoService.CreateVideoAsync(1, dto);

        // Assert
        result.Should().NotBeNull();
        result.VideoId.Should().BeNull();
        result.Status.Should().Be("READY");
        result.ImageUrl.Should().Be("https://res.cloudinary.com/demo/image/upload/img_50.jpg");

        var updatedPost = await _context.Posts.FindAsync(50);
        updatedPost!.ImageUrl.Should().Be("https://res.cloudinary.com/demo/image/upload/img_50.jpg");
    }

    [Fact]
    public async Task CreateVideoAsync_WhenValidVideoUploaded_InsertsVideoRecordAndEnqueuesStt()
    {
        // Arrange
        var post = new Post
        {
            PostId = 60,
            UserId = 1,
            Title = "Cách làm đậu phụ sốt cà chua",
            Content = "Hướng dẫn chi tiết",
            Visibility = "PUBLIC",
            ModerationStatus = "APPROVED_ACTIVE"
        };
        _context.Posts.Add(post);
        await _context.SaveChangesAsync();

        _mockMediaService.Setup(m => m.UploadVideoAsync(
            It.IsAny<Stream>(),
            It.IsAny<string>(),
            "veggielife/videos",
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MediaUploadResponse
            {
                PublicId = "vid_60",
                SecureUrl = "https://res.cloudinary.com/demo/video/upload/vid_60.mp4",
                ResourceType = "video",
                DurationSeconds = 120.0 // 2 phút (hợp lệ trong khoảng 10s - 300s)
            });

        _mockMediaService.Setup(m => m.GetImageUrl("vid_60", null, null, "fill", "jpg"))
            .Returns("https://res.cloudinary.com/demo/video/upload/vid_60.jpg");

        using var stream = new MemoryStream(new byte[1000]);
        var dto = new CreateVideoDto
        {
            FileStream = stream,
            FileName = "cooking.mp4",
            FileLength = 1000,
            PostId = 60,
            Title = "Video hướng dẫn sốt cà chua"
        };

        // Act
        var result = await _videoService.CreateVideoAsync(1, dto);

        // Assert
        result.Should().NotBeNull();
        result.VideoId.Should().BePositive();
        result.Status.Should().Be("PROCESSING");

        var video = await _context.Videos.FirstOrDefaultAsync(v => v.VideoId == result.VideoId);
        video.Should().NotBeNull();
        video!.PostId.Should().Be(60);
        video.Title.Should().Be("Video hướng dẫn sốt cà chua");
        video.VideoUrl.Should().Be("https://res.cloudinary.com/demo/video/upload/vid_60.mp4");
        video.ThumbnailUrl.Should().Be("https://res.cloudinary.com/demo/video/upload/vid_60.jpg");
        video.DurationSeconds.Should().Be(120);
        video.Status.Should().Be("PROCESSING");

        _mockRedisService.Verify(r => r.EnqueueAsync(
            "stt_queue",
            It.IsAny<object>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateVideoAsync_WhenRecipeIdProvided_CreatesRepresentativePostIfMissing()
    {
        // Arrange
        var recipe = new Recipe
        {
            RecipeId = 70,
            UserId = 1,
            CategoryId = 1,
            DietarySchoolId = 1,
            Title = "Canh chua chay",
            Instructions = "Bước 1...",
            CaloriesKcal = 150,
            DifficultyLevel = "MEDIUM",
            Source = "USER",
            Status = "APPROVED_ACTIVE"
        };
        _context.Recipes.Add(recipe);
        await _context.SaveChangesAsync();

        _mockMediaService.Setup(m => m.UploadVideoAsync(
            It.IsAny<Stream>(),
            It.IsAny<string>(),
            "veggielife/videos",
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MediaUploadResponse
            {
                PublicId = "vid_70",
                SecureUrl = "https://res.cloudinary.com/demo/video/upload/vid_70.mp4",
                ResourceType = "video",
                DurationSeconds = 45.0
            });

        _mockMediaService.Setup(m => m.GetImageUrl("vid_70", null, null, "fill", "jpg"))
            .Returns("https://res.cloudinary.com/demo/video/upload/vid_70.jpg");

        using var stream = new MemoryStream(new byte[500]);
        var dto = new CreateVideoDto
        {
            FileStream = stream,
            FileName = "recipe_soup.mov",
            FileLength = 500,
            RecipeId = 70
        };

        // Act
        var result = await _videoService.CreateVideoAsync(1, dto);

        // Assert
        result.Should().NotBeNull();
        result.VideoId.Should().BePositive();
        result.Status.Should().Be("PROCESSING");

        var createdPost = await _context.Posts.FirstOrDefaultAsync(p => p.RecipeId == 70);
        createdPost.Should().NotBeNull();
        createdPost!.UserId.Should().Be(1);

        var video = await _context.Videos.FirstOrDefaultAsync(v => v.VideoId == result.VideoId);
        video!.PostId.Should().Be(createdPost.PostId);
    }
}
