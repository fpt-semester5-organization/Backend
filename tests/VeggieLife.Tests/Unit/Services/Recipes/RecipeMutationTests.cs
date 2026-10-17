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
using VeggieLife.BLL.DTOs.Ai;
using VeggieLife.BLL.DTOs.Recipes.Requests;
using VeggieLife.BLL.DTOs.Recipes.Responses;
using VeggieLife.BLL.Services.Cache;
using VeggieLife.BLL.Services.Knowledge;
using VeggieLife.BLL.Services.Moderation;
using VeggieLife.BLL.Services.Recipes;
using VeggieLife.DAL.Context;
using VeggieLife.DAL.Entities;
using Xunit;

namespace VeggieLife.Tests.Unit.Services.Recipes;

/// <summary>
/// Bộ kiểm thử đơn vị cho nghiệp vụ Đăng, Sửa, Gỡ, Xóa công thức có kiểm duyệt tự động bằng AI (FN-POST-03, FN-POST-04).
/// Kiểm thử đầy đủ: Red Alert trứng trong thuần chay, bảo vệ bản đăng cũ khi sửa không qua kiểm duyệt,
/// giới hạn bài đăng theo ngày, kiểm tra cấu trúc và các chế độ gỡ/xóa.
/// </summary>
public class RecipeMutationTests : IDisposable
{
    private readonly AppDbContext _db;
    private readonly Mock<IRedisService> _mockRedis;
    private readonly Mock<ILogger<RecipeService>> _mockLogger;
    private readonly Mock<IContentModerationService> _mockModeration;
    private readonly Mock<IKnowledgeSyncQueue> _mockKnowledgeQueue;
    private readonly RecipeService _service;

    public RecipeMutationTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: "RecipeMutationDb_" + Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        _db = new AppDbContext(options);
        _mockRedis = new Mock<IRedisService>();
        _mockLogger = new Mock<ILogger<RecipeService>>();
        _mockModeration = new Mock<IContentModerationService>();
        _mockKnowledgeQueue = new Mock<IKnowledgeSyncQueue>();

        _service = new RecipeService(
            _db,
            _mockRedis.Object,
            _mockLogger.Object,
            _mockModeration.Object,
            _mockKnowledgeQueue.Object);

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
            },
            new DietarySchool
            {
                DietarySchoolId = 2,
                SchoolCode = "LACTO",
                SchoolName = "Chay có sữa",
                ForbiddenFoodRules = "{\"forbiddenFoodGroups\":[\"EGG\",\"POULTRY\",\"SEAFOOD\"]}"
            }
        );

        // 2. Danh mục món ăn
        _db.FoodCategories.AddRange(
            new FoodCategory { CategoryId = 1, CategoryName = "Món canh", IsActive = true },
            new FoodCategory { CategoryId = 2, CategoryName = "Món xào", IsActive = true }
        );

        // 3. Nguyên liệu thực phẩm
        _db.Ingredients.AddRange(
            new Ingredient { IngredientId = 1, IngredientName = "Đậu phụ non", FoodGroup = "PLANT", StandardUnit = "g", CaloriesPer100g = 76m, DefaultStallType = "PLANT_PROTEIN" },
            new Ingredient { IngredientId = 2, IngredientName = "Nấm rơm tươi", FoodGroup = "PLANT", StandardUnit = "g", CaloriesPer100g = 34m, DefaultStallType = "PRODUCE" },
            new Ingredient { IngredientId = 3, IngredientName = "Trứng gà công nghiệp", FoodGroup = "EGG", StandardUnit = "quả", CaloriesPer100g = 155m, DefaultStallType = "DAIRY_EGG" },
            new Ingredient { IngredientId = 4, IngredientName = "Thịt bò nạc", FoodGroup = "RED_MEAT", StandardUnit = "g", CaloriesPer100g = 250m, DefaultStallType = "MEAT" },
            new Ingredient { IngredientId = 5, IngredientName = "Hành baro tươi", FoodGroup = "ALLIUM", StandardUnit = "g", CaloriesPer100g = 61m, DefaultStallType = "PRODUCE", IsAllium = true }
        );

        // 4. Người dùng
        _db.Users.AddRange(
            new User
            {
                UserId = 101,
                Email = "senior_member@veggielife.vn",
                FullName = "Thành viên cũ",
                Role = UserRoles.Member,
                AccountStatus = "ACTIVE",
                CreatedAt = DateTime.UtcNow.AddDays(-10) // Tài khoản cũ >= 3 ngày (tối đa 10 bài/ngày)
            },
            new User
            {
                UserId = 102,
                Email = "other_member@veggielife.vn",
                FullName = "Thành viên khác",
                Role = UserRoles.Member,
                AccountStatus = "ACTIVE",
                CreatedAt = DateTime.UtcNow.AddDays(-10)
            },
            new User
            {
                UserId = 201,
                Email = "newbie@veggielife.vn",
                FullName = "Thành viên mới tạo",
                Role = UserRoles.Member,
                AccountStatus = "ACTIVE",
                CreatedAt = DateTime.UtcNow.AddHours(-12) // Tài khoản mới < 3 ngày (tối đa 2 bài/ngày)
            }
        );

        _db.SaveChanges();
    }

    [Fact]
    public async Task CreateRecipe_DraftMode_SavesSuccessfullyWithoutStrictStructureValidation()
    {
        // Arrange: Chế độ lưu nháp (submit = false) với thông tin tối thiểu
        var req = new CreateRecipeRequest
        {
            Title = "Nháp ngắn",
            CategoryId = 1,
            DietarySchoolId = 7,
            Instructions = new List<string> { "Chỉ 1 bước thử nghiệm" },
            CookTimeMinutes = 10,
            Submit = false
        };

        // Act
        var result = await _service.CreateRecipeAsync(101, req, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Status.Should().Be("DRAFT");
        result.RecipeId.Should().BeGreaterThan(0);

        var saved = await _db.Recipes.FirstOrDefaultAsync(r => r.RecipeId == result.RecipeId);
        saved.Should().NotBeNull();
        saved!.Status.Should().Be("DRAFT");
        saved.UserId.Should().Be(101);
    }

    [Theory]
    [InlineData("Ngắn", 1, 7, 20, 2)] // Tiêu đề < 10 ký tự
    [InlineData("Canh nấm đậu phụ thanh đạm thuần chay", 1, 7, 0, 2)] // CookTime <= 0
    public async Task CreateRecipe_SubmitMode_InvalidStructure_Throws422ErrRecipeStructureInvalid(
        string title, int categoryId, int dietarySchoolId, int cookTime, int stepCount)
    {
        // Arrange
        var steps = Enumerable.Range(1, stepCount).Select(i => $"Bước {i} sơ chế").ToList();
        var req = new CreateRecipeRequest
        {
            Title = title,
            CategoryId = categoryId,
            DietarySchoolId = dietarySchoolId,
            Instructions = steps,
            CookTimeMinutes = cookTime,
            Ingredients = new List<RecipeIngredientItemRequest>
            {
                new() { IngredientId = 1, Quantity = 100, Unit = "g" }
            },
            Submit = true
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<AppException>(() => _service.CreateRecipeAsync(101, req, CancellationToken.None));
        ex.StatusCode.Should().Be(422);
        ex.ErrorCode.Should().Be(ErrorCodes.ErrRecipeStructureInvalid);
    }

    [Fact]
    public async Task CreateRecipe_SubmitMode_NoIngredients_Throws422ErrRecipeStructureInvalid()
    {
        // Arrange: submit = true nhưng không có nguyên liệu định lượng
        var req = new CreateRecipeRequest
        {
            Title = "Canh nấm đậu phụ thanh mát thơm ngon",
            CategoryId = 1,
            DietarySchoolId = 7,
            Instructions = new List<string> { "Bước 1: Nấu nước", "Bước 2: Nêm nếm" },
            CookTimeMinutes = 20,
            Ingredients = new List<RecipeIngredientItemRequest>(),
            Submit = true
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<AppException>(() => _service.CreateRecipeAsync(101, req, CancellationToken.None));
        ex.StatusCode.Should().Be(422);
        ex.ErrorCode.Should().Be(ErrorCodes.ErrRecipeStructureInvalid);
    }

    [Fact]
    public async Task CreateRecipe_NewAccountExceedsDailyLimit_Throws429ErrPostDailyLimitExceeded()
    {
        // Arrange: User 201 (< 3 ngày tuổi) đã có 2 bài trong ngày hôm nay
        _db.Recipes.AddRange(
            new Recipe
            {
                UserId = 201,
                CategoryId = 1,
                DietarySchoolId = 7,
                Title = "Bài viết công thức 1 hôm nay",
                Instructions = "[]",
                DifficultyLevel = "MEDIUM",
                Source = "USER",
                Status = "APPROVED_ACTIVE",
                CreatedAt = DateTime.UtcNow.Date.AddHours(2)
            },
            new Recipe
            {
                UserId = 201,
                CategoryId = 1,
                DietarySchoolId = 7,
                Title = "Bài viết công thức 2 hôm nay",
                Instructions = "[]",
                DifficultyLevel = "MEDIUM",
                Source = "USER",
                Status = "APPROVED_ACTIVE",
                CreatedAt = DateTime.UtcNow.Date.AddHours(3)
            }
        );
        await _db.SaveChangesAsync();

        var req = new CreateRecipeRequest
        {
            Title = "Công thức thứ ba vượt giới hạn trong ngày",
            CategoryId = 1,
            DietarySchoolId = 7,
            Instructions = new List<string> { "Bước 1: Rửa rau", "Bước 2: Luộc chín" },
            CookTimeMinutes = 15,
            Ingredients = new List<RecipeIngredientItemRequest>
            {
                new() { IngredientId = 1, Quantity = 100, Unit = "g" }
            },
            Submit = true
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<AppException>(() => _service.CreateRecipeAsync(201, req, CancellationToken.None));
        ex.StatusCode.Should().Be(429);
        ex.ErrorCode.Should().Be(ErrorCodes.ErrPostDailyLimitExceeded);
    }

    [Fact]
    public async Task CreateRecipe_VeganWithEgg_TriggersRedAlert_SavesQuarantinedAndThrows422()
    {
        // Arrange: Đăng công thức gắn nhãn VEGAN (DietarySchoolId = 7) nhưng chứa Trứng gà (IngredientId = 3, FoodGroup = EGG)
        var req = new CreateRecipeRequest
        {
            Title = "Canh trứng đậu hũ thuần chay",
            CategoryId = 1,
            DietarySchoolId = 7, // VEGAN cấm EGG
            Instructions = new List<string> { "Bước 1: Đánh tan trứng gà", "Bước 2: Nấu cùng đậu hũ" },
            CookTimeMinutes = 15,
            Ingredients = new List<RecipeIngredientItemRequest>
            {
                new() { IngredientId = 1, Quantity = 100, Unit = "g" },
                new() { IngredientId = 3, Quantity = 2, Unit = "quả" } // Trứng gà -> Bị cấm theo VEGAN
            },
            Submit = true
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<AppException>(() => _service.CreateRecipeAsync(101, req, CancellationToken.None));

        ex.StatusCode.Should().Be(422);
        ex.ErrorCode.Should().Be(ErrorCodes.ErrRecipeRedAlert);
        ex.Message.Should().ContainEquivalentOf("trứng");

        // Kiểm tra trong CSDL: Bài phải được lưu ở trạng thái QUARANTINED_HIDDEN
        var quarantined = await _db.Recipes
            .Include(r => r.RecipeIngredients)
            .FirstOrDefaultAsync(r => r.Title == req.Title);

        quarantined.Should().NotBeNull();
        quarantined!.Status.Should().Be("QUARANTINED_HIDDEN");
        quarantined.RecipeIngredients.Should().HaveCount(2);

        // Kiểm tra bản ghi ViolationReports được tạo tự động với nguồn AI_SCAN
        var report = await _db.ViolationReports.FirstOrDefaultAsync(v => v.RecipeId == quarantined.RecipeId);
        report.Should().NotBeNull();
        report!.ReportSource.Should().Be("AI_SCAN");
        report.ViolationType.Should().Be("VIO_LABEL");
        report.Status.Should().Be("PENDING");
    }

    [Fact]
    public async Task CreateRecipe_BuddhistWithAllium_TriggersRedAlert_SavesQuarantinedAndThrows422()
    {
        // Arrange: Chay Phật giáo (DietarySchoolId = 1) cấm Ngũ vị tân (Allium)
        var req = new CreateRecipeRequest
        {
            Title = "Canh nấm xào hành baro chay phật giáo",
            CategoryId = 1,
            DietarySchoolId = 1,
            Instructions = new List<string> { "Bước 1: Phi thơm hành baro", "Bước 2: Cho nấm vào đảo đều" },
            CookTimeMinutes = 15,
            Ingredients = new List<RecipeIngredientItemRequest>
            {
                new() { IngredientId = 2, Quantity = 100, Unit = "g" },
                new() { IngredientId = 5, Quantity = 20, Unit = "g" } // Hành baro -> cấm trong Phật giáo
            },
            Submit = true
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<AppException>(() => _service.CreateRecipeAsync(101, req, CancellationToken.None));

        ex.StatusCode.Should().Be(422);
        ex.ErrorCode.Should().Be(ErrorCodes.ErrRecipeRedAlert);
        ex.Message.Should().Contain("Hành baro");

        var quarantined = await _db.Recipes.FirstOrDefaultAsync(r => r.Title == req.Title);
        quarantined.Should().NotBeNull();
        quarantined!.Status.Should().Be("QUARANTINED_HIDDEN");
    }

    [Fact]
    public async Task CreateRecipe_AiModerationFlagsMeat_Throws422ErrContentViolationVegan_AndQuarantines()
    {
        // Arrange: Mô phỏng AI kiểm duyệt phát hiện có thịt (VIO_MEAT)
        _mockModeration.Setup(m => m.CheckContentAsync(It.IsAny<ModerationCheckRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ModerationCheckResponse
            {
                ViolationType = "VIO_MEAT",
                ViolationScore = 0.95,
                Reasons = new List<string> { "Phát hiện từ ngữ miêu tả thịt bò trong bước nấu." }
            });

        var req = new CreateRecipeRequest
        {
            Title = "Công thức nấu chay có thịt bò ẩn",
            CategoryId = 1,
            DietarySchoolId = 7,
            Instructions = new List<string> { "Bước 1: Thái mỏng thịt bò", "Bước 2: Cho vào canh đậu hũ" },
            CookTimeMinutes = 20,
            Ingredients = new List<RecipeIngredientItemRequest>
            {
                new() { IngredientId = 1, Quantity = 100, Unit = "g" }
            },
            Submit = true
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<AppException>(() => _service.CreateRecipeAsync(101, req, CancellationToken.None));
        ex.StatusCode.Should().Be(422);
        ex.ErrorCode.Should().Be(ErrorCodes.ErrContentViolationVegan);

        var saved = await _db.Recipes.FirstOrDefaultAsync(r => r.Title == req.Title);
        saved.Should().NotBeNull();
        saved!.Status.Should().Be("QUARANTINED_HIDDEN");

        var report = await _db.ViolationReports.FirstOrDefaultAsync(v => v.RecipeId == saved.RecipeId);
        report.Should().NotBeNull();
        report!.ViolationType.Should().Be("VIO_MEAT");
    }

    [Fact]
    public async Task CreateRecipe_ValidVegan_PassesAi_SavesActiveAndCreatesRepresentativePost()
    {
        // Arrange: AI phê duyệt (không vi phạm)
        _mockModeration.Setup(m => m.CheckContentAsync(It.IsAny<ModerationCheckRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ModerationCheckResponse
            {
                ViolationType = "NONE",
                ViolationScore = 0.05,
                Reasons = new List<string>()
            });

        var req = new CreateRecipeRequest
        {
            Title = "Canh nấm đậu phụ thanh mát",
            Description = "Món canh thuần chay thanh đạm tuyệt vời",
            CategoryId = 1,
            DietarySchoolId = 7,
            Instructions = new List<string> { "Bước 1: Rửa sạch nấm và đậu phụ", "Bước 2: Nấu sôi nước dùng và nêm gia vị" },
            CookTimeMinutes = 20,
            PrepTimeMinutes = 10,
            Servings = 2,
            Ingredients = new List<RecipeIngredientItemRequest>
            {
                new() { IngredientId = 1, Quantity = 200, Unit = "g" },
                new() { IngredientId = 2, Quantity = 100, Unit = "g" }
            },
            Submit = true
        };

        // Act
        var res = await _service.CreateRecipeAsync(101, req, CancellationToken.None);

        // Assert
        res.Should().NotBeNull();
        res.Status.Should().Be("APPROVED_ACTIVE");
        res.RecipeId.Should().BeGreaterThan(0);

        var recipe = await _db.Recipes.FirstOrDefaultAsync(r => r.RecipeId == res.RecipeId);
        recipe.Should().NotBeNull();
        recipe!.Status.Should().Be("APPROVED_ACTIVE");
        // Calorie dẫn xuất: 200g đậu (76*2 = 152) + 100g nấm (34) = 186
        recipe.CaloriesKcal.Should().Be(186m);

        // Bài viết đại diện (Community Post)
        var repPost = await _db.Posts.FirstOrDefaultAsync(p => p.RecipeId == res.RecipeId);
        repPost.Should().NotBeNull();
        repPost!.Title.Should().Be(req.Title);
        repPost.ModerationStatus.Should().Be("APPROVED_ACTIVE");

        _mockKnowledgeQueue.Verify(k => k.EnqueueUpsertAsync("RECIPE", res.RecipeId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateRecipe_NotOwner_Throws403ErrContentNotOwner()
    {
        // Arrange: Recipe thuộc về User 101, nhưng User 102 cố tình sửa
        var existingRecipe = new Recipe
        {
            RecipeId = 501,
            UserId = 101,
            CategoryId = 1,
            DietarySchoolId = 7,
            Title = "Công thức gốc của 101",
            Instructions = "[\"Bước 1\", \"Bước 2\"]",
            DifficultyLevel = "MEDIUM",
            Source = "USER",
            Status = "APPROVED_ACTIVE",
            CreatedAt = DateTime.UtcNow.AddDays(-1)
        };
        _db.Recipes.Add(existingRecipe);
        await _db.SaveChangesAsync();

        var updateReq = new UpdateRecipeRequest
        {
            Title = "Sửa lén công thức người khác"
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<AppException>(() =>
            _service.UpdateRecipeAsync(501, 102, UserRoles.Member, updateReq, CancellationToken.None));

        ex.StatusCode.Should().Be(403);
        ex.ErrorCode.Should().Be(ErrorCodes.ErrContentNotOwner);
    }

    [Fact]
    public async Task UpdateRecipe_Quarantined_Throws409ErrRecipeLocked()
    {
        // Arrange: Recipe đang bị cách ly
        var quarantinedRecipe = new Recipe
        {
            RecipeId = 502,
            UserId = 101,
            CategoryId = 1,
            DietarySchoolId = 7,
            Title = "Công thức đang bị cách ly",
            Instructions = "[\"Bước 1\", \"Bước 2\"]",
            DifficultyLevel = "MEDIUM",
            Source = "USER",
            Status = "QUARANTINED_HIDDEN",
            CreatedAt = DateTime.UtcNow.AddDays(-1)
        };
        _db.Recipes.Add(quarantinedRecipe);
        await _db.SaveChangesAsync();

        var updateReq = new UpdateRecipeRequest
        {
            Title = "Tự sửa khi đang bị cách ly"
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<AppException>(() =>
            _service.UpdateRecipeAsync(502, 101, UserRoles.Member, updateReq, CancellationToken.None));

        ex.StatusCode.Should().Be(409);
        ex.ErrorCode.Should().Be(ErrorCodes.ErrRecipeLocked);
    }

    [Fact]
    public async Task UpdateRecipe_MetadataOnly_UpdatesImmediatelyWithoutAiModeration()
    {
        // Arrange: Sửa thông tin ngoài lề (Category, CookTime, Servings, Difficulty)
        var recipe = new Recipe
        {
            RecipeId = 503,
            UserId = 101,
            CategoryId = 1,
            DietarySchoolId = 7,
            Title = "Công thức đã duyệt giữ nguyên",
            Instructions = "[\"Bước 1\", \"Bước 2\"]",
            CookTimeMinutes = 20,
            Servings = 2,
            DifficultyLevel = "EASY",
            Source = "USER",
            Status = "APPROVED_ACTIVE",
            CreatedAt = DateTime.UtcNow.AddDays(-2)
        };
        _db.Recipes.Add(recipe);
        await _db.SaveChangesAsync();

        var updateReq = new UpdateRecipeRequest
        {
            CookTimeMinutes = 35,
            Servings = 4,
            DifficultyLevel = "MEDIUM"
        };

        // Act
        var res = await _service.UpdateRecipeAsync(503, 101, UserRoles.Member, updateReq, CancellationToken.None);

        // Assert
        res.Should().NotBeNull();
        res.Status.Should().Be("APPROVED_ACTIVE");
        res.Moderated.Should().BeFalse();

        var updated = await _db.Recipes.FindAsync(503);
        updated!.CookTimeMinutes.Should().Be(35);
        updated.Servings.Should().Be(4);
        updated.DifficultyLevel.Should().Be("MEDIUM");

        // Đảm bảo không gọi tới AI khi chỉ đổi metadata
        _mockModeration.Verify(m => m.CheckContentAsync(It.IsAny<ModerationCheckRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateRecipe_ApprovedActive_AddForbiddenEgg_Throws422AndOriginalRemainsUnchanged()
    {
        // Arrange: Công thức đang APPROVED_ACTIVE của 101
        var originalTitle = "Canh nấm thanh đạm thuần chay";
        var originalInstructions = "[\"Bước 1: Nấu sôi\", \"Bước 2: Nêm muối\"]";
        var recipe = new Recipe
        {
            RecipeId = 504,
            UserId = 101,
            CategoryId = 1,
            DietarySchoolId = 7, // VEGAN
            Title = originalTitle,
            Instructions = originalInstructions,
            CookTimeMinutes = 20,
            DifficultyLevel = "MEDIUM",
            Source = "USER",
            Status = "APPROVED_ACTIVE",
            CreatedAt = DateTime.UtcNow.AddDays(-2)
        };
        _db.Recipes.Add(recipe);
        _db.RecipeIngredients.Add(new RecipeIngredient
        {
            RecipeId = 504,
            IngredientId = 1, // Đậu non
            QuantityNeeded = 100,
            Unit = "g"
        });
        await _db.SaveChangesAsync();

        // Tác giả sửa thêm Trứng gà vào bản sửa
        var updateReq = new UpdateRecipeRequest
        {
            Title = "Canh nấm cho thêm trứng gà",
            Instructions = new List<string> { "Bước 1: Nấu sôi", "Bước 2: Đập trứng gà vào" },
            Ingredients = new List<RecipeIngredientItemRequest>
            {
                new() { IngredientId = 1, Quantity = 100, Unit = "g" },
                new() { IngredientId = 3, Quantity = 1, Unit = "quả" } // Trứng gà -> Cấm theo VEGAN
            }
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<AppException>(() =>
            _service.UpdateRecipeAsync(504, 101, UserRoles.Member, updateReq, CancellationToken.None));

        ex.StatusCode.Should().Be(422);
        ex.ErrorCode.Should().Be(ErrorCodes.ErrRecipeRedAlert);

        // QUY TẮC CỐT LÕI (D107): Bản đang đăng trong CSDL TUYỆT ĐỐI KHÔNG ĐỔI
        var inDb = await _db.Recipes
            .Include(r => r.RecipeIngredients)
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.RecipeId == 504);

        inDb.Should().NotBeNull();
        inDb!.Title.Should().Be(originalTitle);
        inDb.Status.Should().Be("APPROVED_ACTIVE");
        inDb.RecipeIngredients.Should().HaveCount(1);
        inDb.RecipeIngredients.First().IngredientId.Should().Be(1);
    }

    [Fact]
    public async Task UpdateRecipe_ApprovedActive_AiRejectsEdit_Throws422AndOriginalRemainsUnchanged()
    {
        // Arrange: Công thức đang APPROVED_ACTIVE
        var originalTitle = "Đậu sốt cà chua thanh thuần";
        var recipe = new Recipe
        {
            RecipeId = 505,
            UserId = 101,
            CategoryId = 2,
            DietarySchoolId = 7,
            Title = originalTitle,
            Instructions = "[\"Bước 1: Sốt cà chua\", \"Bước 2: Cho đậu\"]",
            CookTimeMinutes = 15,
            DifficultyLevel = "MEDIUM",
            Source = "USER",
            Status = "APPROVED_ACTIVE",
            CreatedAt = DateTime.UtcNow.AddDays(-2)
        };
        _db.Recipes.Add(recipe);
        _db.RecipeIngredients.Add(new RecipeIngredient
        {
            RecipeId = 505,
            IngredientId = 1,
            QuantityNeeded = 100,
            Unit = "g"
        });
        await _db.SaveChangesAsync();

        // AI trả về vi phạm thịt động vật
        _mockModeration.Setup(m => m.CheckContentAsync(It.IsAny<ModerationCheckRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ModerationCheckResponse
            {
                ViolationType = "VIO_MEAT",
                ViolationScore = 0.90,
                Reasons = new List<string> { "Phát hiện nội dung có thịt bò." }
            });

        var updateReq = new UpdateRecipeRequest
        {
            Title = "Đậu sốt cà chua có xào thịt bò",
            Instructions = new List<string> { "Bước 1: Xào thịt bò", "Bước 2: Cho đậu sốt" }
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<AppException>(() =>
            _service.UpdateRecipeAsync(505, 101, UserRoles.Member, updateReq, CancellationToken.None));

        ex.StatusCode.Should().Be(422);
        ex.ErrorCode.Should().Be(ErrorCodes.ErrContentViolationVegan);

        // QUY TẮC CỐT LÕI (D107): Bản đang đăng trong CSDL không bị thay đổi
        var inDb = await _db.Recipes.AsNoTracking().FirstOrDefaultAsync(r => r.RecipeId == 505);
        inDb.Should().NotBeNull();
        inDb!.Title.Should().Be(originalTitle);
        inDb.Status.Should().Be("APPROVED_ACTIVE");
    }

    [Fact]
    public async Task UpdateRecipe_ApprovedActive_ValidEditPassesAi_UpdatesDatabaseAndAuditLog()
    {
        // Arrange
        var recipe = new Recipe
        {
            RecipeId = 506,
            UserId = 101,
            CategoryId = 1,
            DietarySchoolId = 7,
            Title = "Canh nấm bản gốc",
            Instructions = "[\"Bước 1: Nấu canh\", \"Bước 2: Nêm nếm\"]",
            CookTimeMinutes = 20,
            DifficultyLevel = "MEDIUM",
            Source = "USER",
            Status = "APPROVED_ACTIVE",
            CreatedAt = DateTime.UtcNow.AddDays(-3)
        };
        _db.Recipes.Add(recipe);
        _db.RecipeIngredients.Add(new RecipeIngredient
        {
            RecipeId = 506,
            IngredientId = 2,
            QuantityNeeded = 100,
            Unit = "g"
        });
        await _db.SaveChangesAsync();

        _mockModeration.Setup(m => m.CheckContentAsync(It.IsAny<ModerationCheckRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ModerationCheckResponse
            {
                ViolationType = "NONE",
                ViolationScore = 0.05,
                Reasons = new List<string>()
            });

        var updateReq = new UpdateRecipeRequest
        {
            Title = "Canh nấm bản mới cải tiến",
            Description = "Bản mới bổ sung thêm nấm rơm",
            Instructions = new List<string> { "Bước 1: Nấu canh lửa nhỏ", "Bước 2: Nêm nếm gia vị vừa miệng" }
        };

        // Act
        var res = await _service.UpdateRecipeAsync(506, 101, UserRoles.Member, updateReq, CancellationToken.None);

        // Assert
        res.Should().NotBeNull();
        res.Status.Should().Be("APPROVED_ACTIVE");
        res.Moderated.Should().BeTrue();

        var updated = await _db.Recipes.FindAsync(506);
        updated!.Title.Should().Be("Canh nấm bản mới cải tiến");

        // Kiểm tra AuditLogs
        var log = await _db.AuditLogs.FirstOrDefaultAsync(l => l.TargetTable == "Recipes" && l.TargetId == "506");
        log.Should().NotBeNull();
        log!.Action.Should().Be("UPDATE_RECIPE");
        log.ActorUserId.Should().Be(101);

        _mockKnowledgeQueue.Verify(k => k.EnqueueUpsertAsync("RECIPE", 506, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteRecipe_ModeArchive_SetsStatusArchivedAndNullsUserId()
    {
        // Arrange: Công thức đang APPROVED_ACTIVE
        var recipe = new Recipe
        {
            RecipeId = 601,
            UserId = 101,
            CategoryId = 1,
            DietarySchoolId = 7,
            Title = "Công thức cần gỡ khỏi trang cá nhân",
            Instructions = "[]",
            DifficultyLevel = "MEDIUM",
            Source = "USER",
            Status = "APPROVED_ACTIVE",
            CreatedAt = DateTime.UtcNow.AddDays(-2)
        };
        var repPost = new Post
        {
            PostId = 701,
            UserId = 101,
            RecipeId = 601,
            Title = recipe.Title,
            Content = recipe.Title,
            Visibility = "PUBLIC",
            ModerationStatus = "APPROVED_ACTIVE"
        };
        _db.Recipes.Add(recipe);
        _db.Posts.Add(repPost);
        await _db.SaveChangesAsync();

        // Act
        var res = await _service.DeleteRecipeAsync(601, 101, UserRoles.Member, "archive", CancellationToken.None);

        // Assert
        res.Should().NotBeNull();
        res.Status.Should().Be("ARCHIVED");

        var inDb = await _db.Recipes.FindAsync(601);
        inDb.Should().NotBeNull();
        inDb!.Status.Should().Be("ARCHIVED");
        inDb.UserId.Should().BeNull(); // Gỡ tác giả để không còn hiển thị trên profile nhưng hệ thống vẫn lưu

        var postInDb = await _db.Posts.FindAsync(701);
        postInDb!.Visibility.Should().Be("PRIVATE");
        postInDb.ModerationStatus.Should().Be("QUARANTINED_HIDDEN");
    }

    [Fact]
    public async Task DeleteRecipe_ModeAll_SetsStatusDeletedAndEnqueuesQdrantDelete()
    {
        // Arrange: Công thức cần xóa hoàn toàn
        var recipe = new Recipe
        {
            RecipeId = 602,
            UserId = 101,
            CategoryId = 1,
            DietarySchoolId = 7,
            Title = "Công thức cần xóa mềm toàn bộ",
            Instructions = "[]",
            DifficultyLevel = "MEDIUM",
            Source = "USER",
            Status = "APPROVED_ACTIVE",
            CreatedAt = DateTime.UtcNow.AddDays(-2)
        };
        _db.Recipes.Add(recipe);
        await _db.SaveChangesAsync();

        // Act
        var res = await _service.DeleteRecipeAsync(602, 101, UserRoles.Member, "all", CancellationToken.None);

        // Assert
        res.Should().NotBeNull();
        res.Status.Should().Be("DELETED");

        var inDb = await _db.Recipes.FindAsync(602);
        inDb!.Status.Should().Be("DELETED");

        _mockKnowledgeQueue.Verify(k => k.EnqueueDeleteAsync("RECIPE", 602, It.IsAny<CancellationToken>()), Times.Once);

        var auditLog = await _db.AuditLogs.FirstOrDefaultAsync(a => a.TargetTable == "Recipes" && a.TargetId == "602");
        auditLog.Should().NotBeNull();
        auditLog!.Action.Should().Be("DELETE_RECIPE");
    }
}
