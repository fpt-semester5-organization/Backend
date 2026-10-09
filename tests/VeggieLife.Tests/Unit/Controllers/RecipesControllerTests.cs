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
using VeggieLife.BLL.DTOs.Recipes.Requests;
using VeggieLife.BLL.DTOs.Recipes.Responses;
using VeggieLife.BLL.Services.Recipes;
using Xunit;

namespace VeggieLife.Tests.Unit.Controllers;

/// <summary>
/// Unit Tests cho RecipesController (FN-POST-01, FN-POST-02).
/// Kiểm thử cấu trúc envelope response chuẩn D52, PagedMeta và các cờ phân quyền Guest.
/// </summary>
public class RecipesControllerTests
{
    private readonly Mock<IRecipeService> _mockService;
    private readonly RecipesController _controller;

    public RecipesControllerTests()
    {
        _mockService = new Mock<IRecipeService>();
        _controller = new RecipesController(_mockService.Object);

        // Thiết lập HttpContext mặc định
        var httpContext = new DefaultHttpContext();
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };
    }

    [Fact]
    public async Task ListRecipes_GuestUser_ShouldReturnPagedResponseWithSignUpPrompt()
    {
        // Arrange
        var filter = new RecipeFilterRequest { Keyword = "canh" };
        var sampleList = new List<RecipeSummaryDto>
        {
            new()
            {
                RecipeId = 512,
                Type = "RECIPE",
                Title = "Canh nấm đậu phụ rong biển",
                DietarySchool = "VEGAN",
                CookTimeMinutes = 25,
                CaloriesKcal = 310m,
                VoteCount = 42,
                IsVerified = true
            }
        };
        var meta = new PagedMeta(1, 10, 1, signUpPromptAtItem: 5);

        _mockService.Setup(s => s.ListRecipesAsync(filter, true, It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((sampleList, meta));

        // Act
        var result = await _controller.ListRecipes(filter, CancellationToken.None);

        // Assert
        var okResult = result as OkObjectResult;
        okResult.Should().NotBeNull();
        var envelope = okResult!.Value as ApiResponse<IReadOnlyList<RecipeSummaryDto>>;
        envelope.Should().NotBeNull();
        envelope!.StatusCode.Should().Be(200);
        envelope.Message.Should().Be("OK");
        envelope.Data.Should().HaveCount(1);
        envelope.Data![0].RecipeId.Should().Be(512);
        envelope.Meta.Should().NotBeNull();
        envelope.Meta!.SignUpPromptAtItem.Should().Be(5);
    }

    [Fact]
    public async Task GetRecipe_ShouldReturnOkEnvelopeWithFullDetails()
    {
        // Arrange
        var detailDto = new RecipeDetailDto
        {
            RecipeId = 512,
            Type = "RECIPE",
            Title = "Canh nấm đậu phụ rong biển",
            DietarySchool = "VEGAN",
            CookTimeMinutes = 25,
            CaloriesKcal = 310m,
            VoteCount = 42,
            IsVerified = true,
            Description = "Món canh thanh đạm...",
            Servings = 2,
            PrepTimeMinutes = 10,
            Instructions = new List<string> { "Bước 1: ...", "Bước 2: ..." },
            Ingredients = new List<RecipeIngredientDetailDto>
            {
                new() { IngredientId = 12, Name = "Nấm rơm", Quantity = 200, Unit = "g" }
            },
            HasTranscript = true,
            CommentCount = 5,
            MemberActions = new MemberActionsDto { CanVote = false, CanComment = false, CanReport = false }
        };

        _mockService.Setup(s => s.GetRecipeByIdAsync(512, true, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(detailDto);

        // Act
        var result = await _controller.GetRecipe(512, CancellationToken.None);

        // Assert
        var okResult = result as OkObjectResult;
        okResult.Should().NotBeNull();
        var envelope = okResult!.Value as ApiResponse<RecipeDetailDto>;
        envelope.Should().NotBeNull();
        envelope!.StatusCode.Should().Be(200);
        envelope.Message.Should().Be("OK");
        envelope.Data.Should().NotBeNull();
        envelope.Data!.RecipeId.Should().Be(512);
        envelope.Data.Ingredients.Should().HaveCount(1);
        envelope.Data.MemberActions.CanVote.Should().BeFalse();
    }

    [Fact]
    public async Task GetRecipe_AuthenticatedMember_PassesUserIdAndIsGuestFalse()
    {
        // Arrange
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, "42"),
            new(ClaimTypes.Role, "MEMBER")
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        _controller.ControllerContext.HttpContext.User = new ClaimsPrincipal(identity);

        var detailDto = new RecipeDetailDto
        {
            RecipeId = 512,
            MemberActions = new MemberActionsDto { CanVote = true, CanComment = true, CanReport = true }
        };

        _mockService.Setup(s => s.GetRecipeByIdAsync(512, false, 42, It.IsAny<CancellationToken>()))
            .ReturnsAsync(detailDto);

        // Act
        var result = await _controller.GetRecipe(512, CancellationToken.None);

        // Assert
        var okResult = result as OkObjectResult;
        okResult.Should().NotBeNull();
        _mockService.Verify(s => s.GetRecipeByIdAsync(512, false, 42, It.IsAny<CancellationToken>()), Times.Once);
    }
}
