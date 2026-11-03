using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using VeggieLife.BLL.DTOs.Recipes.Requests;
using VeggieLife.BLL.Services.Cache;
using VeggieLife.BLL.Services.Foods;
using VeggieLife.BLL.Services.Knowledge;
using VeggieLife.BLL.Services.Moderation;
using VeggieLife.BLL.Services.Recipes;
using VeggieLife.DAL.Context;
using VeggieLife.DAL.Entities;
using Xunit;

namespace VeggieLife.Tests.Unit.Services.Recipes;

public class RecipeFoodCreationTests : IDisposable
{
    private readonly AppDbContext _db;
    private readonly RecipeService _service;

    public RecipeFoodCreationTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: "RecipeFoodDb_" + Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        _db = new AppDbContext(options);

        // Seed basic category & school
        _db.FoodCategories.Add(new FoodCategory { CategoryId = 1, CategoryName = "Món Nước", IsActive = true });
        _db.DietarySchools.Add(new DietarySchool { DietarySchoolId = 1, SchoolCode = "BUDDHIST", SchoolName = "Chay Phật Giáo", ForbiddenFoodRules = "{}" });
        _db.Users.Add(new User { UserId = 1, Email = "test@veggielife.vn", FullName = "Test User", Role = "MEMBER", AccountStatus = "ACTIVE" });
        _db.SaveChanges();

        var mockRedis = new Mock<IRedisService>();
        var foodService = new FoodService(_db, NullLogger<FoodService>.Instance);

        _service = new RecipeService(
            _db,
            mockRedis.Object,
            NullLogger<RecipeService>.Instance,
            null,
            null,
            foodService
        );
    }

    [Fact]
    public async Task CreateRecipeAsync_WithNewFoodName_ShouldAutoCreateFoodAndLinkRecipe()
    {
        // Arrange
        var request = new CreateRecipeRequest
        {
            Title = "Bún Huế chay thanh đạm",
            FoodName = "Bún Huế chay", // Brand new food name
            FoodId = null,
            DietarySchoolId = 1,
            Submit = false // Draft
        };

        // Act
        var result = await _service.CreateRecipeAsync(1, request, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.FoodId.Should().NotBeNull();
        result.FoodName.Should().Be("Bún Huế chay");

        // Verify in DB
        var createdFood = await _db.Foods.FirstOrDefaultAsync(f => f.FoodName == "Bún Huế chay");
        createdFood.Should().NotBeNull();
        createdFood!.FoodId.Should().Be(result.FoodId!.Value);

        var createdRecipe = await _db.Recipes.FirstOrDefaultAsync(r => r.RecipeId == result.RecipeId);
        createdRecipe.Should().NotBeNull();
        createdRecipe!.FoodId.Should().Be(createdFood.FoodId);
    }

    [Fact]
    public async Task CreateRecipeAsync_WithExistingFoodId_ShouldLinkDirectlyWithoutCreatingNewFood()
    {
        // Arrange: Pre-existing food
        var existingFood = new Food { FoodName = "Phở chay Hà Nội", CategoryId = 1, CreatedAt = DateTime.UtcNow };
        _db.Foods.Add(existingFood);
        await _db.SaveChangesAsync();

        var request = new CreateRecipeRequest
        {
            Title = "Phở chay Hà Nội đặc biệt",
            FoodId = existingFood.FoodId,
            FoodName = "Phở chay Hà Nội",
            CategoryId = 1,
            DietarySchoolId = 1,
            Submit = false
        };

        // Act
        var result = await _service.CreateRecipeAsync(1, request, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.FoodId.Should().Be(existingFood.FoodId);

        var count = await _db.Foods.CountAsync(f => f.FoodName == "Phở chay Hà Nội");
        count.Should().Be(1); // No duplicate food created
    }

    public void Dispose()
    {
        _db.Dispose();
    }
}
