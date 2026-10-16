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
using VeggieLife.BLL.Common.Constants;
using VeggieLife.BLL.Common.Exceptions;
using VeggieLife.BLL.Common.Responses;
using VeggieLife.BLL.DTOs.Community.Requests;
using VeggieLife.BLL.DTOs.Community.Responses;
using VeggieLife.BLL.Services.Community;
using VeggieLife.BLL.Services.Posts;
using VeggieLife.BLL.Services.Recipes;
using Xunit;

namespace VeggieLife.Tests.Unit.Controllers;

/// <summary>
/// Unit Tests cho CommentsController, PostsController (comments), và RecipesController (comments) (FN-COMM-01, FN-COMM-02, FN-COMM-03).
/// </summary>
public class CommentsControllerTests
{
    private readonly Mock<ICommentService> _mockCommentService;
    private readonly CommentsController _commentsController;

    private readonly Mock<IPostService> _mockPostService;
    private readonly PostsController _postsController;

    private readonly Mock<IRecipeService> _mockRecipeService;
    private readonly RecipesController _recipesController;

    public CommentsControllerTests()
    {
        _mockCommentService = new Mock<ICommentService>();

        _commentsController = new CommentsController(_mockCommentService.Object)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };

        _mockPostService = new Mock<IPostService>();
        _postsController = new PostsController(_mockPostService.Object, null, _mockCommentService.Object)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };

        _mockRecipeService = new Mock<IRecipeService>();
        _recipesController = new RecipesController(_mockRecipeService.Object, null, null, _mockCommentService.Object)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };
    }

    private static void SetUserContext(ControllerBase controller, string? userId, string role, bool isAuthenticated = true)
    {
        var claims = new List<Claim>();
        if (!string.IsNullOrEmpty(userId))
        {
            claims.Add(new Claim(ClaimTypes.NameIdentifier, userId));
        }
        if (!string.IsNullOrEmpty(role))
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        var identity = isAuthenticated
            ? new ClaimsIdentity(claims, "TestAuth")
            : new ClaimsIdentity();

        controller.ControllerContext.HttpContext.User = new ClaimsPrincipal(identity);
    }

    // =========================================================================
    // FN-COMM-01: PostsController & RecipesController - List comments
    // =========================================================================

    [Fact]
    public async Task ListPostComments_PublicOrGuest_Returns200WithPagedMeta()
    {
        var expectedItems = new List<CommentDto>
        {
            new()
            {
                CommentId = 301,
                Author = new CommentAuthorDto { UserId = 10, FullName = "Nguyễn An" },
                Content = "Món này rất thanh đạm!",
                Status = "ACTIVE",
                CreatedAt = DateTime.UtcNow,
                Replies = new List<CommentDto>()
            }
        };
        var expectedMeta = new PagedMeta(1, 20, 1);

        _mockCommentService
            .Setup(s => s.ListPostCommentsAsync(1, 1, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync((expectedItems, expectedMeta));

        var result = await _postsController.ListPostComments(1, 1, 20, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
        var okResult = (OkObjectResult)result;
        var apiResponse = okResult.Value as ApiResponse<IReadOnlyList<CommentDto>>;
        apiResponse.Should().NotBeNull();
        apiResponse!.StatusCode.Should().Be(200);
        apiResponse.Message.Should().Be("OK");
        apiResponse.Data.Should().HaveCount(1);
        apiResponse.Meta.Should().NotBeNull();
        apiResponse.Meta!.Total.Should().Be(1);
    }

    [Fact]
    public async Task ListRecipeComments_PublicOrGuest_Returns200WithPagedMeta()
    {
        var expectedItems = new List<CommentDto>();
        var expectedMeta = new PagedMeta(1, 20, 0);

        _mockCommentService
            .Setup(s => s.ListRecipeCommentsAsync(10, 1, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync((expectedItems, expectedMeta));

        var result = await _recipesController.ListRecipeComments(10, 1, 20, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
        var okResult = (OkObjectResult)result;
        var apiResponse = okResult.Value as ApiResponse<IReadOnlyList<CommentDto>>;
        apiResponse.Should().NotBeNull();
        apiResponse!.StatusCode.Should().Be(200);
        apiResponse.Meta!.Total.Should().Be(0);
    }

    // =========================================================================
    // FN-COMM-02: CreatePostComment & CreateRecipeComment
    // =========================================================================

    [Fact]
    public async Task CreatePostComment_WhenCalledByMember_ReturnsCreated201()
    {
        SetUserContext(_postsController, "10", "MEMBER", true);
        var req = new CreateCommentRequest { Content = "Bài viết rất hữu ích!" };
        var created = new CommentDto
        {
            CommentId = 301,
            Author = new CommentAuthorDto { UserId = 10, FullName = "Nguyễn An" },
            Content = req.Content,
            Status = "ACTIVE",
            CreatedAt = DateTime.UtcNow
        };

        _mockCommentService
            .Setup(s => s.CreatePostCommentAsync(1, req, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(created);

        var result = await _postsController.CreatePostComment(1, req, CancellationToken.None);

        result.Should().BeOfType<ObjectResult>();
        var objResult = (ObjectResult)result;
        objResult.StatusCode.Should().Be(StatusCodes.Status201Created);

        var apiResponse = objResult.Value as ApiResponse<CommentDto>;
        apiResponse.Should().NotBeNull();
        apiResponse!.StatusCode.Should().Be(201);
        apiResponse.Message.Should().Be("Đã bình luận.");
        apiResponse.Data!.CommentId.Should().Be(301);
    }

    [Fact]
    public async Task CreatePostComment_WhenCalledByGuest_ThrowsForbidden403()
    {
        SetUserContext(_postsController, "guest-1", "GUEST", true);
        var req = new CreateCommentRequest { Content = "Bài viết rất hữu ích!" };

        var act = () => _postsController.CreatePostComment(1, req, CancellationToken.None);
        var ex = await act.Should().ThrowAsync<AppException>();
        ex.Which.StatusCode.Should().Be(403);
        ex.Which.ErrorCode.Should().Be(ErrorCodes.ErrGuestCommunityActionDenied);
    }

    [Fact]
    public async Task CreatePostComment_WhenUnauthenticated_ThrowsUnauthorized401()
    {
        SetUserContext(_postsController, null, "", false);
        var req = new CreateCommentRequest { Content = "Bài viết rất hữu ích!" };

        var act = () => _postsController.CreatePostComment(1, req, CancellationToken.None);
        var ex = await act.Should().ThrowAsync<AppException>();
        ex.Which.StatusCode.Should().Be(401);
        ex.Which.ErrorCode.Should().Be(ErrorCodes.ErrAuthInvalidToken);
    }

    [Fact]
    public async Task CreateRecipeComment_WhenCalledByMember_ReturnsCreated201()
    {
        SetUserContext(_recipesController, "10", "MEMBER", true);
        var req = new CreateCommentRequest { Content = "Nấu thử rất ngon!" };
        var created = new CommentDto
        {
            CommentId = 302,
            Author = new CommentAuthorDto { UserId = 10, FullName = "Nguyễn An" },
            Content = req.Content,
            Status = "ACTIVE",
            CreatedAt = DateTime.UtcNow
        };

        _mockCommentService
            .Setup(s => s.CreateRecipeCommentAsync(5, req, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(created);

        var result = await _recipesController.CreateRecipeComment(5, req, CancellationToken.None);

        result.Should().BeOfType<ObjectResult>();
        var objResult = (ObjectResult)result;
        objResult.StatusCode.Should().Be(StatusCodes.Status201Created);
        var apiResponse = objResult.Value as ApiResponse<CommentDto>;
        apiResponse!.Message.Should().Be("Đã bình luận.");
        apiResponse.Data!.CommentId.Should().Be(302);
    }

    // =========================================================================
    // FN-COMM-03: CommentsController - Update & Delete
    // =========================================================================

    [Fact]
    public async Task UpdateComment_WhenCalledByAuthor_ReturnsOk200()
    {
        SetUserContext(_commentsController, "10", "MEMBER", true);
        var req = new UpdateCommentRequest { Content = "Nội dung đã sửa" };
        var updated = new CommentDto
        {
            CommentId = 301,
            Author = new CommentAuthorDto { UserId = 10, FullName = "Nguyễn An" },
            Content = req.Content,
            Status = "ACTIVE",
            CreatedAt = DateTime.UtcNow
        };

        _mockCommentService
            .Setup(s => s.UpdateCommentAsync(301, req, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(updated);

        var result = await _commentsController.UpdateComment(301, req, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
        var okResult = (OkObjectResult)result;
        var apiResponse = okResult.Value as ApiResponse<CommentDto>;
        apiResponse.Should().NotBeNull();
        apiResponse!.StatusCode.Should().Be(200);
        apiResponse.Message.Should().Be("Đã cập nhật.");
        apiResponse.Data!.Content.Should().Be("Nội dung đã sửa");
    }

    [Fact]
    public async Task UpdateComment_WhenCalledByGuest_ThrowsForbidden403()
    {
        SetUserContext(_commentsController, "guest-1", "GUEST", true);
        var req = new UpdateCommentRequest { Content = "Nội dung sửa" };

        var act = () => _commentsController.UpdateComment(301, req, CancellationToken.None);
        var ex = await act.Should().ThrowAsync<AppException>();
        ex.Which.StatusCode.Should().Be(403);
        ex.Which.ErrorCode.Should().Be(ErrorCodes.ErrGuestCommunityActionDenied);
    }

    [Fact]
    public async Task DeleteComment_WhenCalledByAuthor_ReturnsOk200WithHiddenReplies()
    {
        SetUserContext(_commentsController, "10", "MEMBER", true);
        var deleteData = new DeleteCommentResponseData { HiddenReplies = 2 };

        _mockCommentService
            .Setup(s => s.DeleteCommentAsync(301, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(deleteData);

        var result = await _commentsController.DeleteComment(301, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
        var okResult = (OkObjectResult)result;
        var apiResponse = okResult.Value as ApiResponse<DeleteCommentResponseData>;
        apiResponse.Should().NotBeNull();
        apiResponse!.StatusCode.Should().Be(200);
        apiResponse.Message.Should().Be("Đã xóa bình luận.");
        apiResponse.Data!.HiddenReplies.Should().Be(2);
    }

    [Fact]
    public async Task DeleteComment_WhenUnauthenticated_ThrowsUnauthorized401()
    {
        SetUserContext(_commentsController, null, "", false);

        var act = () => _commentsController.DeleteComment(301, CancellationToken.None);
        var ex = await act.Should().ThrowAsync<AppException>();
        ex.Which.StatusCode.Should().Be(401);
        ex.Which.ErrorCode.Should().Be(ErrorCodes.ErrAuthInvalidToken);
    }
}
