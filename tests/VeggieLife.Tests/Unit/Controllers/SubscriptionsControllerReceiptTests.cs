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
using VeggieLife.BLL.DTOs.Subscriptions.Responses;
using VeggieLife.BLL.Services.Subscriptions;
using Xunit;

namespace VeggieLife.Tests.Unit.Controllers;

/// <summary>
/// Unit Tests kiểm thử endpoint GetPaymentReceipt của SubscriptionsController.
/// </summary>
public class SubscriptionsControllerReceiptTests
{
    private readonly Mock<ISubscriptionService> _mockService;
    private readonly SubscriptionsController _controller;

    public SubscriptionsControllerReceiptTests()
    {
        _mockService = new Mock<ISubscriptionService>();
        _controller = new SubscriptionsController(_mockService.Object);

        SetupUserClaims(userId: 101, role: UserRoles.Member);
    }

    private void SetupUserClaims(int userId, string role)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId.ToString()),
            new(ClaimTypes.Role, role)
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = principal }
        };
    }

    [Fact]
    public async Task GetPaymentReceipt_WhenAuthorizedMember_ReturnsOkEnvelopeWithReceipt()
    {
        // Arrange
        var expectedReceipt = new PaymentReceiptDto
        {
            ReceiptNumber = "REC-20261103-000101",
            TransactionId = 101,
            OrderCode = "ORD-101",
            Gateway = "VIETQR",
            Status = "SUCCESS",
            PaymentDate = DateTime.UtcNow,
            UserId = 101,
            FullName = "Member Test",
            Email = "member@test.com",
            PlanId = 1,
            PlanName = "VIP 1 Tháng",
            DurationDays = 30,
            AmountVnd = 49000,
            VipStartsAt = DateTime.UtcNow,
            VipEndsAt = DateTime.UtcNow.AddDays(30),
            Note = "Ghi chú thanh toán"
        };

        _mockService.Setup(s => s.GetPaymentReceiptAsync(101, 101, UserRoles.Member, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedReceipt);

        // Act
        var actionResult = await _controller.GetPaymentReceipt(101, CancellationToken.None);

        // Assert
        var okResult = actionResult as OkObjectResult;
        okResult.Should().NotBeNull();
        okResult!.StatusCode.Should().Be(200);

        var payload = okResult.Value as ApiResponse<PaymentReceiptDto>;
        payload.Should().NotBeNull();
        payload!.Data.Should().BeEquivalentTo(expectedReceipt);
        payload.Message.Should().Be("Lấy thông tin biên lai thanh toán thành công.");
    }

    [Fact]
    public async Task GetPaymentReceipt_WhenGuestRole_ThrowsForbidden()
    {
        // Arrange
        SetupUserClaims(userId: 999, role: UserRoles.Guest);

        // Act & Assert
        var act = () => _controller.GetPaymentReceipt(101, CancellationToken.None);
        await act.Should().ThrowAsync<AppException>()
            .Where(e => e.ErrorCode == ErrorCodes.ErrGuestPersonalizationRestricted);
    }
}

