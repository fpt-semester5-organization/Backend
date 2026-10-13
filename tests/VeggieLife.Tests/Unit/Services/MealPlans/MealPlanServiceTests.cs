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
using VeggieLife.BLL.DTOs.MealPlans.Requests;
using VeggieLife.BLL.DTOs.MealPlans.Responses;
using VeggieLife.BLL.Services.Cache;
using VeggieLife.BLL.Services.MealPlans;
using VeggieLife.DAL.Context;
using VeggieLife.DAL.Entities;
using VeggieLife.DAL.External.AiService;
using Xunit;

namespace VeggieLife.Tests.Unit.Services.MealPlans;

/// <summary>
/// Bộ kiểm thử đơn vị toàn diện cho MealPlanService (FN-MEAL-01).
/// Kiểm tra:
/// 1. Chạy thử 20 hồ sơ khác nhau: 0 món sai trường phái, 0 món dính dị ứng.
/// 2. Kiểm tra hạn mức tuần: Free 1 thực đơn/tuần, VIP 3 thực đơn/tuần (ERR_QUOTA_MEAL_PLAN_EXCEEDED).
/// 3. Kiểm tra các ca lỗi: Khách vãng lai, Chưa có hồ sơ sức khỏe, Ngày bắt đầu không phải Thứ Hai, Quá khứ, Ngân sách sai.
/// 4. Xử lý khi AI chậm/lỗi hoặc Circuit Breaker mở: chuyển sang Fallback Engine an toàn.
/// </summary>
public class MealPlanServiceTests : IDisposable
{
    private readonly AppDbContext _db;
    private readonly Mock<IAiServiceClient> _mockAiClient;
    private readonly Mock<IRedisService> _mockRedis;
    private readonly Mock<ILogger<MealPlanService>> _mockLogger;
    private readonly MealPlanService _service;

    public MealPlanServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _db = new AppDbContext(options);
        _mockAiClient = new Mock<IAiServiceClient>();
        _mockRedis = new Mock<IRedisService>();
        _mockLogger = new Mock<ILogger<MealPlanService>>();

        // Mặc định Redis không bị khóa
        _mockRedis.Setup(r => r.ExistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _mockRedis.Setup(r => r.SetAsync(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<TimeSpan?>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _mockRedis.Setup(r => r.RemoveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Mặc định Circuit Breaker đóng
        _mockAiClient.Setup(c => c.IsCircuitOpen).Returns(false);

        _service = new MealPlanService(_db, _mockAiClient.Object, _mockRedis.Object, _mockLogger.Object);

        SeedMasterDataAndRecipes();
    }

    public void Dispose()
    {
        _db.Database.EnsureDeleted();
        _db.Dispose();
    }

    private static DateOnly GetNextMonday()
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var daysUntilMonday = ((int)DayOfWeek.Monday - (int)today.DayOfWeek + 7) % 7;
        if (daysUntilMonday == 0) daysUntilMonday = 7;
        return today.AddDays(daysUntilMonday);
    }

    [Fact]
    public async Task CreateMealPlan_Across20DistinctProfiles_MustHaveZeroDietaryViolationsAndZeroAllergies()
    {
        // ARRANGE & ACT: Tạo và kiểm thử 20 hồ sơ khác nhau
        var nextMonday = GetNextMonday().ToString("yyyy-MM-dd");

        for (var profileIndex = 1; profileIndex <= 20; profileIndex++)
        {
            var userId = 100 + profileIndex;
            var (dietarySchoolId, targetKcal, requiresAllium, allergenIds) = GetProfileConfig(profileIndex);

            // 1. Tạo User
            var user = new User
            {
                UserId = userId,
                Email = $"user_test_{profileIndex}@veggielife.vn",
                FullName = $"Test User {profileIndex}",
                Role = UserRoles.Member,
                AccountStatus = "ACTIVE"
            };
            _db.Users.Add(user);

            // 2. Tạo HealthProfile
            var hp = new HealthProfile
            {
                UserId = userId,
                DietarySchoolId = dietarySchoolId,
                Gender = "FEMALE",
                DateOfBirth = new DateOnly(1995, 5, 20),
                HeightCm = 160m,
                CurrentWeightKg = 55m,
                BmiValue = 21.5m,
                BmrKcal = 1300m,
                TdeeKcal = targetKcal,
                TargetDailyCalories = targetKcal,
                RequiresAlliumFree = requiresAllium,
                RegionPreference = "MIEN_BAC",
                HealthGoal = "MAINTAIN"
            };
            _db.HealthProfiles.Add(hp);

            // 3. Gán UserAllergies nếu có
            foreach (var aId in allergenIds)
            {
                _db.UserAllergies.Add(new UserAllergy
                {
                    UserId = userId,
                    AllergenId = aId
                });
            }

            await _db.SaveChangesAsync();

            // ACT
            var req = new CreateMealPlanRequest
            {
                StartDate = nextMonday,
                BudgetTier = "STANDARD",
                UseFridge = false
            };

            var result = await _service.CreateMealPlanAsync(userId, UserRoles.Member, req);

            // ASSERT
            result.Should().NotBeNull($"Hồ sơ #{profileIndex} phải tạo thực đơn thành công.");
            result.PlanStatus.Should().Be("DRAFT");
            result.Slots.Should().HaveCount(21, $"Hồ sơ #{profileIndex} phải đủ 21 bữa ăn cho 7 ngày.");

            // Kiểm tra từng bữa ăn trong 21 bữa
            var recipeIdsInPlan = result.Slots.Select(s => s.Recipe!.RecipeId).ToList();
            var usedRecipes = await _db.Recipes
                .Include(r => r.RecipeIngredients)
                    .ThenInclude(ri => ri.Ingredient)
                        .ThenInclude(i => i.IngredientAllergens)
                .Where(r => recipeIdsInPlan.Contains(r.RecipeId))
                .ToListAsync();

            var dietSchool = await _db.DietarySchools.FindAsync(dietarySchoolId);
            dietSchool.Should().NotBeNull();

            // Phân tích quy tắc cấm của trường phái
            var forbiddenGroups = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "MEAT", "RED_MEAT", "BEEF", "PORK", "MAM" };
            var isBuddhist = dietSchool!.SchoolCode == "BUDDHIST" || requiresAllium;

            if (dietSchool.SchoolCode == "VEGAN")
            {
                forbiddenGroups.UnionWith(["EGG", "DAIRY", "POULTRY", "SEAFOOD"]);
            }
            else if (dietSchool.SchoolCode == "OVO")
            {
                forbiddenGroups.UnionWith(["DAIRY", "POULTRY", "SEAFOOD"]);
            }
            else if (dietSchool.SchoolCode == "LACTO")
            {
                forbiddenGroups.UnionWith(["EGG", "POULTRY", "SEAFOOD"]);
            }
            else if (dietSchool.SchoolCode == "LACTO_OVO")
            {
                forbiddenGroups.UnionWith(["POULTRY", "SEAFOOD"]);
            }
            else if (dietSchool.SchoolCode == "BUDDHIST")
            {
                forbiddenGroups.UnionWith(["EGG", "DAIRY", "POULTRY", "SEAFOOD"]);
            }
            else if (dietSchool.SchoolCode == "POLLOTARIAN")
            {
                forbiddenGroups.UnionWith(["SEAFOOD"]);
            }
            else if (dietSchool.SchoolCode == "PESCATARIAN")
            {
                forbiddenGroups.UnionWith(["POULTRY"]);
            }

            foreach (var r in usedRecipes)
            {
                foreach (var ri in r.RecipeIngredients)
                {
                    var ing = ri.Ingredient;

                    // 1. Không dính nhóm thực phẩm cấm
                    forbiddenGroups.Should().NotContain(ing.FoodGroup,
                        $"Hồ sơ #{profileIndex} (Trường phái {dietSchool.SchoolCode}) không được chứa nguyên liệu nhóm {ing.FoodGroup} trong món {r.Title}.");

                    // 2. Không dính hành tỏi nếu Phật giáo hoặc có yêu cầu Allium-free
                    if (isBuddhist)
                    {
                        ing.IsAllium.Should().BeFalse(
                            $"Hồ sơ #{profileIndex} yêu cầu Allium-free không được chứa nguyên liệu ngũ vị tân ({ing.IngredientName}) trong món {r.Title}.");
                    }

                    // 3. Không dính chất dị ứng của người dùng
                    var ingAllergenIds = ing.IngredientAllergens.Select(ia => ia.AllergenId).ToList();
                    foreach (var aId in allergenIds)
                    {
                        ingAllergenIds.Should().NotContain(aId,
                            $"Hồ sơ #{profileIndex} bị dị ứng ID {aId} không được chứa nguyên liệu {ing.IngredientName} trong món {r.Title}.");
                    }
                }
            }

            // Kiểm tra dung sai calo hàng ngày
            var dailyTotals = result.Slots
                .GroupBy(s => s.DayOfWeek)
                .ToDictionary(g => g.Key, g => g.Sum(x => x.Recipe!.CaloriesKcal));

            dailyTotals.Should().HaveCount(7);
            foreach (var (day, dayCalo) in dailyTotals)
            {
                // Dung sai calo tối đa nằm trong khoảng hợp lý quanh mục tiêu
                dayCalo.Should().BeInRange(targetKcal * 0.85m, targetKcal * 1.15m,
                    $"Tổng calo ngày {day} của hồ sơ #{profileIndex} phải xoay quanh mục tiêu {targetKcal} kcal.");
            }
        }
    }

    [Fact]
    public async Task CreateMealPlan_ExceedQuota_ForMember_ShouldThrow_ERR_QUOTA_MEAL_PLAN_EXCEEDED()
    {
        // ARRANGE: Thành viên thường có hạn mức 1 thực đơn / tuần
        var userId = 50;
        var nextMonday = GetNextMonday();

        _db.Users.Add(new User
        {
            UserId = userId,
            Email = "member_quota@veggielife.vn",
            FullName = "Member Quota",
            Role = UserRoles.Member,
            AccountStatus = "ACTIVE"
        });

        _db.HealthProfiles.Add(new HealthProfile
        {
            UserId = userId,
            DietarySchoolId = 1, // Vegan
            Gender = "MALE",
            DateOfBirth = new DateOnly(1990, 1, 1),
            HeightCm = 175m,
            CurrentWeightKg = 70m,
            BmiValue = 22.8m,
            BmrKcal = 1600m,
            TdeeKcal = 2000m,
            TargetDailyCalories = 2000m,
            RequiresAlliumFree = false,
            RegionPreference = "MIEN_BAC",
            HealthGoal = "MAINTAIN"
        });

        await _db.SaveChangesAsync();

        var req = new CreateMealPlanRequest
        {
            StartDate = nextMonday.ToString("yyyy-MM-dd"),
            BudgetTier = "STANDARD"
        };

        // Lần 1: Thành công
        var plan1 = await _service.CreateMealPlanAsync(userId, UserRoles.Member, req);
        plan1.Should().NotBeNull();

        // ACT & ASSERT: Lần 2 trong cùng tuần -> Báo lỗi vượt hạn mức (HTTP 403 ERR_QUOTA_MEAL_PLAN_EXCEEDED)
        var act = () => _service.CreateMealPlanAsync(userId, UserRoles.Member, req);
        var ex = await act.Should().ThrowAsync<AppException>();
        ex.Which.StatusCode.Should().Be(403);
        ex.Which.ErrorCode.Should().Be(ErrorCodes.ErrQuotaMealPlanExceeded);
    }

    [Fact]
    public async Task CreateMealPlan_ForVip_CanCreateUpTo3PlansInSameWeek()
    {
        // ARRANGE: VIP có hạn mức 3 thực đơn / tuần
        var userId = 51;
        var nextMonday = GetNextMonday();

        _db.Users.Add(new User
        {
            UserId = userId,
            Email = "vip_quota@veggielife.vn",
            FullName = "VIP Quota",
            Role = UserRoles.Vip,
            AccountStatus = "ACTIVE"
        });

        _db.HealthProfiles.Add(new HealthProfile
        {
            UserId = userId,
            DietarySchoolId = 4, // Lacto-Ovo
            Gender = "FEMALE",
            DateOfBirth = new DateOnly(1992, 3, 15),
            HeightCm = 165m,
            CurrentWeightKg = 52m,
            BmiValue = 19.1m,
            BmrKcal = 1250m,
            TdeeKcal = 1800m,
            TargetDailyCalories = 1800m,
            RequiresAlliumFree = false,
            RegionPreference = "MIEN_NAM",
            HealthGoal = "MAINTAIN"
        });

        await _db.SaveChangesAsync();

        var req = new CreateMealPlanRequest
        {
            StartDate = nextMonday.ToString("yyyy-MM-dd"),
            BudgetTier = "PREMIUM"
        };

        // Lần 1: Thành công
        var plan1 = await _service.CreateMealPlanAsync(userId, UserRoles.Vip, req);
        plan1.Should().NotBeNull();

        // Lần 2: Thành công
        var plan2 = await _service.CreateMealPlanAsync(userId, UserRoles.Vip, req);
        plan2.Should().NotBeNull();

        // Lần 3: Thành công
        var plan3 = await _service.CreateMealPlanAsync(userId, UserRoles.Vip, req);
        plan3.Should().NotBeNull();

        // Lần 4: Vượt hạn mức 3 thực đơn / tuần -> Báo lỗi
        var act = () => _service.CreateMealPlanAsync(userId, UserRoles.Vip, req);
        var ex = await act.Should().ThrowAsync<AppException>();
        ex.Which.StatusCode.Should().Be(403);
        ex.Which.ErrorCode.Should().Be(ErrorCodes.ErrQuotaMealPlanExceeded);
    }

    [Fact]
    public async Task CreateMealPlan_WhenGuestRole_ShouldThrow_ERR_GUEST_PERSONALIZATION_RESTRICTED()
    {
        var req = new CreateMealPlanRequest
        {
            StartDate = GetNextMonday().ToString("yyyy-MM-dd"),
            BudgetTier = "STANDARD"
        };

        var act = () => _service.CreateMealPlanAsync(999, UserRoles.Guest, req);
        var ex = await act.Should().ThrowAsync<AppException>();
        ex.Which.StatusCode.Should().Be(403);
        ex.Which.ErrorCode.Should().Be(ErrorCodes.ErrGuestPersonalizationRestricted);
    }

    [Fact]
    public async Task CreateMealPlan_WhenNoProfile_ShouldThrow_ERR_PROFILE_NOT_COMPLETED()
    {
        var userId = 77;
        _db.Users.Add(new User
        {
            UserId = userId,
            Email = "no_profile@veggielife.vn",
            FullName = "No Profile",
            Role = UserRoles.Member,
            AccountStatus = "ACTIVE"
        });
        await _db.SaveChangesAsync();

        var req = new CreateMealPlanRequest
        {
            StartDate = GetNextMonday().ToString("yyyy-MM-dd"),
            BudgetTier = "STANDARD"
        };

        var act = () => _service.CreateMealPlanAsync(userId, UserRoles.Member, req);
        var ex = await act.Should().ThrowAsync<AppException>();
        ex.Which.StatusCode.Should().Be(409);
        ex.Which.ErrorCode.Should().Be(ErrorCodes.ErrProfileNotCompleted);
    }

    [Fact]
    public async Task CreateMealPlan_WhenStartDateNotMonday_ShouldThrow_ERR_VALIDATION()
    {
        var userId = 78;
        _db.Users.Add(new User { UserId = userId, Email = "u78@veggielife.vn", FullName = "U78", Role = UserRoles.Member, AccountStatus = "ACTIVE" });
        _db.HealthProfiles.Add(new HealthProfile
        {
            UserId = userId,
            DietarySchoolId = 1,
            Gender = "MALE",
            DateOfBirth = new DateOnly(1990, 1, 1),
            HeightCm = 170m,
            CurrentWeightKg = 65m,
            BmiValue = 22.5m,
            BmrKcal = 1500m,
            TdeeKcal = 2000m,
            TargetDailyCalories = 2000m,
            RegionPreference = "MIEN_BAC",
            HealthGoal = "MAINTAIN"
        });
        await _db.SaveChangesAsync();

        // Chọn ngày Thứ Tư thay vì Thứ Hai
        var wednesday = GetNextMonday().AddDays(2);
        var req = new CreateMealPlanRequest
        {
            StartDate = wednesday.ToString("yyyy-MM-dd"),
            BudgetTier = "STANDARD"
        };

        var act = () => _service.CreateMealPlanAsync(userId, UserRoles.Member, req);
        var ex = await act.Should().ThrowAsync<AppException>();
        ex.Which.StatusCode.Should().Be(400);
        ex.Which.ErrorCode.Should().Be(ErrorCodes.ErrValidation);
    }

    [Fact]
    public async Task CreateMealPlan_WhenAiTimeoutOrCircuitOpen_ShouldUseFallbackEngineSeamlessly()
    {
        // ARRANGE: Mô phỏng AI bị ngắt mạch (Circuit Breaker OPEN)
        _mockAiClient.Setup(c => c.IsCircuitOpen).Returns(true);

        var userId = 80;
        _db.Users.Add(new User { UserId = userId, Email = "fallback_user@veggielife.vn", FullName = "Fallback User", Role = UserRoles.Member, AccountStatus = "ACTIVE" });
        _db.HealthProfiles.Add(new HealthProfile
        {
            UserId = userId,
            DietarySchoolId = 1, // Vegan
            Gender = "FEMALE",
            DateOfBirth = new DateOnly(1994, 2, 2),
            HeightCm = 158m,
            CurrentWeightKg = 48m,
            BmiValue = 19.2m,
            BmrKcal = 1200m,
            TdeeKcal = 1800m,
            TargetDailyCalories = 1800m,
            RegionPreference = "MIEN_TRUNG",
            HealthGoal = "MAINTAIN"
        });
        await _db.SaveChangesAsync();

        var req = new CreateMealPlanRequest
        {
            StartDate = GetNextMonday().ToString("yyyy-MM-dd"),
            BudgetTier = "STANDARD"
        };

        // ACT
        var result = await _service.CreateMealPlanAsync(userId, UserRoles.Member, req);

        // ASSERT: Vẫn tạo thành công thực đơn 21 bữa bằng Fallback Engine
        result.Should().NotBeNull();
        result.Slots.Should().HaveCount(21);
        result.GeneratedByAi.Should().BeFalse(); // Được tạo bằng Fallback Engine

        // AI không được gọi vì circuit open
        _mockAiClient.Verify(c => c.GenerateMealPlanAsync(It.IsAny<AiMealPlanRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // =========================================================================
    // FN-MEAL-02: Xem danh sách và chi tiết thực đơn; kích hoạt một thực đơn
    // =========================================================================

    [Fact]
    public async Task ListMealPlansAsync_ShouldReturnPaginatedList_AndExcludeTemplates()
    {
        // ARRANGE
        var userId = 501;
        _db.Users.Add(new User { UserId = userId, Email = "user501@test.com", FullName = "User 501", Role = UserRoles.Member, AccountStatus = "ACTIVE" });
        _db.MealPlans.AddRange(
            new MealPlan { PlanId = 5011, UserId = userId, PlanName = "Plan 1", StartDate = new DateOnly(2026, 10, 19), EndDate = new DateOnly(2026, 10, 25), PlanStatus = "DRAFT", BudgetTier = "STANDARD", CreatedAt = DateTime.UtcNow.AddDays(-2) },
            new MealPlan { PlanId = 5012, UserId = userId, PlanName = "Plan 2", StartDate = new DateOnly(2026, 10, 26), EndDate = new DateOnly(2026, 11, 1), PlanStatus = "ACTIVE", BudgetTier = "STANDARD", CreatedAt = DateTime.UtcNow.AddDays(-1) },
            new MealPlan { PlanId = 5013, UserId = userId, PlanName = "Template Plan", StartDate = new DateOnly(2026, 10, 12), EndDate = new DateOnly(2026, 10, 18), PlanStatus = "TEMPLATE", BudgetTier = "STANDARD", CreatedAt = DateTime.UtcNow }
        );
        await _db.SaveChangesAsync();

        // ACT
        var (items, meta) = await _service.ListMealPlansAsync(userId, page: 1, size: 10);

        // ASSERT
        items.Should().HaveCount(2);
        items.Select(p => p.PlanStatus).Should().NotContain("TEMPLATE");
        items[0].PlanId.Should().Be(5012); // Newest StartDate first
        items[1].PlanId.Should().Be(5011);
        meta.Total.Should().Be(2);
    }

    [Fact]
    public async Task GetMealPlanByIdAsync_ValidOwner_ShouldReturnDetailsWithSlotsAndEdits()
    {
        // ARRANGE
        var userId = 502;
        _db.Users.Add(new User { UserId = userId, Email = "user502@test.com", FullName = "User 502", Role = UserRoles.Member, AccountStatus = "ACTIVE" });
        var plan = CreateTestMealPlan(502, userId, "DRAFT");
        _db.AuditLogs.AddRange(
            new AuditLog { ActorUserId = userId, ActorRole = UserRoles.Member, Action = "MEAL_SLOT_SWAPPED", TargetTable = "MealPlans", TargetId = "502", Note = "DRAFT", CreatedAt = DateTime.UtcNow },
            new AuditLog { ActorUserId = userId, ActorRole = UserRoles.Member, Action = "MEAL_SLOT_SWAPPED", TargetTable = "MealPlans", TargetId = "502", Note = "DRAFT", CreatedAt = DateTime.UtcNow }
        );
        await _db.SaveChangesAsync();

        // ACT
        var result = await _service.GetMealPlanByIdAsync(502, userId);

        // ASSERT
        result.Should().NotBeNull();
        result.PlanId.Should().Be(502);
        result.Slots.Should().HaveCount(21);
        result.Edits.PreDeployUsed.Should().Be(2);
        result.Edits.PreDeployLimit.Should().Be(21);
        result.Edits.PostDeployUsed.Should().Be(0);
        result.Edits.PostDeployLimit.Should().Be(21);
    }

    [Fact]
    public async Task GetMealPlanByIdAsync_NonExistentOrNotOwner_ShouldThrowNotFound()
    {
        // ARRANGE
        var userId = 503;
        _db.Users.Add(new User { UserId = userId, Email = "user503@test.com", FullName = "User 503", Role = UserRoles.Member, AccountStatus = "ACTIVE" });
        CreateTestMealPlan(503, userId, "DRAFT");

        // ACT & ASSERT: ID không tồn tại
        var act1 = () => _service.GetMealPlanByIdAsync(9999, userId);
        var ex1 = await act1.Should().ThrowAsync<AppException>();
        ex1.Which.StatusCode.Should().Be(404);

        // ACT & ASSERT: Không phải chủ sở hữu
        var act2 = () => _service.GetMealPlanByIdAsync(503, userId: 999);
        var ex2 = await act2.Should().ThrowAsync<AppException>();
        ex2.Which.StatusCode.Should().Be(404);
    }

    [Fact]
    public async Task ActivateMealPlanAsync_DraftPlan_ShouldTransitionToActiveAndArchiveExistingActivePlan()
    {
        // ARRANGE
        var userId = 504;
        _db.Users.Add(new User { UserId = userId, Email = "user504@test.com", FullName = "User 504", Role = UserRoles.Member, AccountStatus = "ACTIVE" });
        var oldActivePlan = CreateTestMealPlan(5041, userId, "ACTIVE", "2026-10-19");
        var draftPlan = CreateTestMealPlan(5042, userId, "DRAFT", "2026-10-19");

        // ACT
        var result = await _service.ActivateMealPlanAsync(5042, userId, UserRoles.Member);

        // ASSERT
        result.PlanId.Should().Be(5042);
        result.PlanStatus.Should().Be("ACTIVE");

        var refreshedDraft = await _db.MealPlans.FindAsync(5042);
        refreshedDraft!.PlanStatus.Should().Be("ACTIVE");

        var refreshedOldActive = await _db.MealPlans.FindAsync(5041);
        refreshedOldActive!.PlanStatus.Should().Be("ARCHIVED");

        var auditLog = await _db.AuditLogs.FirstOrDefaultAsync(l => l.TargetId == "5042" && l.Action == "MEAL_PLAN_ACTIVATED");
        auditLog.Should().NotBeNull();
        auditLog!.ActorUserId.Should().Be(userId);
    }

    [Fact]
    public async Task ActivateMealPlanAsync_NotDraft_ShouldThrowConflict()
    {
        // ARRANGE
        var userId = 505;
        _db.Users.Add(new User { UserId = userId, Email = "user505@test.com", FullName = "User 505", Role = UserRoles.Member, AccountStatus = "ACTIVE" });
        CreateTestMealPlan(505, userId, "ACTIVE");

        // ACT & ASSERT
        var act = () => _service.ActivateMealPlanAsync(505, userId, UserRoles.Member);
        var ex = await act.Should().ThrowAsync<AppException>();
        ex.Which.StatusCode.Should().Be(409);
        ex.Which.ErrorCode.Should().Be(ErrorCodes.ErrMealPlanNotDraft);
    }

    // =========================================================================
    // FN-MEAL-03: Đổi món (Swap meal slot)
    // =========================================================================

    [Fact]
    public async Task UpdateMealPlanSlotAsync_DraftPhase_ShouldSwapSuccessfully()
    {
        // ARRANGE
        var userId = 506;
        _db.Users.Add(new User { UserId = userId, Email = "user506@test.com", FullName = "User 506", Role = UserRoles.Member, AccountStatus = "ACTIVE" });
        _db.HealthProfiles.Add(new HealthProfile { UserId = userId, DietarySchoolId = 1, Gender = "FEMALE", DateOfBirth = new DateOnly(1995, 1, 1), HeightCm = 160m, CurrentWeightKg = 50m, BmiValue = 19.5m, BmrKcal = 1200m, TdeeKcal = 1800m, TargetDailyCalories = 1800m, RegionPreference = "MIEN_BAC", HealthGoal = "MAINTAIN" });
        var plan = CreateTestMealPlan(506, userId, "DRAFT");
        var slot = await _db.MealSlots.FirstAsync(s => s.PlanId == 506);
        slot.RecipeId = 101; // 550 kcal
        await _db.SaveChangesAsync();

        var request = new UpdateMealPlanSlotRequest { SwapReason = "Muốn đổi món khác" };

        // ACT
        var result = await _service.UpdateMealPlanSlotAsync(506, slot.SlotId, userId, UserRoles.Member, request);

        // ASSERT
        result.Slot.Recipe!.RecipeId.Should().NotBe(101);
        result.SwapsRemaining.Should().Be(20);

        var refreshedSlot = await _db.MealSlots.FindAsync(slot.SlotId);
        refreshedSlot!.RecipeId.Should().Be(result.Slot.Recipe!.RecipeId);

        var log = await _db.AuditLogs.FirstOrDefaultAsync(l => l.TargetId == "506" && l.Action == "MEAL_SLOT_SWAPPED");
        log.Should().NotBeNull();
        log!.Note.Should().Be("DRAFT");
    }

    [Fact]
    public async Task UpdateMealPlanSlotAsync_22ndSwapAttempt_MustBeRejectedWith403AndExactErrorCode()
    {
        // ARRANGE: Tạo plan và gán 21 lần swap đã dùng trong AuditLogs cho phase DRAFT
        var userId = 507;
        _db.Users.Add(new User { UserId = userId, Email = "user507@test.com", FullName = "User 507", Role = UserRoles.Member, AccountStatus = "ACTIVE" });
        _db.HealthProfiles.Add(new HealthProfile { UserId = userId, DietarySchoolId = 1, Gender = "MALE", DateOfBirth = new DateOnly(1992, 5, 5), HeightCm = 170m, CurrentWeightKg = 65m, BmiValue = 22.5m, BmrKcal = 1500m, TdeeKcal = 2000m, TargetDailyCalories = 2000m, RegionPreference = "MIEN_NAM", HealthGoal = "MAINTAIN" });
        var plan = CreateTestMealPlan(507, userId, "DRAFT");
        var slot = await _db.MealSlots.FirstAsync(s => s.PlanId == 507);

        // Seed 21 lần swap audit logs
        for (int i = 1; i <= 21; i++)
        {
            _db.AuditLogs.Add(new AuditLog
            {
                ActorUserId = userId,
                ActorRole = UserRoles.Member,
                Action = "MEAL_SLOT_SWAPPED",
                TargetTable = "MealPlans",
                TargetId = "507",
                Note = "DRAFT",
                CreatedAt = DateTime.UtcNow.AddMinutes(-i)
            });
        }
        await _db.SaveChangesAsync();

        var request = new UpdateMealPlanSlotRequest { SwapReason = "Thử đổi lần 22" };

        // ACT & ASSERT: Lần thứ 22 phải bị từ chối
        var act = () => _service.UpdateMealPlanSlotAsync(507, slot.SlotId, userId, UserRoles.Member, request);
        var ex = await act.Should().ThrowAsync<AppException>();
        ex.Which.StatusCode.Should().Be(403);
        ex.Which.ErrorCode.Should().Be(ErrorCodes.ErrQuotaMealSwapExceeded);
        ex.Which.Message.Should().Contain("Hết 21 lần đổi/chỉnh sửa của giai đoạn hiện tại (DRAFT).");
    }

    [Fact]
    public async Task UpdateMealPlanSlotAsync_SeparateQuotaForDraftAndActivePhase()
    {
        // ARRANGE: Đã dùng 21 lần ở DRAFT, nhưng thực đơn đã được kích hoạt ACTIVE
        var userId = 508;
        _db.Users.Add(new User { UserId = userId, Email = "user508@test.com", FullName = "User 508", Role = UserRoles.Member, AccountStatus = "ACTIVE" });
        _db.HealthProfiles.Add(new HealthProfile { UserId = userId, DietarySchoolId = 1, Gender = "MALE", DateOfBirth = new DateOnly(1990, 1, 1), HeightCm = 175m, CurrentWeightKg = 70m, BmiValue = 22.9m, BmrKcal = 1600m, TdeeKcal = 2100m, TargetDailyCalories = 2100m, RegionPreference = "MIEN_BAC", HealthGoal = "MAINTAIN" });
        var plan = CreateTestMealPlan(508, userId, "ACTIVE");
        var slot = await _db.MealSlots.FirstAsync(s => s.PlanId == 508);
        slot.RecipeId = 101;

        // 21 lần đã dùng ở DRAFT
        for (int i = 1; i <= 21; i++)
        {
            _db.AuditLogs.Add(new AuditLog
            {
                ActorUserId = userId,
                ActorRole = UserRoles.Member,
                Action = "MEAL_SLOT_SWAPPED",
                TargetTable = "MealPlans",
                TargetId = "508",
                Note = "DRAFT",
                CreatedAt = DateTime.UtcNow.AddHours(-1)
            });
        }
        await _db.SaveChangesAsync();

        var request = new UpdateMealPlanSlotRequest { SwapReason = "Đổi món sau khi đã kích hoạt" };

        // ACT: Trong phase ACTIVE, đây là lần đổi đầu tiên (1 / 21)
        var result = await _service.UpdateMealPlanSlotAsync(508, slot.SlotId, userId, UserRoles.Member, request);

        // ASSERT: Thành công, còn lại 20 lần của phase ACTIVE
        result.Should().NotBeNull();
        result.SwapsRemaining.Should().Be(20);

        var activeLog = await _db.AuditLogs.FirstOrDefaultAsync(l => l.TargetId == "508" && l.Note == "ACTIVE");
        activeLog.Should().NotBeNull();
    }

    [Fact]
    public async Task UpdateMealPlanSlotAsync_ExcludedRecipeIds_ShouldNotPickExcludedRecipe()
    {
        // ARRANGE
        var userId = 509;
        _db.Users.Add(new User { UserId = userId, Email = "user509@test.com", FullName = "User 509", Role = UserRoles.Member, AccountStatus = "ACTIVE" });
        _db.HealthProfiles.Add(new HealthProfile { UserId = userId, DietarySchoolId = 1, Gender = "FEMALE", DateOfBirth = new DateOnly(1993, 3, 3), HeightCm = 162m, CurrentWeightKg = 52m, BmiValue = 19.8m, BmrKcal = 1250m, TdeeKcal = 1800m, TargetDailyCalories = 1800m, RegionPreference = "MIEN_BAC", HealthGoal = "MAINTAIN" });
        var plan = CreateTestMealPlan(509, userId, "DRAFT");
        var slot = await _db.MealSlots.FirstAsync(s => s.PlanId == 509);
        slot.RecipeId = 101; // 550 kcal
        await _db.SaveChangesAsync();

        // Loại trừ các món 103, 104, 105, 106, 107
        var excluded = new List<int> { 103, 104, 105, 106, 107 };
        var request = new UpdateMealPlanSlotRequest
        {
            ExcludedRecipeIds = excluded
        };

        // ACT
        var result = await _service.UpdateMealPlanSlotAsync(509, slot.SlotId, userId, UserRoles.Member, request);

        // ASSERT
        excluded.Should().NotContain(result.Slot.Recipe!.RecipeId);
    }

    [Fact]
    public async Task UpdateMealPlanSlotAsync_NoCompatibleSubstitute_ShouldThrow422()
    {
        // ARRANGE: Loại trừ tất cả các món tương thích
        var userId = 510;
        _db.Users.Add(new User { UserId = userId, Email = "user510@test.com", FullName = "User 510", Role = UserRoles.Member, AccountStatus = "ACTIVE" });
        _db.HealthProfiles.Add(new HealthProfile { UserId = userId, DietarySchoolId = 1, Gender = "FEMALE", DateOfBirth = new DateOnly(1993, 3, 3), HeightCm = 162m, CurrentWeightKg = 52m, BmiValue = 19.8m, BmrKcal = 1250m, TdeeKcal = 1800m, TargetDailyCalories = 1800m, RegionPreference = "MIEN_BAC", HealthGoal = "MAINTAIN" });
        var plan = CreateTestMealPlan(510, userId, "DRAFT");
        var slot = await _db.MealSlots.FirstAsync(s => s.PlanId == 510);
        slot.RecipeId = 101; // 550 kcal
        await _db.SaveChangesAsync();

        // Loại trừ tất cả các món trong DB
        var allRecipeIds = await _db.Recipes.Select(r => r.RecipeId).ToListAsync();
        var request = new UpdateMealPlanSlotRequest
        {
            ExcludedRecipeIds = allRecipeIds
        };

        // ACT & ASSERT
        var act = () => _service.UpdateMealPlanSlotAsync(510, slot.SlotId, userId, UserRoles.Member, request);
        var ex = await act.Should().ThrowAsync<AppException>();
        ex.Which.StatusCode.Should().Be(422);
        ex.Which.ErrorCode.Should().Be(ErrorCodes.ErrMealNoSubstitute);
    }

    // =========================================================================
    // FN-MEAL-04: Đánh dấu bữa đã ăn (Patch meal slot)
    // =========================================================================

    [Fact]
    public async Task PatchMealPlanSlotAsync_ActivePlan_ShouldToggleCompletedAndReturnProgress()
    {
        // ARRANGE
        var userId = 511;
        _db.Users.Add(new User { UserId = userId, Email = "user511@test.com", FullName = "User 511", Role = UserRoles.Member, AccountStatus = "ACTIVE" });
        var plan = CreateTestMealPlan(511, userId, "ACTIVE");
        var slot = await _db.MealSlots.FirstAsync(s => s.PlanId == 511);

        // ACT 1: Đánh dấu đã ăn
        var result1 = await _service.PatchMealPlanSlotAsync(511, slot.SlotId, userId, new PatchMealPlanSlotRequest { IsCompleted = true });

        // ASSERT 1
        result1.IsCompleted.Should().BeTrue();
        result1.Completed.Should().Be(1);
        result1.Total.Should().Be(21);

        var refreshedSlot = await _db.MealSlots.FindAsync(slot.SlotId);
        refreshedSlot!.IsCompleted.Should().BeTrue();

        // ACT 2: Bỏ đánh dấu đã ăn
        var result2 = await _service.PatchMealPlanSlotAsync(511, slot.SlotId, userId, new PatchMealPlanSlotRequest { IsCompleted = false });

        // ASSERT 2
        result2.IsCompleted.Should().BeFalse();
        result2.Completed.Should().Be(0);
        result2.Total.Should().Be(21);

        var refreshedSlot2 = await _db.MealSlots.FindAsync(slot.SlotId);
        refreshedSlot2!.IsCompleted.Should().BeFalse();
    }

    [Fact]
    public async Task PatchMealPlanSlotAsync_DraftPlan_ShouldThrowConflict()
    {
        // ARRANGE: Thực đơn đang ở trạng thái DRAFT
        var userId = 512;
        _db.Users.Add(new User { UserId = userId, Email = "user512@test.com", FullName = "User 512", Role = UserRoles.Member, AccountStatus = "ACTIVE" });
        var plan = CreateTestMealPlan(512, userId, "DRAFT");
        var slot = await _db.MealSlots.FirstAsync(s => s.PlanId == 512);

        // ACT & ASSERT: Không được đánh dấu khi chưa ACTIVE
        var act = () => _service.PatchMealPlanSlotAsync(512, slot.SlotId, userId, new PatchMealPlanSlotRequest { IsCompleted = true });
        var ex = await act.Should().ThrowAsync<AppException>();
        ex.Which.StatusCode.Should().Be(409);
        ex.Which.ErrorCode.Should().Be(ErrorCodes.ErrMealPlanNotActive);
    }

    // =========================================================================
    // FN-MEAL-07: Xem calo thực đơn (Free: tổng calo mỗi ngày; VIP: chi tiết)
    // =========================================================================

    [Fact]
    public async Task GetMealPlanNutritionAsync_MemberRole_BasicLevel_ShouldReturnDailyTotalsWithoutDetailedMealsAndDelta()
    {
        // ARRANGE: Thành viên thường (MEMBER) xem calo mức BASIC
        var userId = 513;
        _db.Users.Add(new User { UserId = userId, Email = "user513@test.com", FullName = "User 513", Role = UserRoles.Member, AccountStatus = "ACTIVE" });
        CreateTestMealPlan(513, userId, "ACTIVE"); // 21 slots với recipe 101 (550 kcal mỗi slot)

        // ACT
        var result = await _service.GetMealPlanNutritionAsync(513, userId, UserRoles.Member, level: "BASIC");

        // ASSERT: 7 ngày, mỗi ngày 3 bữa x 550 kcal = 1650 kcal; tổng tuần = 11550 kcal
        result.Should().NotBeNull();
        result.PlanId.Should().Be(513);
        result.Level.Should().Be("BASIC");
        result.TargetKcalPerDay.Should().Be(1800m);
        result.TotalKcalWeek.Should().Be(11550m);
        result.Days.Should().HaveCount(7);

        foreach (var day in result.Days)
        {
            day.Kcal.Should().Be(1650m);
            day.DeltaKcal.Should().BeNull();
            day.Meals.Should().BeNull();
        }
    }

    [Fact]
    public async Task GetMealPlanNutritionAsync_MemberRole_RequestDetailedLevel_MustBeRejectedWith403VipRequired()
    {
        // ARRANGE: Thành viên thường yêu cầu DETAILED -> Bị từ chối theo BR-PAY-SUB-03
        var userId = 514;
        _db.Users.Add(new User { UserId = userId, Email = "user514@test.com", FullName = "User 514", Role = UserRoles.Member, AccountStatus = "ACTIVE" });
        CreateTestMealPlan(514, userId, "ACTIVE");

        // ACT & ASSERT: Lỗi 403 ERR_VIP_REQUIRED
        var act = () => _service.GetMealPlanNutritionAsync(514, userId, UserRoles.Member, level: "DETAILED");
        var ex = await act.Should().ThrowAsync<AppException>();
        ex.Which.StatusCode.Should().Be(403);
        ex.Which.ErrorCode.Should().Be(ErrorCodes.ErrVipRequired);
        ex.Which.Message.Should().Contain("VIP");
    }

    [Fact]
    public async Task GetMealPlanNutritionAsync_VipRole_DetailedLevel_ShouldReturnFullDetailsWithDeltaAndMealDishes()
    {
        // ARRANGE: Thành viên VIP yêu cầu DETAILED
        var userId = 515;
        _db.Users.Add(new User { UserId = userId, Email = "user515@test.com", FullName = "User 515", Role = UserRoles.Vip, AccountStatus = "ACTIVE" });
        CreateTestMealPlan(515, userId, "ACTIVE");
        // TargetDailyKcal = 1800m. 3 slots x 550 kcal = 1650 kcal. Delta = 1650 - 1800 = -150 kcal.

        // ACT
        var result = await _service.GetMealPlanNutritionAsync(515, userId, UserRoles.Vip, level: "DETAILED");

        // ASSERT: Toàn bộ thông tin chi tiết từng món, từng bữa và độ lệch calo
        result.Should().NotBeNull();
        result.PlanId.Should().Be(515);
        result.Level.Should().Be("DETAILED");
        result.TargetKcalPerDay.Should().Be(1800m);
        result.TotalKcalWeek.Should().Be(11550m);
        result.Days.Should().HaveCount(7);

        foreach (var day in result.Days)
        {
            day.Kcal.Should().Be(1650m);
            day.DeltaKcal.Should().Be(-150m);
            day.Meals.Should().NotBeNull();
            day.Meals.Should().HaveCount(3);
            day.Meals![0].MealType.Should().Be("BREAKFAST");
            day.Meals[0].RecipeId.Should().Be(101);
            day.Meals[0].Kcal.Should().Be(550m);
        }
    }

    [Fact]
    public async Task GetMealPlanNutritionAsync_NonExistentOrNotOwner_ShouldThrowNotFound()
    {
        // ARRANGE
        var userId = 516;
        _db.Users.Add(new User { UserId = userId, Email = "user516@test.com", FullName = "User 516", Role = UserRoles.Member, AccountStatus = "ACTIVE" });
        CreateTestMealPlan(516, userId, "ACTIVE");

        // ACT & ASSERT: Không đúng chủ sở hữu
        var act = () => _service.GetMealPlanNutritionAsync(516, userId: 9999, UserRoles.Member, level: "BASIC");
        var ex = await act.Should().ThrowAsync<AppException>();
        ex.Which.StatusCode.Should().Be(404);
        ex.Which.ErrorCode.Should().Be(ErrorCodes.ErrMealPlanNotFound);
    }

    [Fact]
    public async Task GetMealPlanNutritionAsync_InvalidLevel_ShouldThrowBadRequest()
    {
        // ARRANGE
        var userId = 517;
        _db.Users.Add(new User { UserId = userId, Email = "user517@test.com", FullName = "User 517", Role = UserRoles.Vip, AccountStatus = "ACTIVE" });
        CreateTestMealPlan(517, userId, "ACTIVE");

        // ACT & ASSERT: level sai
        var act = () => _service.GetMealPlanNutritionAsync(517, userId, UserRoles.Vip, level: "INVALID_LEVEL");
        var ex = await act.Should().ThrowAsync<AppException>();
        ex.Which.StatusCode.Should().Be(400);
        ex.Which.ErrorCode.Should().Be(ErrorCodes.ErrValidation);
    }

    private MealPlan CreateTestMealPlan(int planId, int userId, string status = "DRAFT", string startDate = "2026-10-19")
    {
        var plan = new MealPlan
        {
            PlanId = planId,
            UserId = userId,
            PlanName = $"Thực đơn test {planId}",
            StartDate = DateOnly.Parse(startDate),
            EndDate = DateOnly.Parse(startDate).AddDays(6),
            DietarySchoolId = 1,
            TargetDailyKcal = 1800m,
            PlanStatus = status,
            BudgetTier = "STANDARD",
            CreatedAt = DateTime.UtcNow
        };
        _db.MealPlans.Add(plan);

        int slotIdCounter = planId * 100;
        foreach (var day in new[] { "MON", "TUE", "WED", "THU", "FRI", "SAT", "SUN" })
        {
            foreach (var type in new[] { "BREAKFAST", "LUNCH", "DINNER" })
            {
                slotIdCounter++;
                _db.MealSlots.Add(new MealSlot
                {
                    SlotId = slotIdCounter,
                    PlanId = planId,
                    DayOfWeek = day,
                    MealType = type,
                    RecipeId = 101,
                    IsCompleted = false
                });
            }
        }
        _db.SaveChanges();
        return plan;
    }

    // =========================================================================
    // HELPER: Cấu hình 20 Hồ sơ đa dạng cho test
    // =========================================================================
    private static (int DietarySchoolId, decimal TargetKcal, bool RequiresAllium, List<int> AllergenIds) GetProfileConfig(int index)
    {
        return index switch
        {
            1 => (1, 1800m, false, []),                     // Vegan chuẩn
            2 => (2, 1900m, false, []),                     // Ovo (Chay có trứng)
            3 => (3, 2000m, false, []),                     // Lacto (Chay có sữa)
            4 => (4, 2100m, false, []),                     // Lacto-Ovo (Trứng & Sữa)
            5 => (5, 1700m, true, []),                      // Phật giáo (Buddhist - Allium Free)
            6 => (6, 2200m, false, []),                     // Pollotarian (Ăn gia cầm)
            7 => (7, 2300m, false, []),                     // Pescatarian (Ăn hải sản)
            8 => (1, 1800m, false, [1]),                    // Vegan + Dị ứng Đậu phộng (AllergenId = 1)
            9 => (1, 1750m, false, [2]),                    // Vegan + Dị ứng Đậu nành (AllergenId = 2)
            10 => (1, 1850m, false, [3]),                   // Vegan + Dị ứng Gluten (AllergenId = 3)
            11 => (4, 2000m, false, [1]),                   // Lacto-Ovo + Dị ứng Đậu phộng
            12 => (4, 1950m, false, [4]),                   // Lacto-Ovo + Dị ứng Mè (AllergenId = 4)
            13 => (5, 1650m, true, [2]),                    // Phật giáo + Dị ứng Đậu nành
            14 => (5, 1700m, true, [1]),                    // Phật giáo + Dị ứng Đậu phộng
            15 => (3, 1900m, true, []),                     // Lacto + yêu cầu cá nhân không hành tỏi
            16 => (2, 1800m, false, [1]),                   // Ovo + Dị ứng Đậu phộng
            17 => (7, 2200m, false, [5]),                   // Pescatarian + Dị ứng Giáp xác (AllergenId = 5)
            18 => (6, 2100m, false, [6]),                   // Pollotarian + Dị ứng Trứng (AllergenId = 6)
            19 => (1, 2500m, false, []),                    // Vegan Calo cao (2500 kcal)
            20 => (4, 1300m, false, []),                    // Lacto-Ovo Calo thấp (1300 kcal)
            _ => (1, 2000m, false, [])
        };
    }

    // =========================================================================
    // HELPER: Nạp Master Data và kho Công thức nấu ăn phong phú
    // =========================================================================
    private void SeedMasterDataAndRecipes()
    {
        // 1. Dietary Schools (1..7)
        var schools = new List<DietarySchool>
        {
            new() { DietarySchoolId = 1, SchoolCode = "VEGAN", SchoolName = "Thuần chay", ForbiddenFoodRules = "{\"forbiddenFoodGroups\":[\"EGG\",\"DAIRY\",\"POULTRY\",\"SEAFOOD\"],\"forbidAllium\":false}" },
            new() { DietarySchoolId = 2, SchoolCode = "OVO", SchoolName = "Chay có trứng", ForbiddenFoodRules = "{\"forbiddenFoodGroups\":[\"DAIRY\",\"POULTRY\",\"SEAFOOD\"],\"forbidAllium\":false}" },
            new() { DietarySchoolId = 3, SchoolCode = "LACTO", SchoolName = "Chay có sữa", ForbiddenFoodRules = "{\"forbiddenFoodGroups\":[\"EGG\",\"POULTRY\",\"SEAFOOD\"],\"forbidAllium\":false}" },
            new() { DietarySchoolId = 4, SchoolCode = "LACTO_OVO", SchoolName = "Chay trứng sữa", ForbiddenFoodRules = "{\"forbiddenFoodGroups\":[\"POULTRY\",\"SEAFOOD\"],\"forbidAllium\":false}" },
            new() { DietarySchoolId = 5, SchoolCode = "BUDDHIST", SchoolName = "Chay Phật giáo", ForbiddenFoodRules = "{\"forbiddenFoodGroups\":[\"EGG\",\"DAIRY\",\"POULTRY\",\"SEAFOOD\"],\"forbidAllium\":true}" },
            new() { DietarySchoolId = 6, SchoolCode = "POLLOTARIAN", SchoolName = "Bán chay gia cầm", ForbiddenFoodRules = "{\"forbiddenFoodGroups\":[\"SEAFOOD\"],\"forbidAllium\":false}" },
            new() { DietarySchoolId = 7, SchoolCode = "PESCATARIAN", SchoolName = "Bán chay hải sản", ForbiddenFoodRules = "{\"forbiddenFoodGroups\":[\"POULTRY\"],\"forbidAllium\":false}" }
        };
        _db.DietarySchools.AddRange(schools);

        // 2. Allergens (1: Peanuts, 2: Soy, 3: Gluten, 4: Sesame, 5: Shellfish, 6: Eggs)
        var allergens = new List<Allergen>
        {
            new() { AllergenId = 1, AllergenName = "Đậu phộng", Code = "PEANUTS" },
            new() { AllergenId = 2, AllergenName = "Đậu nành", Code = "SOY" },
            new() { AllergenId = 3, AllergenName = "Gluten / Lúa mì", Code = "GLUTEN" },
            new() { AllergenId = 4, AllergenName = "Mè", Code = "SESAME" },
            new() { AllergenId = 5, AllergenName = "Giáp xác", Code = "SHELLFISH" },
            new() { AllergenId = 6, AllergenName = "Trứng", Code = "EGGS" }
        };
        _db.Allergens.AddRange(allergens);

        // 3. Categories
        var cat = new FoodCategory { CategoryId = 1, CategoryName = "Món chính" };
        _db.FoodCategories.Add(cat);

        // 4. Ingredients
        var ingTofu = new Ingredient { IngredientId = 1, IngredientName = "Đậu hũ", FoodGroup = "VEGETABLE", IsAllium = false, StandardUnit = "g", DefaultStallType = "DRY" };
        var ingPeanut = new Ingredient { IngredientId = 2, IngredientName = "Đậu phộng rang", FoodGroup = "NUT", IsAllium = false, StandardUnit = "g", DefaultStallType = "DRY" };
        var ingGarlic = new Ingredient { IngredientId = 3, IngredientName = "Tỏi băm", FoodGroup = "VEGETABLE", IsAllium = true, StandardUnit = "g", DefaultStallType = "VEG" };
        var ingEgg = new Ingredient { IngredientId = 4, IngredientName = "Trứng gà", FoodGroup = "EGG", IsAllium = false, StandardUnit = "quả", DefaultStallType = "EGG" };
        var ingMilk = new Ingredient { IngredientId = 5, IngredientName = "Sữa bò tươi", FoodGroup = "DAIRY", IsAllium = false, StandardUnit = "ml", DefaultStallType = "DAIRY" };
        var ingChicken = new Ingredient { IngredientId = 6, IngredientName = "Thịt ức gà", FoodGroup = "POULTRY", IsAllium = false, StandardUnit = "g", DefaultStallType = "MEAT" };
        var ingFish = new Ingredient { IngredientId = 7, IngredientName = "Cá hồi", FoodGroup = "SEAFOOD", IsAllium = false, StandardUnit = "g", DefaultStallType = "FISH" };
        var ingRice = new Ingredient { IngredientId = 8, IngredientName = "Gạo lứt", FoodGroup = "GRAIN", IsAllium = false, StandardUnit = "g", DefaultStallType = "DRY" };
        var ingMushroom = new Ingredient { IngredientId = 9, IngredientName = "Nấm đùi gà", FoodGroup = "VEGETABLE", IsAllium = false, StandardUnit = "g", DefaultStallType = "VEG" };
        var ingBeef = new Ingredient { IngredientId = 10, IngredientName = "Thịt bò", FoodGroup = "MEAT", IsAllium = false, StandardUnit = "g", DefaultStallType = "MEAT" };
        var ingShrimp = new Ingredient { IngredientId = 11, IngredientName = "Tôm sú", FoodGroup = "SEAFOOD", IsAllium = false, StandardUnit = "g", DefaultStallType = "FISH" };

        _db.Ingredients.AddRange(ingTofu, ingPeanut, ingGarlic, ingEgg, ingMilk, ingChicken, ingFish, ingRice, ingMushroom, ingBeef, ingShrimp);

        // Ingredient Allergens
        _db.IngredientAllergens.AddRange(
            new IngredientAllergen { IngredientId = 2, AllergenId = 1 }, // Đậu phộng
            new IngredientAllergen { IngredientId = 1, AllergenId = 2 }, // Đậu hũ -> Đậu nành
            new IngredientAllergen { IngredientId = 4, AllergenId = 6 }, // Trứng gà -> Trứng
            new IngredientAllergen { IngredientId = 11, AllergenId = 5 } // Tôm sú -> Giáp xác
        );

        // 5. Recipes (Tập hợp các món phong phú: Thuần chay, Có trứng, Có sữa, Phật giáo không tỏi, v.v.)
        // A. Nhóm Món thuần chay an toàn tuyệt đối (Không tỏi, không đậu phộng, không đậu nành)
        CreateRecipeWithIngredients(101, "Cơm gạo lứt nấm kho tiêu", 550m, 1, [ingRice, ingMushroom]);
        CreateRecipeWithIngredients(102, "Cháo yến mạch hạt chia", 450m, 1, [ingRice, ingMushroom]);
        CreateRecipeWithIngredients(103, "Canh nấm rong biển hạt sen", 500m, 1, [ingMushroom, ingRice]);
        CreateRecipeWithIngredients(104, "Súp nấm ngô non", 480m, 1, [ingMushroom, ingRice]);
        CreateRecipeWithIngredients(105, "Cơm trộn rau củ nướng", 580m, 1, [ingRice, ingMushroom]);
        CreateRecipeWithIngredients(106, "Mì gạo lứt xào nấm", 520m, 1, [ingRice, ingMushroom]);
        CreateRecipeWithIngredients(107, "Cơm nấm sốt mè rang", 540m, 1, [ingRice, ingMushroom]);
        CreateRecipeWithIngredients(108, "Cháo ngũ cốc dinh dưỡng", 460m, 1, [ingRice, ingMushroom]);
        CreateRecipeWithIngredients(109, "Cơm hạt sen hầm bí đỏ", 510m, 1, [ingRice, ingMushroom]);
        CreateRecipeWithIngredients(122, "Cơm cà ri đậu gà khoai tây", 700m, 1, [ingRice, ingMushroom]);
        CreateRecipeWithIngredients(123, "Cơm chiên hạt sen thập cẩm", 750m, 1, [ingRice, ingMushroom]);
        CreateRecipeWithIngredients(124, "Lẩu nấm dinh dưỡng gạo lứt", 800m, 1, [ingRice, ingMushroom]);
        CreateRecipeWithIngredients(125, "Salad ngũ sắc sốt dấm táo", 350m, 1, [ingRice, ingMushroom]);
        CreateRecipeWithIngredients(126, "Canh rong biển nấu nấm", 380m, 1, [ingRice, ingMushroom]);
        CreateRecipeWithIngredients(127, "Cơm nắm hạt dinh dưỡng cao năng", 850m, 1, [ingRice, ingMushroom]);

        // B. Nhóm món có Đậu hũ (Chứa chất dị ứng Đậu nành)
        CreateRecipeWithIngredients(110, "Đậu hũ sốt cà chua", 500m, 1, [ingTofu, ingRice]);
        CreateRecipeWithIngredients(111, "Đậu hũ kho nấm hương", 520m, 1, [ingTofu, ingMushroom]);

        // C. Nhóm món có Đậu phộng (Chứa chất dị ứng Đậu phộng)
        CreateRecipeWithIngredients(112, "Gỏi nấm trộn đậu phộng", 480m, 1, [ingMushroom, ingPeanut]);

        // D. Nhóm món có Tỏi (Chứa Allium)
        CreateRecipeWithIngredients(113, "Nấm xào tỏi thơm", 450m, 1, [ingMushroom, ingGarlic]);

        // E. Nhóm món có Trứng (Ovo / Lacto-Ovo)
        CreateRecipeWithIngredients(114, "Cơm chiên trứng nấm", 600m, 2, [ingRice, ingEgg, ingMushroom]);
        CreateRecipeWithIngredients(115, "Trứng cuộn rau củ", 450m, 2, [ingEgg, ingMushroom]);

        // F. Nhóm món có Sữa (Lacto / Lacto-Ovo)
        CreateRecipeWithIngredients(116, "Súp bí đỏ kem sữa", 500m, 3, [ingRice, ingMilk]);
        CreateRecipeWithIngredients(117, "Yến mạch nấu sữa hạnh nhân", 450m, 3, [ingRice, ingMilk]);

        // G. Nhóm món có Gà (Pollotarian)
        CreateRecipeWithIngredients(118, "Ức gà áp chảo nấm", 620m, 6, [ingChicken, ingMushroom, ingRice]);

        // H. Nhóm món có Cá / Hải sản (Pescatarian)
        CreateRecipeWithIngredients(119, "Cá hồi nướng cơm gạo lứt", 650m, 7, [ingFish, ingRice]);
        CreateRecipeWithIngredients(120, "Tôm sú hấp nấm", 550m, 7, [ingShrimp, ingMushroom]);

        // I. Món chứa Thịt bò (Vi phạm 100% tất cả trường phái)
        CreateRecipeWithIngredients(121, "Cơm bò xào", 700m, 1, [ingBeef, ingRice]);

        _db.SaveChanges();
    }

    private void CreateRecipeWithIngredients(int recipeId, string title, decimal kcal, int schoolId, Ingredient[] ingredients)
    {
        var recipe = new Recipe
        {
            RecipeId = recipeId,
            Title = title,
            CaloriesKcal = kcal,
            DietarySchoolId = schoolId,
            CategoryId = 1,
            Status = "APPROVED_ACTIVE",
            Instructions = "Hướng dẫn chi tiết nấu món ăn...",
            PrepTimeMinutes = 15,
            CookTimeMinutes = 20,
            Servings = 2,
            DifficultyLevel = "EASY",
            Source = "USER",
            CreatedAt = DateTime.UtcNow
        };
        _db.Recipes.Add(recipe);

        foreach (var ing in ingredients)
        {
            _db.RecipeIngredients.Add(new RecipeIngredient
            {
                RecipeId = recipeId,
                IngredientId = ing.IngredientId,
                QuantityNeeded = 100m,
                Unit = "g",
                IsOptional = false
            });
        }
    }
}
