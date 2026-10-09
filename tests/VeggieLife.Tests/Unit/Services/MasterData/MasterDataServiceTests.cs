using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using VeggieLife.BLL.Common.Constants;
using VeggieLife.BLL.Common.Exceptions;
using VeggieLife.BLL.DTOs.MasterData.Responses;
using VeggieLife.BLL.Services.Cache;
using VeggieLife.BLL.Services.MasterData;
using VeggieLife.DAL.Context;
using VeggieLife.DAL.Entities;
using Xunit;

namespace VeggieLife.Tests.Unit.Services.MasterData;

/// <summary>
/// Bộ kiểm thử cho MasterDataService: 4 danh mục dùng chung, phân giải JSON quy tắc cấm (D109), phân trang & Redis Cache.
/// </summary>
public class MasterDataServiceTests : IDisposable
{
    private readonly AppDbContext _db;
    private readonly Mock<IRedisService> _mockRedis;
    private readonly Mock<ILogger<MasterDataService>> _mockLogger;
    private readonly MasterDataService _service;

    public MasterDataServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _db = new AppDbContext(options);
        _mockRedis = new Mock<IRedisService>();
        _mockLogger = new Mock<ILogger<MasterDataService>>();

        _service = new MasterDataService(_db, _mockRedis.Object, _mockLogger.Object);

        SeedTestData();
    }

    public void Dispose()
    {
        _db.Database.EnsureDeleted();
        _db.Dispose();
    }

    private void SeedTestData()
    {
        _db.DietarySchools.AddRange(
            new DietarySchool
            {
                DietarySchoolId = 1,
                SchoolCode = "VEGAN",
                SchoolName = "Thuần chay",
                ForbiddenFoodRules = "{\"forbiddenFoodGroups\":[\"EGG\",\"DAIRY\",\"POULTRY\",\"SEAFOOD\"],\"forbidAllium\":false}"
            },
            new DietarySchool
            {
                DietarySchoolId = 5,
                SchoolCode = "BUDDHIST",
                SchoolName = "Chay Phật giáo",
                ForbiddenFoodRules = "{\"forbiddenFoodGroups\":[\"EGG\",\"DAIRY\",\"POULTRY\",\"SEAFOOD\"],\"forbidAllium\":true}"
            }
        );

        _db.Allergens.AddRange(
            new Allergen { AllergenId = 1, Code = "SOY", AllergenName = "Đậu nành" },
            new Allergen { AllergenId = 2, Code = "GLUTEN", AllergenName = "Gluten" },
            new Allergen { AllergenId = 3, Code = "PEANUT", AllergenName = "Đậu phộng" }
        );

        _db.FoodCategories.AddRange(
            new FoodCategory { CategoryId = 1, CategoryName = "Món canh chay", IsCore = true, IsActive = true },
            new FoodCategory { CategoryId = 2, CategoryName = "Món xào chay", IsCore = true, IsActive = true },
            new FoodCategory { CategoryId = 3, CategoryName = "Món cũ ngưng dùng", IsCore = false, IsActive = false }
        );

        _db.Ingredients.AddRange(
            new Ingredient
            {
                IngredientId = 1,
                IngredientName = "Đậu phụ trắng",
                StandardUnit = "miếng",
                DefaultStallType = "PLANT_PROTEIN",
                FoodGroup = "PLANT",
                IsAllium = false,
                CaloriesPer100g = 76m,
                CreatedAt = DateTime.UtcNow
            },
            new Ingredient
            {
                IngredientId = 2,
                IngredientName = "Nấm rơm tươi",
                StandardUnit = "g",
                DefaultStallType = "PRODUCE",
                FoodGroup = "PLANT",
                IsAllium = false,
                CaloriesPer100g = 32m,
                CreatedAt = DateTime.UtcNow
            },
            new Ingredient
            {
                IngredientId = 3,
                IngredientName = "Tỏi tây boa-rô",
                StandardUnit = "g",
                DefaultStallType = "PRODUCE",
                FoodGroup = "PLANT",
                IsAllium = true,
                CaloriesPer100g = 61m,
                CreatedAt = DateTime.UtcNow
            }
        );

        _db.SaveChanges();
    }

    [Fact]
    public async Task GetDietarySchoolsAsync_ShouldReturnSchoolsAndParseJsonRules()
    {
        var result = await _service.GetDietarySchoolsAsync();

        result.Should().HaveCount(2);

        var vegan = result.First(s => s.SchoolCode == "VEGAN");
        vegan.SchoolName.Should().Be("Thuần chay");
        vegan.ForbidAllium.Should().BeFalse();
        vegan.ForbiddenFoodGroups.Should().Contain(new[] { "EGG", "DAIRY", "POULTRY", "SEAFOOD" });

        var buddhist = result.First(s => s.SchoolCode == "BUDDHIST");
        buddhist.ForbidAllium.Should().BeTrue();

        _mockRedis.Verify(r => r.SetAsync("dietary_schools_cache", It.IsAny<List<DietarySchoolDto>>(), It.IsAny<TimeSpan?>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetAllergensAsync_ShouldReturnAllergensAndCache()
    {
        var result = await _service.GetAllergensAsync();

        result.Should().HaveCount(3);
        result.Select(a => a.Code).Should().Contain(new[] { "SOY", "GLUTEN", "PEANUT" });

        _mockRedis.Verify(r => r.SetAsync("allergens_cache", It.IsAny<List<AllergenDto>>(), It.IsAny<TimeSpan?>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetCategoriesAsync_ShouldReturnOnlyActiveCategories()
    {
        var result = await _service.GetCategoriesAsync();

        result.Should().HaveCount(2);
        result.Select(c => c.CategoryName).Should().NotContain("Món cũ ngưng dùng");

        _mockRedis.Verify(r => r.SetAsync("categories_cache", It.IsAny<List<CategoryDto>>(), It.IsAny<TimeSpan?>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetIngredientsAsync_KeywordFilter_ShouldReturnMatchedItems()
    {
        var (items, meta) = await _service.GetIngredientsAsync("Nấm", page: 1, size: 20);

        items.Should().HaveCount(1);
        items.First().Name.Should().Be("Nấm rơm tươi");
        meta.Total.Should().Be(1);
        meta.Page.Should().Be(1);
    }

    [Theory]
    [InlineData(0, 10)] // page < 1
    [InlineData(1, 0)]  // size < 1
    [InlineData(1, 51)] // size > 50
    public async Task GetIngredientsAsync_InvalidPagination_ShouldThrowBadRequest(int page, int size)
    {
        var act = async () => await _service.GetIngredientsAsync(keyword: null, page, size);

        var ex = await act.Should().ThrowAsync<AppException>();
        ex.Which.StatusCode.Should().Be(400);
        ex.Which.ErrorCode.Should().Be(ErrorCodes.ErrValidation);
    }

    [Fact]
    public async Task GetIngredientsAsync_KeywordTooLong_ShouldThrowBadRequest()
    {
        var longKeyword = new string('A', 101);

        var act = async () => await _service.GetIngredientsAsync(keyword: longKeyword, page: 1, size: 20);

        var ex = await act.Should().ThrowAsync<AppException>();
        ex.Which.StatusCode.Should().Be(400);
        ex.Which.ErrorCode.Should().Be(ErrorCodes.ErrValidation);
    }
}
