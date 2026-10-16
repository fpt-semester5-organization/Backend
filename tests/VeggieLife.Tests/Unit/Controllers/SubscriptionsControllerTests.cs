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
using VeggieLife.BLL.DTOs.Subscriptions.Requests;
using VeggieLife.BLL.DTOs.Subscriptions.Responses;
using VeggieLife.BLL.Services.Subscriptions;
using Xunit;

namespace VeggieLife.Tests.Unit.Controllers;

/// <summary>
/// Unit Tests kiểm thử các endpoint của SubscriptionsController (FN-SUB-01, FN-SUB-02, FN-SUB-04).
/// </summary>
public class SubscriptionsControllerTests
{
    private readonly Mock<ISubscriptionService> _mockService;
    private readonly SubscriptionsController _controller;

    public SubscriptionsControllerTests()
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
    public async Task ListSubscriptionPlans_ReturnsOkEnvelope()
    {
        var expected = new ListSubscriptionPlansResponseDto
        {
            Plans = new List<SubscriptionPlanItemDto>
            {
                new() { PlanId = 1, PlanName = "VIP 1 tháng", DurationDays = 30, PriceVnd = 49000, IsActive = true }
            },
            Benefits = new SubscriptionBenefitsDto()
        };

        _mockService.Setup(s => s.GetSubscriptionPlansAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var result = await _controller.ListSubscriptionPlans(CancellationToken.None);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var envelope = okResult.Value.Should().BeOfType<ApiResponse<ListSubscriptionPlansResponseDto>>().Subject;
        envelope.StatusCode.Should().Be(200);
        envelope.Data.Should().NotBeNull();
        envelope.Data!.Plans.Should().HaveCount(1);
    }

    [Fact]
    public async Task CheckoutSubscription_WhenMember_ReturnsCreated201Envelope()
    {
        var request = new CheckoutSubscriptionRequest
        {
            PlanId = 1,
            Gateway = "MOMO"
        };

        var expectedResponse = new CheckoutSubscriptionResponseDto
        {
            TransactionId = 3001,
            OrderCode = "VL_20261015_001",
            AmountVnd = 49000,
            PayUrl = "https://test-payment.momo.vn/...",
            ExpiresAt = DateTime.UtcNow.AddMinutes(5)
        };

        _mockService.Setup(s => s.CheckoutSubscriptionAsync(101, UserRoles.Member, request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        var result = await _controller.CheckoutSubscription(request, CancellationToken.None);

        var objectResult = result.Should().BeOfType<ObjectResult>().Subject;
        objectResult.StatusCode.Should().Be(201);
        var envelope = objectResult.Value.Should().BeOfType<ApiResponse<CheckoutSubscriptionResponseDto>>().Subject;
        envelope.StatusCode.Should().Be(201);
        envelope.Data!.TransactionId.Should().Be(3001);
        envelope.Data.OrderCode.Should().Be("VL_20261015_001");
    }

    [Fact]
    public async Task CheckoutSubscription_WhenGuest_ThrowsForbidden()
    {
        SetupUserClaims(userId: 999, role: UserRoles.Guest);

        var request = new CheckoutSubscriptionRequest
        {
            PlanId = 1,
            Gateway = "MOMO"
        };

        var act = () => _controller.CheckoutSubscription(request, CancellationToken.None);

        var ex = await act.Should().ThrowAsync<AppException>();
        ex.Which.StatusCode.Should().Be(403);
        ex.Which.ErrorCode.Should().Be(ErrorCodes.ErrGuestPersonalizationRestricted);
    }

    [Fact]
    public async Task GetCurrentUserSubscription_WhenMember_ReturnsOkEnvelope()
    {
        var expectedResponse = new CurrentUserSubscriptionResponseDto
        {
            IsVip = true,
            Plan = new SubscriptionPlanItemDto
            {
                PlanId = 1,
                PlanName = "VIP 1 tháng",
                DurationDays = 30,
                PriceVnd = 49000,
                IsActive = true
            },
            EndsAt = DateTime.UtcNow.AddDays(20),
            Transactions = new List<SubscriptionTransactionDto>
            {
                new()
                {
                    TransactionId = 3001,
                    Gateway = "MOMO",
                    AmountVnd = 49000,
                    Status = "SUCCESS",
                    CreatedAt = DateTime.UtcNow.AddDays(-10)
                }
            }
        };

        _mockService.Setup(s => s.GetCurrentUserSubscriptionAsync(101, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        var result = await _controller.GetCurrentUserSubscription(CancellationToken.None);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var envelope = okResult.Value.Should().BeOfType<ApiResponse<CurrentUserSubscriptionResponseDto>>().Subject;
        envelope.StatusCode.Should().Be(200);
        envelope.Data!.IsVip.Should().BeTrue();
        envelope.Data.Plan!.PlanId.Should().Be(1);
    }

    [Fact]
    public async Task GetCurrentUserSubscription_WhenGuest_ThrowsForbidden()
    {
        SetupUserClaims(userId: 999, role: UserRoles.Guest);

        var act = () => _controller.GetCurrentUserSubscription(CancellationToken.None);

        var ex = await act.Should().ThrowAsync<AppException>();
        ex.Which.StatusCode.Should().Be(403);
        ex.Which.ErrorCode.Should().Be(ErrorCodes.ErrGuestPersonalizationRestricted);
    }
}
