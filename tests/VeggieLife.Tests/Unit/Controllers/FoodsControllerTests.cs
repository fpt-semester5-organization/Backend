using System.Collections.Generic;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using VeggieLife.API.Controllers;
using VeggieLife.BLL.Common.Exceptions;
using VeggieLife.BLL.Common.Responses;
using VeggieLife.BLL.DTOs.Foods.Requests;
using VeggieLife.BLL.DTOs.Foods.Responses;
using VeggieLife.BLL.DTOs.Recipes.Requests;
using VeggieLife.BLL.DTOs.Recipes.Responses;
using VeggieLife.BLL.Services.Foods;
using VeggieLife.BLL.Services.Recipes;
using VeggieLife.DAL.Entities;
using Xunit;

namespace VeggieLife.Tests.Unit.Controllers;

/// <summary>
/// Unit Tests kiểm thử các endpoint của FoodsController.
/// </summary>
public class FoodsControllerTests
{
    private readonly Mock<IFoodService> _mockFoodService;
    private readonly Mock<IRecipeService> _mockRecipeService;
    private readonly FoodsController _controller;

    public FoodsControllerTests()
    {
        _mockFoodService = new Mock<IFoodService>();
        _mockRecipeService = new Mock<IRecipeService>();
        _controller = new FoodsController(_mockFoodService.Object, _mockRecipeService.Object);

        var httpContext = new DefaultHttpContext();
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };
    }

    [Fact]
    public async Task SearchFoods_ShouldReturnOkEnvelopeWithList()
    {
        // Arrange
        var foods = new List<FoodSummaryDto>
        {
            new() { FoodId = 1, FoodName = "Bún Huế", CategoryId = 1 }
        };

        _mockFoodService.Setup(s => s.SearchFoodsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(foods);

        // Act
        var result = await _controller.SearchFoods("bún", CancellationToken.None);

        // Assert
        var okResult = result as OkObjectResult;
        okResult.Should().NotBeNull();
        var envelope = okResult!.Value as ApiResponse<IReadOnlyList<FoodSummaryDto>>;
        envelope.Should().NotBeNull();
        envelope!.StatusCode.Should().Be(200);
        envelope.Data.Should().HaveCount(1);
        envelope.Data![0].FoodName.Should().Be("Bún Huế");
    }

    [Fact]
    public async Task GetFoodById_WhenFound_ShouldReturnOkEnvelope()
    {
        // Arrange
        var food = new FoodSummaryDto { FoodId = 1, FoodName = "Bún Huế", CategoryId = 1 };
        _mockFoodService.Setup(s => s.GetFoodByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(food);

        // Act
        var result = await _controller.GetFoodById(1, CancellationToken.None);

        // Assert
        var okResult = result as OkObjectResult;
        okResult.Should().NotBeNull();
        var envelope = okResult!.Value as ApiResponse<FoodSummaryDto>;
        envelope.Should().NotBeNull();
        envelope!.StatusCode.Should().Be(200);
        envelope.Data!.FoodName.Should().Be("Bún Huế");
    }

    [Fact]
    public async Task GetFoodById_WhenNotFound_ShouldThrowNotFound()
    {
        // Arrange
        _mockFoodService.Setup(s => s.GetFoodByIdAsync(99, It.IsAny<CancellationToken>()))
            .ReturnsAsync((FoodSummaryDto?)null);

        // Act & Assert
        var act = async () => await _controller.GetFoodById(99, CancellationToken.None);
        await act.Should().ThrowAsync<AppException>()
            .Where(e => e.StatusCode == 404);
    }

    [Fact]
    public async Task CreateOrGetFood_WhenValid_ShouldReturnCreatedResponse()
    {
        // Arrange
        var request = new CreateFoodRequest { FoodName = "Cơm tấm chay", CategoryId = 2, ImageUrl = "https://example.com/com-tam.jpg" };
        var entity = new Food { FoodId = 10, FoodName = "Cơm tấm chay", CategoryId = 2, ImageUrl = "https://example.com/com-tam.jpg" };

        _mockFoodService.Setup(s => s.GetOrCreateFoodAsync("Cơm tấm chay", 2, "https://example.com/com-tam.jpg", It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);

        // Act
        var result = await _controller.CreateOrGetFood(request, CancellationToken.None);

        // Assert
        var createdResult = result as ObjectResult;
        createdResult.Should().NotBeNull();
        createdResult!.StatusCode.Should().Be(201);
        var envelope = createdResult.Value as ApiResponse<FoodSummaryDto>;
        envelope.Should().NotBeNull();
        envelope!.Data!.FoodId.Should().Be(10);
        envelope.Data.FoodName.Should().Be("Cơm tấm chay");
        envelope.Data.ImageUrl.Should().Be("https://example.com/com-tam.jpg");
    }

    [Fact]
    public async Task CreateOrGetFood_WhenNameIsEmpty_ShouldThrowBadRequest()
    {
        // Arrange
        var request = new CreateFoodRequest { FoodName = "", CategoryId = 2 };

        // Act & Assert
        var act = async () => await _controller.CreateOrGetFood(request, CancellationToken.None);
        await act.Should().ThrowAsync<AppException>()
            .Where(e => e.StatusCode == 400);
    }

    [Fact]
    public async Task ListRecipesByFood_WhenFound_ShouldReturnPagedResponse()
    {
        // Arrange
        var food = new FoodSummaryDto { FoodId = 1, FoodName = "Bún Huế", CategoryId = 1 };
        _mockFoodService.Setup(s => s.GetFoodByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(food);

        var recipes = new List<RecipeSummaryDto>
        {
            new() { RecipeId = 101, Title = "Bún Huế giảm cân", CaloriesKcal = 350 },
            new() { RecipeId = 102, Title = "Bún Huế tăng cân", CaloriesKcal = 650 }
        };
        var meta = new PagedMeta(1, 10, 2);

        _mockRecipeService.Setup(s => s.ListRecipesAsync(It.IsAny<RecipeFilterRequest>(), It.IsAny<bool>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((recipes, meta));

        // Act
        var result = await _controller.ListRecipesByFood(1, new RecipeFilterRequest(), CancellationToken.None);

        // Assert
        var okResult = result as OkObjectResult;
        okResult.Should().NotBeNull();
        var envelope = okResult!.Value as ApiResponse<IReadOnlyList<RecipeSummaryDto>>;
        envelope.Should().NotBeNull();
        envelope!.StatusCode.Should().Be(200);
        envelope.Data.Should().HaveCount(2);
        envelope.Meta!.Total.Should().Be(2);
    }
}
