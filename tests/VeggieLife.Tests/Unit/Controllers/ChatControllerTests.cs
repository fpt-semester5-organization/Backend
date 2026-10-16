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
using VeggieLife.BLL.DTOs.Chat.Requests;
using VeggieLife.BLL.DTOs.Chat.Responses;
using VeggieLife.BLL.Services.Chat;
using Xunit;

namespace VeggieLife.Tests.Unit.Controllers;

/// <summary>
/// Unit Tests cho ChatController (FN-CHAT-01..05).
/// Kiểm thử cấu trúc envelope response D52, phân quyền Guest/Member, và xử lý mã lỗi hợp lệ.
/// </summary>
public class ChatControllerTests
{
    private readonly Mock<IChatService> _mockService;
    private readonly ChatController _controller;

    public ChatControllerTests()
    {
        _mockService = new Mock<IChatService>();
        _controller = new ChatController(_mockService.Object);

        var httpContext = new DefaultHttpContext();
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };
    }

    private void SetUserContext(long? userId, string role, string? guestUuid = null)
    {
        var claims = new List<Claim>();

        if (userId.HasValue)
        {
            claims.Add(new Claim(ClaimTypes.NameIdentifier, userId.Value.ToString()));
        }
        else if (!string.IsNullOrEmpty(guestUuid))
        {
            claims.Add(new Claim("Guest_UUID", guestUuid));
            claims.Add(new Claim(ClaimTypes.NameIdentifier, guestUuid));
        }

        claims.Add(new Claim(ClaimTypes.Role, role));

        var identity = new ClaimsIdentity(claims, "TestAuth");
        _controller.ControllerContext.HttpContext.User = new ClaimsPrincipal(identity);
    }

    [Fact]
    public async Task CreateSession_AsGuest_ShouldReturn201Created()
    {
        // Arrange
        var guestUuid = "guest-uuid-999";
        SetUserContext(null, UserRoles.Guest, guestUuid);

        var request = new CreateChatSessionRequest { ContextType = "GENERAL" };
        var createdDto = new ChatSessionDto
        {
            SessionId = 1,
            Title = "Tư vấn dinh dưỡng mới",
            ContextType = "GENERAL",
            StartedAt = DateTime.UtcNow
        };

        _mockService.Setup(s => s.CreateSessionAsync(null, guestUuid, request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(createdDto);

        // Act
        var result = await _controller.CreateSession(request, CancellationToken.None);

        // Assert
        var statusResult = result as ObjectResult;
        statusResult.Should().NotBeNull();
        statusResult!.StatusCode.Should().Be(StatusCodes.Status201Created);

        var envelope = statusResult.Value as ApiResponse<ChatSessionDto>;
        envelope.Should().NotBeNull();
        envelope!.StatusCode.Should().Be(StatusCodes.Status201Created);
        envelope.Data.Should().NotBeNull();
        envelope.Data!.SessionId.Should().Be(1);
        envelope.Data.Title.Should().Be("Tư vấn dinh dưỡng mới");
    }

    [Fact]
    public async Task CreateSession_AsMember_ShouldReturn201Created()
    {
        // Arrange
        long memberId = 100;
        SetUserContext(memberId, UserRoles.Member);

        var request = new CreateChatSessionRequest { ContextType = "RECIPE" };
        var createdDto = new ChatSessionDto
        {
            SessionId = 2,
            Title = "Tư vấn công thức chay",
            ContextType = "RECIPE",
            StartedAt = DateTime.UtcNow
        };

        _mockService.Setup(s => s.CreateSessionAsync(memberId, null, request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(createdDto);

        // Act
        var result = await _controller.CreateSession(request, CancellationToken.None);

        // Assert
        var statusResult = result as ObjectResult;
        statusResult.Should().NotBeNull();
        statusResult!.StatusCode.Should().Be(StatusCodes.Status201Created);

        var envelope = statusResult.Value as ApiResponse<ChatSessionDto>;
        envelope.Should().NotBeNull();
        envelope!.Data!.SessionId.Should().Be(2);
    }

    [Fact]
    public async Task ListSessions_AsMember_ShouldReturn200WithPagedMeta()
    {
        // Arrange
        long memberId = 100;
        SetUserContext(memberId, UserRoles.Member);

        var sessions = new List<ChatSessionSummaryDto>
        {
            new()
            {
                SessionId = 1,
                Title = "Tư vấn dinh dưỡng",
                LastMessagePreview = "Nên bổ sung vitamin B12...",
                UpdatedAt = DateTime.UtcNow
            }
        };

        var pagedResult = new PagedResult<ChatSessionSummaryDto>(sessions, totalItems: 1, pageNumber: 1, pageSize: 20);
        _mockService.Setup(s => s.ListSessionsAsync(memberId, 1, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagedResult);

        // Act
        var result = await _controller.ListSessions(1, 20, CancellationToken.None);

        // Assert
        var okResult = result as OkObjectResult;
        okResult.Should().NotBeNull();
        var envelope = okResult!.Value as ApiResponse<IReadOnlyList<ChatSessionSummaryDto>>;
        envelope.Should().NotBeNull();
        envelope!.StatusCode.Should().Be(StatusCodes.Status200OK);
        envelope.Data.Should().HaveCount(1);
        envelope.Meta.Should().NotBeNull();
        envelope.Meta!.Page.Should().Be(1);
        envelope.Meta.Total.Should().Be(1);
    }

    [Fact]
    public async Task ListSessions_AsGuest_ShouldThrowForbidden()
    {
        // Arrange
        SetUserContext(null, UserRoles.Guest, "guest-uuid-111");

        // Act
        Func<Task> act = async () => await _controller.ListSessions(1, 20, CancellationToken.None);

        // Assert
        var ex = await act.Should().ThrowAsync<AppException>();
        ex.Which.StatusCode.Should().Be(StatusCodes.Status403Forbidden);
        ex.Which.ErrorCode.Should().Be(ErrorCodes.ErrGuestCommunityActionDenied);
    }

    [Fact]
    public async Task SendMessage_ValidMember_ShouldReturn200WithAnswerAndDisclaimer()
    {
        // Arrange
        long memberId = 100;
        SetUserContext(memberId, UserRoles.Member);

        var request = new CreateChatSessionMessageRequest
        {
            Message = "Tôi nên bổ sung vitamin B12 như thế nào khi ăn chay?"
        };

        var resultDto = new CreateChatMessageResultDto
        {
            Message = new ChatSessionMessageDto
            {
                MessageId = 10,
                SenderType = "ASSISTANT",
                Content = "Bạn nên sử dụng nấm men dinh dưỡng. " + ChatConstants.MedicalDisclaimer,
                Disclaimer = ChatConstants.MedicalDisclaimer,
                Sources = new List<ChatSourceDto>
                {
                    new() { Type = "POST", Id = 5, Title = "Bổ sung B12 an toàn" }
                },
                RagUsed = true,
                CreatedAt = DateTime.UtcNow
            },
            Quota = new ChatQuotaDto
            {
                Used = 1,
                Limit = 20,
                Remaining = 19,
                Scope = "DAILY"
            }
        };

        _mockService.Setup(s => s.SendMessageAsync(
                5,
                memberId,
                null,
                UserRoles.Member,
                request,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(resultDto);

        // Act
        var result = await _controller.SendMessage(5, request, CancellationToken.None);

        // Assert
        var okResult = result as OkObjectResult;
        okResult.Should().NotBeNull();
        var envelope = okResult!.Value as ApiResponse<CreateChatMessageResultDto>;
        envelope.Should().NotBeNull();
        envelope!.StatusCode.Should().Be(StatusCodes.Status200OK);
        envelope.Data.Should().NotBeNull();
        envelope.Data!.Message.Disclaimer.Should().Be(ChatConstants.MedicalDisclaimer);
        envelope.Data.Message.Sources.Should().HaveCount(1);
        envelope.Data.Quota.Remaining.Should().Be(19);
    }

    [Fact]
    public async Task SendMessage_InvalidSessionId_ShouldThrowBadRequest()
    {
        // Arrange
        SetUserContext(100, UserRoles.Member);
        var request = new CreateChatSessionMessageRequest { Message = "Hỏi gì đó" };

        // Act
        Func<Task> act = async () => await _controller.SendMessage(0, request, CancellationToken.None);

        // Assert
        var ex = await act.Should().ThrowAsync<AppException>();
        ex.Which.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
        ex.Which.ErrorCode.Should().Be(ErrorCodes.ErrValidation);
    }

    [Fact]
    public async Task ListMessages_ValidSession_ShouldReturn200()
    {
        // Arrange
        long memberId = 100;
        SetUserContext(memberId, UserRoles.Member);

        var responseDto = new ListChatSessionMessagesResponseDto
        {
            Messages = new List<ChatSessionMessageDto>
            {
                new()
                {
                    MessageId = 1,
                    SenderType = "USER",
                    Content = "Chào bot",
                    CreatedAt = DateTime.UtcNow
                },
                new()
                {
                    MessageId = 2,
                    SenderType = "ASSISTANT",
                    Content = "Chào bạn! " + ChatConstants.MedicalDisclaimer,
                    CreatedAt = DateTime.UtcNow,
                    Disclaimer = ChatConstants.MedicalDisclaimer
                }
            },
            Meta = new ChatMessagesMeta
            {
                Size = 2,
                HasMore = false
            }
        };

        _mockService.Setup(s => s.ListMessagesAsync(
                5,
                memberId,
                null,
                null,
                20,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(responseDto);

        // Act
        var result = await _controller.ListMessages(5, null, 20, CancellationToken.None);

        // Assert
        var okResult = result as OkObjectResult;
        okResult.Should().NotBeNull();
        okResult!.StatusCode.Should().Be(StatusCodes.Status200OK);
    }

    [Fact]
    public async Task ListMessages_InvalidSessionId_ShouldThrowBadRequest()
    {
        // Arrange
        SetUserContext(100, UserRoles.Member);

        // Act
        Func<Task> act = async () => await _controller.ListMessages(-1, null, 20, CancellationToken.None);

        // Assert
        var ex = await act.Should().ThrowAsync<AppException>();
        ex.Which.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
    }

    [Fact]
    public async Task DeleteSession_AsMember_ShouldReturn200Ok()
    {
        // Arrange
        long memberId = 100;
        SetUserContext(memberId, UserRoles.Member);

        _mockService.Setup(s => s.DeleteSessionAsync(10, memberId, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.DeleteSession(10, CancellationToken.None);

        // Assert
        var okResult = result as OkObjectResult;
        okResult.Should().NotBeNull();
        okResult!.StatusCode.Should().Be(StatusCodes.Status200OK);

        _mockService.Verify(s => s.DeleteSessionAsync(10, memberId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteSession_AsGuest_ShouldThrowForbidden()
    {
        // Arrange
        SetUserContext(null, UserRoles.Guest, "guest-uuid-111");

        // Act
        Func<Task> act = async () => await _controller.DeleteSession(10, CancellationToken.None);

        // Assert
        var ex = await act.Should().ThrowAsync<AppException>();
        ex.Which.StatusCode.Should().Be(StatusCodes.Status403Forbidden);
        ex.Which.ErrorCode.Should().Be(ErrorCodes.ErrGuestCommunityActionDenied);
    }

    [Fact]
    public async Task DeleteSession_InvalidSessionId_ShouldThrowBadRequest()
    {
        // Arrange
        SetUserContext(100, UserRoles.Member);

        // Act
        Func<Task> act = async () => await _controller.DeleteSession(0, CancellationToken.None);

        // Assert
        var ex = await act.Should().ThrowAsync<AppException>();
        ex.Which.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
    }
}
