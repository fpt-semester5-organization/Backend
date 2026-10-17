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
using VeggieLife.BLL.DTOs.Surveys.Requests;
using VeggieLife.BLL.DTOs.Surveys.Responses;
using VeggieLife.BLL.Services.Chat;
using Xunit;

namespace VeggieLife.Tests.Unit.Controllers;

/// <summary>
/// Unit Tests cho SurveysController (FN-CHAT-06).
/// Kiểm thử cấu trúc envelope phản hồi D52 và chặn quyền đối với Khách vãng lai.
/// </summary>
public class SurveysControllerTests
{
    private readonly Mock<IChatService> _mockChatService;
    private readonly SurveysController _controller;

    public SurveysControllerTests()
    {
        _mockChatService = new Mock<IChatService>();
        _controller = new SurveysController(_mockChatService.Object);

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
    public async Task CreateSurvey_AsMember_ShouldReturn201CreatedWithSurveyId()
    {
        // Arrange
        long memberId = 100;
        SetUserContext(memberId, UserRoles.Member);

        var request = new CreateSurveyRequest
        {
            ChatSessionId = 1,
            ContextQuestion = "Hỏi về vitamin B12",
            RatingScore = 5,
            FeedbackContent = "Câu trả lời dẫn nguồn rất chi tiết và hữu ích!"
        };

        _mockChatService.Setup(s => s.CreateSurveyAsync(memberId, request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(999);

        // Act
        var result = await _controller.CreateSurvey(request, CancellationToken.None);

        // Assert
        var statusResult = result as ObjectResult;
        statusResult.Should().NotBeNull();
        statusResult!.StatusCode.Should().Be(StatusCodes.Status201Created);

        var envelope = statusResult.Value as ApiResponse<CreateSurveyResponseDto>;
        envelope.Should().NotBeNull();
        envelope!.StatusCode.Should().Be(StatusCodes.Status201Created);
        envelope.Data.Should().NotBeNull();
        envelope.Data!.SurveyId.Should().Be(999);
    }

    [Fact]
    public async Task CreateSurvey_AsGuest_ShouldThrowForbidden()
    {
        // Arrange
        SetUserContext(null, UserRoles.Guest, "guest-uuid-111");

        var request = new CreateSurveyRequest
        {
            ChatSessionId = 1,
            ContextQuestion = "Hỏi về ăn chay",
            RatingScore = 4
        };

        // Act
        Func<Task> act = async () => await _controller.CreateSurvey(request, CancellationToken.None);

        // Assert
        var ex = await act.Should().ThrowAsync<AppException>();
        ex.Which.StatusCode.Should().Be(StatusCodes.Status403Forbidden);
        ex.Which.ErrorCode.Should().Be(ErrorCodes.ErrGuestCommunityActionDenied);
    }

    [Fact]
    public async Task CreateSurvey_Unauthenticated_ShouldThrowForbidden()
    {
        // Arrange
        _controller.ControllerContext.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity());

        var request = new CreateSurveyRequest
        {
            ChatSessionId = 1,
            ContextQuestion = "Hỏi về ăn chay",
            RatingScore = 5
        };

        // Act
        Func<Task> act = async () => await _controller.CreateSurvey(request, CancellationToken.None);

        // Assert
        var ex = await act.Should().ThrowAsync<AppException>();
        ex.Which.StatusCode.Should().Be(StatusCodes.Status403Forbidden);
    }
}
