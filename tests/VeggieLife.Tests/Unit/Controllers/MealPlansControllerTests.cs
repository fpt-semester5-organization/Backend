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
using VeggieLife.BLL.DTOs.ShoppingLists.Requests;
using VeggieLife.BLL.DTOs.ShoppingLists.Responses;
using VeggieLife.BLL.Services.MealPlans;
using VeggieLife.BLL.Services.ShoppingLists;
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

    [Fact]
    public async Task ListMealPlans_AuthenticatedMember_ShouldReturn200OkWithPagedEnvelope()
    {
        // Arrange
        var userId = 45;
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId.ToString()),
            new(ClaimTypes.Role, UserRoles.Member)
        };
        _controller.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));

        var items = new List<MealPlanSummaryDto>
        {
            new() { PlanId = 1, PlanName = "Thực đơn 1", StartDate = "2026-10-19", PlanStatus = "ACTIVE" }
        };
        var meta = new PagedMeta { Page = 1, Size = 10, Total = 1 };

        _mockService.Setup(s => s.ListMealPlansAsync(userId, 1, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync((items, meta));

        // Act
        var actionResult = await _controller.ListMealPlans(page: 1, size: 10, CancellationToken.None);

        // Assert
        var objectResult = actionResult as ObjectResult;
        objectResult.Should().NotBeNull();
        objectResult!.StatusCode.Should().Be(StatusCodes.Status200OK);

        var apiResponse = objectResult.Value as ApiResponse<IReadOnlyList<MealPlanSummaryDto>>;
        apiResponse.Should().NotBeNull();
        apiResponse!.Data.Should().HaveCount(1);
        apiResponse.Meta.Should().NotBeNull();
        apiResponse.Meta!.Total.Should().Be(1);
    }

    [Fact]
    public async Task GetMealPlan_AuthenticatedMember_ShouldReturn200Ok()
    {
        // Arrange
        var userId = 45;
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId.ToString()),
            new(ClaimTypes.Role, UserRoles.Member)
        };
        _controller.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));

        var responseData = new CreateMealPlanResponseData
        {
            PlanId = 1,
            PlanName = "Thực đơn tuần",
            StartDate = "2026-10-19",
            EndDate = "2026-10-25",
            BudgetTier = "STANDARD",
            TargetDailyKcal = 1800m,
            PlanStatus = "DRAFT",
            GeneratedByAi = true,
            Slots = new List<MealSlotDto>(),
            Edits = new MealPlanEditsDto { PreDeployUsed = 0, PreDeployLimit = 21 }
        };

        _mockService.Setup(s => s.GetMealPlanByIdAsync(1, userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(responseData);

        // Act
        var actionResult = await _controller.GetMealPlan(1, CancellationToken.None);

        // Assert
        var objectResult = actionResult as ObjectResult;
        objectResult.Should().NotBeNull();
        objectResult!.StatusCode.Should().Be(StatusCodes.Status200OK);

        var apiResponse = objectResult.Value as ApiResponse<CreateMealPlanResponseData>;
        apiResponse.Should().NotBeNull();
        apiResponse!.Data!.PlanId.Should().Be(1);
    }

    [Fact]
    public async Task ActivateMealPlan_AuthenticatedMember_ShouldReturn200Ok()
    {
        // Arrange
        var userId = 45;
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId.ToString()),
            new(ClaimTypes.Role, UserRoles.Member)
        };
        _controller.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));

        var responseData = new ActivateMealPlanResponseData
        {
            PlanId = 1,
            PlanStatus = "ACTIVE"
        };

        _mockService.Setup(s => s.ActivateMealPlanAsync(1, userId, UserRoles.Member, It.IsAny<CancellationToken>()))
            .ReturnsAsync(responseData);

        // Act
        var actionResult = await _controller.ActivateMealPlan(1, CancellationToken.None);

        // Assert
        var objectResult = actionResult as ObjectResult;
        objectResult.Should().NotBeNull();
        objectResult!.StatusCode.Should().Be(StatusCodes.Status200OK);

        var apiResponse = objectResult.Value as ApiResponse<ActivateMealPlanResponseData>;
        apiResponse.Should().NotBeNull();
        apiResponse!.Data!.PlanStatus.Should().Be("ACTIVE");
    }

    [Fact]
    public async Task UpdateMealPlanSlot_AuthenticatedMember_ShouldReturn200Ok()
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

        var request = new UpdateMealPlanSlotRequest { SwapReason = "Đổi món" };
        var responseData = new UpdateMealPlanSlotResponseData
        {
            Slot = new MealSlotDto
            {
                SlotId = 10,
                DayOfWeek = "MON",
                MealType = "LUNCH",
                IsCompleted = false,
                Recipe = new MealSlotRecipeDto
                {
                    RecipeId = 102,
                    Title = "Cháo yến mạch",
                    CaloriesKcal = 450m
                }
            },
            SwapsRemaining = 20
        };

        _mockService.Setup(s => s.UpdateMealPlanSlotAsync(1, 10, userId, userRole, request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(responseData);

        // Act
        var actionResult = await _controller.UpdateMealPlanSlot(1, 10, request, CancellationToken.None);

        // Assert
        var objectResult = actionResult as ObjectResult;
        objectResult.Should().NotBeNull();
        objectResult!.StatusCode.Should().Be(StatusCodes.Status200OK);

        var apiResponse = objectResult.Value as ApiResponse<UpdateMealPlanSlotResponseData>;
        apiResponse.Should().NotBeNull();
        apiResponse!.Data!.SwapsRemaining.Should().Be(20);
    }

    [Fact]
    public async Task PatchMealPlanSlot_AuthenticatedMember_ShouldReturn200Ok()
    {
        // Arrange
        var userId = 45;
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId.ToString()),
            new(ClaimTypes.Role, UserRoles.Member)
        };
        _controller.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));

        var request = new PatchMealPlanSlotRequest { IsCompleted = true };
        var responseData = new PatchMealPlanSlotResponseData
        {
            SlotId = 10,
            IsCompleted = true,
            Completed = 5,
            Total = 21
        };

        _mockService.Setup(s => s.PatchMealPlanSlotAsync(1, 10, userId, request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(responseData);

        // Act
        var actionResult = await _controller.PatchMealPlanSlot(1, 10, request, CancellationToken.None);

        // Assert
        var objectResult = actionResult as ObjectResult;
        objectResult.Should().NotBeNull();
        objectResult!.StatusCode.Should().Be(StatusCodes.Status200OK);

        var apiResponse = objectResult.Value as ApiResponse<PatchMealPlanSlotResponseData>;
        apiResponse.Should().NotBeNull();
        apiResponse!.Data!.Completed.Should().Be(5);
        apiResponse.Data.Total.Should().Be(21);
    }

    [Fact]
    public async Task GetMealPlanNutrition_MemberBasic_ShouldReturn200Ok()
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

        var responseData = new GetMealPlanNutritionResponseData
        {
            PlanId = 1,
            Level = "BASIC",
            TargetKcalPerDay = 1800m,
            TotalKcalWeek = 11550m,
            Days = new List<MealPlanDayNutritionDto>()
        };

        _mockService.Setup(s => s.GetMealPlanNutritionAsync(1, userId, userRole, "BASIC", It.IsAny<CancellationToken>()))
            .ReturnsAsync(responseData);

        // Act
        var actionResult = await _controller.GetMealPlanNutrition(1, "BASIC", CancellationToken.None);

        // Assert
        var objectResult = actionResult as ObjectResult;
        objectResult.Should().NotBeNull();
        objectResult!.StatusCode.Should().Be(StatusCodes.Status200OK);

        var apiResponse = objectResult.Value as ApiResponse<GetMealPlanNutritionResponseData>;
        apiResponse.Should().NotBeNull();
        apiResponse!.Data!.Level.Should().Be("BASIC");
    }

    [Fact]
    public async Task GetMealPlanNutrition_VipDetailed_ShouldReturn200Ok()
    {
        // Arrange
        var userId = 45;
        var userRole = UserRoles.Vip;
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId.ToString()),
            new(ClaimTypes.Role, userRole)
        };
        _controller.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));

        var responseData = new GetMealPlanNutritionResponseData
        {
            PlanId = 1,
            Level = "DETAILED",
            TargetKcalPerDay = 1800m,
            TotalKcalWeek = 11550m,
            Days = new List<MealPlanDayNutritionDto>()
        };

        _mockService.Setup(s => s.GetMealPlanNutritionAsync(1, userId, userRole, "DETAILED", It.IsAny<CancellationToken>()))
            .ReturnsAsync(responseData);

        // Act
        var actionResult = await _controller.GetMealPlanNutrition(1, "DETAILED", CancellationToken.None);

        // Assert
        var objectResult = actionResult as ObjectResult;
        objectResult.Should().NotBeNull();
        objectResult!.StatusCode.Should().Be(StatusCodes.Status200OK);

        var apiResponse = objectResult.Value as ApiResponse<GetMealPlanNutritionResponseData>;
        apiResponse.Should().NotBeNull();
        apiResponse!.Data!.Level.Should().Be("DETAILED");
    }

    [Fact]
    public async Task GetMealPlanNutrition_GuestUser_ShouldThrowForbidden()
    {
        // Arrange
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, "999"),
            new(ClaimTypes.Role, UserRoles.Guest)
        };
        _controller.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));

        // Act & Assert
        var act = () => _controller.GetMealPlanNutrition(1, "BASIC", CancellationToken.None);
        var ex = await act.Should().ThrowAsync<AppException>();
        ex.Which.StatusCode.Should().Be(403);
        ex.Which.ErrorCode.Should().Be(ErrorCodes.ErrGuestPersonalizationRestricted);
    }

    [Fact]
    public async Task CreateMealPlanShoppingList_AuthorizedUser_ShouldReturn201Created()
    {
        // Arrange
        var mockShoppingListService = new Mock<IShoppingListService>();
        var controller = new MealPlansController(_mockService.Object, mockShoppingListService.Object);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, "45"),
            new(ClaimTypes.Role, UserRoles.Member)
        };
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"))
            }
        };

        var responseData = new CreateMealPlanShoppingListResponseData
        {
            ListId = 12,
            PlanId = 45,
            Status = "PENDING",
            Aisles = new List<ShoppingListAisleDto>()
        };

        mockShoppingListService.Setup(s => s.CreateMealPlanShoppingListAsync(45, 45, It.IsAny<CreateMealPlanShoppingListRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(responseData);

        // Act
        var actionResult = await controller.CreateMealPlanShoppingList(45, new CreateMealPlanShoppingListRequest(), CancellationToken.None);

        // Assert
        var objectResult = actionResult as ObjectResult;
        objectResult.Should().NotBeNull();
        objectResult!.StatusCode.Should().Be(StatusCodes.Status201Created);

        var apiResponse = objectResult.Value as ApiResponse<CreateMealPlanShoppingListResponseData>;
        apiResponse.Should().NotBeNull();
        apiResponse!.Data!.ListId.Should().Be(12);
    }

    [Fact]
    public async Task CreateMealPlanShoppingList_GuestUser_ShouldThrowForbidden()
    {
        // Arrange
        var mockShoppingListService = new Mock<IShoppingListService>();
        var controller = new MealPlansController(_mockService.Object, mockShoppingListService.Object);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, "999"),
            new(ClaimTypes.Role, UserRoles.Guest)
        };
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"))
            }
        };

        // Act & Assert
        var act = () => controller.CreateMealPlanShoppingList(45, new CreateMealPlanShoppingListRequest(), CancellationToken.None);
        var ex = await act.Should().ThrowAsync<AppException>();
        ex.Which.StatusCode.Should().Be(403);
        ex.Which.ErrorCode.Should().Be(ErrorCodes.ErrGuestPersonalizationRestricted);
    }

    [Fact]
    public async Task ExportMealPlan_VipUser_ShouldReturn200FileContentResult()
    {
        // Arrange
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, "45"),
            new(ClaimTypes.Role, UserRoles.Vip)
        };
        _controller.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));

        var dummyPdf = new byte[] { 0x25, 0x50, 0x44, 0x46, 0x01, 0x02 };
        _mockService.Setup(s => s.ExportMealPlanPdfAsync(45, 45, UserRoles.Vip, "PLAN,SHOPPING", It.IsAny<CancellationToken>()))
            .ReturnsAsync(dummyPdf);

        // Act
        var actionResult = await _controller.ExportMealPlan(45, "PLAN,SHOPPING", CancellationToken.None);

        // Assert
        var fileResult = actionResult as FileContentResult;
        fileResult.Should().NotBeNull();
        fileResult!.ContentType.Should().Be("application/pdf");
        fileResult.FileDownloadName.Should().Be("veggielife-meal-plan-45.pdf");
        fileResult.FileContents.Should().Equal(dummyPdf);
    }

    [Fact]
    public async Task ExportMealPlan_GuestUser_ShouldThrowForbidden()
    {
        // Arrange
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, "999"),
            new(ClaimTypes.Role, UserRoles.Guest)
        };
        _controller.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));

        // Act & Assert
        var act = () => _controller.ExportMealPlan(45, "PLAN,SHOPPING", CancellationToken.None);
        var ex = await act.Should().ThrowAsync<AppException>();
        ex.Which.StatusCode.Should().Be(403);
        ex.Which.ErrorCode.Should().Be(ErrorCodes.ErrGuestPersonalizationRestricted);
    }
}
