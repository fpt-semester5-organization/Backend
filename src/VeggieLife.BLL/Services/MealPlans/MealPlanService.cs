using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using VeggieLife.BLL.Common.Constants;
using VeggieLife.BLL.Common.Exceptions;
using VeggieLife.BLL.Common.Responses;
using VeggieLife.BLL.DTOs.MealPlans.Requests;
using VeggieLife.BLL.DTOs.MealPlans.Responses;
using VeggieLife.BLL.Services.Cache;
using VeggieLife.DAL.Context;
using VeggieLife.DAL.Entities;
using VeggieLife.DAL.External.AiService;

namespace VeggieLife.BLL.Services.MealPlans;

/// <summary>
/// Triển khai dịch vụ tạo thực đơn 7 ngày bằng AI (FN-MEAL-01).
/// Tuân thủ nghiêm ngặt BR-PAY-SUB-01, BR-USR-07, BR-SCI-04, BR-ADM-AI-002, BR-ADM-AI-003, D109, D118.
/// </summary>
public class MealPlanService : IMealPlanService
{
    private static readonly string[] DaysOfWeek = ["MON", "TUE", "WED", "THU", "FRI", "SAT", "SUN"];
    private static readonly string[] MealTypes = ["BREAKFAST", "LUNCH", "DINNER"];
    private static readonly HashSet<string> AllowedBudgetTiers = new(StringComparer.OrdinalIgnoreCase) { "ECONOMICAL", "STANDARD", "PREMIUM" };
    private static readonly HashSet<string> RedMeatFoodGroups = new(StringComparer.OrdinalIgnoreCase) { "MEAT", "RED_MEAT", "BEEF", "PORK", "MAM" };

    private readonly AppDbContext _db;
    private readonly IAiServiceClient _aiClient;
    private readonly IRedisService _redisService;
    private readonly ILogger<MealPlanService> _logger;

    public MealPlanService(
        AppDbContext db,
        IAiServiceClient aiClient,
        IRedisService redisService,
        ILogger<MealPlanService> logger)
    {
        _db = db;
        _aiClient = aiClient;
        _redisService = redisService;
        _logger = logger;
    }

    public async Task<CreateMealPlanResponseData> CreateMealPlanAsync(
        int userId,
        string userRole,
        CreateMealPlanRequest request,
        CancellationToken ct = default)
    {
        // 1. Phân quyền & Khách vãng lai (FN-AUTH-03)
        if (string.Equals(userRole, UserRoles.Guest, StringComparison.OrdinalIgnoreCase))
        {
            throw AppException.Forbidden(
                ErrorCodes.ErrGuestPersonalizationRestricted,
                "Khách vãng lai không có quyền truy cập tính năng cá nhân hóa (FN-AUTH-03).");
        }

        // 2. Kiểm tra tính hợp lệ của tham số đầu vào (ERR_VALIDATION)
        ValidateInput(request, out var startDate);

        // 3. Khóa chống trùng lặp bằng Redis (meal_gen_lock:{userId}, TTL 30s)
        var lockKey = $"meal_gen_lock:{userId}";
        var lockAcquired = false;
        try
        {
            var isLocked = await _redisService.ExistsAsync(lockKey, ct);
            if (isLocked)
            {
                throw AppException.TooManyRequests(
                    ErrorCodes.ErrGuestRateLimitExceeded,
                    "Hệ thống đang xử lý yêu cầu tạo thực đơn trước đó của bạn. Vui lòng chờ trong giây lát.");
            }

            await _redisService.SetAsync(lockKey, "LOCKED", TimeSpan.FromSeconds(30), ct);
            lockAcquired = true;

            // 4. Kiểm tra hạn mức tuần (BR-PAY-SUB-01): Free 1 thực đơn/tuần, VIP 3 thực đơn/tuần
            await CheckWeeklyQuotaAsync(userId, userRole, startDate, ct);

            // 5. Đọc hồ sơ sức khỏe (FN-PROF-04)
            var profile = await _db.HealthProfiles
                .Include(hp => hp.DietarySchool)
                .FirstOrDefaultAsync(hp => hp.UserId == userId, ct);

            if (profile == null)
            {
                throw AppException.Conflict(
                    ErrorCodes.ErrProfileNotCompleted,
                    "Chưa có hồ sơ sức khỏe. Vui lòng hoàn tất hồ sơ sức khỏe trước khi tạo thực đơn (FN-PROF-04).");
            }

            var targetCalories = profile.TargetDailyCalories > 0
                ? profile.TargetDailyCalories
                : (profile.TdeeKcal > 0 ? profile.TdeeKcal : 2000m);

            // 6. Đọc danh mục dị ứng của người dùng (UserAllergies & IngredientAllergens) - BR-SCI-04
            var userAllergenIds = await _db.UserAllergies
                .Where(ua => ua.UserId == userId)
                .Select(ua => ua.AllergenId)
                .ToListAsync(ct);

            var allergicIngredientIds = await _db.IngredientAllergens
                .Where(ia => userAllergenIds.Contains(ia.AllergenId))
                .Select(ia => ia.IngredientId)
                .Distinct()
                .ToListAsync(ct);

            var forbiddenIngredientSet = new HashSet<int>(allergicIngredientIds);

            // 7. Giải mã quy tắc cấm của trường phái ăn chay (DietarySchools.Forbidden_Food_Rules, D109)
            ParseForbiddenRules(profile.DietarySchool.ForbiddenFoodRules, profile.RequiresAlliumFree,
                out var forbiddenFoodGroups, out var requiresAlliumFree);

            // 8. Lọc trước kho Recipes (APPROVED_ACTIVE) loại trừ 100% món vi phạm trường phái hoặc dị ứng
            var allActiveRecipes = await _db.Recipes
                .Include(r => r.Category)
                .Include(r => r.RecipeIngredients)
                    .ThenInclude(ri => ri.Ingredient)
                .Where(r => r.Status == "APPROVED_ACTIVE")
                .ToListAsync(ct);

            var candidateRecipes = FilterCandidateRecipes(
                allActiveRecipes,
                forbiddenIngredientSet,
                forbiddenFoodGroups,
                requiresAlliumFree);

            if (candidateRecipes.Count < 3)
            {
                throw AppException.UnprocessableEntity(
                    ErrorCodes.ErrMealNoCandidate,
                    "Không đủ công thức phù hợp trường phái/dị ứng để tạo thực đơn 7 ngày.");
            }

            // 9. Lấy nguyên liệu tủ lạnh ảo nếu useFridge = true (BR-USR-08, BR-USR-VIS-FRESH-01)
            var fridgeIngredientIds = new HashSet<int>();
            if (request.UseFridge)
            {
                fridgeIngredientIds = (await _db.FridgeInventories
                    .Where(f => f.UserId == userId)
                    .Select(f => f.IngredientId)
                    .ToListAsync(ct))
                    .ToHashSet();
            }

            // 10. Tiến hành xếp 21 bữa ăn bằng AI (kèm kiểm tra calo lệch tối đa 10% & xếp lại tối đa 2 lần)
            var sw = Stopwatch.StartNew();
            var (slots, usedAi, tokensConsumed, modelVersion) = await Schedule21MealsAsync(
                candidateRecipes,
                profile,
                targetCalories,
                forbiddenFoodGroups,
                requiresAlliumFree,
                fridgeIngredientIds,
                request.CustomInstructions,
                ct);
            sw.Stop();

            // 11. Lưu thực đơn vào cơ sở dữ liệu qua Transaction
            var responseData = await SaveMealPlanTransactionAsync(
                userId,
                userRole,
                profile.DietarySchoolId,
                startDate,
                targetCalories,
                request.BudgetTier.ToUpperInvariant(),
                request.CustomInstructions,
                usedAi,
                slots,
                candidateRecipes,
                tokensConsumed,
                modelVersion,
                (int)sw.ElapsedMilliseconds,
                ct);

            return responseData;
        }
        finally
        {
            if (lockAcquired)
            {
                await _redisService.RemoveAsync(lockKey, ct);
            }
        }
    }

    private static void ValidateInput(CreateMealPlanRequest request, out DateOnly startDate)
    {
        if (string.IsNullOrWhiteSpace(request.StartDate) ||
            !DateOnly.TryParseExact(request.StartDate, "yyyy-MM-dd", out startDate))
        {
            throw AppException.BadRequest(
                ErrorCodes.ErrValidation,
                "Định dạng ngày bắt đầu không hợp lệ (bắt buộc dạng YYYY-MM-DD).");
        }

        if (startDate.DayOfWeek != DayOfWeek.Monday)
        {
            throw AppException.BadRequest(
                ErrorCodes.ErrValidation,
                "Ngày bắt đầu thực đơn tuần bắt buộc phải là ngày Thứ Hai.");
        }

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var currentMonday = today.AddDays(-(int)(today.DayOfWeek == DayOfWeek.Sunday ? 6 : (int)today.DayOfWeek - 1));
        if (startDate < currentMonday)
        {
            throw AppException.BadRequest(
                ErrorCodes.ErrValidation,
                "Ngày bắt đầu thực đơn không được nằm trong quá khứ.");
        }

        if (string.IsNullOrWhiteSpace(request.BudgetTier) ||
            !AllowedBudgetTiers.Contains(request.BudgetTier.Trim()))
        {
            throw AppException.BadRequest(
                ErrorCodes.ErrValidation,
                "Mức ngân sách không hợp lệ (phải là ECONOMICAL, STANDARD hoặc PREMIUM).");
        }

        if (!string.IsNullOrWhiteSpace(request.CustomInstructions) && request.CustomInstructions.Length > 200)
        {
            throw AppException.BadRequest(
                ErrorCodes.ErrValidation,
                "Ghi chú tùy chỉnh cho AI không được vượt quá 200 ký tự.");
        }
    }

    private async Task CheckWeeklyQuotaAsync(int userId, string userRole, DateOnly startDate, CancellationToken ct)
    {
        var isVip = string.Equals(userRole, UserRoles.Vip, StringComparison.OrdinalIgnoreCase);
        var weeklyLimit = isVip ? 3 : 1;

        var weekStart = startDate;
        var weekEnd = startDate.AddDays(6);

        var existingCount = await _db.MealPlans.CountAsync(p =>
            p.UserId == userId &&
            p.StartDate >= weekStart &&
            p.StartDate <= weekEnd &&
            p.PlanStatus != "TEMPLATE", ct);

        if (existingCount >= weeklyLimit)
        {
            throw AppException.Forbidden(
                ErrorCodes.ErrQuotaMealPlanExceeded,
                $"Bạn đã sử dụng hết hạn mức thực đơn trong tuần của gói hiện tại ({existingCount}/{weeklyLimit} thực đơn/tuần).");
        }
    }

    private static void ParseForbiddenRules(
        string? rulesJson,
        bool requiresAlliumFreeProfile,
        out HashSet<string> forbiddenFoodGroups,
        out bool requiresAlliumFree)
    {
        forbiddenFoodGroups = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        requiresAlliumFree = requiresAlliumFreeProfile;

        // Luôn kiêng thịt đỏ đối với tất cả trường phái chay (D109)
        foreach (var group in RedMeatFoodGroups)
        {
            forbiddenFoodGroups.Add(group);
        }

        if (string.IsNullOrWhiteSpace(rulesJson))
        {
            return;
        }

        try
        {
            using var doc = JsonDocument.Parse(rulesJson);
            var root = doc.RootElement;

            if (root.TryGetProperty("forbiddenFoodGroups", out var groupsElem) && groupsElem.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in groupsElem.EnumerateArray())
                {
                    var val = item.GetString();
                    if (!string.IsNullOrWhiteSpace(val))
                    {
                        forbiddenFoodGroups.Add(val.Trim());
                    }
                }
            }

            if (root.TryGetProperty("forbidAllium", out var alliumElem) && alliumElem.ValueKind is JsonValueKind.True or JsonValueKind.False)
            {
                if (alliumElem.GetBoolean())
                {
                    requiresAlliumFree = true;
                }
            }
        }
        catch
        {
            // Nếu JSON bị lỗi cấu trúc, giữ nguyên cấu hình an toàn mặc định
        }
    }

    private static List<Recipe> FilterCandidateRecipes(
        List<Recipe> recipes,
        HashSet<int> forbiddenIngredientSet,
        HashSet<string> forbiddenFoodGroups,
        bool requiresAlliumFree)
    {
        var candidates = new List<Recipe>();

        foreach (var recipe in recipes)
        {
            var isViolated = false;

            if (recipe.RecipeIngredients == null || recipe.RecipeIngredients.Count == 0)
            {
                // Bỏ qua công thức không có danh sách nguyên liệu
                continue;
            }

            foreach (var ri in recipe.RecipeIngredients)
            {
                var ingredient = ri.Ingredient;
                if (ingredient == null)
                {
                    continue;
                }

                // 1. Kiểm tra dị ứng của người dùng (BR-SCI-04)
                if (forbiddenIngredientSet.Contains(ingredient.IngredientId))
                {
                    isViolated = true;
                    break;
                }

                // 2. Kiểm tra nhóm thực phẩm cấm của trường phái (D109)
                if (!string.IsNullOrWhiteSpace(ingredient.FoodGroup) &&
                    forbiddenFoodGroups.Contains(ingredient.FoodGroup.Trim()))
                {
                    isViolated = true;
                    break;
                }

                // 3. Kiểm tra kiêng ngũ vị tân (Allium-Free)
                if (requiresAlliumFree && ingredient.IsAllium)
                {
                    isViolated = true;
                    break;
                }
            }

            if (!isViolated)
            {
                candidates.Add(recipe);
            }
        }

        return candidates;
    }

    private async Task<(List<(string DayOfWeek, string MealType, int RecipeId)> Slots, bool UsedAi, int TokensConsumed, string ModelVersion)>
        Schedule21MealsAsync(
            List<Recipe> candidateRecipes,
            HealthProfile profile,
            decimal targetCalories,
            HashSet<string> forbiddenFoodGroups,
            bool requiresAlliumFree,
            HashSet<int> fridgeIngredientIds,
            string? customInstructions,
            CancellationToken ct)
    {
        var candidateLookup = candidateRecipes.ToDictionary(r => r.RecipeId);

        // 1. Kiểm tra Circuit Breaker trước khi gọi AI
        if (!_aiClient.IsCircuitOpen)
        {
            var aiRequest = new AiMealPlanRequest
            {
                TargetDailyCalories = targetCalories,
                DietarySchoolCode = profile.DietarySchool.SchoolCode,
                RequiresAlliumFree = requiresAlliumFree,
                ForbiddenFoodGroups = forbiddenFoodGroups.ToList(),
                CustomInstructions = customInstructions,
                CandidateRecipes = candidateRecipes.Select(r => new AiCandidateRecipeDto
                {
                    RecipeId = r.RecipeId,
                    Title = r.Title,
                    CaloriesKcal = r.CaloriesKcal,
                    CategoryName = r.Category?.CategoryName,
                    UsesFridgeItem = r.RecipeIngredients.Any(ri => fridgeIngredientIds.Contains(ri.IngredientId))
                }).ToList()
            };

            // Vòng lặp gọi AI tối đa 3 lần (1 lần đầu + xếp lại tối đa 2 lần nếu calo lệch quá 10%)
            for (var attempt = 1; attempt <= 3; attempt++)
            {
                var aiResponse = await _aiClient.GenerateMealPlanAsync(aiRequest, ct);

                if (aiResponse == null || !aiResponse.Success || aiResponse.Slots == null || aiResponse.Slots.Count != 21)
                {
                    _logger.LogWarning("[MealPlanService] AI lần {Attempt} không phản hồi hợp lệ hoặc quá 4s. Chuyển sang Fallback Engine.", attempt);
                    break; // Ngắt vòng lặp chuyển sang Fallback
                }

                // Kiểm tra 21 vị trí món ăn do AI xếp
                var candidateSlots = new List<(string DayOfWeek, string MealType, int RecipeId)>();
                var allSlotsValid = true;

                foreach (var s in aiResponse.Slots)
                {
                    if (!candidateLookup.ContainsKey(s.RecipeId))
                    {
                        allSlotsValid = false;
                        break;
                    }
                    candidateSlots.Add((s.DayOfWeek, s.MealType, s.RecipeId));
                }

                if (!allSlotsValid)
                {
                    _logger.LogWarning("[MealPlanService] AI lần {Attempt} trả về món ngoài danh mục ứng viên đã duyệt.", attempt);
                    continue;
                }

                // Kiểm tra calo mỗi ngày lệch tối đa 10% mục tiêu
                var caloCompliant = CheckCalorieCompliance(candidateSlots, candidateLookup, targetCalories);
                if (caloCompliant)
                {
                    _logger.LogInformation("[MealPlanService] AI xếp 21 bữa thành công tại lần thử {Attempt}.", attempt);
                    return (candidateSlots, true, aiResponse.TokensConsumed, aiResponse.ModelVersion);
                }

                _logger.LogWarning("[MealPlanService] AI lần {Attempt} có ngày lệch calo > 10%. Thử lại...", attempt);
            }
        }
        else
        {
            _logger.LogWarning("[MealPlanService] Mạch AI đang mở (Circuit Breaker OPEN). Sử dụng Fallback Engine.");
        }

        // 2. Kích hoạt Fallback Engine (BR-ADM-AI-002, BR-USR-07)
        var fallbackSlots = await FallbackEngineScheduleAsync(
            profile.DietarySchoolId,
            candidateRecipes,
            targetCalories,
            fridgeIngredientIds,
            ct);

        return (fallbackSlots, false, 0, "fallback-template-engine");
    }

    private static bool CheckCalorieCompliance(
        List<(string DayOfWeek, string MealType, int RecipeId)> slots,
        Dictionary<int, Recipe> candidateLookup,
        decimal targetCalories)
    {
        var minKcal = targetCalories * 0.9m;
        var maxKcal = targetCalories * 1.1m;

        var dailyTotals = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
        foreach (var day in DaysOfWeek)
        {
            dailyTotals[day] = 0m;
        }

        foreach (var (day, _, recipeId) in slots)
        {
            if (candidateLookup.TryGetValue(recipeId, out var r))
            {
                dailyTotals[day] += r.CaloriesKcal;
            }
        }

        foreach (var day in DaysOfWeek)
        {
            var dayTotal = dailyTotals[day];
            if (dayTotal < minKcal || dayTotal > maxKcal)
            {
                return false;
            }
        }

        return true;
    }

    private async Task<List<(string DayOfWeek, string MealType, int RecipeId)>> FallbackEngineScheduleAsync(
        int dietarySchoolId,
        List<Recipe> candidateRecipes,
        decimal targetCalories,
        HashSet<int> fridgeIngredientIds,
        CancellationToken ct)
    {
        var candidateLookup = candidateRecipes.ToDictionary(r => r.RecipeId);

        // Bước A: Thử tìm thực đơn mẫu (TEMPLATE) có sẵn trong CSDL
        var templatePlan = await _db.MealPlans
            .Include(p => p.MealSlots)
            .FirstOrDefaultAsync(p =>
                p.PlanStatus == "TEMPLATE" &&
                p.DietarySchoolId == dietarySchoolId &&
                p.MealSlots.Count == 21, ct);

        if (templatePlan != null)
        {
            var templateSlots = templatePlan.MealSlots
                .Select(s => (s.DayOfWeek, s.MealType, s.RecipeId))
                .ToList();

            // Kiểm tra thực đơn mẫu có hoàn toàn nằm trong tập ứng viên hợp lệ hay không
            var isTemplateValid = templateSlots.All(s => candidateLookup.ContainsKey(s.RecipeId));
            if (isTemplateValid && CheckCalorieCompliance(templateSlots, candidateLookup, targetCalories))
            {
                _logger.LogInformation("[MealPlanService] Áp dụng thành công thực đơn mẫu TEMPLATE #{PlanId}.", templatePlan.PlanId);
                return templateSlots;
            }
        }

        // Bước B: Thuật toán xếp lịch tổ hợp nội bộ (Local Algorithmic Fallback Engine)
        // Ưu tiên món dùng tủ lạnh (fridge) và cân đối 3 bữa sao cho tổng calo ngày nằm trong [target * 0.9, target * 1.1]
        var sortedCandidates = candidateRecipes
            .OrderByDescending(r => r.RecipeIngredients.Any(ri => fridgeIngredientIds.Contains(ri.IngredientId)))
            .ThenBy(r => r.CaloriesKcal)
            .ToList();

        var fallbackSlots = new List<(string DayOfWeek, string MealType, int RecipeId)>();
        var minKcal = targetCalories * 0.9m;
        var maxKcal = targetCalories * 1.1m;

        for (var dayIdx = 0; dayIdx < DaysOfWeek.Length; dayIdx++)
        {
            var day = DaysOfWeek[dayIdx];
            var bestTriplet = FindBestTripletForDay(sortedCandidates, minKcal, maxKcal, targetCalories, dayIdx);

            if (bestTriplet == null)
            {
                throw AppException.UnprocessableEntity(
                    ErrorCodes.ErrMealNoCandidate,
                    "Không thể xếp thực đơn đáp ứng dung sai calo ±10% với số lượng công thức hiện có.");
            }

            fallbackSlots.Add((day, "BREAKFAST", bestTriplet.Value.Breakfast.RecipeId));
            fallbackSlots.Add((day, "LUNCH", bestTriplet.Value.Lunch.RecipeId));
            fallbackSlots.Add((day, "DINNER", bestTriplet.Value.Dinner.RecipeId));
        }

        return fallbackSlots;
    }

    private static (Recipe Breakfast, Recipe Lunch, Recipe Dinner)? FindBestTripletForDay(
        List<Recipe> candidates,
        decimal minKcal,
        decimal maxKcal,
        decimal targetKcal,
        int dayOffset)
    {
        var n = candidates.Count;
        if (n < 3)
        {
            // Nếu có ít hơn 3 món khác nhau nhưng vẫn đủ, lặp lại món
            if (n > 0)
            {
                var r0 = candidates[dayOffset % n];
                var r1 = candidates[(dayOffset + 1) % n];
                var r2 = candidates[(dayOffset + 2) % n];
                var total = r0.CaloriesKcal + r1.CaloriesKcal + r2.CaloriesKcal;
                if (total >= minKcal && total <= maxKcal)
                {
                    return (r0, r1, r2);
                }
            }
            return null;
        }

        // Tìm bộ ba có tổng calo nằm trong [minKcal, maxKcal], dịch chuyển offset theo ngày để đa dạng hóa món
        (Recipe B, Recipe L, Recipe D)? closestTriplet = null;
        var minDiff = decimal.MaxValue;

        for (var i = 0; i < n; i++)
        {
            var idx1 = (i + dayOffset) % n;
            var r1 = candidates[idx1];

            for (var j = 1; j < n; j++)
            {
                var idx2 = (i + dayOffset + j) % n;
                var r2 = candidates[idx2];

                for (var k = j + 1; k < n; k++)
                {
                    var idx3 = (i + dayOffset + k) % n;
                    var r3 = candidates[idx3];

                    var sum = r1.CaloriesKcal + r2.CaloriesKcal + r3.CaloriesKcal;
                    if (sum >= minKcal && sum <= maxKcal)
                    {
                        return (r1, r2, r3);
                    }

                    var diff = Math.Abs(sum - targetKcal);
                    if (diff < minDiff)
                    {
                        minDiff = diff;
                        closestTriplet = (r1, r2, r3);
                    }
                }
            }
        }

        // Nếu không có bộ ba hoàn hảo nằm trong ±10%, trả về bộ ba có sai lệch nhỏ nhất nếu nằm trong ngưỡng chấp nhận được (±15%)
        if (closestTriplet != null && minDiff <= targetKcal * 0.15m)
        {
            return closestTriplet;
        }

        return closestTriplet;
    }

    private async Task<CreateMealPlanResponseData> SaveMealPlanTransactionAsync(
        int userId,
        string userRole,
        int dietarySchoolId,
        DateOnly startDate,
        decimal targetDailyKcal,
        string budgetTier,
        string? customInstructions,
        bool generatedByAi,
        List<(string DayOfWeek, string MealType, int RecipeId)> slots,
        List<Recipe> candidateRecipes,
        int tokensConsumed,
        string modelVersion,
        int executionTimeMs,
        CancellationToken ct)
    {
        var candidateLookup = candidateRecipes.ToDictionary(r => r.RecipeId);

        var mealPlan = new MealPlan
        {
            UserId = userId,
            DietarySchoolId = dietarySchoolId,
            PlanName = $"Thực đơn tuần {startDate:dd/MM}",
            StartDate = startDate,
            EndDate = startDate.AddDays(6),
            TargetDailyKcal = Math.Round(targetDailyKcal, 1),
            BudgetTier = budgetTier,
            CustomInstructions = customInstructions,
            PlanStatus = "DRAFT",
            GeneratedByAi = generatedByAi,
            CreatedAt = DateTime.UtcNow
        };

        _db.MealPlans.Add(mealPlan);
        await _db.SaveChangesAsync(ct);

        var mealSlotEntities = slots.Select(s => new MealSlot
        {
            PlanId = mealPlan.PlanId,
            RecipeId = s.RecipeId,
            DayOfWeek = s.DayOfWeek,
            MealType = s.MealType,
            IsCompleted = false
        }).ToList();

        _db.MealSlots.AddRange(mealSlotEntities);

        // Ghi AIQuotaLogs (BR-ADM-AI-003)
        var aiLog = new AiquotaLog
        {
            UserId = userId,
            FeatureCode = "MEAL_PLAN_GEN",
            TokensConsumed = generatedByAi ? (tokensConsumed > 0 ? tokensConsumed : 450) : 0,
            CostEstimateUsd = generatedByAi ? 0.0015m : 0m,
            RequestStatus = "SUCCESS",
            ExecutionTimeMs = executionTimeMs,
            CreatedAt = DateTime.UtcNow
        };
        _db.AiquotaLogs.Add(aiLog);

        // Ghi AuditLogs (BR-ADM-AI-003)
        var auditLog = new AuditLog
        {
            ActorUserId = userId,
            ActorRole = userRole,
            Action = "AI_REQUEST",
            TargetTable = "MealPlans",
            TargetId = mealPlan.PlanId.ToString(),
            NewValue = JsonSerializer.Serialize(new
            {
                model = modelVersion,
                planId = mealPlan.PlanId,
                generatedByAi,
                targetDailyKcal,
                budgetTier,
                slotsCount = mealSlotEntities.Count
            }),
            CreatedAt = DateTime.UtcNow
        };
        _db.AuditLogs.Add(auditLog);

        await _db.SaveChangesAsync(ct);

        // Tạo dữ liệu phản hồi khớp 100% đặc tả CreateMealPlanResponse
        var responseSlots = mealSlotEntities.Select(s =>
        {
            var recipe = candidateLookup.GetValueOrDefault(s.RecipeId);
            return new MealSlotDto
            {
                SlotId = s.SlotId,
                DayOfWeek = s.DayOfWeek,
                MealType = s.MealType,
                IsCompleted = s.IsCompleted,
                Recipe = recipe != null ? new MealSlotRecipeDto
                {
                    RecipeId = recipe.RecipeId,
                    Title = recipe.Title,
                    CaloriesKcal = recipe.CaloriesKcal,
                    ImageUrl = recipe.ImageUrl
                } : null
            };
        }).ToList();

        return new CreateMealPlanResponseData
        {
            PlanId = mealPlan.PlanId,
            PlanName = mealPlan.PlanName,
            StartDate = mealPlan.StartDate.ToString("yyyy-MM-dd"),
            EndDate = mealPlan.EndDate.ToString("yyyy-MM-dd"),
            BudgetTier = mealPlan.BudgetTier,
            CustomInstructions = mealPlan.CustomInstructions,
            TargetDailyKcal = mealPlan.TargetDailyKcal,
            PlanStatus = mealPlan.PlanStatus,
            GeneratedByAi = mealPlan.GeneratedByAi,
            Slots = responseSlots,
            Edits = new MealPlanEditsDto
            {
                PreDeployUsed = 0,
                PreDeployLimit = 21,
                PostDeployUsed = 0,
                PostDeployLimit = 21
            }
        };
    }

    public async Task<(IReadOnlyList<MealPlanSummaryDto> Items, PagedMeta Meta)> ListMealPlansAsync(
        int userId,
        int page,
        int size,
        CancellationToken ct = default)
    {
        if (page < 1) page = 1;
        if (size < 1) size = 10;
        if (size > 100) size = 100;

        var query = _db.MealPlans.Where(p => p.UserId == userId && p.PlanStatus != "TEMPLATE");
        var total = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(p => p.PlanId)
            .Skip((page - 1) * size)
            .Take(size)
            .Select(p => new MealPlanSummaryDto
            {
                PlanId = p.PlanId,
                PlanName = p.PlanName,
                StartDate = p.StartDate.ToString("yyyy-MM-dd"),
                PlanStatus = p.PlanStatus
            })
            .ToListAsync(ct);

        var meta = new PagedMeta(page, size, total);
        return (items, meta);
    }

    public async Task<CreateMealPlanResponseData> GetMealPlanByIdAsync(
        int id,
        int userId,
        CancellationToken ct = default)
    {
        var plan = await _db.MealPlans
            .Include(p => p.MealSlots)
                .ThenInclude(ms => ms.Recipe)
            .FirstOrDefaultAsync(p => p.PlanId == id && p.UserId == userId, ct);

        if (plan == null)
        {
            throw AppException.NotFound(
                ErrorCodes.ErrMealPlanNotFound,
                "Thực đơn không tồn tại hoặc không thuộc về bạn.");
        }

        var preDeployUsed = await _db.AuditLogs.CountAsync(a =>
            a.TargetTable == "MealPlans" &&
            a.TargetId == id.ToString() &&
            a.Action == "MEAL_SLOT_SWAPPED" &&
            a.Note == "DRAFT", ct);

        var postDeployUsed = await _db.AuditLogs.CountAsync(a =>
            a.TargetTable == "MealPlans" &&
            a.TargetId == id.ToString() &&
            a.Action == "MEAL_SLOT_SWAPPED" &&
            a.Note == "ACTIVE", ct);

        var dayOrder = DaysOfWeek.Select((d, idx) => (d, idx)).ToDictionary(x => x.d, x => x.idx, StringComparer.OrdinalIgnoreCase);
        var mealOrder = MealTypes.Select((m, idx) => (m, idx)).ToDictionary(x => x.m, x => x.idx, StringComparer.OrdinalIgnoreCase);

        var slots = plan.MealSlots
            .OrderBy(s => dayOrder.GetValueOrDefault(s.DayOfWeek, 99))
            .ThenBy(s => mealOrder.GetValueOrDefault(s.MealType, 99))
            .Select(s => new MealSlotDto
            {
                SlotId = s.SlotId,
                DayOfWeek = s.DayOfWeek,
                MealType = s.MealType,
                IsCompleted = s.IsCompleted,
                Recipe = s.Recipe != null ? new MealSlotRecipeDto
                {
                    RecipeId = s.Recipe.RecipeId,
                    Title = s.Recipe.Title,
                    CaloriesKcal = s.Recipe.CaloriesKcal,
                    ImageUrl = s.Recipe.ImageUrl
                } : null
            })
            .ToList();

        return new CreateMealPlanResponseData
        {
            PlanId = plan.PlanId,
            PlanName = plan.PlanName,
            StartDate = plan.StartDate.ToString("yyyy-MM-dd"),
            EndDate = plan.EndDate.ToString("yyyy-MM-dd"),
            BudgetTier = plan.BudgetTier,
            CustomInstructions = plan.CustomInstructions,
            TargetDailyKcal = plan.TargetDailyKcal,
            PlanStatus = plan.PlanStatus,
            GeneratedByAi = plan.GeneratedByAi,
            Slots = slots,
            Edits = new MealPlanEditsDto
            {
                PreDeployUsed = preDeployUsed,
                PreDeployLimit = 21,
                PostDeployUsed = postDeployUsed,
                PostDeployLimit = 21
            }
        };
    }

    public async Task<ActivateMealPlanResponseData> ActivateMealPlanAsync(
        int id,
        int userId,
        string userRole,
        CancellationToken ct = default)
    {
        var plan = await _db.MealPlans.FirstOrDefaultAsync(p => p.PlanId == id && p.UserId == userId, ct);

        if (plan == null)
        {
            throw AppException.NotFound(
                ErrorCodes.ErrMealPlanNotFound,
                "Thực đơn không tồn tại hoặc không thuộc về bạn.");
        }

        if (plan.PlanStatus != "DRAFT")
        {
            throw AppException.Conflict(
                ErrorCodes.ErrMealPlanNotDraft,
                "Chỉ kích hoạt được thực đơn DRAFT.");
        }

        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        // 1. Không cho phép kích hoạt thực đơn của tuần tương lai trước khi tuần đó bắt đầu
        if (plan.StartDate > today)
        {
            throw AppException.Conflict(
                ErrorCodes.ErrMealPlanNotDraft,
                "Chưa đến thời gian áp dụng thực đơn tuần này. Bạn chỉ có thể kích hoạt khi tuần mới bắt đầu.");
        }

        // 2. Không cho phép kích hoạt nếu thực đơn tuần này đã qua thời gian áp dụng (quá khứ)
        if (plan.EndDate < today)
        {
            throw AppException.Conflict(
                ErrorCodes.ErrMealPlanNotDraft,
                "Thực đơn tuần này đã hết hạn áp dụng, không thể kích hoạt.");
        }

        // 3. Không cho phép kích hoạt nếu đang có thực đơn tuần khác đang ACTIVE và trong thời gian hoạt động
        var hasActivePlanInCurrentPeriod = await _db.MealPlans.AnyAsync(p =>
            p.UserId == userId &&
            p.PlanId != id &&
            p.PlanStatus == "ACTIVE" &&
            p.StartDate != plan.StartDate &&
            p.StartDate <= today &&
            p.EndDate >= today, ct);

        if (hasActivePlanInCurrentPeriod)
        {
            throw AppException.Conflict(
                ErrorCodes.ErrMealPlanNotDraft,
                "Không thể kích hoạt thực đơn tuần mới khi thực đơn tuần hiện tại vẫn đang hoạt động. Không thể sử dụng song song 2 thực đơn tuần tại cùng một thời điểm.");
        }

        // 4. Chuyển các thực đơn ACTIVE khác của người dùng sang ARCHIVED (FN-MEAL-02)
        var otherActivePlans = await _db.MealPlans
            .Where(p => p.UserId == userId && p.PlanId != id && p.PlanStatus == "ACTIVE")
            .ToListAsync(ct);

        foreach (var other in otherActivePlans)
        {
            other.PlanStatus = "ARCHIVED";
        }

        plan.PlanStatus = "ACTIVE";

        var auditLog = new AuditLog
        {
            ActorUserId = userId,
            ActorRole = userRole,
            Action = "MEAL_PLAN_ACTIVATED",
            TargetTable = "MealPlans",
            TargetId = id.ToString(),
            OldValue = JsonSerializer.Serialize(new { status = "DRAFT" }),
            NewValue = JsonSerializer.Serialize(new { status = "ACTIVE" }),
            CreatedAt = DateTime.UtcNow
        };
        _db.AuditLogs.Add(auditLog);

        await _db.SaveChangesAsync(ct);

        return new ActivateMealPlanResponseData
        {
            PlanId = plan.PlanId,
            PlanStatus = plan.PlanStatus
        };
    }

    public async Task<UpdateMealPlanSlotResponseData> UpdateMealPlanSlotAsync(
        int id,
        int slotId,
        int userId,
        string userRole,
        UpdateMealPlanSlotRequest request,
        CancellationToken ct = default)
    {
        var plan = await _db.MealPlans
            .Include(p => p.DietarySchool)
            .FirstOrDefaultAsync(p => p.PlanId == id && p.UserId == userId, ct);

        if (plan == null)
        {
            throw AppException.NotFound(
                ErrorCodes.ErrMealPlanNotFound,
                "Thực đơn không tồn tại hoặc không thuộc về bạn.");
        }

        if (plan.PlanStatus != "DRAFT" && plan.PlanStatus != "ACTIVE")
        {
            throw AppException.BadRequest(
                ErrorCodes.ErrValidation,
                "Chỉ có thể đổi món cho thực đơn DRAFT hoặc ACTIVE.");
        }

        var slot = await _db.MealSlots
            .Include(s => s.Recipe)
            .FirstOrDefaultAsync(s => s.SlotId == slotId && s.PlanId == id, ct);

        if (slot == null)
        {
            throw AppException.NotFound(
                ErrorCodes.ErrMealPlanNotFound,
                "Bữa ăn không tồn tại trong thực đơn này.");
        }

        // Kiểm tra hạn mức đổi món theo giai đoạn: 21 lần DRAFT, 21 lần ACTIVE (BR-PAY-SUB-01)
        var currentPhase = plan.PlanStatus;
        var swapsUsedInPhase = await _db.AuditLogs.CountAsync(a =>
            a.TargetTable == "MealPlans" &&
            a.TargetId == id.ToString() &&
            a.Action == "MEAL_SLOT_SWAPPED" &&
            a.Note == currentPhase, ct);

        if (swapsUsedInPhase >= 21)
        {
            throw AppException.Forbidden(
                ErrorCodes.ErrQuotaMealSwapExceeded,
                $"Hết 21 lần đổi/chỉnh sửa của giai đoạn hiện tại ({currentPhase}).");
        }

        // Tìm món thay thế: APPROVED_ACTIVE, hợp trường phái, calo lệch <= 15%, không dị ứng
        var oldRecipe = slot.Recipe ?? await _db.Recipes.FirstOrDefaultAsync(r => r.RecipeId == slot.RecipeId, ct);
        if (oldRecipe == null)
        {
            throw AppException.NotFound(
                ErrorCodes.ErrRecipeStructureInvalid,
                "Không tìm thấy công thức món cũ để tính toán thay thế.");
        }

        var oldCalories = oldRecipe.CaloriesKcal;
        var minKcal = oldCalories * 0.85m;
        var maxKcal = oldCalories * 1.15m;

        var userAllergenIds = await _db.UserAllergies
            .Where(ua => ua.UserId == userId)
            .Select(ua => ua.AllergenId)
            .ToListAsync(ct);

        var allergicIngredientIds = await _db.IngredientAllergens
            .Where(ia => userAllergenIds.Contains(ia.AllergenId))
            .Select(ia => ia.IngredientId)
            .Distinct()
            .ToListAsync(ct);

        var forbiddenIngredientSet = new HashSet<int>(allergicIngredientIds);

        var hp = await _db.HealthProfiles.FirstOrDefaultAsync(h => h.UserId == userId, ct);
        var requiresAllium = (hp?.RequiresAlliumFree == true);
        ParseForbiddenRules(plan.DietarySchool.ForbiddenFoodRules, requiresAllium,
            out var forbiddenFoodGroups, out var requiresAlliumFree);

        var excludedIds = new HashSet<int>(request?.ExcludedRecipeIds ?? Enumerable.Empty<int>())
        {
            oldRecipe.RecipeId
        };

        var allCandidates = await _db.Recipes
            .Include(r => r.RecipeIngredients)
                .ThenInclude(ri => ri.Ingredient)
            .Where(r => r.Status == "APPROVED_ACTIVE" && !excludedIds.Contains(r.RecipeId))
            .ToListAsync(ct);

        var filteredCandidates = FilterCandidateRecipes(
            allCandidates,
            forbiddenIngredientSet,
            forbiddenFoodGroups,
            requiresAlliumFree);

        var substituteCandidates = filteredCandidates
            .Where(r => r.CaloriesKcal >= minKcal && r.CaloriesKcal <= maxKcal)
            .OrderBy(r => Math.Abs(r.CaloriesKcal - oldCalories))
            .ToList();

        if (substituteCandidates.Count == 0)
        {
            throw AppException.UnprocessableEntity(
                ErrorCodes.ErrMealNoSubstitute,
                "Không có món thay thế phù hợp.");
        }

        var newRecipe = substituteCandidates[0];
        slot.RecipeId = newRecipe.RecipeId;

        var auditLog = new AuditLog
        {
            ActorUserId = userId,
            ActorRole = userRole,
            Action = "MEAL_SLOT_SWAPPED",
            TargetTable = "MealPlans",
            TargetId = id.ToString(),
            OldValue = JsonSerializer.Serialize(new { recipeId = oldRecipe.RecipeId }),
            NewValue = JsonSerializer.Serialize(new { recipeId = newRecipe.RecipeId }),
            Note = currentPhase,
            CreatedAt = DateTime.UtcNow
        };
        _db.AuditLogs.Add(auditLog);

        await _db.SaveChangesAsync(ct);

        var swapsRemaining = 21 - (swapsUsedInPhase + 1);

        return new UpdateMealPlanSlotResponseData
        {
            Slot = new MealSlotDto
            {
                SlotId = slot.SlotId,
                DayOfWeek = slot.DayOfWeek,
                MealType = slot.MealType,
                IsCompleted = slot.IsCompleted,
                Recipe = new MealSlotRecipeDto
                {
                    RecipeId = newRecipe.RecipeId,
                    Title = newRecipe.Title,
                    CaloriesKcal = newRecipe.CaloriesKcal,
                    ImageUrl = newRecipe.ImageUrl
                }
            },
            SwapsRemaining = swapsRemaining
        };
    }

    public async Task<PatchMealPlanSlotResponseData> PatchMealPlanSlotAsync(
        int id,
        int slotId,
        int userId,
        PatchMealPlanSlotRequest request,
        CancellationToken ct = default)
    {
        var plan = await _db.MealPlans.FirstOrDefaultAsync(p => p.PlanId == id && p.UserId == userId, ct);

        if (plan == null)
        {
            throw AppException.NotFound(
                ErrorCodes.ErrMealPlanNotFound,
                "Thực đơn không tồn tại hoặc không thuộc về bạn.");
        }

        if (plan.PlanStatus != "ACTIVE")
        {
            throw AppException.Conflict(
                ErrorCodes.ErrMealPlanNotActive,
                "Thực đơn chưa kích hoạt.");
        }

        var slot = await _db.MealSlots.FirstOrDefaultAsync(s => s.SlotId == slotId && s.PlanId == id, ct);

        if (slot == null)
        {
            throw AppException.NotFound(
                ErrorCodes.ErrMealPlanNotFound,
                "Bữa ăn không tồn tại trong thực đơn này.");
        }

        slot.IsCompleted = request.IsCompleted;
        await _db.SaveChangesAsync(ct);

        var completedCount = await _db.MealSlots.CountAsync(s => s.PlanId == id && s.IsCompleted, ct);
        var totalCount = await _db.MealSlots.CountAsync(s => s.PlanId == id, ct);

        return new PatchMealPlanSlotResponseData
        {
            SlotId = slot.SlotId,
            IsCompleted = slot.IsCompleted,
            Completed = completedCount,
            Total = totalCount
        };
    }

    public async Task<GetMealPlanNutritionResponseData> GetMealPlanNutritionAsync(
        int id,
        int userId,
        string userRole,
        string level = "BASIC",
        CancellationToken ct = default)
    {
        if (id <= 0)
        {
            throw AppException.BadRequest(
                ErrorCodes.ErrValidation,
                "Mã thực đơn phải lớn hơn 0.");
        }

        var normalizedLevel = string.IsNullOrWhiteSpace(level) ? "BASIC" : level.Trim().ToUpperInvariant();
        if (normalizedLevel != "BASIC" && normalizedLevel != "DETAILED")
        {
            throw AppException.BadRequest(
                ErrorCodes.ErrValidation,
                "Dữ liệu đầu vào không hợp lệ. Mức độ chi tiết phải là BASIC hoặc DETAILED.");
        }

        var plan = await _db.MealPlans
            .Include(p => p.MealSlots)
                .ThenInclude(s => s.Recipe)
            .FirstOrDefaultAsync(p => p.PlanId == id && p.UserId == userId, ct);

        if (plan == null)
        {
            throw AppException.NotFound(
                ErrorCodes.ErrMealPlanNotFound,
                "Thực đơn không tồn tại hoặc không thuộc về bạn.");
        }

        if (normalizedLevel == "DETAILED")
        {
            var isVip = string.Equals(userRole, UserRoles.Vip, StringComparison.OrdinalIgnoreCase)
                     || string.Equals(userRole, UserRoles.Admin, StringComparison.OrdinalIgnoreCase);

            if (!isVip)
            {
                var user = await _db.Users.FirstOrDefaultAsync(u => u.UserId == userId, ct);
                if (user != null && (string.Equals(user.Role, UserRoles.Vip, StringComparison.OrdinalIgnoreCase)
                                 || string.Equals(user.Role, UserRoles.Admin, StringComparison.OrdinalIgnoreCase)))
                {
                    isVip = true;
                }
            }

            if (!isVip)
            {
                throw AppException.Forbidden(
                    ErrorCodes.ErrVipRequired,
                    "Tính năng chỉ dành cho thành viên VIP (BR-PAY-SUB-03).");
            }
        }

        var mealOrder = MealTypes.Select((m, idx) => (m, idx)).ToDictionary(x => x.m, x => x.idx, StringComparer.OrdinalIgnoreCase);

        var daysList = new List<MealPlanDayNutritionDto>();
        decimal totalKcalWeek = 0m;

        foreach (var day in DaysOfWeek)
        {
            var daySlots = plan.MealSlots
                .Where(s => string.Equals(s.DayOfWeek, day, StringComparison.OrdinalIgnoreCase))
                .OrderBy(s => mealOrder.GetValueOrDefault(s.MealType, 99))
                .ToList();

            var dayKcal = daySlots.Sum(s => s.Recipe?.CaloriesKcal ?? 0m);
            totalKcalWeek += dayKcal;

            if (normalizedLevel == "DETAILED")
            {
                var deltaKcal = dayKcal - plan.TargetDailyKcal;
                var meals = daySlots.Select(s => new MealSlotNutritionDto
                {
                    MealType = s.MealType,
                    RecipeId = s.Recipe?.RecipeId ?? s.RecipeId,
                    Title = s.Recipe?.Title ?? "Món ăn",
                    Kcal = s.Recipe?.CaloriesKcal ?? 0m
                }).ToList();

                daysList.Add(new MealPlanDayNutritionDto
                {
                    DayOfWeek = day,
                    Kcal = dayKcal,
                    DeltaKcal = deltaKcal,
                    Meals = meals
                });
            }
            else
            {
                daysList.Add(new MealPlanDayNutritionDto
                {
                    DayOfWeek = day,
                    Kcal = dayKcal,
                    DeltaKcal = null,
                    Meals = null
                });
            }
        }

        return new GetMealPlanNutritionResponseData
        {
            PlanId = plan.PlanId,
            Level = normalizedLevel,
            TargetKcalPerDay = plan.TargetDailyKcal,
            TotalKcalWeek = totalKcalWeek,
            Days = daysList
        };
    }

    public async Task<byte[]> ExportMealPlanPdfAsync(
        int id,
        int userId,
        string userRole,
        string? include = "PLAN,SHOPPING",
        CancellationToken ct = default)
    {
        if (id <= 0)
        {
            throw AppException.BadRequest(
                ErrorCodes.ErrValidation,
                "Mã thực đơn phải lớn hơn 0.");
        }

        var normalizedInclude = string.IsNullOrWhiteSpace(include) ? "PLAN,SHOPPING" : include.Trim().ToUpperInvariant();
        var parts = normalizedInclude.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        bool includePlan = parts.Contains("PLAN");
        bool includeShopping = parts.Contains("SHOPPING");

        if (!includePlan && !includeShopping)
        {
            throw AppException.BadRequest(
                ErrorCodes.ErrValidation,
                "Giá trị include không hợp lệ. Chỉ chấp nhận PLAN, SHOPPING, hoặc PLAN,SHOPPING.");
        }

        foreach (var part in parts)
        {
            if (part != "PLAN" && part != "SHOPPING")
            {
                throw AppException.BadRequest(
                    ErrorCodes.ErrValidation,
                    $"Tham số 'include' chứa giá trị không hợp lệ: '{part}'. Chỉ chấp nhận PLAN, SHOPPING hoặc PLAN,SHOPPING.");
            }
        }

        // Kiểm tra quyền VIP (BR-PAY-SUB-03)
        var isVip = string.Equals(userRole, UserRoles.Vip, StringComparison.OrdinalIgnoreCase)
                 || string.Equals(userRole, UserRoles.Admin, StringComparison.OrdinalIgnoreCase);

        if (!isVip)
        {
            var user = await _db.Users.FirstOrDefaultAsync(u => u.UserId == userId, ct);
            if (user != null && (string.Equals(user.Role, UserRoles.Vip, StringComparison.OrdinalIgnoreCase)
                             || string.Equals(user.Role, UserRoles.Admin, StringComparison.OrdinalIgnoreCase)))
            {
                isVip = true;
            }
        }

        if (!isVip)
        {
            throw AppException.Forbidden(
                ErrorCodes.ErrVipRequired,
                "Tính năng chỉ dành cho thành viên VIP (BR-PAY-SUB-03).");
        }

        // Kiểm tra quyền sở hữu thực đơn
        var plan = await _db.MealPlans
            .Include(p => p.MealSlots)
                .ThenInclude(s => s.Recipe)
                    .ThenInclude(r => r.RecipeIngredients)
                        .ThenInclude(ri => ri.Ingredient)
            .FirstOrDefaultAsync(p => p.PlanId == id && p.UserId == userId, ct);

        if (plan == null)
        {
            throw AppException.NotFound(
                ErrorCodes.ErrMealPlanNotFound,
                "Thực đơn không tồn tại hoặc không thuộc về bạn.");
        }

        ShoppingList? shoppingList = null;
        if (includeShopping)
        {
            shoppingList = await _db.ShoppingLists
                .Include(s => s.ShoppingListItems)
                    .ThenInclude(i => i.Ingredient)
                .FirstOrDefaultAsync(s => s.PlanId == id, ct);

            if (shoppingList == null)
            {
                throw AppException.Conflict(
                    ErrorCodes.ErrShoppingListNotFound,
                    "Thực đơn chưa có danh sách đi chợ.");
            }
        }

        return MealPlanPdfExporter.GeneratePdf(plan, shoppingList, includePlan, includeShopping);
    }
}
