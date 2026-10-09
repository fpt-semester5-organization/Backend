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
using VeggieLife.BLL.DTOs.Recipes.Requests;
using VeggieLife.BLL.DTOs.Recipes.Responses;
using VeggieLife.BLL.Services.Cache;
using VeggieLife.BLL.Services.Recipes;
using VeggieLife.DAL.Context;
using VeggieLife.DAL.Entities;
using Xunit;

namespace VeggieLife.Tests.Unit.Services.Recipes;

/// <summary>
/// Bộ kiểm thử đơn vị toàn diện cho RecipeService (FN-POST-01, FN-POST-02).
/// Kiểm tra tìm kiếm, lọc theo trường phái/danh mục/nguyên liệu/thời gian, phân trang, signUpPromptAtItem,
/// xem chi tiết công thức, vòng đời phê duyệt APPROVED_ACTIVE, và Redis cache.
/// </summary>
public class RecipeServiceTests : IDisposable
{
    private readonly AppDbContext _db;
    private readonly Mock<IRedisService> _mockRedis;
    private readonly Mock<ILogger<RecipeService>> _mockLogger;
    private readonly RecipeService _service;

    public RecipeServiceTests()
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
        // 1. Trường phái chay (DietarySchools)
        var veganSchool = new DietarySchool
        {
            DietarySchoolId = 7,
            SchoolCode = "VEGAN",
            SchoolName = "Thuần chay (Vegan)",
            ForbiddenFoodRules = "{\"forbiddenFoodGroups\":[\"EGG\",\"DAIRY\",\"POULTRY\",\"SEAFOOD\"]}"
        };
        var buddhistSchool = new DietarySchool
        {
            DietarySchoolId = 1,
            SchoolCode = "BUDDHIST",
            SchoolName = "Chay Phật giáo",
            ForbiddenFoodRules = "{\"forbiddenFoodGroups\":[\"EGG\",\"DAIRY\",\"POULTRY\",\"SEAFOOD\"],\"forbidAllium\":true}"
        };
        _db.DietarySchools.AddRange(veganSchool, buddhistSchool);

        // 2. Danh mục món ăn (FoodCategories)
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
            CategoryId = 3,
            CategoryName = "Danh mục ẩn",
            IsActive = false
        };
        _db.FoodCategories.AddRange(soupCategory, stirFryCategory, inactiveCategory);

        // 3. Nguyên liệu thực phẩm (Ingredients)
        var tofu = new Ingredient
        {
            IngredientId = 101,
            IngredientName = "Đậu phụ non",
            StandardUnit = "g",
            DefaultStallType = "PLANT_PROTEIN",
            CaloriesPer100g = 76.0m,
            FoodGroup = "PLANT"
        };
        var mushroom = new Ingredient
        {
            IngredientId = 102,
            IngredientName = "Nấm đông cô",
            StandardUnit = "g",
            DefaultStallType = "PRODUCE",
            CaloriesPer100g = 34.0m,
            FoodGroup = "PLANT"
        };
        var carrot = new Ingredient
        {
            IngredientId = 103,
            IngredientName = "Cà rốt",
            StandardUnit = "g",
            DefaultStallType = "PRODUCE",
            CaloriesPer100g = 41.0m,
            FoodGroup = "PLANT"
        };
        _db.Ingredients.AddRange(tofu, mushroom, carrot);

        // 4. Người dùng mẫu
        var author = new User
        {
            UserId = 1,
            Email = "chef@veggielife.vn",
            FullName = "Đầu bếp chay",
            Role = "MEMBER",
            AccountStatus = "ACTIVE"
        };
        _db.Users.Add(author);

        // 5. Công thức 1: Đã duyệt APPROVED_ACTIVE, Món canh, Vegan, Nấu 25 phút, Có nguyên liệu Đậu phụ non & Nấm đông cô
        var recipe1 = new Recipe
        {
            RecipeId = 501,
            UserId = 1,
            CategoryId = 1,
            DietarySchoolId = 7,
            Title = "Canh nấm đậu phụ rong biển",
            Description = "Món canh thanh đạm bổ dưỡng",
            Instructions = "[\"Bước 1: Ngâm nấm và cắt đậu.\", \"Bước 2: Nấu nước dùng và cho đậu vào sôi 5 phút.\"]",
            PrepTimeMinutes = 10,
            CookTimeMinutes = 25,
            Servings = 2,
            CaloriesKcal = 310.0m,
            DifficultyLevel = "EASY",
            IsVerified = true,
            ImageUrl = "https://img.veggielife.vn/r501.webp",
            Status = "APPROVED_ACTIVE",
            ViewCount = 100,
            Source = "USER",
            CreatedAt = DateTime.UtcNow.AddDays(-2)
        };

        // 6. Công thức 2: Đã duyệt APPROVED_ACTIVE, Món xào, Buddhist, Nấu 15 phút, Có nguyên liệu Cà rốt & Nấm đông cô, Có Video READY
        var recipe2 = new Recipe
        {
            RecipeId = 502,
            UserId = 1,
            CategoryId = 2,
            DietarySchoolId = 1,
            Title = "Nấm xào thập cẩm thanh tịnh",
            Description = "Nấm xào ngũ sắc hấp dẫn",
            Instructions = "Bước 1: Rửa sạch rau củ.\nBước 2: Xào chín đều với dầu mè.",
            PrepTimeMinutes = 10,
            CookTimeMinutes = 15,
            Servings = 4,
            CaloriesKcal = 180.0m,
            DifficultyLevel = "MEDIUM",
            IsVerified = false,
            ImageUrl = "https://img.veggielife.vn/r502.webp",
            Status = "APPROVED_ACTIVE",
            ViewCount = 200,
            Source = "USER",
            CreatedAt = DateTime.UtcNow.AddDays(-1)
        };

        // 7. Công thức 3: Chưa duyệt DRAFT (không được trả ra ngoài)
        var recipeDraft = new Recipe
        {
            RecipeId = 503,
            UserId = 1,
            CategoryId = 1,
            DietarySchoolId = 7,
            Title = "Canh bí đỏ nháp",
            Description = "Bản nháp",
            Instructions = "Đang viết",
            PrepTimeMinutes = 5,
            CookTimeMinutes = 20,
            Servings = 2,
            CaloriesKcal = 200m,
            DifficultyLevel = "EASY",
            IsVerified = false,
            Status = "DRAFT",
            ViewCount = 0,
            Source = "USER",
            CreatedAt = DateTime.UtcNow
        };

        // 8. Công thức 4: Bị cách ly QUARANTINED_HIDDEN (không được trả ra ngoài)
        var recipeQuarantined = new Recipe
        {
            RecipeId = 504,
            UserId = 1,
            CategoryId = 1,
            DietarySchoolId = 7,
            Title = "Công thức nghi vấn vi phạm",
            Description = "Cách ly",
            Instructions = "Cách ly",
            PrepTimeMinutes = 5,
            CookTimeMinutes = 20,
            Servings = 2,
            CaloriesKcal = 200m,
            DifficultyLevel = "EASY",
            IsVerified = false,
            Status = "QUARANTINED_HIDDEN",
            ViewCount = 0,
            Source = "USER",
            CreatedAt = DateTime.UtcNow
        };

        _db.Recipes.AddRange(recipe1, recipe2, recipeDraft, recipeQuarantined);

        // RecipeIngredients
        _db.RecipeIngredients.AddRange(
            new RecipeIngredient { RecipeId = 501, IngredientId = 101, QuantityNeeded = 200, Unit = "g" },
            new RecipeIngredient { RecipeId = 501, IngredientId = 102, QuantityNeeded = 100, Unit = "g" },
            new RecipeIngredient { RecipeId = 502, IngredientId = 102, QuantityNeeded = 150, Unit = "g" },
            new RecipeIngredient { RecipeId = 502, IngredientId = 103, QuantityNeeded = 100, Unit = "g" }
        );

        // Post đại diện cho Recipe 501 (có 5 vote, 2 comment)
        var post1 = new Post
        {
            PostId = 1001,
            UserId = 1,
            RecipeId = 501,
            Title = recipe1.Title,
            Content = "Bài đăng canh nấm",
            Visibility = "PUBLIC",
            ModerationStatus = "APPROVED_ACTIVE",
            CreatedAt = DateTime.UtcNow.AddDays(-2)
        };
        _db.Posts.Add(post1);

        for (int i = 1; i <= 5; i++)
        {
            _db.Votes.Add(new Vote { VoteId = i, PostId = 1001, UserId = 1, CreatedAt = DateTime.UtcNow });
        }
        _db.Comments.AddRange(
            new Comment { CommentId = 1, PostId = 1001, UserId = 1, Content = "Món ngon", Status = "APPROVED_ACTIVE", CreatedAt = DateTime.UtcNow },
            new Comment { CommentId = 2, PostId = 1001, UserId = 1, Content = "Dễ nấu", Status = "APPROVED_ACTIVE", CreatedAt = DateTime.UtcNow }
        );

        // Post đại diện cho Recipe 502 (có Video READY và transcript COMPLETED, 2 vote)
        var post2 = new Post
        {
            PostId = 1002,
            UserId = 1,
            RecipeId = 502,
            Title = recipe2.Title,
            Content = "Bài đăng nấm xào kèm video",
            Visibility = "PUBLIC",
            ModerationStatus = "APPROVED_ACTIVE",
            CreatedAt = DateTime.UtcNow.AddDays(-1)
        };
        _db.Posts.Add(post2);

        var video2 = new Video
        {
            VideoId = 2001,
            PostId = 1002,
            Title = "Video hướng dẫn nấm xào",
            VideoUrl = "https://video.veggielife.vn/v2001.mp4",
            DurationSeconds = 120,
            Status = "READY",
            CreatedAt = DateTime.UtcNow.AddDays(-1)
        };
        _db.Videos.Add(video2);

        var transcript2 = new VideoTranscript
        {
            TranscriptId = 3001,
            VideoId = 2001,
            RecipeId = 502,
            RawTranscript = "Hôm nay chúng ta làm nấm xào...",
            Status = "COMPLETED",
            CreatedAt = DateTime.UtcNow.AddDays(-1)
        };
        _db.VideoTranscripts.Add(transcript2);

        _db.Votes.AddRange(
            new Vote { VoteId = 10, PostId = 1002, UserId = 1, CreatedAt = DateTime.UtcNow },
            new Vote { VoteId = 11, PostId = 1002, UserId = 1, CreatedAt = DateTime.UtcNow }
        );

        _db.SaveChanges();
    }

    [Fact]
    public async Task ListRecipesAsync_DefaultFilter_OnlyReturnsApprovedActiveRecipes()
    {
        // Act
        var filter = new RecipeFilterRequest();
        var (items, meta) = await _service.ListRecipesAsync(filter, isGuest: false, guestId: null);

        // Assert: Chỉ có Recipe 501 và 502 được trả về (DRAFT và QUARANTINED bị loại)
        items.Should().HaveCount(2);
        items.Select(x => x.RecipeId).Should().Contain(new[] { 501, 502 });
        meta.Total.Should().Be(2);
        meta.SignUpPromptAtItem.Should().BeNull();
    }

    [Fact]
    public async Task ListRecipesAsync_FilterByKeyword_ReturnsMatchingRecipes()
    {
        // Act
        var filter = new RecipeFilterRequest { Keyword = "rong biển" };
        var (items, meta) = await _service.ListRecipesAsync(filter, isGuest: false, guestId: null);

        // Assert
        items.Should().HaveCount(1);
        items[0].RecipeId.Should().Be(501);
        items[0].Title.Should().Be("Canh nấm đậu phụ rong biển");
    }

    [Fact]
    public async Task ListRecipesAsync_FilterByDietarySchool_ReturnsCorrectRecipes()
    {
        // Act: Lọc trường phái Vegan (ID 7)
        var filter = new RecipeFilterRequest { DietarySchoolId = 7 };
        var (items, meta) = await _service.ListRecipesAsync(filter, isGuest: false, guestId: null);

        // Assert
        items.Should().HaveCount(1);
        items[0].RecipeId.Should().Be(501);
        items[0].DietarySchool.Should().Be("VEGAN");
    }

    [Fact]
    public async Task ListRecipesAsync_FilterByCategory_ReturnsCorrectRecipes()
    {
        // Act: Lọc Món xào (ID 2)
        var filter = new RecipeFilterRequest { CategoryId = 2 };
        var (items, meta) = await _service.ListRecipesAsync(filter, isGuest: false, guestId: null);

        // Assert
        items.Should().HaveCount(1);
        items[0].RecipeId.Should().Be(502);
    }

    [Fact]
    public async Task ListRecipesAsync_FilterByIngredient_ReturnsMatchingRecipes()
    {
        // Act: Lọc nguyên liệu "Đậu phụ"
        var filter = new RecipeFilterRequest { Ingredient = "Đậu phụ" };
        var (items, meta) = await _service.ListRecipesAsync(filter, isGuest: false, guestId: null);

        // Assert
        items.Should().HaveCount(1);
        items[0].RecipeId.Should().Be(501);
    }

    [Fact]
    public async Task ListRecipesAsync_FilterByMaxCookTime_ReturnsWithinTimeLimit()
    {
        // Act: Lọc maxCookTime = 20 phút (chỉ có recipe 502 mất 15 phút thỏa mãn)
        var filter = new RecipeFilterRequest { MaxCookTime = 20 };
        var (items, meta) = await _service.ListRecipesAsync(filter, isGuest: false, guestId: null);

        // Assert
        items.Should().HaveCount(1);
        items[0].RecipeId.Should().Be(502);
        items[0].CookTimeMinutes.Should().Be(15);
    }

    [Fact]
    public async Task ListRecipesAsync_FilterByContentType_VideoOnlyReturnsVideos()
    {
        // Act: Lọc contentType = VIDEO
        var filter = new RecipeFilterRequest { ContentType = "VIDEO" };
        var (items, meta) = await _service.ListRecipesAsync(filter, isGuest: false, guestId: null);

        // Assert
        items.Should().HaveCount(1);
        items[0].RecipeId.Should().Be(502);
        items[0].Type.Should().Be("VIDEO");
    }

    [Fact]
    public async Task ListRecipesAsync_GuestUser_SetsSignUpPromptAtItem5()
    {
        // Act: Khách vãng lai gọi
        var filter = new RecipeFilterRequest();
        var (items, meta) = await _service.ListRecipesAsync(filter, isGuest: true, guestId: "guest-test-uuid");

        // Assert: BR-GUEST-01
        meta.SignUpPromptAtItem.Should().Be(5);
    }

    [Fact]
    public async Task ListRecipesAsync_GuestKeyword_SavesHistoryToRedis()
    {
        // Act
        var filter = new RecipeFilterRequest { Keyword = "canh nấm" };
        await _service.ListRecipesAsync(filter, isGuest: true, guestId: "guest-123");

        // Assert: BR-GUEST-AUTH-01
        _mockRedis.Verify(r => r.SetAsync(
            "guest_search_history:guest-123",
            It.Is<List<string>>(list => list.Contains("canh nấm")),
            It.IsAny<TimeSpan?>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ListRecipesAsync_SortPopular_OrdersByVoteCountDescending()
    {
        // Act: Sắp xếp theo POPULAR (Recipe 501 có 5 votes, Recipe 502 có 2 votes)
        var filter = new RecipeFilterRequest { Sort = "POPULAR" };
        var (items, meta) = await _service.ListRecipesAsync(filter, isGuest: false, guestId: null);

        // Assert
        items.Should().HaveCount(2);
        items[0].RecipeId.Should().Be(501);
        items[0].VoteCount.Should().Be(5);
        items[1].RecipeId.Should().Be(502);
        items[1].VoteCount.Should().Be(2);
    }

    [Fact]
    public async Task ListRecipesAsync_SortNewest_OrdersByCreatedAtDescending()
    {
        // Act: Sắp xếp theo NEWEST (Recipe 502 tạo gần hơn Recipe 501)
        var filter = new RecipeFilterRequest { Sort = "NEWEST" };
        var (items, meta) = await _service.ListRecipesAsync(filter, isGuest: false, guestId: null);

        // Assert
        items.Should().HaveCount(2);
        items[0].RecipeId.Should().Be(502);
        items[1].RecipeId.Should().Be(501);
    }

    [Theory]
    [InlineData(0, 10)]   // Page < 1
    [InlineData(1, 0)]    // Size < 1
    [InlineData(1, 25)]   // Size > 20
    public async Task ListRecipesAsync_InvalidPaging_ThrowsAppExceptionBadRequest(int page, int size)
    {
        var filter = new RecipeFilterRequest { Page = page, Size = size };
        var act = async () => await _service.ListRecipesAsync(filter, false, null);

        await act.Should().ThrowAsync<AppException>()
            .Where(e => e.StatusCode == 400 && e.ErrorCode == ErrorCodes.ErrValidation);
    }

    [Theory]
    [InlineData("INVALID_TYPE")]
    [InlineData("UNKNOWN")]
    public async Task ListRecipesAsync_InvalidContentType_ThrowsAppExceptionBadRequest(string contentType)
    {
        var filter = new RecipeFilterRequest { ContentType = contentType };
        var act = async () => await _service.ListRecipesAsync(filter, false, null);

        await act.Should().ThrowAsync<AppException>()
            .Where(e => e.StatusCode == 400 && e.ErrorCode == ErrorCodes.ErrValidation);
    }

    [Fact]
    public async Task GetRecipeByIdAsync_ApprovedRecipe_ReturnsFullDetails()
    {
        // Act
        var result = await _service.GetRecipeByIdAsync(501, isGuest: false, currentUserId: 1);

        // Assert
        result.Should().NotBeNull();
        result.RecipeId.Should().Be(501);
        result.Title.Should().Be("Canh nấm đậu phụ rong biển");
        result.DietarySchool.Should().Be("VEGAN");
        result.CookTimeMinutes.Should().Be(25);
        result.CaloriesKcal.Should().Be(310.0m);
        result.VoteCount.Should().Be(5);
        result.CommentCount.Should().Be(2);
        result.Instructions.Should().HaveCount(2);
        result.Instructions[0].Should().Contain("Bước 1");
        result.Ingredients.Should().HaveCount(2);
        result.Ingredients.First().Name.Should().Be("Đậu phụ non");
        result.Ingredients.First().Quantity.Should().Be(200);
        result.Ingredients.First().Unit.Should().Be("g");

        // Member actions
        result.MemberActions.CanVote.Should().BeTrue();
        result.MemberActions.CanComment.Should().BeTrue();
        result.MemberActions.CanReport.Should().BeTrue();
    }

    [Fact]
    public async Task GetRecipeByIdAsync_GuestUser_MemberActionsAllFalse()
    {
        // Act
        var result = await _service.GetRecipeByIdAsync(501, isGuest: true, currentUserId: null);

        // Assert: BR-AUTH-03
        result.MemberActions.CanVote.Should().BeFalse();
        result.MemberActions.CanComment.Should().BeFalse();
        result.MemberActions.CanReport.Should().BeFalse();
    }

    [Fact]
    public async Task GetRecipeByIdAsync_VideoRecipeWithTranscript_HasTranscriptIsTrue()
    {
        // Act
        var result = await _service.GetRecipeByIdAsync(502, isGuest: false);

        // Assert
        result.Type.Should().Be("VIDEO");
        result.HasTranscript.Should().BeTrue();
    }

    [Fact]
    public async Task GetRecipeByIdAsync_UnapprovedRecipe_GuestOrOtherUser_ThrowsNotFound()
    {
        // Act: Recipe 503 là DRAFT, user khác (ID 99) xem
        var act = async () => await _service.GetRecipeByIdAsync(503, isGuest: false, currentUserId: 99);

        // Assert: BR-ADM-CNT-001 -> 404 ERR_CONTENT_NOT_FOUND
        await act.Should().ThrowAsync<AppException>()
            .Where(e => e.StatusCode == 404 && e.ErrorCode == ErrorCodes.ErrContentNotFound);
    }

    [Fact]
    public async Task GetRecipeByIdAsync_InvalidId_ThrowsBadRequest()
    {
        // Act: ID <= 0
        var act = async () => await _service.GetRecipeByIdAsync(0, false);

        await act.Should().ThrowAsync<AppException>()
            .Where(e => e.StatusCode == 400 && e.ErrorCode == ErrorCodes.ErrValidation);
    }

    [Fact]
    public async Task GetRecipeByIdAsync_IncrementsViewCount()
    {
        // Arrange
        var initialViews = (await _db.Recipes.FindAsync(501))!.ViewCount;

        // Act
        await _service.GetRecipeByIdAsync(501, false);

        // Assert
        var updatedRecipe = await _db.Recipes.FindAsync(501);
        updatedRecipe!.ViewCount.Should().Be(initialViews + 1);

        _mockRedis.Verify(r => r.IncrementAsync("view_counter:recipe:501", 1, It.IsAny<TimeSpan?>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}
