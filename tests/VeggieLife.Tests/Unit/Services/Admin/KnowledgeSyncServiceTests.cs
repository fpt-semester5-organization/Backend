using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using VeggieLife.BLL.Services.AI;
using VeggieLife.BLL.Services.Knowledge;
using VeggieLife.DAL.Context;
using VeggieLife.DAL.Entities;
using Xunit;

namespace VeggieLife.Tests.Unit.Services.Admin;

public class KnowledgeSyncServiceTests
{
    private readonly Mock<IAiServiceClient> _aiClientMock = new();

    private AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: $"KnowledgeSyncDb_{Guid.NewGuid()}")
            .Options;
        return new AppDbContext(options);
    }

    [Fact]
    public async Task SyncRecipeAsync_WhenApprovedActive_FormatsAndCallsUpsert()
    {
        // Arrange
        using var db = CreateDbContext();
        var category = new FoodCategory { CategoryId = 1, CategoryName = "Món canh", Description = "Món nước" };
        var school = new DietarySchool { DietarySchoolId = 1, SchoolCode = "VEGAN", SchoolName = "Thuần chay", ForbiddenFoodRules = "[]" };
        var ingredient = new Ingredient { IngredientId = 1, IngredientName = "Đậu phụ mơ", StandardUnit = "bìa", FoodGroup = "PLANT", DefaultStallType = "PRODUCE" };

        var recipe = new Recipe
        {
            RecipeId = 101,
            Title = "Canh chua đậu phụ",
            Description = "Món canh chua thanh đạm bổ dưỡng",
            Status = "APPROVED_ACTIVE",
            Instructions = "Bước 1: Nấu nước sôi. Bước 2: Cho đậu phụ.",
            PrepTimeMinutes = 10,
            CookTimeMinutes = 15,
            Servings = 4,
            CaloriesKcal = 180,
            DifficultyLevel = "Dễ",
            Category = category,
            DietarySchool = school,
            Source = "USER",
            CreatedAt = DateTime.UtcNow
        };
        recipe.RecipeIngredients.Add(new RecipeIngredient
        {
            RecipeId = 101,
            Ingredient = ingredient,
            QuantityNeeded = 2,
            Unit = "bìa",
            IsOptional = false
        });

        db.Recipes.Add(recipe);
        await db.SaveChangesAsync();

        _aiClientMock
            .Setup(c => c.UpsertKnowledgeAsync(
                "RECIPE",
                "101",
                It.Is<string>(t => t.Contains("Canh chua đậu phụ")),
                It.Is<string>(txt => txt.Contains("Đậu phụ mơ") && txt.Contains("Bước 1")),
                It.IsAny<DateTime?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var service = new KnowledgeSyncService(db, _aiClientMock.Object, NullLogger<KnowledgeSyncService>.Instance);

        // Act
        var result = await service.SyncRecipeAsync(101);

        // Assert
        Assert.True(result);
        _aiClientMock.Verify(c => c.UpsertKnowledgeAsync(
            "RECIPE",
            "101",
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<DateTime?>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SyncRecipeAsync_WhenStatusNotApproved_CallsDelete()
    {
        // Arrange
        using var db = CreateDbContext();
        var recipe = new Recipe
        {
            RecipeId = 102,
            Title = "Công thức bị từ chối",
            Instructions = "N/A",
            DifficultyLevel = "Dễ",
            Status = "REJECTED",
            Source = "USER",
            CreatedAt = DateTime.UtcNow
        };
        db.Recipes.Add(recipe);
        await db.SaveChangesAsync();

        var service = new KnowledgeSyncService(db, _aiClientMock.Object, NullLogger<KnowledgeSyncService>.Instance);

        // Act
        var result = await service.SyncRecipeAsync(102);

        // Assert
        Assert.False(result);
        _aiClientMock.Verify(c => c.DeleteKnowledgeAsync("RECIPE", "102", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SyncENumberAsync_WhenExists_CallsUpsertWithFullInfo()
    {
        // Arrange
        using var db = CreateDbContext();
        var enumber = new EnumberDictionary
        {
            Code = "E100",
            Name = "Curcumin",
            CategoryType = "Chất tạo màu",
            OriginSource = "Chiết xuất củ nghệ",
            VeganStatus = "VEGAN",
            HealthRiskNotes = "An toàn",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        db.EnumberDictionaries.Add(enumber);
        await db.SaveChangesAsync();

        _aiClientMock
            .Setup(c => c.UpsertKnowledgeAsync(
                "E_NUMBER",
                "E100",
                It.Is<string>(t => t.Contains("E100") && t.Contains("Curcumin")),
                It.Is<string>(txt => txt.Contains("Chất tạo màu") && txt.Contains("VEGAN")),
                It.IsAny<DateTime?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var service = new KnowledgeSyncService(db, _aiClientMock.Object, NullLogger<KnowledgeSyncService>.Instance);

        // Act
        var result = await service.SyncENumberAsync("e100");

        // Assert
        Assert.True(result);
        _aiClientMock.Verify(c => c.UpsertKnowledgeAsync(
            "E_NUMBER",
            "E100",
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<DateTime?>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
