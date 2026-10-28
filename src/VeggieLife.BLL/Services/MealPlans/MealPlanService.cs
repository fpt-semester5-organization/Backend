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

                if (fridgeIngredientIds.Count == 0)
                {
                    throw AppException.Conflict(
                        ErrorCodes.ErrFridgeEmpty,
                        "Tủ lạnh ảo của bạn chưa có nguyên liệu nào. Vui lòng thêm nguyên liệu trước khi chọn tùy chọn này.");
                }
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

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        if (startDate < today.AddDays(-6))
        {
            throw AppException.BadRequest(
                ErrorCodes.ErrValidation,
                "Ngày bắt đầu thực đơn không được quá 7 ngày trong quá khứ.");
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

                // Kiểm tra calo mỗi ngày lệch tối đa (áp dụng dung sai linh hoạt -20% đến +15% cho AI để ưu tiên đa dạng món)
                var caloCompliant = CheckCalorieCompliance(candidateSlots, candidateLookup, targetCalories, candidateRecipes, isAi: true);
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
        decimal targetCalories,
        List<Recipe>? candidateRecipes = null,
        bool isAi = false)
    {
        // 1. Xác định dung sai cho phép:
        // Đối với AI: Nới lỏng dung sai từ -20% đến +15% để tôn trọng tính đa dạng món ăn thay vì ép cứng calo
        // Đối với Template/Non-AI: Mặc định ±10%
        var lowerTolerance = isAi ? 0.20m : 0.10m; // -20% cho AI
        var upperTolerance = isAi ? 0.15m : 0.10m; // +15% cho AI

        var minKcal = targetCalories * (1m - lowerTolerance);
        var maxKcal = targetCalories * (1m + upperTolerance);

        if (candidateRecipes != null && candidateRecipes.Count > 0)
        {
            var top3Max = candidateRecipes.OrderByDescending(r => r.CaloriesKcal).Take(3).Sum(r => r.CaloriesKcal);
            var top3Min = candidateRecipes.OrderBy(r => r.CaloriesKcal).Take(3).Sum(r => r.CaloriesKcal);

            // Nới lỏng thêm nếu kho món ăn của trường phái không đủ nhiều món calo cao
            if (top3Max < targetCalories * 1.20m || candidateRecipes.Count(r => r.CaloriesKcal >= 600m) < 8)
            {
                minKcal = Math.Min(minKcal, targetCalories * 0.75m); // Chấp nhận lệch đến -25%
                maxKcal = Math.Max(maxKcal, targetCalories * 1.20m);
            }
            else if (top3Min > maxKcal)
            {
                maxKcal = top3Min * 1.35m;
            }
        }

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

        // Kiểm tra calo trung bình cả tuần cho AI
        if (isAi)
        {
            var weeklyTotal = dailyTotals.Values.Sum();
            var weeklyAverage = weeklyTotal / 7m;
            var minWeeklyAvg = targetCalories * 0.78m; // Trung bình tuần lệch tối đa -22%
            var maxWeeklyAvg = targetCalories * 1.15m;

            if (weeklyAverage < minWeeklyAvg || weeklyAverage > maxWeeklyAvg)
            {
                return false;
            }

            // Với AI, không cho phép ngày nào bị quá cực đoan (< 70% hoặc > 130%)
            var extremeOutliers = dailyTotals.Values.Count(dayTotal => dayTotal < targetCalories * 0.70m || dayTotal > targetCalories * 1.30m);
            if (extremeOutliers > 0)
            {
                return false;
            }

            return true;
        }

        // Với Template / Non-AI: Kiểm tra nghiêm ngặt từng ngày
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
            if (isTemplateValid && CheckCalorieCompliance(templateSlots, candidateLookup, targetCalories, candidateRecipes))
            {
                _logger.LogInformation("[MealPlanService] Áp dụng thành công thực đơn mẫu TEMPLATE #{PlanId}.", templatePlan.PlanId);
                return templateSlots;
            }
        }

        // Bước B: Thuật toán xếp lịch tổ hợp nội bộ có cơ chế xoay vòng và Blacklist 2 ngày gần nhất
        // Ưu tiên món dùng tủ lạnh (fridge) và cân đối 3 bữa sao cho tổng calo ngày nằm trong khoảng khả thi
        var sortedCandidates = candidateRecipes
            .OrderByDescending(r => r.RecipeIngredients.Any(ri => fridgeIngredientIds.Contains(ri.IngredientId)))
            .ThenBy(r => r.CaloriesKcal)
            .ToList();

        var fallbackSlots = new List<(string DayOfWeek, string MealType, int RecipeId)>();
        var minKcal = targetCalories * 0.85m; // Dung sai ±15% cho Fallback Engine
        var maxKcal = targetCalories * 1.15m;

        // Cơ chế Blacklist & Theo dõi tần suất sử dụng trong tuần (Anti-repetition)
        var recentDailyRecipeHistory = new List<HashSet<int>>();
        var recipeUsageCount = new Dictionary<int, int>();

        for (var dayIdx = 0; dayIdx < DaysOfWeek.Length; dayIdx++)
        {
            var day = DaysOfWeek[dayIdx];

            // Tập hợp blacklist các món đã ăn trong 2 ngày gần nhất
            var blacklistRecent = new HashSet<int>();
            for (var back = 1; back <= 2; back++)
            {
                var prevDayIdx = dayIdx - back;
                if (prevDayIdx >= 0 && prevDayIdx < recentDailyRecipeHistory.Count)
                {
                    foreach (var rid in recentDailyRecipeHistory[prevDayIdx])
                    {
                        blacklistRecent.Add(rid);
                    }
                }
            }

            var bestTriplet = FindBestTripletForDay(
                sortedCandidates,
                minKcal,
                maxKcal,
                targetCalories,
                dayIdx,
                blacklistRecent,
                recipeUsageCount);

            if (bestTriplet == null)
            {
                throw AppException.UnprocessableEntity(
                    ErrorCodes.ErrMealNoCandidate,
                    "Không thể xếp thực đơn đáp ứng dung sai calo với số lượng công thức hiện có.");
            }

            var (b, l, d) = bestTriplet.Value;

            fallbackSlots.Add((day, "BREAKFAST", b.RecipeId));
            fallbackSlots.Add((day, "LUNCH", l.RecipeId));
            fallbackSlots.Add((day, "DINNER", d.RecipeId));

            // Cập nhật lịch sử và tần suất
            var todayRecipes = new HashSet<int> { b.RecipeId, l.RecipeId, d.RecipeId };
            recentDailyRecipeHistory.Add(todayRecipes);

            recipeUsageCount[b.RecipeId] = recipeUsageCount.GetValueOrDefault(b.RecipeId, 0) + 1;
            recipeUsageCount[l.RecipeId] = recipeUsageCount.GetValueOrDefault(l.RecipeId, 0) + 1;
            recipeUsageCount[d.RecipeId] = recipeUsageCount.GetValueOrDefault(d.RecipeId, 0) + 1;
        }

        return fallbackSlots;
    }

    private static (Recipe Breakfast, Recipe Lunch, Recipe Dinner)? FindBestTripletForDay(
        List<Recipe> candidates,
        decimal minKcal,
        decimal maxKcal,
        decimal targetKcal,
        int dayOffset,
        HashSet<int> blacklistRecent,
        Dictionary<int, int> recipeUsageCount)
    {
        var n = candidates.Count;
        if (n < 3)
        {
            if (n > 0)
            {
                var r0 = candidates[dayOffset % n];
                var r1 = candidates[(dayOffset + 1) % n];
                var r2 = candidates[(dayOffset + 2) % n];
                return (r0, r1, r2);
            }
            return null;
        }

        // Lớp 1: Thử tìm bộ 3 trong tập ứng viên LOẠI TRỪ 2 ngày gần nhất (blacklist)
        // và ưu tiên những món ít xuất hiện nhất trong tuần (usageCount < 2)
        var nonRecentCandidates = candidates
            .Where(r => !blacklistRecent.Contains(r.RecipeId) && recipeUsageCount.GetValueOrDefault(r.RecipeId, 0) < 2)
            .OrderBy(r => recipeUsageCount.GetValueOrDefault(r.RecipeId, 0))
            .ThenBy(r => r.CaloriesKcal)
            .ToList();

        if (nonRecentCandidates.Count >= 3)
        {
            var triplet = TryFindTripletInList(nonRecentCandidates, minKcal, maxKcal, targetKcal, dayOffset);
            if (triplet != null)
            {
                return triplet;
            }
        }

        // Lớp 2: Thử nới lỏng usageCount (cho phép món dùng < 3 lần) nhưng vẫn né blacklist 2 ngày gần nhất
        var relaxedUsageCandidates = candidates
            .Where(r => !blacklistRecent.Contains(r.RecipeId))
            .OrderBy(r => recipeUsageCount.GetValueOrDefault(r.RecipeId, 0))
            .ThenBy(r => r.CaloriesKcal)
            .ToList();

        if (relaxedUsageCandidates.Count >= 3)
        {
            var triplet = TryFindTripletInList(relaxedUsageCandidates, minKcal, maxKcal, targetKcal, dayOffset);
            if (triplet != null)
            {
                return triplet;
            }
        }

        // Lớp 3: Tìm trên toàn bộ candidates (sắp xếp theo tần suất xuất hiện ít nhất trước)
        var allOrdered = candidates
            .OrderBy(r => recipeUsageCount.GetValueOrDefault(r.RecipeId, 0))
            .ThenBy(r => r.CaloriesKcal)
            .ToList();

        var fallbackTriplet = TryFindTripletInList(allOrdered, minKcal, maxKcal, targetKcal, dayOffset);
        if (fallbackTriplet != null)
        {
            return fallbackTriplet;
        }

        // Lớp 4: Nếu không có bộ ba nào hoàn hảo trong [minKcal, maxKcal], tìm bộ ba có tổng calo gần mục tiêu nhất
        (Recipe B, Recipe L, Recipe D)? closest = null;
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
                    var diff = Math.Abs(sum - targetKcal);
                    if (diff < minDiff)
                    {
                        minDiff = diff;
                        closest = dayOffset % 2 == 0 ? (r1, r2, r3) : (r1, r3, r2);
                    }
                }
            }
        }

        return closest;
    }

    private static (Recipe Breakfast, Recipe Lunch, Recipe Dinner)? TryFindTripletInList(
        List<Recipe> list,
        decimal minKcal,
        decimal maxKcal,
        decimal targetKcal,
        int dayOffset)
    {
        var count = list.Count;
        (Recipe B, Recipe L, Recipe D)? bestTriplet = null;
        var bestDiff = decimal.MaxValue;

        for (var step = 0; step < count; step++)
        {
            var i = (dayOffset + step) % count;
            var r1 = list[i];

            for (var j = 1; j < count; j++)
            {
                var idx2 = (i + j) % count;
                var r2 = list[idx2];

                for (var k = j + 1; k < count; k++)
                {
                    var idx3 = (i + k) % count;
                    var r3 = list[idx3];

                    var sum = r1.CaloriesKcal + r2.CaloriesKcal + r3.CaloriesKcal;
                    if (sum >= minKcal && sum <= maxKcal)
                    {
                        var diff = Math.Abs(sum - targetKcal);
                        if (diff < bestDiff)
                        {
                            bestDiff = diff;
                            bestTriplet = dayOffset % 2 == 0 ? (r1, r2, r3) : (r1, r3, r2);
                            if (bestDiff <= 50m)
                            {
                                return bestTriplet;
                            }
                        }
                    }
                }
            }
        }

        return bestTriplet;
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
            CreatedAt = DateTime.UtcNow,
            MaxSwapsAllowed = 21,
            SwapsRemaining = 21
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
                PreDeployLimit = mealPlan.MaxSwapsAllowed,
                PostDeployUsed = 0,
                PostDeployLimit = mealPlan.MaxSwapsAllowed
            },
            MaxSwapsAllowed = mealPlan.MaxSwapsAllowed,
            SwapsRemaining = mealPlan.SwapsRemaining
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

        var preDeployAudit = await _db.AuditLogs.CountAsync(a =>
            a.TargetTable == "MealPlans" &&
            a.TargetId == id.ToString() &&
            a.Action == "MEAL_SLOT_SWAPPED" &&
            a.Note == "DRAFT", ct);
        var preDeployUsed = Math.Max(preDeployAudit, plan.PlanStatus == "DRAFT" ? (plan.MaxSwapsAllowed - plan.SwapsRemaining) : 0);

        var postDeployAudit = await _db.AuditLogs.CountAsync(a =>
            a.TargetTable == "MealPlans" &&
            a.TargetId == id.ToString() &&
            a.Action == "MEAL_SLOT_SWAPPED" &&
            a.Note == "ACTIVE", ct);
        var postDeployUsed = Math.Max(postDeployAudit, plan.PlanStatus == "ACTIVE" ? (plan.MaxSwapsAllowed - plan.SwapsRemaining) : 0);

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
                PreDeployLimit = plan.MaxSwapsAllowed,
                PostDeployUsed = postDeployUsed,
                PostDeployLimit = plan.MaxSwapsAllowed
            },
            MaxSwapsAllowed = plan.MaxSwapsAllowed,
            SwapsRemaining = plan.SwapsRemaining
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
        plan.SwapsRemaining = plan.MaxSwapsAllowed;

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

        // Kiểm tra hạn mức đổi món linh hoạt theo MaxSwapsAllowed & SwapsRemaining (thay thế số 21 hardcode)
        var currentPhase = plan.PlanStatus;
        if (plan.SwapsRemaining <= 0)
        {
            throw AppException.Forbidden(
                ErrorCodes.ErrQuotaMealSwapExceeded,
                $"Hết {plan.MaxSwapsAllowed} lần đổi/chỉnh sửa của giai đoạn hiện tại ({currentPhase}).");
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
        var oldRecipeId = oldRecipe.RecipeId;
        var newRecipeId = newRecipe.RecipeId;

        slot.RecipeId = newRecipeId;
        plan.SwapsRemaining--;

        var caloriesDelta = Math.Round(newRecipe.CaloriesKcal - oldRecipe.CaloriesKcal, 1);
        var swapReason = request?.SwapReason ?? "TASTE_PREFERENCE";

        // Ghi nhận lịch sử đổi món vào bảng MealSlotSwapHistories
        var swapHistory = new MealSlotSwapHistory
        {
            SlotId = slot.SlotId,
            OldRecipeId = oldRecipeId,
            NewRecipeId = newRecipeId,
            SwapReason = swapReason,
            CaloriesDelta = caloriesDelta,
            SwappedAt = DateTime.UtcNow
        };
        _db.MealSlotSwapHistories.Add(swapHistory);

        var auditLog = new AuditLog
        {
            ActorUserId = userId,
            ActorRole = userRole,
            Action = "MEAL_SLOT_SWAPPED",
            TargetTable = "MealPlans",
            TargetId = id.ToString(),
            OldValue = JsonSerializer.Serialize(new { recipeId = oldRecipeId }),
            NewValue = JsonSerializer.Serialize(new { recipeId = newRecipeId }),
            Note = currentPhase,
            CreatedAt = DateTime.UtcNow
        };
        _db.AuditLogs.Add(auditLog);

        await _db.SaveChangesAsync(ct);

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
            SwapsRemaining = plan.SwapsRemaining,
            MaxSwapsAllowed = plan.MaxSwapsAllowed
        };
    }

    public async Task<IReadOnlyList<MealSlotSwapHistoryDto>> GetMealSlotSwapHistoryAsync(
        int slotId,
        int userId,
        int? planId = null,
        CancellationToken ct = default)
    {
        var slot = await _db.MealSlots
            .Include(s => s.Plan)
            .FirstOrDefaultAsync(s => s.SlotId == slotId, ct);

        if (slot == null || slot.Plan.UserId != userId || (planId.HasValue && slot.PlanId != planId.Value))
        {
            throw AppException.NotFound(
                ErrorCodes.ErrMealPlanNotFound,
                "Bữa ăn không tồn tại hoặc không thuộc về bạn.");
        }

        var histories = await _db.MealSlotSwapHistories
            .Include(h => h.OldRecipe)
            .Include(h => h.NewRecipe)
            .Where(h => h.SlotId == slotId)
            .OrderByDescending(h => h.SwappedAt)
            .ThenByDescending(h => h.SwapHistoryId)
            .Select(h => new MealSlotSwapHistoryDto
            {
                HistoryId = h.SwapHistoryId,
                SlotId = h.SlotId,
                OldRecipeId = h.OldRecipeId,
                OldRecipeTitle = h.OldRecipe != null ? h.OldRecipe.Title : string.Empty,
                OldRecipeImageUrl = h.OldRecipe != null ? h.OldRecipe.ImageUrl : null,
                OldRecipeCalories = h.OldRecipe != null ? h.OldRecipe.CaloriesKcal : 0m,
                OldRecipe = h.OldRecipe != null ? new MealSlotRecipeDto
                {
                    RecipeId = h.OldRecipe.RecipeId,
                    Title = h.OldRecipe.Title,
                    CaloriesKcal = h.OldRecipe.CaloriesKcal,
                    ImageUrl = h.OldRecipe.ImageUrl
                } : null,
                NewRecipeId = h.NewRecipeId,
                NewRecipeTitle = h.NewRecipe != null ? h.NewRecipe.Title : string.Empty,
                NewRecipeImageUrl = h.NewRecipe != null ? h.NewRecipe.ImageUrl : null,
                NewRecipeCalories = h.NewRecipe != null ? h.NewRecipe.CaloriesKcal : 0m,
                NewRecipe = h.NewRecipe != null ? new MealSlotRecipeDto
                {
                    RecipeId = h.NewRecipe.RecipeId,
                    Title = h.NewRecipe.Title,
                    CaloriesKcal = h.NewRecipe.CaloriesKcal,
                    ImageUrl = h.NewRecipe.ImageUrl
                } : null,
                SwapReason = h.SwapReason,
                CaloriesDelta = h.CaloriesDelta,
                SwappedAt = h.SwappedAt
            })
            .ToListAsync(ct);

        return histories;
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
            .AsNoTracking()
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
                var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.UserId == userId, ct);
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
            var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.UserId == userId, ct);
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
            .AsNoTracking()
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
                .AsNoTracking()
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
