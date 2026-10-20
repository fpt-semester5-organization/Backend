using System;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using VeggieLife.BLL.Common.Constants;
using VeggieLife.BLL.Common.Exceptions;
using VeggieLife.BLL.DTOs.Admin.Categories;
using VeggieLife.BLL.Services.Admin;
using VeggieLife.BLL.Services.Cache;
using VeggieLife.DAL.Context;
using VeggieLife.DAL.Entities;
using Xunit;

namespace VeggieLife.Tests.Unit.Services.Admin;

public class AdminCategoryServiceTests
{
    private readonly AppDbContext _db;
    private readonly Mock<IRedisService> _mockRedis = new();
    private readonly AdminCategoryService _service;
    private const int AdminId = 999;

    public AdminCategoryServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase("AdminCategoryServiceTestsDb_" + Guid.NewGuid())
            .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        _db = new AppDbContext(options);
        _service = new AdminCategoryService(_db, _mockRedis.Object, NullLogger<AdminCategoryService>.Instance);

        SeedTestData();
    }

    private void SeedTestData()
    {
        _db.FoodCategories.AddRange(
            new FoodCategory { CategoryId = 1, CategoryName = "Món kho cốt lõi", IsCore = true, IsActive = true, CreatedAt = DateTime.UtcNow },
            new FoodCategory { CategoryId = 2, CategoryName = "Món canh thường", IsCore = false, IsActive = true, CreatedAt = DateTime.UtcNow },
            new FoodCategory { CategoryId = 3, CategoryName = "Món xào đích", IsCore = false, IsActive = true, CreatedAt = DateTime.UtcNow },
            new FoodCategory { CategoryId = 4, CategoryName = "Món tráng miệng rỗng", IsCore = false, IsActive = true, CreatedAt = DateTime.UtcNow }
        );

        _db.Recipes.AddRange(
            new Recipe
            {
                RecipeId = 101,
                CategoryId = 2,
                Title = "Canh chua chay",
                Instructions = "[\"B1\", \"B2\"]",
                CookTimeMinutes = 20,
                DifficultyLevel = "EASY",
                Source = "USER",
                Status = "APPROVED_ACTIVE",
                CreatedAt = DateTime.UtcNow
            },
            new Recipe
            {
                RecipeId = 102,
                CategoryId = 2,
                Title = "Canh nấm chay",
                Instructions = "[\"B1\", \"B2\"]",
                CookTimeMinutes = 15,
                DifficultyLevel = "EASY",
                Source = "USER",
                Status = "APPROVED_ACTIVE",
                CreatedAt = DateTime.UtcNow
            }
        );

        _db.VeganVenues.AddRange(
            new VeganVenue { VenueId = 1, ExternalSource = "MANUAL", VenueName = "Quán Chay An Lạc", Address = "123 Đỗ Xuân Hợp", Latitude = 10.8m, Longitude = 106.7m, IsActive = true, CreatedAt = DateTime.UtcNow },
            new VeganVenue { VenueId = 2, ExternalSource = "MANUAL", VenueName = "Quán Chay Bồ Đề", Address = "456 Lê Văn Việt", Latitude = 10.8m, Longitude = 106.7m, IsActive = true, CreatedAt = DateTime.UtcNow }
        );

        _db.VenueCategories.AddRange(
            new VenueCategory { CategoryId = 2, VenueId = 1, IsPrimary = true, AssignedAt = DateTime.UtcNow },
            new VenueCategory { CategoryId = 2, VenueId = 2, IsPrimary = false, AssignedAt = DateTime.UtcNow },
            // Venue 1 already also has category 3 to test deduplication
            new VenueCategory { CategoryId = 3, VenueId = 1, IsPrimary = false, AssignedAt = DateTime.UtcNow }
        );

        _db.SaveChanges();
    }

    [Fact]
    public async Task CreateCategory_NullOrEmptyName_ThrowsBadRequestValidation()
    {
        var act1 = () => _service.CreateCategoryAsync(new CreateAdminCategoryRequest { CategoryName = "" }, AdminId);
        var act2 = () => _service.CreateCategoryAsync(new CreateAdminCategoryRequest { CategoryName = "   " }, AdminId);

        var ex1 = await act1.Should().ThrowAsync<AppException>();
        ex1.Which.StatusCode.Should().Be(400);
        ex1.Which.ErrorCode.Should().Be(ErrorCodes.ErrValidation);

        var ex2 = await act2.Should().ThrowAsync<AppException>();
        ex2.Which.StatusCode.Should().Be(400);
        ex2.Which.ErrorCode.Should().Be(ErrorCodes.ErrValidation);
    }

    [Fact]
    public async Task CreateCategory_NameOutOfRange_ThrowsBadRequestValidation()
    {
        var actTooShort = () => _service.CreateCategoryAsync(new CreateAdminCategoryRequest { CategoryName = "A" }, AdminId);
        var actTooLong = () => _service.CreateCategoryAsync(new CreateAdminCategoryRequest { CategoryName = new string('A', 101) }, AdminId);

        var ex1 = await actTooShort.Should().ThrowAsync<AppException>();
        ex1.Which.StatusCode.Should().Be(400);

        var ex2 = await actTooLong.Should().ThrowAsync<AppException>();
        ex2.Which.StatusCode.Should().Be(400);
    }

    [Fact]
    public async Task CreateCategory_DuplicateName_ThrowsConflictCategoryDuplicate()
    {
        var act = () => _service.CreateCategoryAsync(new CreateAdminCategoryRequest { CategoryName = "Món canh thường" }, AdminId);

        var ex = await act.Should().ThrowAsync<AppException>();
        ex.Which.StatusCode.Should().Be(409);
        ex.Which.ErrorCode.Should().Be(ErrorCodes.ErrCategoryDuplicate);
    }

    [Fact]
    public async Task CreateCategory_ValidInput_CreatesCategoryAndAuditLogAndInvalidatesCache()
    {
        var req = new CreateAdminCategoryRequest
        {
            CategoryName = "Món lẩu chay",
            Description = "Các món lẩu thanh đạm thuần chay"
        };

        var result = await _service.CreateCategoryAsync(req, AdminId);

        result.Should().NotBeNull();
        result.CategoryId.Should().BeGreaterThan(0);
        result.CategoryName.Should().Be("Món lẩu chay");

        var createdInDb = await _db.FoodCategories.FindAsync(result.CategoryId);
        createdInDb.Should().NotBeNull();
        createdInDb!.IsCore.Should().BeFalse();
        createdInDb.IsActive.Should().BeTrue();
        createdInDb.Description.Should().Be("Các món lẩu thanh đạm thuần chay");

        var audit = await _db.AuditLogs.FirstOrDefaultAsync(a => a.TargetTable == "FoodCategories" && a.TargetId == result.CategoryId.ToString());
        audit.Should().NotBeNull();
        audit!.Action.Should().Be("CREATE_CATEGORY");
        audit.ActorUserId.Should().Be(AdminId);

        _mockRedis.Verify(r => r.RemoveAsync("categories_cache", default), Times.Once);
        _mockRedis.Verify(r => r.RemoveAsync("recipe_search:*", default), Times.Once);
    }

    [Fact]
    public async Task UpdateCategory_InvalidId_ThrowsBadRequestValidation()
    {
        var act = () => _service.UpdateCategoryAsync(0, new UpdateAdminCategoryRequest(), AdminId);
        var ex = await act.Should().ThrowAsync<AppException>();
        ex.Which.StatusCode.Should().Be(400);
        ex.Which.ErrorCode.Should().Be(ErrorCodes.ErrValidation);
    }

    [Fact]
    public async Task UpdateCategory_NotFound_ThrowsNotFoundAdmNotFound()
    {
        var act = () => _service.UpdateCategoryAsync(9999, new UpdateAdminCategoryRequest(), AdminId);
        var ex = await act.Should().ThrowAsync<AppException>();
        ex.Which.StatusCode.Should().Be(404);
        ex.Which.ErrorCode.Should().Be(ErrorCodes.ErrAdmNotFound);
    }

    [Fact]
    public async Task UpdateCategory_DuplicateName_ThrowsConflictCategoryDuplicate()
    {
        // Try to update Category 2's name to Category 1's name
        var act = () => _service.UpdateCategoryAsync(2, new UpdateAdminCategoryRequest { CategoryName = "Món kho cốt lõi" }, AdminId);
        var ex = await act.Should().ThrowAsync<AppException>();
        ex.Which.StatusCode.Should().Be(409);
        ex.Which.ErrorCode.Should().Be(ErrorCodes.ErrCategoryDuplicate);
    }

    [Fact]
    public async Task UpdateCategory_ValidInput_UpdatesEntityAndPreservesIsCore()
    {
        var req = new UpdateAdminCategoryRequest
        {
            CategoryName = "Món canh thanh đạm",
            IsActive = false,
            Description = "Mô tả mới"
        };

        var result = await _service.UpdateCategoryAsync(2, req, AdminId);

        result.CategoryId.Should().Be(2);
        result.IsActive.Should().BeFalse();
        result.CategoryName.Should().Be("Món canh thanh đạm");

        var updated = await _db.FoodCategories.FindAsync(2);
        updated!.CategoryName.Should().Be("Món canh thanh đạm");
        updated.IsActive.Should().BeFalse();
        updated.IsCore.Should().BeFalse(); // Preserved

        var audit = await _db.AuditLogs.FirstOrDefaultAsync(a => a.TargetTable == "FoodCategories" && a.TargetId == "2" && a.Action == "UPDATE_CATEGORY");
        audit.Should().NotBeNull();
    }

    [Fact]
    public async Task DeleteCategory_CoreCategory_ThrowsForbiddenCategoryCoreProtected()
    {
        // Category 1 is IsCore = true
        var act = () => _service.DeleteCategoryAsync(1, 3, AdminId);
        var ex = await act.Should().ThrowAsync<AppException>();
        ex.Which.StatusCode.Should().Be(403);
        ex.Which.ErrorCode.Should().Be(ErrorCodes.ErrCategoryCoreProtected);
    }

    [Fact]
    public async Task DeleteCategory_HasDependentRecipes_MissingTargetCategoryId_ThrowsConflictCategoryNeedsMigration()
    {
        // Category 2 has 2 recipes and 2 venues
        var actNull = () => _service.DeleteCategoryAsync(2, null, AdminId);
        var actZero = () => _service.DeleteCategoryAsync(2, 0, AdminId);

        var ex1 = await actNull.Should().ThrowAsync<AppException>();
        ex1.Which.StatusCode.Should().Be(409);
        ex1.Which.ErrorCode.Should().Be(ErrorCodes.ErrCategoryNeedsMigration);

        var ex2 = await actZero.Should().ThrowAsync<AppException>();
        ex2.Which.StatusCode.Should().Be(409);
        ex2.Which.ErrorCode.Should().Be(ErrorCodes.ErrCategoryNeedsMigration);
    }

    [Fact]
    public async Task DeleteCategory_TargetCategorySameAsSource_ThrowsBadRequestValidation()
    {
        var act = () => _service.DeleteCategoryAsync(2, 2, AdminId);
        var ex = await act.Should().ThrowAsync<AppException>();
        ex.Which.StatusCode.Should().Be(400);
        ex.Which.ErrorCode.Should().Be(ErrorCodes.ErrValidation);
    }

    [Fact]
    public async Task DeleteCategory_TargetCategoryNotFound_ThrowsNotFoundAdmNotFound()
    {
        var act = () => _service.DeleteCategoryAsync(2, 8888, AdminId);
        var ex = await act.Should().ThrowAsync<AppException>();
        ex.Which.StatusCode.Should().Be(404);
        ex.Which.ErrorCode.Should().Be(ErrorCodes.ErrAdmNotFound);
    }

    [Fact]
    public async Task DeleteCategory_HasDependencies_MigratesDataAndDeduplicatesVenues()
    {
        // Category 2 has 2 recipes (101, 102) and 2 venues (1, 2).
        // Target Category 3 already has Venue 1, but doesn't have Venue 2.
        var result = await _service.DeleteCategoryAsync(2, 3, AdminId);

        result.MigratedRecipes.Should().Be(2);
        result.MigratedVenues.Should().Be(2);

        // Check recipes migrated to Category 3
        var recipe101 = await _db.Recipes.FindAsync(101);
        var recipe102 = await _db.Recipes.FindAsync(102);
        recipe101!.CategoryId.Should().Be(3);
        recipe102!.CategoryId.Should().Be(3);

        // Check VenueCategories for Category 2 are gone
        var oldVenueCategories = await _db.VenueCategories.Where(vc => vc.CategoryId == 2).ToListAsync();
        oldVenueCategories.Should().BeEmpty();

        // Check VenueCategories for Category 3 has both Venue 1 and Venue 2 without duplicate key error
        var targetVenueCategories = await _db.VenueCategories.Where(vc => vc.CategoryId == 3).ToListAsync();
        targetVenueCategories.Select(vc => vc.VenueId).Should().BeEquivalentTo(new[] { 1, 2 });

        // Check Category 2 is soft deleted
        var oldCat = await _db.FoodCategories.FindAsync(2);
        oldCat!.IsActive.Should().BeFalse();

        // Check AuditLog
        var audit = await _db.AuditLogs.FirstOrDefaultAsync(a => a.TargetTable == "FoodCategories" && a.TargetId == "2" && a.Action == "DELETE_CATEGORY");
        audit.Should().NotBeNull();
        audit!.ActorUserId.Should().Be(AdminId);

        _mockRedis.Verify(r => r.RemoveAsync("categories_cache", default), Times.AtLeastOnce);
        _mockRedis.Verify(r => r.RemoveAsync("recipe_search:*", default), Times.AtLeastOnce);
    }

    [Fact]
    public async Task DeleteCategory_NoDependencies_DirectSoftDeleteWithoutTargetCategory()
    {
        // Category 4 has 0 recipes and 0 venues
        var result = await _service.DeleteCategoryAsync(4, null, AdminId);

        result.MigratedRecipes.Should().Be(0);
        result.MigratedVenues.Should().Be(0);

        var cat4 = await _db.FoodCategories.FindAsync(4);
        cat4!.IsActive.Should().BeFalse();
    }
}
