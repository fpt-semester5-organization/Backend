using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using VeggieLife.BLL.Common.Constants;
using VeggieLife.BLL.Common.Exceptions;
using VeggieLife.BLL.DTOs.Admin.Ingredients;
using VeggieLife.BLL.Services.Admin;
using VeggieLife.BLL.Services.Cache;
using VeggieLife.BLL.Services.Knowledge;
using VeggieLife.BLL.Services.Media;
using VeggieLife.DAL.Context;
using VeggieLife.DAL.Entities;
using Xunit;

namespace VeggieLife.Tests.Unit.Services.Admin;

/// <summary>
/// Bộ kiểm thử đơn vị cho AdminIngredientService (FN-ADM-12).
/// Kiểm tra:
/// 1. Lấy danh sách nguyên liệu theo từ khóa, nhóm dị ứng, phân trang.
/// 2. Thêm mới nguyên liệu: kiểm tra trùng tên (ERR_INGREDIENT_DUPLICATE / 409 Conflict), ghi audit log, đồng bộ AI.
/// 3. Cập nhật nguyên liệu: kiểm tra không tìm thấy (404), kiểm tra trùng tên (409), ghi audit log, xóa cache.
/// 4. Gán/thay thế danh sách nhóm dị ứng cho nguyên liệu: kiểm tra mã dị ứng không tồn tại (400), gán thành công.
/// 5. Tạo nhóm dị ứng mới: kiểm tra trùng tên/mã (409), xóa cache dị ứng, đồng bộ AI.
/// </summary>
public class AdminIngredientServiceTests : IDisposable
{
    private readonly AppDbContext _db;
    private readonly Mock<IRedisService> _mockRedis = new();
    private readonly Mock<IMediaService> _mockMediaService = new();
    private readonly Mock<IKnowledgeSyncQueue> _mockKnowledgeSyncQueue = new();
    private readonly AdminIngredientService _service;
    private const int AdminId = 999;

    public AdminIngredientServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase("AdminIngredientServiceTestsDb_" + Guid.NewGuid())
            .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        _db = new AppDbContext(options);
        _service = new AdminIngredientService(
            _db,
            _mockRedis.Object,
            _mockMediaService.Object,
            _mockKnowledgeSyncQueue.Object,
            NullLogger<AdminIngredientService>.Instance);

        SeedTestData();
    }

    public void Dispose()
    {
        _db.Database.EnsureDeleted();
        _db.Dispose();
    }

    private void SeedTestData()
    {
        var allergen1 = new Allergen
        {
            AllergenId = 1,
            Code = "SOY",
            AllergenName = "Đậu nành",
            Description = "Chế phẩm từ đậu nành",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        var allergen2 = new Allergen
        {
            AllergenId = 2,
            Code = "GLUTEN",
            AllergenName = "Gluten",
            Description = "Lúa mì, lúa mạch",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _db.Allergens.AddRange(allergen1, allergen2);

        var ing1 = new Ingredient
        {
            IngredientId = 1,
            IngredientName = "Đậu phụ trắng",
            StandardUnit = "miếng",
            DefaultStallType = "DRY_GOODS",
            FoodGroup = "LEGUME",
            CaloriesPer100g = 76.0m,
            IsAllium = false,
            CreatedAt = DateTime.UtcNow
        };
        var ing2 = new Ingredient
        {
            IngredientId = 2,
            IngredientName = "Hành tây",
            StandardUnit = "củ",
            DefaultStallType = "PRODUCE",
            FoodGroup = "VEGETABLE",
            CaloriesPer100g = 40.0m,
            IsAllium = true,
            CreatedAt = DateTime.UtcNow
        };
        _db.Ingredients.AddRange(ing1, ing2);

        _db.IngredientAllergens.Add(new IngredientAllergen
        {
            IngredientId = 1,
            AllergenId = 1
        });

        _db.SaveChanges();
    }

    #region 1. GetIngredientsAsync

    [Fact]
    public async Task GetIngredientsAsync_NoFilter_ReturnsAllWithPagination()
    {
        var request = new AdminIngredientFilterRequest
        {
            Page = 1,
            Size = 10
        };

        var (items, meta) = await _service.GetIngredientsAsync(request);

        meta.Total.Should().Be(2);
        items.Should().HaveCount(2);
        items[0].IngredientId.Should().Be(1);
        items[0].Allergens.Should().HaveCount(1);
        items[0].Allergens[0].AllergenId.Should().Be(1);
    }

    [Fact]
    public async Task GetIngredientsAsync_FilterByKeyword_ReturnsMatching()
    {
        var request = new AdminIngredientFilterRequest
        {
            Keyword = "Hành",
            Page = 1,
            Size = 10
        };

        var (items, meta) = await _service.GetIngredientsAsync(request);

        meta.Total.Should().Be(1);
        items.Should().HaveCount(1);
        items[0].Name.Should().Be("Hành tây");
        items[0].IsAllium.Should().BeTrue();
    }

    [Fact]
    public async Task GetIngredientsAsync_FilterByAllergenId_ReturnsMatching()
    {
        var request = new AdminIngredientFilterRequest
        {
            AllergenId = 1,
            Page = 1,
            Size = 10
        };

        var (items, meta) = await _service.GetIngredientsAsync(request);

        meta.Total.Should().Be(1);
        items.Should().HaveCount(1);
        items[0].Name.Should().Be("Đậu phụ trắng");
    }

    #endregion

    #region 2. CreateIngredientAsync

    [Fact]
    public async Task CreateIngredientAsync_DuplicateName_ThrowsConflict()
    {
        var request = new CreateAdminIngredientRequest
        {
            Name = "ĐẬU PHỤ TRẮNG",
            CaloriesPer100g = 75,
            DefaultStallType = "DRY_GOODS",
            StandardUnit = "miếng",
            FoodGroup = "PLANT",
            IsAllium = false
        };

        var act = async () => await _service.CreateIngredientAsync(request, AdminId);

        var ex = await act.Should().ThrowAsync<AppException>();
        ex.Which.StatusCode.Should().Be(409);
        ex.Which.ErrorCode.Should().Be(ErrorCodes.ErrIngredientDuplicate);
    }

    [Fact]
    public async Task CreateIngredientAsync_ValidData_CreatesEntity_WritesAuditLog_AndSyncsAi()
    {
        var request = new CreateAdminIngredientRequest
        {
            Name = "Nấm đùi gà",
            CaloriesPer100g = 35.5m,
            DefaultStallType = "PRODUCE",
            StandardUnit = "g",
            FoodGroup = "PLANT",
            IsAllium = false
        };

        var response = await _service.CreateIngredientAsync(request, AdminId);

        response.IngredientId.Should().BeGreaterThan(0);

        var created = await _db.Ingredients.FindAsync(response.IngredientId);
        created.Should().NotBeNull();
        created!.IngredientName.Should().Be("Nấm đùi gà");
        created.CaloriesPer100g.Should().Be(35.5m);

        var auditLog = await _db.AuditLogs.FirstOrDefaultAsync(a => a.TargetTable == "Ingredients" && a.TargetId == response.IngredientId.ToString());
        auditLog.Should().NotBeNull();
        auditLog!.Action.Should().Be("CREATE_INGREDIENT");

        _mockKnowledgeSyncQueue.Verify(
            q => q.EnqueueUpsertAsync("INGREDIENT", response.IngredientId, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion

    #region 3. UpdateIngredientAsync

    [Fact]
    public async Task UpdateIngredientAsync_NotFound_ThrowsNotFound()
    {
        var request = new UpdateAdminIngredientRequest
        {
            Name = "Tên mới"
        };

        var act = async () => await _service.UpdateIngredientAsync(99999, request, AdminId);

        var ex = await act.Should().ThrowAsync<AppException>();
        ex.Which.StatusCode.Should().Be(404);
        ex.Which.ErrorCode.Should().Be(ErrorCodes.ErrAdmNotFound);
    }

    [Fact]
    public async Task UpdateIngredientAsync_DuplicateNameOfOtherIngredient_ThrowsConflict()
    {
        var request = new UpdateAdminIngredientRequest
        {
            Name = "Hành tây" // Đã có ở ID 2, ta cố tình update cho ID 1
        };

        var act = async () => await _service.UpdateIngredientAsync(1, request, AdminId);

        var ex = await act.Should().ThrowAsync<AppException>();
        ex.Which.StatusCode.Should().Be(409);
        ex.Which.ErrorCode.Should().Be(ErrorCodes.ErrIngredientDuplicate);
    }

    [Fact]
    public async Task UpdateIngredientAsync_ValidData_UpdatesEntity_AndSyncsAi()
    {
        var request = new UpdateAdminIngredientRequest
        {
            Name = "Đậu phụ non",
            CaloriesPer100g = 60.0m
        };

        var response = await _service.UpdateIngredientAsync(1, request, AdminId);

        response.Name.Should().Be("Đậu phụ non");
        response.CaloriesPer100g.Should().Be(60.0m);

        var updated = await _db.Ingredients.FindAsync(1);
        updated!.IngredientName.Should().Be("Đậu phụ non");

        _mockKnowledgeSyncQueue.Verify(
            q => q.EnqueueUpsertAsync("INGREDIENT", 1, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion

    #region 4. ReplaceIngredientAllergensAsync

    [Fact]
    public async Task ReplaceIngredientAllergensAsync_NonExistentAllergen_ThrowsBadRequest()
    {
        var request = new ReplaceAdminIngredientAllergensRequest
        {
            AllergenIds = new List<int> { 1, 9999 }
        };

        var act = async () => await _service.ReplaceIngredientAllergensAsync(1, request, AdminId);

        var ex = await act.Should().ThrowAsync<AppException>();
        ex.Which.StatusCode.Should().Be(400);
        ex.Which.ErrorCode.Should().Be(ErrorCodes.ErrValidation);
    }

    [Fact]
    public async Task ReplaceIngredientAllergensAsync_ValidIds_ReplacesAllergensSuccessfully()
    {
        var request = new ReplaceAdminIngredientAllergensRequest
        {
            AllergenIds = new List<int> { 2 } // Đổi từ 1 sang 2
        };

        var response = await _service.ReplaceIngredientAllergensAsync(1, request, AdminId);

        response.AllergenIds.Should().ContainSingle().Which.Should().Be(2);

        var currentAllergens = await _db.IngredientAllergens
            .Where(ia => ia.IngredientId == 1)
            .Select(ia => ia.AllergenId)
            .ToListAsync();

        currentAllergens.Should().ContainSingle().Which.Should().Be(2);
    }

    #endregion

    #region 5. CreateAllergenAsync

    [Fact]
    public async Task CreateAllergenAsync_DuplicateCodeOrName_ThrowsConflict()
    {
        var request = new CreateAdminAllergenRequest
        {
            Code = "SOY",
            AllergenName = "Đậu nành mới"
        };

        var act = async () => await _service.CreateAllergenAsync(request, AdminId);

        var ex = await act.Should().ThrowAsync<AppException>();
        ex.Which.StatusCode.Should().Be(409);
        ex.Which.ErrorCode.Should().Be(ErrorCodes.ErrAllergenDuplicate);
    }

    [Fact]
    public async Task CreateAllergenAsync_ValidData_CreatesEntity_AndInvalidatesCache()
    {
        var request = new CreateAdminAllergenRequest
        {
            Code = "PEANUT",
            AllergenName = "Đậu phộng",
            Description = "Dị ứng lạc/đậu phộng"
        };

        var response = await _service.CreateAllergenAsync(request, AdminId);

        response.AllergenId.Should().BeGreaterThan(0);

        var created = await _db.Allergens.FindAsync(response.AllergenId);
        created.Should().NotBeNull();
        created!.Code.Should().Be("PEANUT");

        _mockRedis.Verify(
            r => r.RemoveAsync("allergens_cache", It.IsAny<CancellationToken>()),
            Times.Once);

        _mockKnowledgeSyncQueue.Verify(
            q => q.EnqueueUpsertAsync("ALLERGEN", response.AllergenId, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion

    #region Khóa Nguyên Liệu & Tải Ảnh Cloudinary (BR-ADM-ING-01)

    [Fact]
    public async Task ToggleLockAsync_ValidIngredient_TogglesStatus()
    {
        var result = await _service.ToggleLockAsync(1, true, AdminId);
        result.Should().BeTrue();

        var entity = await _db.Ingredients.FindAsync(1);
        entity!.IsLocked.Should().BeTrue();

        // Mở khóa lại
        var unlocked = await _service.ToggleLockAsync(1, false, AdminId);
        unlocked.Should().BeFalse();
        entity.IsLocked.Should().BeFalse();
    }

    [Fact]
    public async Task UpdateIngredientAsync_WhenLocked_ThrowsForbidden()
    {
        // Khóa nguyên liệu ID 1
        await _service.ToggleLockAsync(1, true, AdminId);

        var request = new UpdateAdminIngredientRequest
        {
            Name = "Tên Mới Khi Đã Khóa"
        };

        var act = () => _service.UpdateIngredientAsync(1, request, AdminId);

        await act.Should().ThrowAsync<AppException>()
            .Where(e => e.StatusCode == 403 && e.ErrorCode == ErrorCodes.ErrAdmForbidden);
    }

    [Fact]
    public async Task UploadImageAsync_WhenLocked_ThrowsForbidden()
    {
        await _service.ToggleLockAsync(1, true, AdminId);

        using var stream = new System.IO.MemoryStream(new byte[] { 1, 2, 3 });
        var act = () => _service.UploadImageAsync(1, stream, "test.png", AdminId);

        await act.Should().ThrowAsync<AppException>()
            .Where(e => e.StatusCode == 403 && e.ErrorCode == ErrorCodes.ErrAdmForbidden);
    }

    [Fact]
    public async Task UploadImageAsync_WhenNotLocked_UploadsToCloudinary_AndUpdatesDb()
    {
        await _service.ToggleLockAsync(1, false, AdminId);

        var dummyUploadResponse = new VeggieLife.BLL.DTOs.Media.Responses.MediaUploadResponse
        {
            PublicId = "veggielife/ingredients/test_ing_1",
            SecureUrl = "https://res.cloudinary.com/swrsuenp/image/upload/v1/veggielife/ingredients/test_ing_1.webp"
        };

        _mockMediaService
            .Setup(m => m.UploadImageAsync(It.IsAny<System.IO.Stream>(), "test.png", "veggielife/ingredients", It.IsAny<CancellationToken>()))
            .ReturnsAsync(dummyUploadResponse);

        _mockMediaService
            .Setup(m => m.GetImageUrl(dummyUploadResponse.PublicId, 400, 400, "fill", "webp"))
            .Returns("https://res.cloudinary.com/swrsuenp/image/upload/c_fill,w_400,h_400/v1/veggielife/ingredients/test_ing_1.webp");

        using var stream = new System.IO.MemoryStream(new byte[] { 1, 2, 3 });
        var url = await _service.UploadImageAsync(1, stream, "test.png", AdminId);

        url.Should().Contain("test_ing_1");

        var entity = await _db.Ingredients.FindAsync(1);
        entity!.ImageUrl.Should().Be(url);
    }

    #endregion
}
