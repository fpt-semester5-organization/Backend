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
using VeggieLife.BLL.DTOs.MealPlans.Requests;
using VeggieLife.BLL.DTOs.MealPlans.Responses;
using VeggieLife.BLL.Services.MealPlans;
using Xunit;

namespace VeggieLife.Tests.Unit.Controllers;

/// <summary>
/// Unit Tests cho MealPlansController (FN-MEAL-01).
/// Kiểm thử cấu trúc envelope response chuẩn D52, HTTP 201 Created và kiểm tra phân quyền.
/// </summary>
public class MealPlansControllerTests
{
    private readonly Mock<IMealPlanService> _mockService;
    private readonly MealPlansController _controller;

    public MealPlansControllerTests()
    {
        _mockService = new Mock<IMealPlanService>();
        _controller = new MealPlansController(_mockService.Object);

        var httpContext = new DefaultHttpContext();
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };
    }

    [Fact]
    public async Task CreateMealPlan_AuthenticatedMember_ShouldReturn201Created()
    {
        // Arrange
        var userId = 45;
        var userRole = UserRoles.Member;
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId.ToString()),
            new(ClaimTypes.Role, userRole)
        };
        _controller.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));

        var request = new CreateMealPlanRequest
        {
            StartDate = "2026-10-19",
            BudgetTier = "STANDARD",
            UseFridge = true,
            CustomInstructions = "ít dầu, không cay"
        };

        var responseData = new CreateMealPlanResponseData
        {
            PlanId = 45,
            PlanName = "Thực đơn tuần 19/10",
            StartDate = "2026-10-19",
            EndDate = "2026-10-25",
            BudgetTier = "STANDARD",
            CustomInstructions = "ít dầu, không cay",
            TargetDailyKcal = 1800m,
            PlanStatus = "DRAFT",
            GeneratedByAi = true,
            Slots = new List<MealSlotDto>(),
            Edits = new MealPlanEditsDto { PreDeployUsed = 0, PreDeployLimit = 21 }
        };

        _mockService.Setup(s => s.CreateMealPlanAsync(userId, userRole, request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(responseData);

        // Act
        var actionResult = await _controller.CreateMealPlan(request, CancellationToken.None);

        // Assert
        var objectResult = actionResult as ObjectResult;
        objectResult.Should().NotBeNull();
        objectResult!.StatusCode.Should().Be(StatusCodes.Status201Created);

        var apiResponse = objectResult.Value as ApiResponse<CreateMealPlanResponseData>;
        apiResponse.Should().NotBeNull();
        apiResponse!.StatusCode.Should().Be(201);
        apiResponse.Message.Should().Be("Đã tạo thực đơn nháp.");
        apiResponse.Data.Should().NotBeNull();
        apiResponse.Data!.PlanId.Should().Be(45);
        apiResponse.Data.PlanStatus.Should().Be("DRAFT");
    }

    [Fact]
    public async Task CreateMealPlan_Unauthenticated_ShouldThrowUnauthorized()
    {
        // Arrange: No user claims
        _controller.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity());

        var request = new CreateMealPlanRequest
        {
            StartDate = "2026-10-19",
            BudgetTier = "STANDARD"
        };

        // Act & Assert
        var act = () => _controller.CreateMealPlan(request, CancellationToken.None);
        var ex = await act.Should().ThrowAsync<AppException>();
        ex.Which.StatusCode.Should().Be(401);
        ex.Which.ErrorCode.Should().Be(ErrorCodes.ErrAuthInvalidToken);
    }

    [Fact]
    public async Task CreateMealPlan_GuestUser_ShouldThrowForbidden()
    {
        // Arrange: Role GUEST
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, "999"),
            new(ClaimTypes.Role, UserRoles.Guest)
        };
        _controller.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));

        var request = new CreateMealPlanRequest
        {
            StartDate = "2026-10-19",
            BudgetTier = "STANDARD"
        };

        // Act & Assert
        var act = () => _controller.CreateMealPlan(request, CancellationToken.None);
        var ex = await act.Should().ThrowAsync<AppException>();
        ex.Which.StatusCode.Should().Be(403);
        ex.Which.ErrorCode.Should().Be(ErrorCodes.ErrGuestPersonalizationRestricted);
    }
}
