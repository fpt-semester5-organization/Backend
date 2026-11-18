using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using VeggieLife.BLL.Common.Constants;
using VeggieLife.BLL.Common.Exceptions;
using VeggieLife.BLL.DTOs.Profile.Requests;
using VeggieLife.BLL.DTOs.ShoppingLists.Requests;
using VeggieLife.BLL.Services.Admin;
using VeggieLife.BLL.Services.Cache;
using VeggieLife.BLL.Services.Media;
using VeggieLife.BLL.Services.Profile;
using VeggieLife.BLL.Services.ShoppingLists;
using VeggieLife.DAL.Context;
using VeggieLife.DAL.Entities;
using Xunit;

namespace VeggieLife.Tests.Unit.Regression;

/// <summary>
/// BỘ KIỂM THỬ HỒI QUY TOÀN DIỆN CHO BACKEND .NET (VEGGIELIFE).
/// Xác nhận các quy tắc nghiệp vụ cốt lõi, ràng buộc bảo vệ dữ liệu và các điểm nhạy cảm về thời gian.
/// </summary>
public class SystemRegressionTestSuite : IDisposable
{
    private readonly AppDbContext _db;
    private readonly Mock<IRedisService> _mockRedis = new();
    private readonly Mock<IMediaService> _mockMedia = new();
    private readonly Mock<ILogger<ProfileService>> _mockProfileLogger = new();

    public SystemRegressionTestSuite()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase("RegressionDb_" + Guid.NewGuid())
            .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        _db = new AppDbContext(options);
    }

    public void Dispose()
    {
        _db.Database.EnsureDeleted();
        _db.Dispose();
        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task Regression_AdminCategory_CoreProtection_CannotBeDeleted()
    {
        // ARRANGE: Tạo danh mục cốt lõi
        _db.FoodCategories.Add(new FoodCategory
        {
            CategoryId = 1,
            CategoryName = "Món kho cốt lõi",
            IsCore = true,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();

        var service = new AdminCategoryService(_db, _mockRedis.Object, NullLogger<AdminCategoryService>.Instance);

        // ACT
        var act = () => service.DeleteCategoryAsync(1, 2, 999);

        // ASSERT: Phải từ chối với 403 ErrCategoryCoreProtected
        var ex = await act.Should().ThrowAsync<AppException>();
        ex.Which.StatusCode.Should().Be(403);
        ex.Which.ErrorCode.Should().Be(ErrorCodes.ErrCategoryCoreProtected);
    }

    [Fact]
    public async Task Regression_AdminCategory_HasDependentFoods_RequiresTargetCategoryId()
    {
        // ARRANGE: Tạo danh mục thường và món ăn tham chiếu
        _db.FoodCategories.AddRange(
            new FoodCategory { CategoryId = 10, CategoryName = "Danh mục nguồn", IsCore = false, IsActive = true, CreatedAt = DateTime.UtcNow },
            new FoodCategory { CategoryId = 20, CategoryName = "Danh mục đích", IsCore = false, IsActive = true, CreatedAt = DateTime.UtcNow }
        );

        _db.Foods.Add(new Food
        {
            FoodId = 101,
            CategoryId = 10,
            FoodName = "Món phụ thuộc hồi quy",
            CreatedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();

        var service = new AdminCategoryService(_db, _mockRedis.Object, NullLogger<AdminCategoryService>.Instance);

        // ACT: Xóa không truyền targetCategoryId
        var actMissing = () => service.DeleteCategoryAsync(10, null, 999);

        // ASSERT: Phải ném 409 ErrCategoryNeedsMigration
        var ex = await actMissing.Should().ThrowAsync<AppException>();
        ex.Which.StatusCode.Should().Be(409);
        ex.Which.ErrorCode.Should().Be(ErrorCodes.ErrCategoryNeedsMigration);
    }

    [Fact]
    public async Task Regression_Profile_WeightEntry_TimezoneAndDateBounds()
    {
        // ARRANGE: Seed User và HealthProfile
        var userId = 501;
        _db.Users.Add(new User
        {
            UserId = userId,
            Email = "regression_user@veggielife.vn",
            FullName = "Regression Tester",
            PasswordHash = "hash",
            Role = "MEMBER",
            AccountStatus = "ACTIVE",
            CreatedAt = DateTime.UtcNow
        });

        _db.HealthProfiles.Add(new HealthProfile
        {
            UserId = userId,
            DietarySchoolId = 1,
            Gender = "MALE",
            DateOfBirth = new DateOnly(1995, 1, 1),
            HeightCm = 170m,
            CurrentWeightKg = 65m,
            BmiValue = 22.5m,
            BmrKcal = 1600m,
            TdeeKcal = 2000m,
            TargetDailyCalories = 2000m,
            RegionPreference = "NORTH",
            HealthGoal = "MAINTAIN",
            CreatedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();

        var service = new ProfileService(_db, _mockRedis.Object, _mockMedia.Object, _mockProfileLogger.Object, null, null);

        // 1. Quá khứ: Bị từ chối
        var pastRequest = new CreateCurrentUserWeightEntryRequest
        {
            WeightKg = 64m,
            LoggedDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-2))
        };
        var actPast = () => service.CreateWeightEntryAsync(userId, pastRequest);
        await actPast.Should().ThrowAsync<AppException>();

        // 2. Tương lai: Bị từ chối
        var futureRequest = new CreateCurrentUserWeightEntryRequest
        {
            WeightKg = 64m,
            LoggedDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(2))
        };
        var actFuture = () => service.CreateWeightEntryAsync(userId, futureRequest);
        await actFuture.Should().ThrowAsync<AppException>();

        // 3. Hôm nay: Thành công (201 Created logic)
        var todayRequest = new CreateCurrentUserWeightEntryRequest
        {
            WeightKg = 64.5m,
            LoggedDate = DateOnly.FromDateTime(DateTime.UtcNow),
            Note = "Cân sáng kiểm tra hồi quy"
        };
        var result = await service.CreateWeightEntryAsync(userId, todayRequest);
        result.Should().NotBeNull();
        result.WeightKg.Should().Be(64.5m);
        result.Bmi.Should().BeApproximately(22.32m, 0.1m);
    }

    [Fact]
    public async Task Regression_ShoppingList_ServingsMultiplier_BoundaryValidation()
    {
        var service = new ShoppingListService(_db);

        // Multiplier < 1.0 -> 400 ErrValidation
        var actLow = () => service.CreateMealPlanShoppingListAsync(1, 1, new CreateMealPlanShoppingListRequest { ServingsMultiplier = 0.5m });
        var exLow = await actLow.Should().ThrowAsync<AppException>();
        exLow.Which.StatusCode.Should().Be(400);

        // Multiplier > 10.0 -> 400 ErrValidation
        var actHigh = () => service.CreateMealPlanShoppingListAsync(1, 1, new CreateMealPlanShoppingListRequest { ServingsMultiplier = 15.0m });
        var exHigh = await actHigh.Should().ThrowAsync<AppException>();
        exHigh.Which.StatusCode.Should().Be(400);
    }
}
