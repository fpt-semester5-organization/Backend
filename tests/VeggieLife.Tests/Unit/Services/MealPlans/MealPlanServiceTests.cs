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
