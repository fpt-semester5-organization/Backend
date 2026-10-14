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
using Xunit;

namespace VeggieLife.Tests.Unit.Controllers;

/// <summary>
/// Unit Tests cho ReportsController (FN-COMM-05).
/// Kiểm tra xác thực quyền truy cập Member/Guest và cấu trúc phản hồi HTTP 201 Created.
/// </summary>
public class ReportsControllerTests
{
    private readonly Mock<ICommunityInteractionService> _mockService;
    private readonly ReportsController _controller;

    public ReportsControllerTests()
    {
        _mockService = new Mock<ICommunityInteractionService>();
        _controller = new ReportsController(_mockService.Object);

        var httpContext = new DefaultHttpContext();
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };
    }

    private void SetUserContext(string? userId, string role, bool isAuthenticated = true)
    {
        var claims = new System.Collections.Generic.List<Claim>();
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

        _controller.ControllerContext.HttpContext.User = new ClaimsPrincipal(identity);
    }

    [Fact]
    public async Task CreateReport_WhenCalledByMember_ShouldReturnCreated201()
    {
        // Arrange
        SetUserContext("15", "MEMBER", true);
        var request = new CreateReportRequest
        {
            TargetType = "COMMENT",
            TargetId = 301,
            ViolationType = "VIO_SPAM",
            EvidenceSummary = "Bình luận chứa quảng cáo trái phép vi phạm nội quy."
        };

        var expectedResponse = new ReportResponseData
        {
            ReportId = 91,
            Status = "PENDING"
        };

        _mockService
            .Setup(s => s.CreateReportAsync(request, 15, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _controller.CreateReport(request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<ObjectResult>();
        var objResult = (ObjectResult)result;
        objResult.StatusCode.Should().Be(StatusCodes.Status201Created);

        var apiResponse = objResult.Value as ApiResponse<ReportResponseData>;
        apiResponse.Should().NotBeNull();
        apiResponse!.StatusCode.Should().Be(201);
        apiResponse.Message.Should().Be("Đã ghi nhận báo cáo. Nội dung đã được ẩn chờ Admin xử lý.");
        apiResponse.Data.Should().NotBeNull();
        apiResponse.Data!.ReportId.Should().Be(91);
        apiResponse.Data.Status.Should().Be("PENDING");
    }

    [Fact]
    public async Task CreateReport_WhenCalledByGuest_ShouldThrowForbidden403()
    {
        // Arrange
        SetUserContext("guest-123", "GUEST", true);
        var request = new CreateReportRequest
        {
            TargetType = "POST",
            TargetId = 10,
            ViolationType = "VIO_SPAM",
            EvidenceSummary = "Spam nội dung"
        };

        // Act & Assert
        var act = () => _controller.CreateReport(request, CancellationToken.None);
        var ex = await act.Should().ThrowAsync<AppException>();
        ex.Which.StatusCode.Should().Be(403);
        ex.Which.ErrorCode.Should().Be(ErrorCodes.ErrGuestCommunityActionDenied);
    }

    [Fact]
    public async Task CreateReport_WhenUnauthenticated_ShouldThrowUnauthorized401()
    {
        // Arrange
        SetUserContext(null, "", false);
        var request = new CreateReportRequest
        {
            TargetType = "POST",
            TargetId = 10,
            ViolationType = "VIO_SPAM",
            EvidenceSummary = "Spam nội dung"
        };

        // Act & Assert
        var act = () => _controller.CreateReport(request, CancellationToken.None);
        var ex = await act.Should().ThrowAsync<AppException>();
        ex.Which.StatusCode.Should().Be(401);
        ex.Which.ErrorCode.Should().Be(ErrorCodes.ErrAuthInvalidToken);
    }
}
