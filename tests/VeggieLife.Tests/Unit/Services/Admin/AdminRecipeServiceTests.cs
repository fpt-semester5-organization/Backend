using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using VeggieLife.BLL.Common.Constants;
using VeggieLife.BLL.Common.Exceptions;
using VeggieLife.BLL.DTOs.Admin.Recipes;
using VeggieLife.BLL.Services.Admin;
using VeggieLife.BLL.Services.Cache;
using VeggieLife.DAL.Context;
using VeggieLife.DAL.Entities;
using Xunit;

namespace VeggieLife.Tests.Unit.Services.Admin;

public class AdminRecipeServiceTests
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    private readonly AppDbContext _db;
    private readonly Mock<IRedisService> _mockRedis = new();
    private readonly AdminRecipeService _service;
    private const int AdminId = 999;

    public AdminRecipeServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase("AdminRecipeServiceTestsDb_" + Guid.NewGuid())
            .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        _db = new AppDbContext(options);
        _service = new AdminRecipeService(_db, _mockRedis.Object, NullLogger<AdminRecipeService>.Instance);

        SeedTestData();
    }

    private void SeedTestData()
    {
        // 1. Dietary Schools
        _db.DietarySchools.AddRange(
            new DietarySchool
            {
                DietarySchoolId = 7,
                SchoolCode = "VEGAN",
                SchoolName = "Thuần chay (Vegan)",
                ForbiddenFoodRules = "{\"forbiddenFoodGroups\":[\"EGG\",\"DAIRY\",\"POULTRY\",\"SEAFOOD\"]}"
            },
            new DietarySchool
            {
                DietarySchoolId = 1,
                SchoolCode = "BUDDHIST",
                SchoolName = "Chay Phật giáo",
                ForbiddenFoodRules = "{\"forbiddenFoodGroups\":[\"EGG\",\"DAIRY\",\"POULTRY\",\"SEAFOOD\"],\"forbidAllium\":true}"
            }
        );

        // 2. Ingredients
        _db.Ingredients.AddRange(
            new Ingredient { IngredientId = 1, IngredientName = "Đậu phụ non", FoodGroup = "PLANT", StandardUnit = "g", CaloriesPer100g = 76m, DefaultStallType = "PLANT_PROTEIN" },
            new Ingredient { IngredientId = 2, IngredientName = "Nấm hương", FoodGroup = "PLANT", StandardUnit = "g", CaloriesPer100g = 34m, DefaultStallType = "PRODUCE" },
            new Ingredient { IngredientId = 3, IngredientName = "Trứng gà", FoodGroup = "EGG", StandardUnit = "quả", CaloriesPer100g = 155m, DefaultStallType = "DAIRY_EGG" },
            new Ingredient { IngredientId = 4, IngredientName = "Tỏi tây", FoodGroup = "ALLIUM", IsAllium = true, StandardUnit = "g", CaloriesPer100g = 61m, DefaultStallType = "PRODUCE" }
        );

        // 3. Categories
        _db.FoodCategories.Add(new FoodCategory { CategoryId = 1, CategoryName = "Món xào", IsActive = true });

        // 4. Eligible Recipe (passes all 4 criteria)
        var eligibleRecipe = new Recipe
        {
            RecipeId = 501,
            CategoryId = 1,
            DietarySchoolId = 7,
            Title = "Đậu phụ sốt nấm thuần chay chuẩn",
            Instructions = "[\"Bước 1: Sơ chế đậu phụ và nấm\", \"Bước 2: Xào chín và nêm nếm gia vị vừa ăn\"]",
            CookTimeMinutes = 15,
            PrepTimeMinutes = 10,
            Servings = 2,
            CaloriesKcal = 150m,
            DifficultyLevel = "EASY",
            Source = "USER",
            Status = "APPROVED_ACTIVE",
            IsVerified = false,
            CreatedAt = DateTime.UtcNow
        };
        _db.Recipes.Add(eligibleRecipe);

        _db.RecipeIngredients.AddRange(
            new RecipeIngredient { RecipeId = 501, IngredientId = 1, QuantityNeeded = 200, Unit = "g" },
            new RecipeIngredient { RecipeId = 501, IngredientId = 2, QuantityNeeded = 100, Unit = "g" }
        );

        // Associate with post and 10 votes
        var post501 = new Post
        {
            PostId = 501,
            RecipeId = 501,
            UserId = 10,
            Title = "Post 501",
            Content = "Content 501",
            ModerationStatus = "APPROVED_ACTIVE",
            Visibility = "PUBLIC"
        };
        _db.Posts.Add(post501);

        for (int i = 1; i <= 10; i++)
        {
            _db.Votes.Add(new Vote { VoteId = 100 + i, PostId = 501, UserId = 200 + i, CreatedAt = DateTime.UtcNow });
        }

        _db.SaveChanges();
    }

    [Fact]
    public async Task VerifyRecipe_InvalidId_ThrowsBadRequestValidation()
    {
        var act = () => _service.VerifyRecipeAsync(0, null, AdminId);
        var ex = await act.Should().ThrowAsync<AppException>();
        ex.Which.StatusCode.Should().Be(400);
        ex.Which.ErrorCode.Should().Be(ErrorCodes.ErrValidation);
    }

    [Fact]
    public async Task VerifyRecipe_NoteTooLong_ThrowsBadRequestValidation()
    {
        var act = () => _service.VerifyRecipeAsync(501, new VerifyAdminRecipeRequest { Note = new string('A', 501) }, AdminId);
        var ex = await act.Should().ThrowAsync<AppException>();
        ex.Which.StatusCode.Should().Be(400);
        ex.Which.ErrorCode.Should().Be(ErrorCodes.ErrValidation);
    }

    [Fact]
    public async Task VerifyRecipe_NotFound_ThrowsNotFoundAdmNotFound()
    {
        var act = () => _service.VerifyRecipeAsync(9999, null, AdminId);
        var ex = await act.Should().ThrowAsync<AppException>();
        ex.Which.StatusCode.Should().Be(404);
        ex.Which.ErrorCode.Should().Be(ErrorCodes.ErrAdmNotFound);
    }

    [Fact]
    public async Task VerifyRecipe_Criterion1Failed_MissingStepsOrIngredientsOrCookTime_ThrowsUnprocessableEntity()
    {
        // Create recipe with 0 cook time and 1 step
        var badRecipe = new Recipe
        {
            RecipeId = 502,
            CategoryId = 1,
            DietarySchoolId = 7,
            Title = "Món thiếu bước và thời gian",
            Instructions = "[\"Chỉ có 1 bước duy nhất\"]",
            CookTimeMinutes = 0,
            Status = "APPROVED_ACTIVE",
            Source = "USER",
            DifficultyLevel = "EASY"
        };
        _db.Recipes.Add(badRecipe);
        var badPost = new Post { PostId = 502, RecipeId = 502, UserId = 1, Title = "P", Content = "C", ModerationStatus = "APPROVED_ACTIVE", Visibility = "PUBLIC" };
        _db.Posts.Add(badPost);
        for (int i = 1; i <= 10; i++)
        {
            _db.Votes.Add(new Vote { VoteId = 200 + i, PostId = 502, UserId = 300 + i, CreatedAt = DateTime.UtcNow });
        }
        await _db.SaveChangesAsync();

        var act = () => _service.VerifyRecipeAsync(502, null, AdminId);
        var ex = await act.Should().ThrowAsync<AppException>();
        ex.Which.StatusCode.Should().Be(422);
        ex.Which.ErrorCode.Should().Be(ErrorCodes.ErrRecipeNotEligible);

        // Check details contains Criterion 1 message
        var detailsJson = JsonSerializer.Serialize(ex.Which.Details, JsonOptions);
        detailsJson.Should().Contain("Chưa cấu hình đầy đủ định lượng gia vị");
    }

    [Fact]
    public async Task VerifyRecipe_Criterion2Failed_StatusNotApprovedActive_ThrowsUnprocessableEntity()
    {
        // Clone 501 with Status = DRAFT
        var draftRecipe = new Recipe
        {
            RecipeId = 503,
            CategoryId = 1,
            DietarySchoolId = 7,
            Title = "Công thức đang ở dạng nháp",
            Instructions = "[\"B1\", \"B2\"]",
            CookTimeMinutes = 20,
            Status = "DRAFT",
            Source = "USER",
            DifficultyLevel = "EASY"
        };
        _db.Recipes.Add(draftRecipe);
        _db.RecipeIngredients.Add(new RecipeIngredient { RecipeId = 503, IngredientId = 1, QuantityNeeded = 100, Unit = "g" });
        var post = new Post { PostId = 503, RecipeId = 503, UserId = 1, Title = "P", Content = "C", ModerationStatus = "APPROVED_ACTIVE", Visibility = "PUBLIC" };
        _db.Posts.Add(post);
        for (int i = 1; i <= 10; i++)
        {
            _db.Votes.Add(new Vote { VoteId = 300 + i, PostId = 503, UserId = 400 + i, CreatedAt = DateTime.UtcNow });
        }
        await _db.SaveChangesAsync();

        var act = () => _service.VerifyRecipeAsync(503, null, AdminId);
        var ex = await act.Should().ThrowAsync<AppException>();
        ex.Which.StatusCode.Should().Be(422);
        ex.Which.ErrorCode.Should().Be(ErrorCodes.ErrRecipeNotEligible);

        var detailsJson = JsonSerializer.Serialize(ex.Which.Details, JsonOptions);
        detailsJson.Should().Contain("Chưa qua kiểm duyệt phê duyệt");
    }

    [Fact]
    public async Task VerifyRecipe_Criterion2Failed_ForbiddenIngredientForDietarySchool_ThrowsUnprocessableEntity()
    {
        // Vegan school with egg ingredient (Id 3)
        var nonVeganRecipe = new Recipe
        {
            RecipeId = 504,
            CategoryId = 1,
            DietarySchoolId = 7, // VEGAN cấm EGG
            Title = "Công thức thuần chay có trứng gà",
            Instructions = "[\"B1\", \"B2\"]",
            CookTimeMinutes = 20,
            Status = "APPROVED_ACTIVE",
            Source = "USER",
            DifficultyLevel = "EASY"
        };
        _db.Recipes.Add(nonVeganRecipe);
        _db.RecipeIngredients.Add(new RecipeIngredient { RecipeId = 504, IngredientId = 3, QuantityNeeded = 2, Unit = "quả" });
        var post = new Post { PostId = 504, RecipeId = 504, UserId = 1, Title = "P", Content = "C", ModerationStatus = "APPROVED_ACTIVE", Visibility = "PUBLIC" };
        _db.Posts.Add(post);
        for (int i = 1; i <= 10; i++)
        {
            _db.Votes.Add(new Vote { VoteId = 400 + i, PostId = 504, UserId = 500 + i, CreatedAt = DateTime.UtcNow });
        }
        await _db.SaveChangesAsync();

        var act = () => _service.VerifyRecipeAsync(504, null, AdminId);
        var ex = await act.Should().ThrowAsync<AppException>();
        ex.Which.StatusCode.Should().Be(422);
        ex.Which.ErrorCode.Should().Be(ErrorCodes.ErrRecipeNotEligible);

        var detailsJson = JsonSerializer.Serialize(ex.Which.Details, JsonOptions);
        detailsJson.Should().Contain("vi phạm tiêu chuẩn ăn chay");
    }

    [Fact]
    public async Task VerifyRecipe_Criterion3Failed_Under10Votes_ThrowsUnprocessableEntity()
    {
        // 9 votes (boundary case: vừa dưới ngưỡng)
        var fewVotesRecipe = new Recipe
        {
            RecipeId = 505,
            CategoryId = 1,
            DietarySchoolId = 7,
            Title = "Công thức mới có 9 lượt thích",
            Instructions = "[\"B1\", \"B2\"]",
            CookTimeMinutes = 20,
            Status = "APPROVED_ACTIVE",
            Source = "USER",
            DifficultyLevel = "EASY"
        };
        _db.Recipes.Add(fewVotesRecipe);
        _db.RecipeIngredients.Add(new RecipeIngredient { RecipeId = 505, IngredientId = 1, QuantityNeeded = 100, Unit = "g" });
        var post = new Post { PostId = 505, RecipeId = 505, UserId = 1, Title = "P", Content = "C", ModerationStatus = "APPROVED_ACTIVE", Visibility = "PUBLIC" };
        _db.Posts.Add(post);
        for (int i = 1; i <= 9; i++)
        {
            _db.Votes.Add(new Vote { VoteId = 500 + i, PostId = 505, UserId = 600 + i, CreatedAt = DateTime.UtcNow });
        }
        await _db.SaveChangesAsync();

        var act = () => _service.VerifyRecipeAsync(505, null, AdminId);
        var ex = await act.Should().ThrowAsync<AppException>();
        ex.Which.StatusCode.Should().Be(422);
        ex.Which.ErrorCode.Should().Be(ErrorCodes.ErrRecipeNotEligible);

        var detailsJson = JsonSerializer.Serialize(ex.Which.Details, JsonOptions);
        detailsJson.Should().Contain("Chưa nhận được tối thiểu 10 lượt thả tim");
    }

    [Fact]
    public async Task VerifyRecipe_Criterion4Failed_HasPendingReport_ThrowsUnprocessableEntity()
    {
        // Recipe 506 has 10 votes, valid steps & ingredients, but has a PENDING report
        var reportedRecipe = new Recipe
        {
            RecipeId = 506,
            CategoryId = 1,
            DietarySchoolId = 7,
            Title = "Công thức đang bị báo cáo vi phạm",
            Instructions = "[\"B1\", \"B2\"]",
            CookTimeMinutes = 20,
            Status = "APPROVED_ACTIVE",
            Source = "USER",
            DifficultyLevel = "EASY"
        };
        _db.Recipes.Add(reportedRecipe);
        _db.RecipeIngredients.Add(new RecipeIngredient { RecipeId = 506, IngredientId = 1, QuantityNeeded = 100, Unit = "g" });
        var post = new Post { PostId = 506, RecipeId = 506, UserId = 1, Title = "P", Content = "C", ModerationStatus = "APPROVED_ACTIVE", Visibility = "PUBLIC" };
        _db.Posts.Add(post);
        for (int i = 1; i <= 10; i++)
        {
            _db.Votes.Add(new Vote { VoteId = 600 + i, PostId = 506, UserId = 700 + i, CreatedAt = DateTime.UtcNow });
        }
        _db.ViolationReports.Add(new ViolationReport
        {
            ReportId = 901,
            RecipeId = 506,
            ViolationType = "VIO_MEAT",
            EvidenceSummary = "Nghi vấn chứa thịt trá hình",
            ReportSource = "COMMUNITY",
            Status = "PENDING",
            CreatedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();

        var act = () => _service.VerifyRecipeAsync(506, null, AdminId);
        var ex = await act.Should().ThrowAsync<AppException>();
        ex.Which.StatusCode.Should().Be(422);
        ex.Which.ErrorCode.Should().Be(ErrorCodes.ErrRecipeNotEligible);

        var detailsJson = JsonSerializer.Serialize(ex.Which.Details, JsonOptions);
        detailsJson.Should().Contain("Còn báo cáo vi phạm đang chờ xử lý");
    }

    [Fact]
    public async Task VerifyRecipe_AllCriteriaMet_SetsIsVerifiedAndWritesAuditLogAndInvalidatesCache()
    {
        // Recipe 501 meets all 4 criteria
        var req = new VerifyAdminRecipeRequest { Note = "Đạt chuẩn công thức chay cao cấp" };

        var result = await _service.VerifyRecipeAsync(501, req, AdminId);

        result.Should().NotBeNull();
        result.RecipeId.Should().Be(501);
        result.IsVerified.Should().BeTrue();

        var recipeInDb = await _db.Recipes.FindAsync(501);
        recipeInDb!.IsVerified.Should().BeTrue();

        var audit = await _db.AuditLogs.FirstOrDefaultAsync(a => a.TargetTable == "Recipes" && a.TargetId == "501" && a.Action == "RECIPE_VERIFY");
        audit.Should().NotBeNull();
        audit!.ActorUserId.Should().Be(AdminId);
        audit.Note.Should().Be("Đạt chuẩn công thức chay cao cấp");

        _mockRedis.Verify(r => r.RemoveAsync("recipe_detail:501", default), Times.Once);
    }

    [Fact]
    public async Task UnverifyRecipe_InvalidId_ThrowsBadRequestValidation()
    {
        var act = () => _service.UnverifyRecipeAsync(0, AdminId);
        var ex = await act.Should().ThrowAsync<AppException>();
        ex.Which.StatusCode.Should().Be(400);
        ex.Which.ErrorCode.Should().Be(ErrorCodes.ErrValidation);
    }

    [Fact]
    public async Task UnverifyRecipe_NotFound_ThrowsNotFoundAdmNotFound()
    {
        var act = () => _service.UnverifyRecipeAsync(8888, AdminId);
        var ex = await act.Should().ThrowAsync<AppException>();
        ex.Which.StatusCode.Should().Be(404);
        ex.Which.ErrorCode.Should().Be(ErrorCodes.ErrAdmNotFound);
    }

    [Fact]
    public async Task UnverifyRecipe_ValidInput_SetsIsVerifiedFalseAndWritesAuditLog()
    {
        // First verify 501
        var recipe = await _db.Recipes.FindAsync(501);
        recipe!.IsVerified = true;
        await _db.SaveChangesAsync();

        var result = await _service.UnverifyRecipeAsync(501, AdminId);

        result.RecipeId.Should().Be(501);
        result.IsVerified.Should().BeFalse();

        var recipeInDb = await _db.Recipes.FindAsync(501);
        recipeInDb!.IsVerified.Should().BeFalse();

        var audit = await _db.AuditLogs.FirstOrDefaultAsync(a => a.TargetTable == "Recipes" && a.TargetId == "501" && a.Action == "RECIPE_UNVERIFY");
        audit.Should().NotBeNull();
        audit!.ActorUserId.Should().Be(AdminId);

        _mockRedis.Verify(r => r.RemoveAsync("recipe_detail:501", default), Times.AtLeastOnce);
    }
}
