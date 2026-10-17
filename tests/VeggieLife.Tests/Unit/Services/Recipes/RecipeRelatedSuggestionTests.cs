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
using VeggieLife.BLL.DTOs.Recipes.Responses;
using VeggieLife.BLL.Services.Cache;
using VeggieLife.BLL.Services.Recipes;
using VeggieLife.DAL.Context;
using VeggieLife.DAL.Entities;
using Xunit;

namespace VeggieLife.Tests.Unit.Services.Recipes;

/// <summary>
/// Unit Tests cho FN-POST-11: Gợi ý nội dung liên quan khi xem chi tiết công thức
/// Đảm bảo tuân thủ BR-USR-16, BR-SYS-CACHE-01, BR-ADM-CNT-001.
/// </summary>
public class RecipeRelatedSuggestionTests : IDisposable
{
    private readonly AppDbContext _db;
    private readonly Mock<IRedisService> _mockRedis;
    private readonly Mock<ILogger<RecipeService>> _mockLogger;
    private readonly RecipeService _service;

    public RecipeRelatedSuggestionTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _db = new AppDbContext(options);
        _mockRedis = new Mock<IRedisService>();
        _mockLogger = new Mock<ILogger<RecipeService>>();

        _service = new RecipeService(_db, _mockRedis.Object, _mockLogger.Object);

        SeedTestData();
    }

    public void Dispose()
    {
        _db.Database.EnsureDeleted();
        _db.Dispose();
    }

    private void SeedTestData()
    {
        // 1. Trường phái ăn chay
        var veganSchool = new DietarySchool
        {
            DietarySchoolId = 7,
            SchoolCode = "VEGAN",
            SchoolName = "Thuần chay (Vegan)",
            ForbiddenFoodRules = "{}"
        };
        var buddhistSchool = new DietarySchool
        {
            DietarySchoolId = 1,
            SchoolCode = "BUDDHIST",
            SchoolName = "Chay Phật giáo",
            ForbiddenFoodRules = "{}"
        };
        _db.DietarySchools.AddRange(veganSchool, buddhistSchool);

        // 2. Danh mục
        var soupCategory = new FoodCategory
        {
            CategoryId = 1,
            CategoryName = "Món canh",
            IsActive = true
        };
        var stirFryCategory = new FoodCategory
        {
            CategoryId = 2,
            CategoryName = "Món xào",
            IsActive = true
        };
        var inactiveCategory = new FoodCategory
        {
            CategoryId = 99,
            CategoryName = "Danh mục ẩn",
            IsActive = false
        };
        _db.FoodCategories.AddRange(soupCategory, stirFryCategory, inactiveCategory);

        // 3. Nguyên liệu
        var tofu = new Ingredient { IngredientId = 101, IngredientName = "Đậu phụ non", StandardUnit = "g", DefaultStallType = "PLANT_PROTEIN", FoodGroup = "PLANT" };
        var mushroom = new Ingredient { IngredientId = 102, IngredientName = "Nấm đông cô", StandardUnit = "g", DefaultStallType = "PRODUCE", FoodGroup = "PLANT" };
        var seaweed = new Ingredient { IngredientId = 103, IngredientName = "Rong biển", StandardUnit = "g", DefaultStallType = "DRY_GOODS", FoodGroup = "PLANT" };
        var carrot = new Ingredient { IngredientId = 104, IngredientName = "Cà rốt", StandardUnit = "g", DefaultStallType = "PRODUCE", FoodGroup = "PLANT" };
        _db.Ingredients.AddRange(tofu, mushroom, seaweed, carrot);

        // 4. Người dùng & Hồ sơ thể trạng
        var user1 = new User { UserId = 1, FullName = "Nguyễn Văn Chay", Email = "user1@veggielife.vn", Role = "MEMBER", AccountStatus = "ACTIVE" };
        var userWithBuddhistProfile = new User { UserId = 2, FullName = "Trần Tu", Email = "user2@veggielife.vn", Role = "MEMBER", AccountStatus = "ACTIVE" };
        _db.Users.AddRange(user1, userWithBuddhistProfile);

        var healthProfileUser2 = new HealthProfile
        {
            UserId = 2,
            DietarySchoolId = 1, // BUDDHIST
            Gender = "OTHER",
            RegionPreference = "SOUTH",
            HealthGoal = "MAINTAIN"
        };
        _db.HealthProfiles.Add(healthProfileUser2);

        // 5. Công thức gốc đang xem: Recipe 100 (Món canh, VEGAN, nguyên liệu: tofu, mushroom, seaweed)
        var sourceRecipe = new Recipe
        {
            RecipeId = 100,
            UserId = 1,
            CategoryId = 1, // Canh
            DietarySchoolId = 7, // VEGAN
            Title = "Canh nấm rong biển hạt sen",
            Description = "Món canh bổ dưỡng thanh mát",
            Instructions = "[\"B1\", \"B2\"]",
            CookTimeMinutes = 25,
            CaloriesKcal = 150m,
            DifficultyLevel = "EASY",
            Source = "USER",
            Status = "APPROVED_ACTIVE",
            ViewCount = 50,
            CreatedAt = DateTime.UtcNow.AddDays(-10)
        };
        _db.Recipes.Add(sourceRecipe);
        _db.RecipeIngredients.AddRange(
            new RecipeIngredient { RecipeId = 100, IngredientId = 101, QuantityNeeded = 100, Unit = "g" },
            new RecipeIngredient { RecipeId = 100, IngredientId = 102, QuantityNeeded = 50, Unit = "g" },
            new RecipeIngredient { RecipeId = 100, IngredientId = 103, QuantityNeeded = 30, Unit = "g" }
        );

        // Recipe 101 (VEGAN, Món canh (same cat), 2 nguyên liệu chung: tofu, mushroom; 5 votes)
        var r101 = new Recipe
        {
            RecipeId = 101,
            UserId = 1,
            CategoryId = 1,
            DietarySchoolId = 7,
            Title = "Canh đậu hũ nấm rơm",
            Instructions = "[\"B1\"]",
            DifficultyLevel = "EASY",
            Source = "USER",
            CookTimeMinutes = 20,
            CaloriesKcal = 140m,
            Status = "APPROVED_ACTIVE",
            ViewCount = 100,
            CreatedAt = DateTime.UtcNow.AddDays(-5)
        };
        _db.Recipes.Add(r101);
        _db.RecipeIngredients.AddRange(
            new RecipeIngredient { RecipeId = 101, IngredientId = 101, QuantityNeeded = 100, Unit = "g" },
            new RecipeIngredient { RecipeId = 101, IngredientId = 102, QuantityNeeded = 50, Unit = "g" }
        );

        // Recipe 102 (VEGAN, Món xào (diff cat), 2 nguyên liệu chung: tofu, mushroom; 2 votes)
        var r102 = new Recipe
        {
            RecipeId = 102,
            UserId = 1,
            CategoryId = 2,
            DietarySchoolId = 7,
            Title = "Đậu hũ sốt nấm xào thập cẩm",
            Instructions = "[\"B1\"]",
            DifficultyLevel = "MEDIUM",
            Source = "USER",
            CookTimeMinutes = 15,
            CaloriesKcal = 200m,
            Status = "APPROVED_ACTIVE",
            ViewCount = 80,
            CreatedAt = DateTime.UtcNow.AddDays(-4)
        };
        _db.Recipes.Add(r102);
        _db.RecipeIngredients.AddRange(
            new RecipeIngredient { RecipeId = 102, IngredientId = 101, QuantityNeeded = 100, Unit = "g" },
            new RecipeIngredient { RecipeId = 102, IngredientId = 102, QuantityNeeded = 50, Unit = "g" }
        );

        // Recipe 103 (VEGAN, Món canh (same cat), 1 nguyên liệu chung: seaweed; 10 votes)
        var r103 = new Recipe
        {
            RecipeId = 103,
            UserId = 1,
            CategoryId = 1,
            DietarySchoolId = 7,
            Title = "Canh rong biển đậu nành",
            Instructions = "[\"B1\"]",
            DifficultyLevel = "EASY",
            Source = "USER",
            CookTimeMinutes = 15,
            CaloriesKcal = 120m,
            Status = "APPROVED_ACTIVE",
            ViewCount = 90,
            CreatedAt = DateTime.UtcNow.AddDays(-3)
        };
        _db.Recipes.Add(r103);
        _db.RecipeIngredients.AddRange(
            new RecipeIngredient { RecipeId = 103, IngredientId = 103, QuantityNeeded = 50, Unit = "g" }
        );

        // Recipe 104 (BUDDHIST, Món canh, 2 nguyên liệu chung, 20 votes)
        var r104 = new Recipe
        {
            RecipeId = 104,
            UserId = 1,
            CategoryId = 1,
            DietarySchoolId = 1, // BUDDHIST
            Title = "Canh thanh tịnh chay Phật giáo",
            Instructions = "[\"B1\"]",
            DifficultyLevel = "MEDIUM",
            Source = "USER",
            CookTimeMinutes = 25,
            CaloriesKcal = 130m,
            Status = "APPROVED_ACTIVE",
            ViewCount = 300,
            CreatedAt = DateTime.UtcNow.AddDays(-2)
        };
        _db.Recipes.Add(r104);
        _db.RecipeIngredients.AddRange(
            new RecipeIngredient { RecipeId = 104, IngredientId = 101, QuantityNeeded = 100, Unit = "g" },
            new RecipeIngredient { RecipeId = 104, IngredientId = 102, QuantityNeeded = 50, Unit = "g" }
        );

        // Recipe 105 (VEGAN, Món canh, DRAFT - Chưa duyệt, không được xuất hiện)
        var rDraft = new Recipe
        {
            RecipeId = 105,
            UserId = 1,
            CategoryId = 1,
            DietarySchoolId = 7,
            Title = "Canh nháp chưa duyệt",
            Instructions = "[\"B1\"]",
            DifficultyLevel = "EASY",
            Source = "USER",
            Status = "DRAFT",
            CreatedAt = DateTime.UtcNow
        };
        _db.Recipes.Add(rDraft);

        // Recipe 106 (VEGAN, Thuộc danh mục ẩn - CategoryId = 99, IsActive = false)
        var rInactiveCat = new Recipe
        {
            RecipeId = 106,
            UserId = 1,
            CategoryId = 99,
            DietarySchoolId = 7,
            Title = "Canh trong danh mục bị ẩn",
            Instructions = "[\"B1\"]",
            DifficultyLevel = "EASY",
            Source = "USER",
            Status = "APPROVED_ACTIVE",
            CreatedAt = DateTime.UtcNow
        };
        _db.Recipes.Add(rInactiveCat);

        // Posts & Votes
        var post101 = new Post { PostId = 201, RecipeId = 101, Title = r101.Title, Content = "Content", UserId = 1, ModerationStatus = "APPROVED_ACTIVE", Visibility = "PUBLIC" };
        var post102 = new Post { PostId = 202, RecipeId = 102, Title = r102.Title, Content = "Content", UserId = 1, ModerationStatus = "APPROVED_ACTIVE", Visibility = "PUBLIC" };
        var post103 = new Post { PostId = 203, RecipeId = 103, Title = r103.Title, Content = "Content", UserId = 1, ModerationStatus = "APPROVED_ACTIVE", Visibility = "PUBLIC" };
        var post104 = new Post { PostId = 204, RecipeId = 104, Title = r104.Title, Content = "Content", UserId = 1, ModerationStatus = "APPROVED_ACTIVE", Visibility = "PUBLIC" };
        _db.Posts.AddRange(post101, post102, post103, post104);

        // Votes: 101 có 5 votes, 102 có 2 votes, 103 có 10 votes, 104 có 20 votes, 501 có 50 votes, 502 có 100 votes
        for (int i = 1; i <= 5; i++) _db.Votes.Add(new Vote { VoteId = 100 + i, PostId = 201, UserId = 1 });
        for (int i = 1; i <= 2; i++) _db.Votes.Add(new Vote { VoteId = 200 + i, PostId = 202, UserId = 1 });
        for (int i = 1; i <= 10; i++) _db.Votes.Add(new Vote { VoteId = 300 + i, PostId = 203, UserId = 1 });
        for (int i = 1; i <= 20; i++) _db.Votes.Add(new Vote { VoteId = 400 + i, PostId = 204, UserId = 1 });
        for (int i = 1; i <= 50; i++) _db.Votes.Add(new Vote { VoteId = 500 + i, PostId = 501, UserId = 1 });
        for (int i = 1; i <= 100; i++) _db.Votes.Add(new Vote { VoteId = 600 + i, PostId = 502, UserId = 1 });

        // Bài viết liên quan (keywords: "nấm", "rong", "biển")
        var relatedPostKeyword = new Post
        {
            PostId = 501,
            Title = "Bí quyết sơ chế nấm đông cô giòn ngọt",
            Content = "Nội dung bài viết nấm",
            UserId = 1,
            ModerationStatus = "APPROVED_ACTIVE",
            Visibility = "PUBLIC",
            CreatedAt = DateTime.UtcNow
        };
        var unrelatedPost = new Post
        {
            PostId = 502,
            Title = "Lợi ích ăn chuối mỗi ngày",
            Content = "Không liên quan",
            UserId = 1,
            ModerationStatus = "APPROVED_ACTIVE",
            Visibility = "PUBLIC",
            CreatedAt = DateTime.UtcNow
        };
        _db.Posts.AddRange(relatedPostKeyword, unrelatedPost);

        // Video liên quan (keywords: "rong", "biển")
        var videoPost = new Post
        {
            PostId = 601,
            Title = "Hướng dẫn nấu canh rong biển thanh lọc",
            Content = "Nội dung video canh rong biển",
            UserId = 1,
            ModerationStatus = "APPROVED_ACTIVE",
            Visibility = "PUBLIC"
        };
        _db.Posts.Add(videoPost);
        var video = new Video
        {
            VideoId = 701,
            PostId = 601,
            Title = "Video hướng dẫn canh rong biển",
            VideoUrl = "https://cdn.veggielife.vn/v701.mp4",
            Status = "READY",
            DurationSeconds = 90,
            ViewCount = 150
        };
        _db.Videos.Add(video);

        _db.SaveChanges();
    }

    [Fact]
    public async Task GetRelatedRecipesAsync_InvalidRecipeId_ThrowsBadRequest()
    {
        // Act: RecipeId <= 0
        var actZero = async () => await _service.GetRelatedRecipesAsync(0);
        var actNegative = async () => await _service.GetRelatedRecipesAsync(-5);

        // Assert
        await actZero.Should().ThrowAsync<AppException>()
            .Where(e => e.StatusCode == 400 && e.ErrorCode == ErrorCodes.ErrValidation);
        await actNegative.Should().ThrowAsync<AppException>()
            .Where(e => e.StatusCode == 400 && e.ErrorCode == ErrorCodes.ErrValidation);
    }

    [Fact]
    public async Task GetRelatedRecipesAsync_InvalidLimit_ThrowsBadRequest()
    {
        // Act: limit < 1 hoặc limit > 10
        var actZeroLimit = async () => await _service.GetRelatedRecipesAsync(100, limit: 0);
        var actOverLimit = async () => await _service.GetRelatedRecipesAsync(100, limit: 11);

        // Assert
        await actZeroLimit.Should().ThrowAsync<AppException>()
            .Where(e => e.StatusCode == 400 && e.ErrorCode == ErrorCodes.ErrValidation);
        await actOverLimit.Should().ThrowAsync<AppException>()
            .Where(e => e.StatusCode == 400 && e.ErrorCode == ErrorCodes.ErrValidation);
    }

    [Fact]
    public async Task GetRelatedRecipesAsync_NonExistentRecipe_ThrowsNotFound()
    {
        // Act: RecipeId không tồn tại
        var act = async () => await _service.GetRelatedRecipesAsync(9999);

        // Assert: 404 ErrContentNotFound
        await act.Should().ThrowAsync<AppException>()
            .Where(e => e.StatusCode == 404 && e.ErrorCode == ErrorCodes.ErrContentNotFound);
    }

    [Fact]
    public async Task GetRelatedRecipesAsync_RecipeNotApprovedActive_ThrowsNotFound()
    {
        // Act: Recipe 105 đang là DRAFT
        var act = async () => await _service.GetRelatedRecipesAsync(105);

        // Assert: 404 ErrContentNotFound
        await act.Should().ThrowAsync<AppException>()
            .Where(e => e.StatusCode == 404 && e.ErrorCode == ErrorCodes.ErrContentNotFound);
    }

    [Fact]
    public async Task GetRelatedRecipesAsync_ExcludesCurrentRecipeFromSuggestions()
    {
        // Act
        var result = await _service.GetRelatedRecipesAsync(100, limit: 6);

        // Assert: RecipeId 100 tuyệt đối không nằm trong danh sách gợi ý
        result.Recipes.Should().NotContain(r => r.RecipeId == 100);
    }

    [Fact]
    public async Task GetRelatedRecipesAsync_ExcludesInactiveCategoryAndDraftRecipes()
    {
        // Act
        var result = await _service.GetRelatedRecipesAsync(100, limit: 6);

        // Assert: Recipe 105 (DRAFT) và Recipe 106 (Category inactive) không được xuất hiện
        result.Recipes.Should().NotContain(r => r.RecipeId == 105);
        result.Recipes.Should().NotContain(r => r.RecipeId == 106);
    }

    [Fact]
    public async Task GetRelatedRecipesAsync_FallbackToRecipeDietarySchool_WhenUserNotLoggedIn()
    {
        // Act: currentUserId = null -> lấy theo trường phái VEGAN (7) của recipe 100
        var result = await _service.GetRelatedRecipesAsync(100, limit: 6, currentUserId: null);

        // Assert: Chỉ trả về recipe thuần chay VEGAN (101, 102, 103), không chứa r104 (BUDDHIST)
        result.Recipes.Should().OnlyContain(r => r.DietarySchool == "VEGAN");
        result.Recipes.Should().NotContain(r => r.RecipeId == 104);
    }

    [Fact]
    public async Task GetRelatedRecipesAsync_UsesUserProfileDietarySchool_WhenUserLoggedInWithProfile()
    {
        // Act: User 2 có HealthProfile với DietarySchoolId = 1 (BUDDHIST)
        var result = await _service.GetRelatedRecipesAsync(100, limit: 6, currentUserId: 2);

        // Assert: Gợi ý theo trường phái BUDDHIST của user 2, kết quả chứa Recipe 104
        result.Recipes.Should().Contain(r => r.RecipeId == 104);
        result.Recipes.Should().NotContain(r => r.RecipeId == 101);
    }

    [Fact]
    public async Task GetRelatedRecipesAsync_FallbackToRecipeSchool_WhenLoggedInUserHasNoHealthProfile()
    {
        // Act: User 1 đăng nhập nhưng chưa có HealthProfile
        var result = await _service.GetRelatedRecipesAsync(100, limit: 6, currentUserId: 1);

        // Assert: Fallback sang VEGAN (7) của Recipe 100
        result.Recipes.Should().OnlyContain(r => r.DietarySchool == "VEGAN");
        result.Recipes.Should().NotContain(r => r.RecipeId == 104);
    }

    [Fact]
    public async Task GetRelatedRecipesAsync_RankingOrder_SharedIngredientsThenCategoryThenVotes()
    {
        // Act: Recipe 100 có nguyên liệu: tofu (101), mushroom (102), seaweed (103)
        // Candidate:
        // - r101: 2 nguyên liệu chung (101, 102), cùng danh mục (Canh), 5 votes
        // - r102: 2 nguyên liệu chung (101, 102), khác danh mục (Xào), 2 votes
        // - r103: 1 nguyên liệu chung (103), cùng danh mục (Canh), 10 votes
        var result = await _service.GetRelatedRecipesAsync(100, limit: 6);

        // Assert theo BR-USR-16:
        // Thứ hạng 1: r101 (2 NL chung, cùng danh mục)
        // Thứ hạng 2: r102 (2 NL chung, khác danh mục)
        // Thứ hạng 3: r103 (1 NL chung, cùng danh mục)
        result.Recipes.Should().HaveCount(3);
        result.Recipes[0].RecipeId.Should().Be(101);
        result.Recipes[1].RecipeId.Should().Be(102);
        result.Recipes[2].RecipeId.Should().Be(103);
    }

    [Fact]
    public async Task GetRelatedRecipesAsync_LimitParameter_RestrictsCount()
    {
        // Act: Chỉ lấy 2 công thức
        var result = await _service.GetRelatedRecipesAsync(100, limit: 2);

        // Assert: Giới hạn đúng 2 phần tử theo thứ tự ưu tiên
        result.Recipes.Should().HaveCount(2);
        result.Recipes[0].RecipeId.Should().Be(101);
        result.Recipes[1].RecipeId.Should().Be(102);
    }

    [Fact]
    public async Task GetRelatedRecipesAsync_ReturnsRelatedPostsAndVideos_ByTitleKeywords()
    {
        // Act: Tiêu đề recipe 100 là "Canh nấm rong biển hạt sen" -> từ khóa: nấm, rong, biển
        var result = await _service.GetRelatedRecipesAsync(100, limit: 6);

        // Assert bài viết liên quan (tối đa 3)
        result.Posts.Should().NotBeEmpty();
        result.Posts.Should().Contain(p => p.PostId == 501); // "sơ chế nấm đông cô"
        result.Posts.Should().NotContain(p => p.PostId == 502); // "ăn chuối" (không khớp)

        // Assert video liên quan (tối đa 3)
        result.Videos.Should().NotBeEmpty();
        result.Videos.Should().Contain(v => v.VideoId == 701); // Video canh rong biển
    }

    [Fact]
    public async Task GetRelatedRecipesAsync_RedisCacheHit_ReturnsCachedDataWithoutDbQuery()
    {
        // Arrange
        var cachedData = new RelatedRecipesResponseData
        {
            Recipes = new List<RecipeSummaryDto>
            {
                new() { RecipeId = 888, Title = "Công thức từ cache Redis", DietarySchool = "VEGAN" }
            },
            Posts = new List<RelatedPostDto>(),
            Videos = new List<RelatedVideoDto>()
        };

        _mockRedis.Setup(r => r.GetAsync<RelatedRecipesResponseData>("recipe_related:100:lim_6", It.IsAny<CancellationToken>()))
            .ReturnsAsync(cachedData);

        // Act
        var result = await _service.GetRelatedRecipesAsync(100, limit: 6);

        // Assert
        result.Should().BeSameAs(cachedData);
        result.Recipes.Should().ContainSingle(r => r.RecipeId == 888);
        _mockRedis.Verify(r => r.GetAsync<RelatedRecipesResponseData>("recipe_related:100:lim_6", It.IsAny<CancellationToken>()), Times.Once);
        // Không gọi SetAsync khi cache hit
        _mockRedis.Verify(r => r.SetAsync(It.IsAny<string>(), It.IsAny<RelatedRecipesResponseData>(), It.IsAny<TimeSpan?>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetRelatedRecipesAsync_RedisCacheMiss_SetsCacheWith24HoursTtl()
    {
        // Arrange: Cache miss (trả về null)
        _mockRedis.Setup(r => r.GetAsync<RelatedRecipesResponseData>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((RelatedRecipesResponseData?)null);

        // Act
        var result = await _service.GetRelatedRecipesAsync(100, limit: 6);

        // Assert: Đã gọi SetAsync với key đúng và TTL 24 giờ (BR-SYS-CACHE-01)
        result.Should().NotBeNull();
        _mockRedis.Verify(r => r.SetAsync(
            "recipe_related:100:lim_6",
            It.Is<RelatedRecipesResponseData>(data => data.Recipes.Count == 3),
            TimeSpan.FromHours(24),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetRelatedRecipesAsync_RedisCacheMiss_WithUserSchool_UsesPersonalizedCacheKey()
    {
        // Arrange: User 2 có trường phái BUDDHIST (Id = 1)
        _mockRedis.Setup(r => r.GetAsync<RelatedRecipesResponseData>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((RelatedRecipesResponseData?)null);

        // Act
        var result = await _service.GetRelatedRecipesAsync(100, limit: 4, currentUserId: 2);

        // Assert: Cache key chứa trường phái của user
        var expectedKey = "recipe_related:100:ds_1:lim_4";
        _mockRedis.Verify(r => r.SetAsync(
            expectedKey,
            It.IsAny<RelatedRecipesResponseData>(),
            TimeSpan.FromHours(24),
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
