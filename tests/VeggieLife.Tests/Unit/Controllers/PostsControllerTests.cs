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
    private readonly PostsController _controller;

    public PostsControllerTests()
    {
        _mockService = new Mock<IPostService>();
        _controller = new PostsController(_mockService.Object);

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
}
