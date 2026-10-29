using System;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using VeggieLife.BLL.Common.Exceptions;
using VeggieLife.BLL.Services.Foods;
using VeggieLife.DAL.Context;
using VeggieLife.DAL.Entities;
using Xunit;

namespace VeggieLife.Tests.Unit.Services.Foods;

/// <summary>
/// Bộ kiểm thử đơn vị cho FoodService (tra cứu autocomplete, lấy chi tiết, và pattern Get-or-Create).
/// </summary>
public class FoodServiceTests : IDisposable
{
    private readonly AppDbContext _db;
    private readonly Mock<ILogger<FoodService>> _mockLogger;
    private readonly FoodService _service;

    public FoodServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _db = new AppDbContext(options);
        _mockLogger = new Mock<ILogger<FoodService>>();
        _service = new FoodService(_db, _mockLogger.Object);

        SeedTestData();
    }

    public void Dispose()
    {
        _db.Database.EnsureDeleted();
        _db.Dispose();
    }

    private void SeedTestData()
    {
        var category = new FoodCategory
        {
            CategoryId = 1,
            CategoryName = "Món nước",
            IsActive = true
        };
        _db.FoodCategories.Add(category);

        _db.Foods.AddRange(
            new Food { FoodId = 1, CategoryId = 1, FoodName = "Bún bò Huế chay", CreatedAt = DateTime.UtcNow },
            new Food { FoodId = 2, CategoryId = 1, FoodName = "Bún riêu chay", CreatedAt = DateTime.UtcNow },
            new Food { FoodId = 3, CategoryId = 1, FoodName = "Phở chay Hà Nội", CreatedAt = DateTime.UtcNow }
        );

        _db.SaveChanges();
    }

    [Fact]
    public async Task SearchFoodsAsync_WithMatchingKeyword_ReturnsFilteredFoods()
    {
        // Act
        var results = await _service.SearchFoodsAsync("Bún");

        // Assert
        results.Should().NotBeNull();
        results.Should().HaveCount(2);
        results.Select(f => f.FoodName).Should().Contain("Bún bò Huế chay");
        results.Select(f => f.FoodName).Should().Contain("Bún riêu chay");
    }

    [Fact]
    public async Task SearchFoodsAsync_WithEmptyKeyword_ReturnsAllUpTo10()
    {
        // Act
        var results = await _service.SearchFoodsAsync("");

        // Assert
        results.Should().HaveCount(3);
    }

    [Fact]
    public async Task SearchFoodsAsync_WithNonMatchingKeyword_ReturnsEmptyList()
    {
        // Act
        var results = await _service.SearchFoodsAsync("Cơm tấm");

        // Assert
        results.Should().BeEmpty();
    }

    [Fact]
    public async Task GetOrCreateFoodAsync_WhenFoodAlreadyExists_ReturnsExistingEntityWithoutDuplicating()
    {
        // Act (case-insensitive check: "bún bò huế chay" vs "Bún bò Huế chay")
        var food = await _service.GetOrCreateFoodAsync("bún bò huế chay", 1);

        // Assert
        food.Should().NotBeNull();
        food.FoodId.Should().Be(1);
        _db.Foods.Count().Should().Be(3); // Không tăng số lượng
    }

    [Fact]
    public async Task GetOrCreateFoodAsync_WhenFoodDoesNotExist_InsertsNewAndReturnsIt()
    {
        // Act
        var food = await _service.GetOrCreateFoodAsync("Mì Quảng chay", 1);

        // Assert
        food.Should().NotBeNull();
        food.FoodName.Should().Be("Mì Quảng chay");
        food.FoodId.Should().BeGreaterThan(0);
        _db.Foods.Count().Should().Be(4);
    }

    [Fact]
    public async Task GetOrCreateFoodAsync_WhenFoodNameIsWhitespace_ThrowsBadRequest()
    {
        // Act & Assert
        var act = async () => await _service.GetOrCreateFoodAsync("   ", 1);
        await act.Should().ThrowAsync<AppException>();
    }

    [Fact]
    public async Task GetFoodByIdAsync_WhenIdExists_ReturnsDto()
    {
        // Act
        var result = await _service.GetFoodByIdAsync(1);

        // Assert
        result.Should().NotBeNull();
        result!.FoodId.Should().Be(1);
        result.FoodName.Should().Be("Bún bò Huế chay");
        result.CategoryId.Should().Be(1);
    }

    [Fact]
    public async Task GetFoodByIdAsync_WhenIdDoesNotExist_ReturnsNull()
    {
        // Act
        var result = await _service.GetFoodByIdAsync(999);

        // Assert
        result.Should().BeNull();
    }
}

