using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using VeggieLife.API.Controllers;
using VeggieLife.BLL.Common.Responses;
using VeggieLife.BLL.DTOs.Posts.Requests;
using VeggieLife.BLL.DTOs.Posts.Responses;
using VeggieLife.BLL.Services.Posts;
using Xunit;

namespace VeggieLife.Tests.Unit.Controllers;

/// <summary>
/// Unit Tests cho PostsController (FN-POST-05, FN-POST-06).
/// Kiểm thử cấu trúc envelope response chuẩn D52, PagedMeta, và phân quyền Guest/Member.
/// </summary>
public class PostsControllerTests
{
    private readonly Mock<IPostService> _mockService;
    private readonly Mock<VeggieLife.BLL.Services.Community.ICommunityInteractionService> _mockCommunityService;
    private readonly PostsController _controller;

    public PostsControllerTests()
    {
        _mockService = new Mock<IPostService>();
        _mockCommunityService = new Mock<VeggieLife.BLL.Services.Community.ICommunityInteractionService>();
        _controller = new PostsController(_mockService.Object, _mockCommunityService.Object);

        var httpContext = new DefaultHttpContext();
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };
    }

    [Fact]
    public async Task ListPosts_GuestUser_ShouldReturnPagedResponseWithSignUpPrompt()
    {
        // Arrange
        var filter = new PostFilterRequest { Keyword = "ăn chay" };
        var sampleList = new List<PostSummaryDto>
        {
            new()
            {
                PostId = 77,
                Title = "Một tuần ăn chay đầu tiên",
                AuthorName = "Nguyễn An",
                ImageUrl = "https://res.cloudinary.com/veggielife/p/77.webp",
                VoteCount = 12,
                CommentCount = 3,
                CreatedAt = new DateTime(2026, 10, 1, 9, 0, 0, DateTimeKind.Utc)
            }
        };
        var meta = new PagedMeta(1, 10, 1, signUpPromptAtItem: 5);

        _mockService.Setup(s => s.ListPostsAsync(filter, true, It.IsAny<string?>(), null, It.IsAny<CancellationToken>()))
            .ReturnsAsync((sampleList, meta));

        // Act
        var result = await _controller.ListPosts(filter, CancellationToken.None);

        // Assert
        var okResult = result as OkObjectResult;
        okResult.Should().NotBeNull();
        var envelope = okResult!.Value as ApiResponse<IReadOnlyList<PostSummaryDto>>;
        envelope.Should().NotBeNull();
        envelope!.StatusCode.Should().Be(200);
        envelope.Message.Should().Be("OK");
        envelope.Data.Should().HaveCount(1);
        envelope.Data![0].PostId.Should().Be(77);
        envelope.Data[0].Title.Should().Be("Một tuần ăn chay đầu tiên");
        envelope.Data[0].AuthorName.Should().Be("Nguyễn An");
        envelope.Data[0].ImageUrl.Should().Be("https://res.cloudinary.com/veggielife/p/77.webp");
        envelope.Data[0].VoteCount.Should().Be(12);
        envelope.Data[0].CommentCount.Should().Be(3);
        envelope.Meta.Should().NotBeNull();
        envelope.Meta!.Page.Should().Be(1);
        envelope.Meta.Size.Should().Be(10);
        envelope.Meta.Total.Should().Be(1);
        envelope.Meta.SignUpPromptAtItem.Should().Be(5);
    }

    [Fact]
    public async Task ListPosts_AuthenticatedMember_ShouldPassUserIdToService()
    {
        // Arrange
        var filter = new PostFilterRequest();
        var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "1024"),
            new Claim(ClaimTypes.Role, "MEMBER")
        }, "TestAuth"));

        _controller.ControllerContext.HttpContext.User = user;

        var sampleList = new List<PostSummaryDto>();
        var meta = new PagedMeta(1, 10, 0);

        _mockService.Setup(s => s.ListPostsAsync(filter, false, It.IsAny<string?>(), 1024, It.IsAny<CancellationToken>()))
            .ReturnsAsync((sampleList, meta));

        // Act
        var result = await _controller.ListPosts(filter, CancellationToken.None);

        // Assert
        var okResult = result as OkObjectResult;
        okResult.Should().NotBeNull();
        _mockService.Verify(s => s.ListPostsAsync(filter, false, It.IsAny<string?>(), 1024, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetPost_GuestUser_ShouldReturnOkEnvelopeWithGuestMemberActions()
    {
        // Arrange
        var postId = 77;
        var detailDto = new PostDetailDto
        {
            PostId = 77,
            Title = "Một tuần ăn chay đầu tiên",
            Content = "Nội dung chia sẻ...",
            ImageUrl = null,
            Author = new PostAuthorDto { UserId = 1024, FullName = "Nguyễn An" },
            Videos = new List<PostVideoDto>(),
            Recipes = new List<PostRecipeDto>(),
            VoteCount = 12,
            CommentCount = 3,
            MemberActions = new PostMemberActionsDto
            {
                CanVote = false,
                CanComment = false
            }
        };

        _mockService.Setup(s => s.GetPostByIdAsync(postId, true, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(detailDto);

        // Act
        var result = await _controller.GetPost(postId, CancellationToken.None);

        // Assert
        var okResult = result as OkObjectResult;
        okResult.Should().NotBeNull();
        var envelope = okResult!.Value as ApiResponse<PostDetailDto>;
        envelope.Should().NotBeNull();
        envelope!.StatusCode.Should().Be(200);
        envelope.Message.Should().Be("OK");
        envelope.Data.Should().NotBeNull();
        envelope.Data!.PostId.Should().Be(77);
        envelope.Data.Title.Should().Be("Một tuần ăn chay đầu tiên");
        envelope.Data.Author.UserId.Should().Be(1024);
        envelope.Data.Author.FullName.Should().Be("Nguyễn An");
        envelope.Data.MemberActions.CanVote.Should().BeFalse();
        envelope.Data.MemberActions.CanComment.Should().BeFalse();
    }

    [Fact]
    public async Task GetPost_AuthenticatedMember_ShouldPassUserIdAndReturnActiveMemberActions()
    {
        // Arrange
        var postId = 77;
        var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "1024"),
            new Claim(ClaimTypes.Role, "MEMBER")
        }, "TestAuth"));

        _controller.ControllerContext.HttpContext.User = user;

        var detailDto = new PostDetailDto
        {
            PostId = 77,
            Title = "Một tuần ăn chay đầu tiên",
            Content = "Nội dung...",
            Author = new PostAuthorDto { UserId = 1024, FullName = "Nguyễn An" },
            MemberActions = new PostMemberActionsDto
            {
                CanVote = true,
                CanComment = true
            }
        };

        _mockService.Setup(s => s.GetPostByIdAsync(postId, false, 1024, It.IsAny<CancellationToken>()))
            .ReturnsAsync(detailDto);

        // Act
        var result = await _controller.GetPost(postId, CancellationToken.None);

        // Assert
        var okResult = result as OkObjectResult;
        okResult.Should().NotBeNull();
        var envelope = okResult!.Value as ApiResponse<PostDetailDto>;
        envelope.Should().NotBeNull();
        envelope!.Data!.MemberActions.CanVote.Should().BeTrue();
        envelope.Data.MemberActions.CanComment.Should().BeTrue();
        _mockService.Verify(s => s.GetPostByIdAsync(postId, false, 1024, It.IsAny<CancellationToken>()), Times.Once);
    }

    // =========================================================================
    // FN-POST-07: CONTROLLER TESTS (CREATE, UPDATE, DELETE)
    // =========================================================================

    [Fact]
    public async Task CreatePost_Unauthenticated_ShouldThrowUnauthorized_401()
    {
        // Arrange: Chưa đăng nhập
        _controller.ControllerContext.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity());

        var req = new CreatePostRequest { Title = "Tiêu đề hợp lệ từ 10 ký tự", Content = "Nội dung bài viết..." };

        var act = async () => await _controller.CreatePost(req, CancellationToken.None);
        var ex = await act.Should().ThrowAsync<VeggieLife.BLL.Common.Exceptions.AppException>();
        ex.Which.StatusCode.Should().Be(401);
        ex.Which.ErrorCode.Should().Be(VeggieLife.BLL.Common.Constants.ErrorCodes.ErrAuthInvalidToken);
    }

    [Fact]
    public async Task CreatePost_GuestUser_ShouldThrowForbidden_403()
    {
        // Arrange: Khách vãng lai (GUEST)
        var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "guest-uuid-123"),
            new Claim(ClaimTypes.Role, "GUEST")
        }, "GuestAuth"));
        _controller.ControllerContext.HttpContext.User = user;

        var req = new CreatePostRequest { Title = "Tiêu đề hợp lệ từ 10 ký tự", Content = "Nội dung bài viết..." };

        var act = async () => await _controller.CreatePost(req, CancellationToken.None);
        var ex = await act.Should().ThrowAsync<VeggieLife.BLL.Common.Exceptions.AppException>();
        ex.Which.StatusCode.Should().Be(403);
        ex.Which.ErrorCode.Should().Be(VeggieLife.BLL.Common.Constants.ErrorCodes.ErrGuestCommunityActionDenied);
    }

    [Fact]
    public async Task CreatePost_AuthenticatedMember_ShouldReturnCreated201()
    {
        // Arrange: Member hợp lệ
        var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "1024"),
            new Claim(ClaimTypes.Role, "MEMBER")
        }, "TestAuth"));
        _controller.ControllerContext.HttpContext.User = user;

        var req = new CreatePostRequest
        {
            Title = "Một tuần ăn chay đầu tiên",
            Content = "Chia sẻ trải nghiệm ăn chay...",
            Visibility = "PUBLIC",
            Submit = true
        };

        var responseDto = new CreatePostResponseDto
        {
            PostId = 77,
            ModerationStatus = "APPROVED_ACTIVE"
        };

        _mockService.Setup(s => s.CreatePostAsync(req, 1024, "MEMBER", It.IsAny<CancellationToken>()))
            .ReturnsAsync(responseDto);

        // Act
        var result = await _controller.CreatePost(req, CancellationToken.None);

        // Assert
        var objectResult = result as ObjectResult;
        objectResult.Should().NotBeNull();
        objectResult!.StatusCode.Should().Be(201);

        var envelope = objectResult.Value as ApiResponse<CreatePostResponseDto>;
        envelope.Should().NotBeNull();
        envelope!.StatusCode.Should().Be(201);
        envelope.Message.Should().Be("Đã đăng bài.");
        envelope.Data.Should().NotBeNull();
        envelope.Data!.PostId.Should().Be(77);
        envelope.Data.ModerationStatus.Should().Be("APPROVED_ACTIVE");
    }

    [Fact]
    public async Task UpdatePost_AuthenticatedMember_ShouldReturnOk200()
    {
        // Arrange: Member hợp lệ
        var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "1024"),
            new Claim(ClaimTypes.Role, "MEMBER")
        }, "TestAuth"));
        _controller.ControllerContext.HttpContext.User = user;

        var req = new UpdatePostRequest
        {
            Title = "Một tuần ăn chay đầu tiên (bản sửa)"
        };

        var responseDto = new UpdatePostResponseDto
        {
            PostId = 77,
            ModerationStatus = "PENDING_REVIEW"
        };

        _mockService.Setup(s => s.UpdatePostAsync(77, req, 1024, "MEMBER", It.IsAny<CancellationToken>()))
            .ReturnsAsync(responseDto);

        // Act
        var result = await _controller.UpdatePost(77, req, CancellationToken.None);

        // Assert
        var okResult = result as OkObjectResult;
        okResult.Should().NotBeNull();
        okResult!.StatusCode.Should().Be(200);

        var envelope = okResult.Value as ApiResponse<UpdatePostResponseDto>;
        envelope.Should().NotBeNull();
        envelope!.StatusCode.Should().Be(200);
        envelope.Message.Should().Be("Đã cập nhật.");
        envelope.Data!.PostId.Should().Be(77);
        envelope.Data.ModerationStatus.Should().Be("PENDING_REVIEW");
    }

    [Fact]
    public async Task DeletePost_AuthenticatedMember_ShouldReturnOk200WithNullData()
    {
        // Arrange: Member hợp lệ
        var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "1024"),
            new Claim(ClaimTypes.Role, "MEMBER")
        }, "TestAuth"));
        _controller.ControllerContext.HttpContext.User = user;

        _mockService.Setup(s => s.DeletePostAsync(77, 1024, "MEMBER", It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.DeletePost(77, CancellationToken.None);

        // Assert
        var okResult = result as OkObjectResult;
        okResult.Should().NotBeNull();
        okResult!.StatusCode.Should().Be(200);

        var envelope = okResult.Value as ApiResponse<object?>;
        envelope.Should().NotBeNull();
        envelope!.StatusCode.Should().Be(200);
        envelope.Message.Should().Be("Đã xóa bài viết.");
        envelope.Data.Should().BeNull();
    }

    [Fact]
    public async Task CreatePostVote_AuthenticatedMember_ShouldReturnOk200WithVoteData()
    {
        // Arrange
        var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "1024"),
            new Claim(ClaimTypes.Role, "MEMBER")
        }, "TestAuth"));
        _controller.ControllerContext.HttpContext.User = user;

        var voteData = new VeggieLife.BLL.DTOs.Community.Responses.VoteResponseData
        {
            Voted = true,
            VoteCount = 13
        };

        _mockCommunityService
            .Setup(s => s.VotePostAsync(77, 1024, It.IsAny<CancellationToken>()))
            .ReturnsAsync(voteData);

        // Act
        var result = await _controller.CreatePostVote(77, CancellationToken.None);

        // Assert
        var okResult = result as OkObjectResult;
        okResult.Should().NotBeNull();
        okResult!.StatusCode.Should().Be(200);

        var envelope = okResult.Value as ApiResponse<VeggieLife.BLL.DTOs.Community.Responses.VoteResponseData>;
        envelope.Should().NotBeNull();
        envelope!.StatusCode.Should().Be(200);
        envelope.Message.Should().Be("OK");
        envelope.Data!.Voted.Should().BeTrue();
        envelope.Data.VoteCount.Should().Be(13);
    }

    [Fact]
    public async Task CreatePostVote_GuestUser_ShouldThrowForbidden403()
    {
        // Arrange: Khách vãng lai
        var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "guest-uuid-123"),
            new Claim(ClaimTypes.Role, "GUEST")
        }, "TestAuth"));
        _controller.ControllerContext.HttpContext.User = user;

        // Act & Assert
        var act = () => _controller.CreatePostVote(77, CancellationToken.None);
        var ex = await act.Should().ThrowAsync<VeggieLife.BLL.Common.Exceptions.AppException>();
        ex.Which.StatusCode.Should().Be(403);
        ex.Which.ErrorCode.Should().Be(VeggieLife.BLL.Common.Constants.ErrorCodes.ErrGuestCommunityActionDenied);
    }
}

