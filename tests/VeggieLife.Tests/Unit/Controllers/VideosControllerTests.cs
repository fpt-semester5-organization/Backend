using System;
using System.IO;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using VeggieLife.API.Controllers;
using VeggieLife.BLL.Common.Constants;
using VeggieLife.BLL.Common.Exceptions;
using VeggieLife.BLL.Common.Responses;
using VeggieLife.BLL.DTOs.Videos.Requests;
using VeggieLife.BLL.DTOs.Videos.Responses;
using VeggieLife.BLL.Services.Videos;
using Xunit;

namespace VeggieLife.Tests.Unit.Controllers;

public class VideosControllerTests
{
    private readonly Mock<IVideoService> _mockVideoService = new();
    private readonly VideosController _controller;

    public VideosControllerTests()
    {
        _controller = new VideosController(_mockVideoService.Object);
        SetUserContext(null, null);
    }

    private void SetUserContext(string? userId, string? role)
    {
        var claims = new System.Collections.Generic.List<Claim>();
        if (userId != null)
        {
            claims.Add(new Claim(ClaimTypes.NameIdentifier, userId));
        }
        if (role != null)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        var identity = new ClaimsIdentity(claims, userId != null ? "TestAuth" : null);
        var principal = new ClaimsPrincipal(identity);

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = principal }
        };
    }

    [Fact]
    public async Task CreateVideo_WhenUserIsGuest_ThrowsForbiddenCommunityActionDenied()
    {
        // Arrange
        SetUserContext(userId: "100", role: UserRoles.Guest);

        // Act
        var act = () => _controller.CreateVideo(file: null, postId: 1, recipeId: null, title: null, CancellationToken.None);

        // Assert
        var ex = await act.Should().ThrowAsync<AppException>();
        ex.Which.StatusCode.Should().Be(403);
        ex.Which.ErrorCode.Should().Be(ErrorCodes.ErrGuestCommunityActionDenied);
    }

    [Fact]
    public async Task CreateVideo_WhenUserIdIsMissing_ThrowsUnauthorized()
    {
        // Arrange
        SetUserContext(userId: null, role: UserRoles.Member);

        // Act
        var act = () => _controller.CreateVideo(file: null, postId: 1, recipeId: null, title: null, CancellationToken.None);

        // Assert
        var ex = await act.Should().ThrowAsync<AppException>();
        ex.Which.StatusCode.Should().Be(401);
        ex.Which.ErrorCode.Should().Be(ErrorCodes.ErrAuthInvalidToken);
    }

    [Fact]
    public async Task CreateVideo_WhenFileIsNull_ThrowsBadRequest()
    {
        // Arrange
        SetUserContext(userId: "1", role: UserRoles.Member);

        // Act
        var act = () => _controller.CreateVideo(file: null, postId: 10, recipeId: null, title: null, CancellationToken.None);

        // Assert
        var ex = await act.Should().ThrowAsync<AppException>();
        ex.Which.StatusCode.Should().Be(400);
        ex.Which.ErrorCode.Should().Be(ErrorCodes.ErrValidation);
    }

    [Fact]
    public async Task CreateVideo_WhenVideoUploadSucceeds_ReturnsAccepted202WithProcessingStatus()
    {
        // Arrange
        SetUserContext(userId: "1", role: UserRoles.Member);

        var formFileMock = new Mock<IFormFile>();
        var stream = new MemoryStream(new byte[] { 1, 2, 3 });
        formFileMock.Setup(f => f.OpenReadStream()).Returns(stream);
        formFileMock.Setup(f => f.FileName).Returns("demo.mp4");
        formFileMock.Setup(f => f.Length).Returns(3);

        _mockVideoService.Setup(s => s.CreateVideoAsync(1, It.IsAny<CreateVideoDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CreateVideoResponseData
            {
                VideoId = 88,
                Status = "PROCESSING"
            });

        // Act
        var result = await _controller.CreateVideo(
            file: formFileMock.Object,
            postId: 10,
            recipeId: null,
            title: "Video test",
            ct: CancellationToken.None);

        // Assert
        var objectResult = result.Should().BeOfType<ObjectResult>().Subject;
        objectResult.StatusCode.Should().Be(StatusCodes.Status202Accepted);

        var response = objectResult.Value.Should().BeOfType<ApiResponse<CreateVideoResponseData>>().Subject;
        response.StatusCode.Should().Be(202);
        response.Message.Should().Be("Đã nhận video, đang xử lý.");
        response.Data!.VideoId.Should().Be(88);
        response.Data.Status.Should().Be("PROCESSING");
    }

    [Fact]
    public async Task CreateVideo_WhenImageUploadSucceeds_ReturnsAccepted202WithReadyStatus()
    {
        // Arrange
        SetUserContext(userId: "1", role: UserRoles.Member);

        var formFileMock = new Mock<IFormFile>();
        var stream = new MemoryStream(new byte[] { 1, 2, 3 });
        formFileMock.Setup(f => f.OpenReadStream()).Returns(stream);
        formFileMock.Setup(f => f.FileName).Returns("demo.jpg");
        formFileMock.Setup(f => f.Length).Returns(3);

        _mockVideoService.Setup(s => s.CreateVideoAsync(1, It.IsAny<CreateVideoDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CreateVideoResponseData
            {
                VideoId = null,
                Status = "READY",
                ImageUrl = "https://res.cloudinary.com/demo/image/upload/demo.jpg"
            });

        // Act
        var result = await _controller.CreateVideo(
            file: formFileMock.Object,
            postId: 10,
            recipeId: null,
            title: null,
            ct: CancellationToken.None);

        // Assert
        var objectResult = result.Should().BeOfType<ObjectResult>().Subject;
        objectResult.StatusCode.Should().Be(StatusCodes.Status202Accepted);

        var response = objectResult.Value.Should().BeOfType<ApiResponse<CreateVideoResponseData>>().Subject;
        response.StatusCode.Should().Be(202);
        response.Message.Should().Be("Đã tải ảnh lên thành công.");
        response.Data!.Status.Should().Be("READY");
        response.Data.ImageUrl.Should().Be("https://res.cloudinary.com/demo/image/upload/demo.jpg");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task GetVideoTranscript_WhenIdIsInvalid_ThrowsBadRequest(int videoId)
    {
        // Act
        var act = () => _controller.GetVideoTranscript(videoId, CancellationToken.None);

        // Assert
        var ex = await act.Should().ThrowAsync<AppException>();
        ex.Which.StatusCode.Should().Be(400);
        ex.Which.ErrorCode.Should().Be(ErrorCodes.ErrValidation);
    }

    [Fact]
    public async Task GetVideoTranscript_WhenGuestUser_PassesGuestUuidAndReturnsOk200()
    {
        // Arrange
        SetUserContext(userId: null, role: null);
        _controller.HttpContext.Request.Headers["X-Guest-Id"] = "guest-guid-12345";

        var transcriptData = new VideoTranscriptResponseData
        {
            VideoId = 10,
            RawTranscript = "Bước 1...",
            Quota = new VideoTranscriptQuotaDto { Used = 1, Limit = 3, Remaining = 2 }
        };

        _mockVideoService.Setup(s => s.GetVideoTranscriptAsync(10, true, "guest-guid-12345", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(transcriptData);

        // Act
        var result = await _controller.GetVideoTranscript(10, CancellationToken.None);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(StatusCodes.Status200OK);

        var response = okResult.Value.Should().BeOfType<ApiResponse<VideoTranscriptResponseData>>().Subject;
        response.StatusCode.Should().Be(200);
        response.Data!.VideoId.Should().Be(10);
        response.Data.Quota!.Remaining.Should().Be(2);
    }

    [Fact]
    public async Task GetVideoTranscript_WhenMemberUser_PassesUserIdAndReturnsOk200()
    {
        // Arrange
        SetUserContext(userId: "5", role: UserRoles.Member);

        var transcriptData = new VideoTranscriptResponseData
        {
            VideoId = 12,
            RawTranscript = "Công thức nấu chay..."
        };

        _mockVideoService.Setup(s => s.GetVideoTranscriptAsync(12, false, null, 5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(transcriptData);

        // Act
        var result = await _controller.GetVideoTranscript(12, CancellationToken.None);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(StatusCodes.Status200OK);

        var response = okResult.Value.Should().BeOfType<ApiResponse<VideoTranscriptResponseData>>().Subject;
        response.StatusCode.Should().Be(200);
        response.Data!.VideoId.Should().Be(12);
        response.Data.Quota.Should().BeNull();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public async Task ExtractRecipe_WhenIdIsInvalid_ThrowsBadRequest(int videoId)
    {
        // Arrange
        SetUserContext(userId: "5", role: UserRoles.Member);

        // Act
        var act = () => _controller.ExtractRecipe(videoId, CancellationToken.None);

        // Assert
        var ex = await act.Should().ThrowAsync<AppException>();
        ex.Which.StatusCode.Should().Be(400);
        ex.Which.ErrorCode.Should().Be(ErrorCodes.ErrValidation);
    }

    [Fact]
    public async Task ExtractRecipe_WhenGuestUser_ThrowsForbiddenGuestActionDenied()
    {
        // Arrange
        SetUserContext(userId: "100", role: UserRoles.Guest);

        // Act
        var act = () => _controller.ExtractRecipe(15, CancellationToken.None);

        // Assert
        var ex = await act.Should().ThrowAsync<AppException>();
        ex.Which.StatusCode.Should().Be(403);
        ex.Which.ErrorCode.Should().Be(ErrorCodes.ErrGuestCommunityActionDenied);
    }

    [Fact]
    public async Task ExtractRecipe_WhenUserIdIsMissing_ThrowsUnauthorized()
    {
        // Arrange
        SetUserContext(userId: null, role: UserRoles.Member);

        // Act
        var act = () => _controller.ExtractRecipe(15, CancellationToken.None);

        // Assert
        var ex = await act.Should().ThrowAsync<AppException>();
        ex.Which.StatusCode.Should().Be(401);
        ex.Which.ErrorCode.Should().Be(ErrorCodes.ErrAuthInvalidToken);
    }

    [Fact]
    public async Task ExtractRecipe_WhenMemberUser_CallsServiceAndReturnsAccepted202()
    {
        // Arrange
        SetUserContext(userId: "5", role: UserRoles.Member);

        _mockVideoService.Setup(s => s.ExtractRecipeAsync(20, 5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ExtractVideoRecipeResponseData
            {
                VideoId = 20,
                TranscriptStatus = "PENDING"
            });

        // Act
        var result = await _controller.ExtractRecipe(20, CancellationToken.None);

        // Assert
        var objectResult = result.Should().BeOfType<ObjectResult>().Subject;
        objectResult.StatusCode.Should().Be(StatusCodes.Status202Accepted);

        var response = objectResult.Value.Should().BeOfType<ApiResponse<ExtractVideoRecipeResponseData>>().Subject;
        response.StatusCode.Should().Be(202);
        response.Message.Should().Be("Đang bóc tách công thức.");
        response.Data!.VideoId.Should().Be(20);
        response.Data.TranscriptStatus.Should().Be("PENDING");
    }
}
