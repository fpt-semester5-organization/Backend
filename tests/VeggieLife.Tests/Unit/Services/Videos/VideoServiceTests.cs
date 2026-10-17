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

    [Fact]
    public async Task GetVideoTranscriptAsync_InvalidId_ThrowsBadRequest()
    {
        // Act
        var act = () => _videoService.GetVideoTranscriptAsync(0, false, null, null);

        // Assert
        var ex = await act.Should().ThrowAsync<AppException>();
        ex.Which.StatusCode.Should().Be(400);
        ex.Which.ErrorCode.Should().Be(ErrorCodes.ErrValidation);
    }

    [Fact]
    public async Task GetVideoTranscriptAsync_GuestQuotaExceeded_ThrowsForbidden()
    {
        // Arrange
        _mockRedisService.Setup(r => r.TryConsumeQuotaAsync("guest_quota:ai:guest-1", 3, It.IsAny<TimeSpan?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Allowed: false, Current: 3, Remaining: 0));

        // Act
        var act = () => _videoService.GetVideoTranscriptAsync(1, true, "guest-1", null);

        // Assert
        var ex = await act.Should().ThrowAsync<AppException>();
        ex.Which.StatusCode.Should().Be(403);
        ex.Which.ErrorCode.Should().Be(ErrorCodes.ErrQuotaAiExceeded);
    }

    [Fact]
    public async Task GetVideoTranscriptAsync_VideoNotFound_ThrowsNotFound()
    {
        // Act
        var act = () => _videoService.GetVideoTranscriptAsync(999, false, null, 1);

        // Assert
        var ex = await act.Should().ThrowAsync<AppException>();
        ex.Which.StatusCode.Should().Be(404);
        ex.Which.ErrorCode.Should().Be(ErrorCodes.ErrContentNotFound);
    }

    [Fact]
    public async Task GetVideoTranscriptAsync_TranscriptNotReady_ThrowsNotFound()
    {
        // Arrange
        var user = new User { UserId = 20, Email = "u20@veggie.vn", FullName = "U20", Role = "MEMBER", AccountStatus = "ACTIVE" };
        var post = new Post { PostId = 20, UserId = 20, Title = "P20", Content = "C20", ModerationStatus = "APPROVED_ACTIVE", Visibility = "PUBLIC" };
        var video = new Video { VideoId = 20, PostId = 20, Title = "Video 20", VideoUrl = "https://demo.mp4", Status = "PROCESSING" };
        _context.Users.Add(user);
        _context.Posts.Add(post);
        _context.Videos.Add(video);
        await _context.SaveChangesAsync();

        // Act
        var act = () => _videoService.GetVideoTranscriptAsync(20, false, null, 20);

        // Assert
        var ex = await act.Should().ThrowAsync<AppException>();
        ex.Which.StatusCode.Should().Be(404);
        ex.Which.ErrorCode.Should().Be(ErrorCodes.ErrTranscriptNotReady);
    }

    [Fact]
    public async Task GetVideoTranscriptAsync_ValidReadyVideo_ReturnsTranscriptAndExtractedRecipe()
    {
        // Arrange
        var user = new User { UserId = 30, Email = "u30@veggie.vn", FullName = "U30", Role = "MEMBER", AccountStatus = "ACTIVE" };
        var post = new Post { PostId = 30, UserId = 30, Title = "P30", Content = "C30", ModerationStatus = "APPROVED_ACTIVE", Visibility = "PUBLIC" };
        var video = new Video { VideoId = 30, PostId = 30, Title = "Video 30", VideoUrl = "https://demo30.mp4", Status = "READY" };
        var transcript = new VideoTranscript
        {
            TranscriptId = 30,
            VideoId = 30,
            RawTranscript = "Bước 1: Nấu canh cà chua đậu hũ.",
            ExtractedRecipeJson = "{\"title\":\"Canh cà chua đậu hũ\",\"ingredients\":[\"2 quả cà chua\",\"1 bìa đậu\"],\"steps\":[\"Cắt nhỏ\",\"Nấu 10p\"]}",
            Status = "READY"
        };
        _context.Users.Add(user);
        _context.Posts.Add(post);
        _context.Videos.Add(video);
        _context.VideoTranscripts.Add(transcript);
        await _context.SaveChangesAsync();

        // Act
        var result = await _videoService.GetVideoTranscriptAsync(30, false, null, 30);

        // Assert
        result.Should().NotBeNull();
        result.VideoId.Should().Be(30);
        result.RawTranscript.Should().Be("Bước 1: Nấu canh cà chua đậu hũ.");
        result.ExtractedRecipe.Should().NotBeNull();
        result.ExtractedRecipe!.Title.Should().Be("Canh cà chua đậu hũ");
        result.ExtractedRecipe.Ingredients.Should().HaveCount(2);
        result.ExtractedRecipe.Steps.Should().HaveCount(2);
    }

    [Fact]
    public async Task ExtractRecipeAsync_InvalidVideoId_ThrowsBadRequest()
    {
        // Act
        var act = () => _videoService.ExtractRecipeAsync(0, 1);

        // Assert
        var ex = await act.Should().ThrowAsync<AppException>();
        ex.Which.StatusCode.Should().Be(400);
        ex.Which.ErrorCode.Should().Be(ErrorCodes.ErrValidation);
    }

    [Fact]
    public async Task ExtractRecipeAsync_VideoNotFound_ThrowsNotFound()
    {
        // Act
        var act = () => _videoService.ExtractRecipeAsync(999, 1);

        // Assert
        var ex = await act.Should().ThrowAsync<AppException>();
        ex.Which.StatusCode.Should().Be(404);
        ex.Which.ErrorCode.Should().Be(ErrorCodes.ErrContentNotFound);
    }

    [Fact]
    public async Task ExtractRecipeAsync_NotOwner_ThrowsForbidden()
    {
        // Arrange
        var user = new User { UserId = 40, Email = "u40@veggie.vn", FullName = "U40", Role = "MEMBER", AccountStatus = "ACTIVE" };
        var post = new Post { PostId = 40, UserId = 40, Title = "P40", Content = "C40", ModerationStatus = "APPROVED_ACTIVE", Visibility = "PUBLIC" };
        var video = new Video { VideoId = 40, PostId = 40, Title = "Video 40", VideoUrl = "https://demo40.mp4", Status = "READY", DurationSeconds = 60 };
        _context.Users.Add(user);
        _context.Posts.Add(post);
        _context.Videos.Add(video);
        await _context.SaveChangesAsync();

        // Act: User 99 tries to extract video owned by User 40
        var act = () => _videoService.ExtractRecipeAsync(40, 99);

        // Assert
        var ex = await act.Should().ThrowAsync<AppException>();
        ex.Which.StatusCode.Should().Be(403);
        ex.Which.ErrorCode.Should().Be(ErrorCodes.ErrContentNotOwner);
    }

    [Theory]
    [InlineData(5)] // Less than 10s
    [InlineData(301)] // Greater than 300s
    public async Task ExtractRecipeAsync_InvalidDuration_ThrowsUnprocessableEntity(int duration)
    {
        // Arrange
        var user = new User { UserId = 50 + duration, Email = $"u{duration}@veggie.vn", FullName = "U", Role = "MEMBER", AccountStatus = "ACTIVE" };
        var post = new Post { PostId = 50 + duration, UserId = user.UserId, Title = "P", Content = "C", ModerationStatus = "APPROVED_ACTIVE", Visibility = "PUBLIC" };
        var video = new Video { VideoId = 50 + duration, PostId = post.PostId, Title = $"Video {duration}", VideoUrl = "https://demo.mp4", Status = "READY", DurationSeconds = duration };
        _context.Users.Add(user);
        _context.Posts.Add(post);
        _context.Videos.Add(video);
        await _context.SaveChangesAsync();

        // Act
        var act = () => _videoService.ExtractRecipeAsync(video.VideoId, user.UserId);

        // Assert
        var ex = await act.Should().ThrowAsync<AppException>();
        ex.Which.StatusCode.Should().Be(422);
        ex.Which.ErrorCode.Should().Be(ErrorCodes.ErrSttDurationInvalid);
    }

    [Fact]
    public async Task ExtractRecipeAsync_AlreadyDoneOrProcessing_ThrowsConflict()
    {
        // Arrange
        var user = new User { UserId = 60, Email = "u60@veggie.vn", FullName = "U60", Role = "MEMBER", AccountStatus = "ACTIVE" };
        var post = new Post { PostId = 60, UserId = 60, Title = "P60", Content = "C60", ModerationStatus = "APPROVED_ACTIVE", Visibility = "PUBLIC" };
        var video = new Video { VideoId = 60, PostId = 60, Title = "Video 60", VideoUrl = "https://demo60.mp4", Status = "READY", DurationSeconds = 90 };
        var transcript = new VideoTranscript { TranscriptId = 60, VideoId = 60, Status = "READY", RawTranscript = "done" };
        _context.Users.Add(user);
        _context.Posts.Add(post);
        _context.Videos.Add(video);
        _context.VideoTranscripts.Add(transcript);
        await _context.SaveChangesAsync();

        // Act
        var act = () => _videoService.ExtractRecipeAsync(60, 60);

        // Assert
        var ex = await act.Should().ThrowAsync<AppException>();
        ex.Which.StatusCode.Should().Be(409);
        ex.Which.ErrorCode.Should().Be(ErrorCodes.ErrSttAlreadyDone);
    }

    [Fact]
    public async Task ExtractRecipeAsync_Success_EnqueuesToRedisAndReturnsPending()
    {
        // Arrange
        var user = new User { UserId = 70, Email = "u70@veggie.vn", FullName = "U70", Role = "MEMBER", AccountStatus = "ACTIVE" };
        var post = new Post { PostId = 70, UserId = 70, Title = "P70", Content = "C70", ModerationStatus = "APPROVED_ACTIVE", Visibility = "PUBLIC" };
        var video = new Video { VideoId = 70, PostId = 70, Title = "Video 70", VideoUrl = "https://demo70.mp4", Status = "READY", DurationSeconds = 120 };
        _context.Users.Add(user);
        _context.Posts.Add(post);
        _context.Videos.Add(video);
        await _context.SaveChangesAsync();

        // Act
        var result = await _videoService.ExtractRecipeAsync(70, 70);

        // Assert
        result.Should().NotBeNull();
        result.VideoId.Should().Be(70);
        result.TranscriptStatus.Should().Be("PENDING");

        var updatedVideo = await _context.Videos.FindAsync(70);
        updatedVideo!.Status.Should().Be("PROCESSING");

        var createdTranscript = await _context.VideoTranscripts.FirstOrDefaultAsync(t => t.VideoId == 70);
        createdTranscript.Should().NotBeNull();
        createdTranscript!.Status.Should().Be("PENDING");

        _mockRedisService.Verify(r => r.EnqueueAsync("stt_queue", It.IsAny<object>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}
