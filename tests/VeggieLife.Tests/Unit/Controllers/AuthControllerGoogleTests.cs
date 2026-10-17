using System.Collections.Generic;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using VeggieLife.API.Controllers;
using VeggieLife.BLL.Common.Constants;
using VeggieLife.BLL.Common.Exceptions;
using VeggieLife.BLL.Common.Responses;
using VeggieLife.BLL.DTOs.Auth;
using VeggieLife.BLL.Services.Auth;
using VeggieLife.BLL.Services.Guest;
using Xunit;

namespace VeggieLife.Tests.Unit.Controllers;

/// <summary>
/// Unit Tests kiểm thử endpoint POST /api/v1/auth/google của AuthController (FN-AUTH-07).
/// </summary>
public class AuthControllerGoogleTests
{
    private readonly Mock<IAuthService> _mockAuthService;
    private readonly Mock<IGuestService> _mockGuestService;
    private readonly Mock<IWebHostEnvironment> _mockEnvironment;
    private readonly AuthController _controller;
    private readonly DefaultHttpContext _httpContext;

    public AuthControllerGoogleTests()
    {
        _mockAuthService = new Mock<IAuthService>();
        _mockGuestService = new Mock<IGuestService>();
        _mockEnvironment = new Mock<IWebHostEnvironment>();

        _mockEnvironment.Setup(e => e.EnvironmentName).Returns("Development");

        _controller = new AuthController(
            _mockAuthService.Object,
            _mockGuestService.Object,
            _mockEnvironment.Object);

        _httpContext = new DefaultHttpContext();
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = _httpContext
        };
    }

    [Fact]
    public async Task LoginWithGoogle_WhenSuccessful_Returns200Ok_AndSetsHttpOnlyRefreshTokenCookie()
    {
        // ARRANGE
        var request = new LoginWithGoogleRequest
        {
            IdToken = "valid_google_id_token"
        };

        var expectedResponse = new LoginWithGoogleResponse
        {
            TokenType = "Bearer",
            AccessToken = "mock_access_token_jwt",
            RefreshToken = "mock_refresh_token_jwt",
            ExpiresIn = 900,
            IsNewAccount = true,
            User = new AuthUserDto
            {
                UserId = 1024,
                Email = "google.user@gmail.com",
                FullName = "Google User",
                Role = UserRoles.Member,
                IsVip = false,
                AccountStatus = AccountStatuses.Active,
                IsProfileCompleted = false
            }
        };

        _mockAuthService.Setup(s => s.LoginWithGoogleAsync(
                It.Is<LoginWithGoogleRequest>(r => r.IdToken == "valid_google_id_token"),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // ACT
        var result = await _controller.LoginWithGoogle(request, CancellationToken.None);

        // ASSERT
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(StatusCodes.Status200OK);

        var apiResponse = okResult.Value.Should().BeOfType<ApiResponse<LoginWithGoogleResponse>>().Subject;
        apiResponse.StatusCode.Should().Be(200);
        apiResponse.Message.Should().Be("Đăng nhập Google thành công.");
        apiResponse.Data.Should().NotBeNull();
        apiResponse.Data.User.Email.Should().Be("google.user@gmail.com");
        apiResponse.Data.IsNewAccount.Should().BeTrue();

        // Kiểm tra cookie refresh_token được set
        var setCookieHeader = _httpContext.Response.Headers["Set-Cookie"].ToString();
        setCookieHeader.Should().Contain("refresh_token=mock_refresh_token_jwt");
        setCookieHeader.Should().Contain("httponly");
    }

    [Fact]
    public async Task LoginWithGoogle_WhenGuestUuidInCookie_PassesGuestUuidToService()
    {
        // ARRANGE
        _httpContext.Request.Headers["Cookie"] = "guest_uuid=guest-cookie-uuid-123";

        var request = new LoginWithGoogleRequest
        {
            IdToken = "valid_token"
        };

        _mockAuthService.Setup(s => s.LoginWithGoogleAsync(
                It.Is<LoginWithGoogleRequest>(r => r.GuestUuid == "guest-cookie-uuid-123"),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new LoginWithGoogleResponse
            {
                RefreshToken = "ref_token"
            });

        // ACT
        var result = await _controller.LoginWithGoogle(request, CancellationToken.None);

        // ASSERT
        result.Should().BeOfType<OkObjectResult>();
        _mockAuthService.Verify(s => s.LoginWithGoogleAsync(
            It.Is<LoginWithGoogleRequest>(r => r.GuestUuid == "guest-cookie-uuid-123"),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task LoginWithGoogle_WhenGuestUuidInHeader_PassesGuestUuidToService()
    {
        // ARRANGE
        _httpContext.Request.Headers["X-Guest-UUID"] = "guest-header-uuid-456";

        var request = new LoginWithGoogleRequest
        {
            IdToken = "valid_token"
        };

        _mockAuthService.Setup(s => s.LoginWithGoogleAsync(
                It.Is<LoginWithGoogleRequest>(r => r.GuestUuid == "guest-header-uuid-456"),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new LoginWithGoogleResponse
            {
                RefreshToken = "ref_token"
            });

        // ACT
        var result = await _controller.LoginWithGoogle(request, CancellationToken.None);

        // ASSERT
        result.Should().BeOfType<OkObjectResult>();
        _mockAuthService.Verify(s => s.LoginWithGoogleAsync(
            It.Is<LoginWithGoogleRequest>(r => r.GuestUuid == "guest-header-uuid-456"),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task LoginWithGoogle_WhenServiceThrowsAccountLinkRequired_PropagatesException()
    {
        // ARRANGE
        var request = new LoginWithGoogleRequest
        {
            IdToken = "valid_token"
        };

        _mockAuthService.Setup(s => s.LoginWithGoogleAsync(
                It.IsAny<LoginWithGoogleRequest>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(AppException.Conflict(
                ErrorCodes.ErrAuthAccountLinkRequired,
                "Email đã có tài khoản mật khẩu; cần currentPassword."));

        // ACT & ASSERT
        var ex = await Assert.ThrowsAsync<AppException>(() =>
            _controller.LoginWithGoogle(request, CancellationToken.None));

        ex.StatusCode.Should().Be(409);
        ex.ErrorCode.Should().Be(ErrorCodes.ErrAuthAccountLinkRequired);
    }
}
