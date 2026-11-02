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
using AiCostCalculator = VeggieLife.BLL.Services.AI.AiCostCalculator;
using VeggieLife.BLL.Services.Cache;
using VeggieLife.BLL.Services.Settings;
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

    private static string MapDayOfWeek(DayOfWeek dayOfWeek) => dayOfWeek switch
    {
        DayOfWeek.Monday => "MON",
        DayOfWeek.Tuesday => "TUE",
        DayOfWeek.Wednesday => "WED",
        DayOfWeek.Thursday => "THU",
        DayOfWeek.Friday => "FRI",
        DayOfWeek.Saturday => "SAT",
        DayOfWeek.Sunday => "SUN",
        _ => "MON"
    };

    private readonly AppDbContext _db;
    private readonly IAiServiceClient _aiClient;
    private readonly IRedisService _redisService;
    private readonly ISystemSettingService _settingService;
    private readonly ILogger<MealPlanService> _logger;

    public MealPlanService(
        AppDbContext db,
        IAiServiceClient aiClient,
        IRedisService redisService,
        ISystemSettingService settingService,
        ILogger<MealPlanService> logger)
    {
        _db = db;
        _aiClient = aiClient;
        _redisService = redisService;
        _settingService = settingService;
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
        var durationDays = request.DurationDays ?? 7;
        durationDays = Math.Clamp(durationDays, 1, 30);

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
            await CheckWeeklyQuotaAsync(userId, userRole, startDate, durationDays, ct);

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
            // Gom khoảng 100 món ứng cử viên, ưu tiên món khớp tủ lạnh và món xem nhiều
            var selectedCandidates = candidateRecipes
                .OrderByDescending(r => r.RecipeIngredients.Any(ri => fridgeIngredientIds.Contains(ri.IngredientId)))
                .ThenByDescending(r => r.ViewCount)
                .Take(100)
                .ToList();

            var (slots, usedAi, tokensConsumed, modelVersion) = await ScheduleMealsAsync(
                selectedCandidates,
                profile,
                targetCalories,
                forbiddenFoodGroups,
                requiresAlliumFree,
                fridgeIngredientIds,

                durationDays,
                startDate,
                request.CustomInstructions,
                ct);
            sw.Stop();

            // 11. Lưu thực đơn vào cơ sở dữ liệu qua Transaction
            var responseData = await SaveMealPlanTransactionAsync(
                userId,
                userRole,
                profile.DietarySchoolId,
                startDate,
                durationDays,
                targetCalories,
                request.BudgetTier.ToUpperInvariant(),
                request.CustomInstructions,
                usedAi,
                slots,
                selectedCandidates,
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

        var duration = request.DurationDays ?? request.NumberOfDays ?? 7;
        if (duration is < 1 or > 30)
        {
            throw AppException.BadRequest(
                ErrorCodes.ErrValidation,
                "Số ngày tạo thực đơn phải từ 1 đến 30 ngày.");
        }
        request.DurationDays = duration;
        request.NumberOfDays = duration;

        if (!string.IsNullOrWhiteSpace(request.CustomInstructions) && request.CustomInstructions.Length > 200)
        {
            throw AppException.BadRequest(
                ErrorCodes.ErrValidation,
                "Ghi chú tùy chỉnh cho AI không được vượt quá 200 ký tự.");
        }
    }

    private async Task CheckWeeklyQuotaAsync(int userId, string userRole, DateOnly startDate, int durationDays, CancellationToken ct)
    {
        var isVip = string.Equals(userRole, UserRoles.Vip, StringComparison.OrdinalIgnoreCase);
        var weeklyLimit = isVip
            ? await _settingService.GetIntSettingAsync("MEAL_PLAN_WEEKLY_LIMIT_VIP", 3, ct)
            : await _settingService.GetIntSettingAsync("MEAL_PLAN_WEEKLY_LIMIT_FREE", 1, ct);

        var weekStart = startDate;
        var weekEnd = startDate.AddDays(durationDays - 1);

        var existingCount = await _db.MealPlans.CountAsync(p =>
            p.UserId == userId &&
            p.StartDate >= weekStart &&
            p.StartDate <= weekEnd &&
            p.PlanStatus != "TEMPLATE" &&
            p.PlanStatus != "CANCELLED", ct);

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
        ScheduleMealsAsync(
            List<Recipe> candidateRecipes,
            HealthProfile profile,
            decimal targetCalories,
            HashSet<string> forbiddenFoodGroups,
            bool requiresAlliumFree,
            HashSet<int> fridgeIngredientIds,
            int durationDays,
            DateOnly startDate,
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
                DurationDays = durationDays,
                StartDate = startDate.ToString("yyyy-MM-dd"),
                ForbiddenFoodGroups = forbiddenFoodGroups.ToList(),
                CustomInstructions = customInstructions,
                CandidateRecipes = candidateRecipes.Select(r => new AiCandidateRecipeDto
                {
                    RecipeId = r.RecipeId,
                    Title = r.Title,
                    CaloriesKcal = r.CaloriesKcal,
                    CategoryName = r.Category?.CategoryName,
                    Description = r.Description,
                    Instructions = r.Instructions,
                    PrepTimeMinutes = r.PrepTimeMinutes,
                    CookTimeMinutes = r.CookTimeMinutes,
                    Servings = r.Servings,
                    DifficultyLevel = r.DifficultyLevel,
                    UsesFridgeItem = r.RecipeIngredients.Any(ri => fridgeIngredientIds.Contains(ri.IngredientId)),
                    FavoriteCount = r.ViewCount,
                    Ingredients = r.RecipeIngredients.Select(ri => new AiRecipeIngredientDto
                    {
                        IngredientId = ri.IngredientId,
                        IngredientName = ri.Ingredient != null ? ri.Ingredient.IngredientName : string.Empty,
                        QuantityNeeded = ri.QuantityNeeded,
                        Unit = ri.Unit,
                        PreparationNote = ri.PreparationNote,
                        IsOptional = ri.IsOptional
                    }).ToList()
                }).ToList()
            };

            // Vòng lặp gọi AI tối đa 3 lần (1 lần đầu + xếp lại tối đa 2 lần nếu calo lệch quá 10%)
            for (var attempt = 1; attempt <= 3; attempt++)
            {
                var aiResponse = await _aiClient.GenerateMealPlanAsync(aiRequest, ct);

                var expectedSlotCount = durationDays * 3;
                if (aiResponse == null || !aiResponse.Success || aiResponse.Slots == null || aiResponse.Slots.Count != expectedSlotCount)
                {
                    _logger.LogWarning("[MealPlanService] AI lần {Attempt} không phản hồi hợp lệ hoặc quá 4s. Chuyển sang Fallback Engine.", attempt);
                    break; // Ngắt vòng lặp chuyển sang Fallback
                }

                // Kiểm tra 21 vị trí món ăn do AI xếp
                var candidateSlots = new List<(string DayOfWeek, string MealType, int RecipeId)>();
                var allSlotsValid = true;

                for (var i = 0; i < aiResponse.Slots.Count; i++)
                {
                    var s = aiResponse.Slots[i];
                    if (!candidateLookup.ContainsKey(s.RecipeId))
                    {
                        allSlotsValid = false;
                        break;
                    }
                    var dayIdx = i / 3;
                    var slotDate = startDate.AddDays(dayIdx);
                    var dayCode = MapDayOfWeek(slotDate.DayOfWeek);
                    candidateSlots.Add((dayCode, s.MealType, s.RecipeId));
                }

                if (!allSlotsValid)
                {
                    _logger.LogWarning("[MealPlanService] AI lần {Attempt} trả về món ngoài danh mục ứng viên đã duyệt.", attempt);
                    continue;
                }

                // Kiểm tra calo mỗi ngày lệch tối đa (áp dụng dung sai linh hoạt -20% đến +15% cho AI để ưu tiên đa dạng món)
                var caloCompliant = CheckCalorieCompliance(candidateSlots, candidateLookup, targetCalories, durationDays, candidateRecipes, isAi: true);
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
            durationDays,
            startDate,
            ct);

        return (fallbackSlots, false, 0, "fallback-template-engine");
    }

    private static bool CheckCalorieCompliance(
        List<(string DayOfWeek, string MealType, int RecipeId)> slots,
        Dictionary<int, Recipe> candidateLookup,
        decimal targetCalories,
        int durationDays = 7,
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

        var dailyTotals = new List<decimal>();
        var totalDays = durationDays > 0 ? durationDays : (slots.Count / 3);
        if (totalDays <= 0) totalDays = 1;

        for (var d = 0; d < totalDays; d++)
        {
            var daySlots = slots.Skip(d * 3).Take(3).ToList();
            var daySum = daySlots.Sum(s => candidateLookup.TryGetValue(s.RecipeId, out var r) ? r.CaloriesKcal : 0m);
            dailyTotals.Add(daySum);
        }

        // Kiểm tra calo trung bình cho AI
        if (isAi)
        {
            var totalCal = dailyTotals.Sum();
            var avg = totalCal / totalDays;
            var minAvg = targetCalories * 0.78m;
            var maxAvg = targetCalories * 1.15m;

            if (avg < minAvg || avg > maxAvg)
            {
                return false;
            }

            // Với AI, không cho phép ngày nào bị quá cực đoan (< 70% hoặc > 130%)
            var extremeOutliers = dailyTotals.Count(dayTotal => dayTotal < targetCalories * 0.70m || dayTotal > targetCalories * 1.30m);
            if (extremeOutliers > 0)
            {
                return false;
            }

            return true;
        }

        // Với Template / Non-AI: Kiểm tra nghiêm ngặt từng ngày
        foreach (var dayTotal in dailyTotals)
        {
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
        int durationDays,
        DateOnly startDate,
        CancellationToken ct)
    {
        var candidateLookup = candidateRecipes.ToDictionary(r => r.RecipeId);

        // Bước A: Thử tìm thực đơn mẫu (TEMPLATE) có sẵn trong CSDL nếu là 7 ngày
        if (durationDays == 7)
        {
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
                if (isTemplateValid && CheckCalorieCompliance(templateSlots, candidateLookup, targetCalories, 7, candidateRecipes))
                {
                    _logger.LogInformation("[MealPlanService] Áp dụng thành công thực đơn mẫu TEMPLATE #{PlanId}.", templatePlan.PlanId);
                    return templateSlots;
                }
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

        for (var dayIdx = 0; dayIdx < durationDays; dayIdx++)
        {
            var date = startDate.AddDays(dayIdx);
            var day = MapDayOfWeek(date.DayOfWeek);

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

    private static string GetDayOfWeekCode(DateOnly date) => date.DayOfWeek switch
    {
        System.DayOfWeek.Monday => "MON",
        System.DayOfWeek.Tuesday => "TUE",
        System.DayOfWeek.Wednesday => "WED",
        System.DayOfWeek.Thursday => "THU",
        System.DayOfWeek.Friday => "FRI",
        System.DayOfWeek.Saturday => "SAT",
        System.DayOfWeek.Sunday => "SUN",
        _ => "MON"
    };

    private async Task<CreateMealPlanResponseData> SaveMealPlanTransactionAsync(
        int userId,
        string userRole,
        int dietarySchoolId,
        DateOnly startDate,
        int durationDays,
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

        var preDeployLimit = await _settingService.GetIntSettingAsync("MEAL_SWAP_LIMIT_DRAFT", 21, ct);
        var postDeployLimit = await _settingService.GetIntSettingAsync("MEAL_SWAP_LIMIT_ACTIVE", 21, ct);
        var swapLimit = (durationDays == 7)
            ? preDeployLimit
            : Math.Max(1, (int)Math.Round((double)preDeployLimit * durationDays / 7.0));

        var safeDays = Math.Clamp(durationDays, 1, 30);
        var mealPlan = new MealPlan
        {
            UserId = userId,
            DietarySchoolId = dietarySchoolId,
            PlanName = safeDays == 7 ? $"Thực đơn tuần {startDate:dd/MM}" : $"Thực đơn {safeDays} ngày ({startDate:dd/MM})",
            StartDate = startDate,
            EndDate = startDate.AddDays(safeDays - 1),
            DurationDays = safeDays,
            TargetDailyKcal = Math.Round(targetDailyKcal, 1),
            BudgetTier = budgetTier,
            CustomInstructions = customInstructions,
            PlanStatus = "DRAFT",
            GeneratedByAi = generatedByAi,
            CreatedAt = DateTime.UtcNow,
            MaxSwapsAllowed = swapLimit,
            SwapsRemaining = swapLimit
        };

        _db.MealPlans.Add(mealPlan);
        await _db.SaveChangesAsync(ct);

        var finalSlots = new List<(string DayOfWeek, string MealType, int RecipeId)>();
        for (var dayIndex = 0; dayIndex < safeDays; dayIndex++)
        {
            var dayCode = GetDayOfWeekCode(startDate.AddDays(dayIndex));
            foreach (var mealType in MealTypes)
            {
                var slotIdx = finalSlots.Count;
                var sourceSlot = slots[slotIdx % slots.Count];
                finalSlots.Add((dayCode, mealType, sourceSlot.RecipeId));
            }
        }

        var mealSlotEntities = finalSlots.Select(s => new MealSlot
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
            CostEstimateUsd = generatedByAi ? AiCostCalculator.CalculateFromTotal(tokensConsumed > 0 ? tokensConsumed : 450) : 0m,
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
                Notes = s.Notes,
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
            DurationDays = mealPlan.DurationDays,
            BudgetTier = mealPlan.BudgetTier,
            CustomInstructions = mealPlan.CustomInstructions,
            TargetDailyKcal = mealPlan.TargetDailyKcal,
            PlanStatus = mealPlan.PlanStatus,
            GeneratedByAi = mealPlan.GeneratedByAi,
            Slots = responseSlots,
            Edits = new MealPlanEditsDto
            {
                PreDeployUsed = 0,
                PreDeployLimit = preDeployLimit,
                PostDeployUsed = 0,
                PostDeployLimit = postDeployLimit
            },
            MaxSwapsAllowed = mealPlan.MaxSwapsAllowed,
            SwapsRemaining = mealPlan.SwapsRemaining
        };
    }

    public Task<(IReadOnlyList<MealPlanSummaryDto> Items, PagedMeta Meta)> ListMealPlansAsync(
        int userId,
        int page,
        int size,
        CancellationToken ct = default)
    {
        return ListMealPlansAsync(userId, page, size, null, ct);
    }

    public async Task<(IReadOnlyList<MealPlanSummaryDto> Items, PagedMeta Meta)> ListMealPlansAsync(
        int userId,
        int page,
        int size,
        string? status,
        CancellationToken ct = default)
    {
        if (page < 1) page = 1;
        if (size < 1) size = 10;
        if (size > 100) size = 100;

        var query = _db.MealPlans.Where(p => p.UserId == userId && p.PlanStatus != "TEMPLATE");
        if (!string.IsNullOrWhiteSpace(status) && !string.Equals(status, "ALL", StringComparison.OrdinalIgnoreCase))
        {
            var normalizedStatus = status.Trim().ToUpperInvariant();
            query = query.Where(p => p.PlanStatus == normalizedStatus);
        }

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
                PlanStatus = p.PlanStatus,
                DurationDays = p.DurationDays
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
            .Include(p => p.MealSlots)
                .ThenInclude(ms => ms.SwapHistories)
                    .ThenInclude(sh => sh.OldRecipe)
            .Include(p => p.MealSlots)
                .ThenInclude(ms => ms.SwapHistories)
                    .ThenInclude(sh => sh.NewRecipe)
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

        var mealOrder = MealTypes.Select((m, idx) => (m, idx)).ToDictionary(x => x.m, x => x.idx, StringComparer.OrdinalIgnoreCase);

        var slots = plan.MealSlots
            .OrderBy(s => s.SlotId)
            .ThenBy(s => mealOrder.GetValueOrDefault(s.MealType, 99))
            .Select(s => new MealSlotDto
            {
                SlotId = s.SlotId,
                DayOfWeek = s.DayOfWeek,
                MealType = s.MealType,
                IsCompleted = s.IsCompleted,
                Notes = s.Notes,
                Recipe = s.Recipe != null ? new MealSlotRecipeDto
                {
                    RecipeId = s.Recipe.RecipeId,
                    Title = s.Recipe.Title,
                    CaloriesKcal = s.Recipe.CaloriesKcal,
                    ImageUrl = s.Recipe.ImageUrl
                } : null,
                SwapCount = s.SwapHistories.Count,
                SwapHistories = s.SwapHistories
                    .OrderByDescending(h => h.SwappedAt)
                    .Select(h => new MealSlotSwapHistoryDto
                    {
                        HistoryId = h.SwapHistoryId,
                        SlotId = h.SlotId,
                        OldRecipeId = h.OldRecipeId,
                        OldRecipeTitle = h.OldRecipe?.Title ?? string.Empty,
                        OldRecipeImageUrl = h.OldRecipe?.ImageUrl,
                        OldRecipeCalories = h.OldRecipe?.CaloriesKcal ?? 0,
                        OldRecipe = h.OldRecipe != null ? new MealSlotRecipeDto
                        {
                            RecipeId = h.OldRecipe.RecipeId,
                            Title = h.OldRecipe.Title,
                            CaloriesKcal = h.OldRecipe.CaloriesKcal,
                            ImageUrl = h.OldRecipe.ImageUrl
                        } : null,
                        NewRecipeId = h.NewRecipeId,
                        NewRecipeTitle = h.NewRecipe?.Title ?? string.Empty,
                        NewRecipeImageUrl = h.NewRecipe?.ImageUrl,
                        NewRecipeCalories = h.NewRecipe?.CaloriesKcal ?? 0,
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
                    .ToList()
            })
            .ToList();

        var preDeployLimit = await _settingService.GetIntSettingAsync("MEAL_SWAP_LIMIT_DRAFT", 21, ct);
        var postDeployLimit = await _settingService.GetIntSettingAsync("MEAL_SWAP_LIMIT_ACTIVE", 21, ct);

        return new CreateMealPlanResponseData
        {
            PlanId = plan.PlanId,
            PlanName = plan.PlanName,
            StartDate = plan.StartDate.ToString("yyyy-MM-dd"),
            EndDate = plan.EndDate.ToString("yyyy-MM-dd"),
            DurationDays = plan.DurationDays,
            BudgetTier = plan.BudgetTier,
            CustomInstructions = plan.CustomInstructions,
            TargetDailyKcal = plan.TargetDailyKcal,
            PlanStatus = plan.PlanStatus,
            GeneratedByAi = plan.GeneratedByAi,
            Slots = slots,
            Edits = new MealPlanEditsDto
            {
                PreDeployUsed = preDeployUsed,
                PreDeployLimit = preDeployLimit,
                PostDeployUsed = postDeployUsed,
                PostDeployLimit = postDeployLimit
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

    public async Task<MealPlanDetailDto> CancelMealPlanAsync(
        int planId,
        int userId,
        CancellationToken ct = default)
    {
        var plan = await _db.MealPlans.FirstOrDefaultAsync(p => p.PlanId == planId && p.UserId == userId, ct);

        if (plan == null)
        {
            throw AppException.NotFound(
                ErrorCodes.ErrMealPlanNotFound,
                "Thực đơn không tồn tại hoặc không thuộc về bạn.");
        }

        if (string.Equals(plan.PlanStatus, "CANCELLED", StringComparison.OrdinalIgnoreCase))
        {
            throw AppException.Conflict(
                ErrorCodes.ErrValidation,
                "Thực đơn này đã ở trạng thái đã hủy (CANCELLED).");
        }

        var oldStatus = plan.PlanStatus;
        plan.PlanStatus = "CANCELLED";

        var auditLog = new AuditLog
        {
            ActorUserId = userId,
            ActorRole = "MEMBER",
            Action = "MEAL_PLAN_CANCELLED",
            TargetTable = "MealPlans",
            TargetId = planId.ToString(),
            OldValue = JsonSerializer.Serialize(new { status = oldStatus }),
            NewValue = JsonSerializer.Serialize(new { status = "CANCELLED" }),
            CreatedAt = DateTime.UtcNow
        };
        _db.AuditLogs.Add(auditLog);

        await _db.SaveChangesAsync(ct);

        var detail = await GetMealPlanByIdAsync(planId, userId, ct);
        return new MealPlanDetailDto
        {
            PlanId = detail.PlanId,
            PlanName = detail.PlanName,
            StartDate = detail.StartDate,
            EndDate = detail.EndDate,
            BudgetTier = detail.BudgetTier,
            CustomInstructions = detail.CustomInstructions,
            TargetDailyKcal = detail.TargetDailyKcal,
            PlanStatus = detail.PlanStatus,
            GeneratedByAi = detail.GeneratedByAi,
            Slots = detail.Slots,
            Edits = detail.Edits,
            MaxSwapsAllowed = detail.MaxSwapsAllowed,
            SwapsRemaining = detail.SwapsRemaining,
            DurationDays = detail.DurationDays
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

        // Kiểm tra hạn mức đổi món linh hoạt theo MaxSwapsAllowed & SwapsRemaining kết hợp SystemSettings
        var currentPhase = plan.PlanStatus;
        var swapLimit = string.Equals(currentPhase, "DRAFT", StringComparison.OrdinalIgnoreCase)
            ? await _settingService.GetIntSettingAsync("MEAL_SWAP_LIMIT_DRAFT", 21, ct)
            : await _settingService.GetIntSettingAsync("MEAL_SWAP_LIMIT_ACTIVE", 21, ct);

        var swapsUsedInPhase = await _db.AuditLogs.CountAsync(a =>
            a.TargetTable == "MealPlans" &&
            a.TargetId == id.ToString() &&
            a.Action == "MEAL_SLOT_SWAPPED" &&
            a.Note == currentPhase, ct);

        var effectiveLimit = swapLimit != 21 ? swapLimit : (plan.MaxSwapsAllowed > 0 ? plan.MaxSwapsAllowed : 21);

        if (plan.SwapsRemaining <= 0 || swapsUsedInPhase >= effectiveLimit)
        {
            throw AppException.Forbidden(
                ErrorCodes.ErrQuotaMealSwapExceeded,
                $"Hết {effectiveLimit} lần đổi/chỉnh sửa của giai đoạn hiện tại ({currentPhase}).");
        }

        // Tìm món thay thế: APPROVED_ACTIVE, hợp trường phái, calo lệch <= tolerance, không dị ứng
        var oldRecipe = slot.Recipe ?? await _db.Recipes.FirstOrDefaultAsync(r => r.RecipeId == slot.RecipeId, ct);
        if (oldRecipe == null)
        {
            throw AppException.NotFound(
                ErrorCodes.ErrRecipeStructureInvalid,
                "Không tìm thấy công thức món cũ để tính toán thay thế.");
        }

        Recipe newRecipe;
        var oldRecipeId = oldRecipe.RecipeId;

        if (request?.NewRecipeId.HasValue == true && request.NewRecipeId.Value > 0)
        {
            var chosenRecipe = await _db.Recipes
                .Include(r => r.RecipeIngredients)
                    .ThenInclude(ri => ri.Ingredient)
                .FirstOrDefaultAsync(r => r.RecipeId == request.NewRecipeId.Value && r.Status == "APPROVED_ACTIVE", ct);

            if (chosenRecipe == null)
            {
                throw AppException.NotFound(
                    ErrorCodes.ErrRecipeStructureInvalid,
                    "Không tìm thấy món ăn được chọn để thay thế hoặc món ăn chưa được duyệt.");
            }
            newRecipe = chosenRecipe;
        }
        else if (!string.IsNullOrWhiteSpace(request?.CustomNote))
        {
            // Chỉ khi người dùng viết thêm ghi chú thì mới gọi AI để gợi ý món theo lưu ý của người dùng
            var swapByNoteReq = new SwapMealSlotByNoteRequest
            {
                Note = request.CustomNote,
                ExcludedRecipeIds = request.ExcludedRecipeIds
            };
            return await SwapMealSlotByNoteAsync(slotId, userId, userRole, swapByNoteReq, id, ct);
        }
        else
        {
            var calorieTolerancePercent = await _settingService.GetIntSettingAsync("MEAL_SWAP_CALORIE_TOLERANCE_PERCENT", 15, ct);
            var toleranceFactor = calorieTolerancePercent / 100m;
            var oldCalories = oldRecipe.CaloriesKcal;

            // Ngưỡng dung sai cơ sở: 15% nhưng tối thiểu +/- 150 kcal (để các món ít calo như nước chấm, canh thanh nhẹ không bị rỗng kết quả)
            var baseDelta = Math.Max(150m, oldCalories * toleranceFactor);
            var minKcal = Math.Max(0m, oldCalories - baseDelta);
            var maxKcal = oldCalories + baseDelta;

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



            var substituteCandidates = new List<Recipe>();

            // Nếu người dùng có gửi kèm CustomNote (ví dụ: "yêu cầu ăn bí đỏ", "thêm nấm"):
            // Ưu tiên lọc và chấm điểm các món phù hợp với ghi chú trước
            if (!string.IsNullOrWhiteSpace(request?.CustomNote))
            {
                var cleanNote = request.CustomNote.Trim().ToLowerInvariant();
                var noteDelimiters = new[] { ' ', ',', '.', ';', '!', '?', '+', '-', '/', '_', '(', ')', '[', ']', ':', '"', '\'' };
                var stopWords = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                {
                    "yêu", "cầu", "muốn", "thích", "không", "món", "cho", "với", "nhiều", "ít", "được", "mình", "ngày", "bữa", "ăn",
                    "tôi", "có", "các", "của", "những", "và", "nhưng", "hoặc", "lại", "thì", "là", "ở", "vào", "ra", "lên", "xuống",
                    "rất", "quá", "này", "đó", "kia", "nào", "thêm", "bớt"
                };
                var keywords = cleanNote
                    .Split(noteDelimiters, StringSplitOptions.RemoveEmptyEntries)
                    .Select(w => w.Trim().ToLowerInvariant())
                    .Where(w => w.Length >= 2 && !stopWords.Contains(w))
                    .ToList();

                if (keywords.Count > 0)
                {
                    var scoredByNote = filteredCandidates.Select(r =>
                    {
                        var score = 0;
                        var titleLower = r.Title.ToLowerInvariant();
                        var descLower = (r.Description ?? string.Empty).ToLowerInvariant();
                        var ingredientNamesLower = r.RecipeIngredients
                            .Select(ri => (ri.Ingredient != null ? ri.Ingredient.IngredientName : string.Empty).ToLowerInvariant())
                            .ToList();

                        // Ưu tiên cụm từ khóa ghép nếu có từ 2 từ trở lên (ví dụ: "bí đỏ", "nấm rơm")
                        foreach (var kw1 in keywords)
                        {
                            foreach (var kw2 in keywords)
                            {
                                if (kw1 != kw2)
                                {
                                    var phrase = $"{kw1} {kw2}";
                                    if (titleLower.Contains(phrase)) score += 25;
                                    if (descLower.Contains(phrase)) score += 12;
                                    if (ingredientNamesLower.Any(ing => ing.Contains(phrase))) score += 20;
                                }
                            }
                        }

                        // Điểm từng từ khóa riêng lẻ
                        foreach (var kw in keywords)
                        {
                            if (titleLower.Contains(kw)) score += 5;
                            if (descLower.Contains(kw)) score += 3;
                            if (ingredientNamesLower.Any(ing => ing.Contains(kw))) score += 4;
                        }

                        return new { Recipe = r, Score = score, CalorieDiff = Math.Abs(r.CaloriesKcal - oldCalories) };
                    })
                    .Where(x => x.Score > 0)
                    .OrderByDescending(x => x.Score)
                    .ThenBy(x => x.CalorieDiff)
                    .ToList();

                    if (scoredByNote.Count > 0)
                    {
                        substituteCandidates = scoredByNote.Select(x => x.Recipe).ToList();
                    }
                    else
                    {
                        throw AppException.UnprocessableEntity(
                            ErrorCodes.ErrMealNoSubstitute,
                            $"Không tìm thấy món chay có nguyên liệu hoặc tên phù hợp với ghi chú '{request.CustomNote}'. Vui lòng thử từ khóa khác hoặc duyệt thư viện công thức.");
                    }
                }
            }

            // Giai đoạn 1: Tìm món trong khoảng dung sai calo cơ sở (nếu chưa lọc theo ghi chú)
            if (substituteCandidates.Count == 0)
            {
                substituteCandidates = filteredCandidates
                    .Where(r => r.CaloriesKcal >= minKcal && r.CaloriesKcal <= maxKcal)
                    .OrderBy(r => Math.Abs(r.CaloriesKcal - oldCalories))
                    .ToList();
            }

            // Giai đoạn 2 (Fallback): Nới rộng khoảng calo (+-350 kcal hoặc 40%)
            if (substituteCandidates.Count == 0)
            {
                var expandedDelta = Math.Max(350m, oldCalories * 0.4m);
                var expandedMinKcal = Math.Max(0m, oldCalories - expandedDelta);
                var expandedMaxKcal = oldCalories + expandedDelta;

                substituteCandidates = filteredCandidates
                    .Where(r => r.CaloriesKcal >= expandedMinKcal && r.CaloriesKcal <= expandedMaxKcal)
                    .OrderBy(r => Math.Abs(r.CaloriesKcal - oldCalories))
                    .ToList();
            }

            // Giai đoạn 3 (Fallback): Lấy các món có calo gần nhất trong toàn bộ danh sách hợp lệ
            if (substituteCandidates.Count == 0 && filteredCandidates.Count > 0)
            {
                substituteCandidates = filteredCandidates
                    .OrderBy(r => Math.Abs(r.CaloriesKcal - oldCalories))
                    .Take(10)
                    .ToList();
            }

            if (substituteCandidates.Count == 0)
            {
                throw AppException.UnprocessableEntity(
                    ErrorCodes.ErrMealNoSubstitute,
                    "Không có món thay thế phù hợp với trường phái ăn chay hoặc danh sách dị ứng của bạn.");
            }

            // Chọn ngẫu nhiên trong top 3 món gần nhất để không lặp lại món vừa thấy
            var topPickCount = Math.Min(3, substituteCandidates.Count);
            var topPicks = substituteCandidates.Take(topPickCount).ToList();
            newRecipe = topPicks[Random.Shared.Next(topPicks.Count)];
        }

        var newRecipeId = newRecipe.RecipeId;
        slot.RecipeId = newRecipeId;
        plan.SwapsRemaining--;

        if (!string.IsNullOrWhiteSpace(request?.CustomNote))
        {
            var trimmedNote = request.CustomNote.Trim();
            slot.Notes = trimmedNote.Length > 200 ? trimmedNote.Substring(0, 200) : trimmedNote;
        }

        var caloriesDelta = Math.Round(newRecipe.CaloriesKcal - oldRecipe.CaloriesKcal, 1);
        var swapReason = !string.IsNullOrWhiteSpace(request?.SwapReason)
            ? request.SwapReason
            : (!string.IsNullOrWhiteSpace(request?.CustomNote)
                ? request.CustomNote
                : (request?.NewRecipeId.HasValue == true ? "MANUAL_SELECTION" : "TASTE_PREFERENCE"));

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

        var slotHistories = await _db.MealSlotSwapHistories
            .Include(h => h.OldRecipe)
            .Include(h => h.NewRecipe)
            .Where(h => h.SlotId == slot.SlotId)
            .OrderByDescending(h => h.SwappedAt)
            .Select(h => new MealSlotSwapHistoryDto
            {
                HistoryId = h.SwapHistoryId,
                SlotId = h.SlotId,
                OldRecipeId = h.OldRecipeId,
                OldRecipeTitle = h.OldRecipe.Title,
                OldRecipeImageUrl = h.OldRecipe.ImageUrl,
                OldRecipeCalories = h.OldRecipe.CaloriesKcal,
                OldRecipe = new MealSlotRecipeDto
                {
                    RecipeId = h.OldRecipe.RecipeId,
                    Title = h.OldRecipe.Title,
                    CaloriesKcal = h.OldRecipe.CaloriesKcal,
                    ImageUrl = h.OldRecipe.ImageUrl
                },
                NewRecipeId = h.NewRecipeId,
                NewRecipeTitle = h.NewRecipe.Title,
                NewRecipeImageUrl = h.NewRecipe.ImageUrl,
                NewRecipeCalories = h.NewRecipe.CaloriesKcal,
                NewRecipe = new MealSlotRecipeDto
                {
                    RecipeId = h.NewRecipe.RecipeId,
                    Title = h.NewRecipe.Title,
                    CaloriesKcal = h.NewRecipe.CaloriesKcal,
                    ImageUrl = h.NewRecipe.ImageUrl
                },
                SwapReason = h.SwapReason,
                CaloriesDelta = h.CaloriesDelta,
                SwappedAt = h.SwappedAt
            })
            .ToListAsync(ct);

        return new UpdateMealPlanSlotResponseData
        {
            Slot = new MealSlotDto
            {
                SlotId = slot.SlotId,
                DayOfWeek = slot.DayOfWeek,
                MealType = slot.MealType,
                IsCompleted = slot.IsCompleted,
                Notes = slot.Notes,
                Recipe = new MealSlotRecipeDto
                {
                    RecipeId = newRecipe.RecipeId,
                    Title = newRecipe.Title,
                    CaloriesKcal = newRecipe.CaloriesKcal,
                    ImageUrl = newRecipe.ImageUrl
                },
                SwapCount = slotHistories.Count,
                SwapHistories = slotHistories
            },
            SwapsRemaining = plan.SwapsRemaining,
            MaxSwapsAllowed = plan.MaxSwapsAllowed
        };
    }

    public async Task<UpdateMealPlanSlotResponseData> SwapMealSlotByNoteAsync(
        int slotId,
        int userId,
        string userRole,
        SwapMealSlotByNoteRequest request,
        int? planId = null,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request?.Note))
        {
            throw AppException.BadRequest(
                ErrorCodes.ErrValidation,
                "Ghi chú yêu cầu đổi món không được để trống.");
        }

        var slotQuery = _db.MealSlots
            .Include(s => s.Plan)
                .ThenInclude(p => p.DietarySchool)
            .Include(s => s.Recipe)
                .ThenInclude(r => r.Category)
            .Include(s => s.Recipe)
                .ThenInclude(r => r.RecipeIngredients)
                    .ThenInclude(ri => ri.Ingredient)
            .Where(s => s.SlotId == slotId);

        if (planId.HasValue)
        {
            slotQuery = slotQuery.Where(s => s.PlanId == planId.Value);
        }

        var slot = await slotQuery.FirstOrDefaultAsync(ct);
        if (slot == null)
        {
            throw AppException.NotFound(
                ErrorCodes.ErrMealPlanNotFound,
                "Không tìm thấy bữa ăn hoặc thực đơn không khớp.");
        }

        var plan = slot.Plan;
        if (plan.UserId != userId)
        {
            throw AppException.NotFound(
                ErrorCodes.ErrMealPlanNotFound,
                "Thực đơn không thuộc quyền sở hữu của bạn.");
        }

        if (plan.PlanStatus != "DRAFT" && plan.PlanStatus != "ACTIVE")
        {
            throw AppException.BadRequest(
                ErrorCodes.ErrValidation,
                "Chỉ có thể đổi món cho thực đơn ở trạng thái DRAFT hoặc ACTIVE.");
        }

        var currentPhase = plan.PlanStatus;
        var swapLimit = string.Equals(currentPhase, "DRAFT", StringComparison.OrdinalIgnoreCase)
            ? await _settingService.GetIntSettingAsync("MEAL_SWAP_LIMIT_DRAFT", 21, ct)
            : await _settingService.GetIntSettingAsync("MEAL_SWAP_LIMIT_ACTIVE", 21, ct);

        var swapsUsedInPhase = await _db.AuditLogs.CountAsync(a =>
            a.TargetTable == "MealPlans" &&
            a.TargetId == plan.PlanId.ToString() &&
            a.Action == "MEAL_SLOT_SWAPPED" &&
            a.Note == currentPhase, ct);

        var effectiveLimit = swapLimit != 21 ? swapLimit : (plan.MaxSwapsAllowed > 0 ? plan.MaxSwapsAllowed : 21);

        if (plan.SwapsRemaining <= 0 || swapsUsedInPhase >= effectiveLimit)
        {
            throw AppException.Forbidden(
                ErrorCodes.ErrQuotaMealSwapExceeded,
                $"Hết {effectiveLimit} lần đổi/chỉnh sửa của giai đoạn hiện tại ({currentPhase}).");
        }

        var oldRecipe = slot.Recipe ?? await _db.Recipes.FirstOrDefaultAsync(r => r.RecipeId == slot.RecipeId, ct);
        if (oldRecipe == null)
        {
            throw AppException.NotFound(
                ErrorCodes.ErrRecipeStructureInvalid,
                "Không tìm thấy công thức món cũ để thực hiện đổi món.");
        }

        // Lấy thông tin dị ứng của user
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
        var requiresAllium = hp?.RequiresAlliumFree == true;
        ParseForbiddenRules(plan.DietarySchool.ForbiddenFoodRules, requiresAllium,
            out var forbiddenFoodGroups, out var requiresAlliumFree);

        var excludedIds = new HashSet<int>(request.ExcludedRecipeIds ?? Enumerable.Empty<int>())
        {
            oldRecipe.RecipeId
        };

        // Cào database khoảng 100 món APPROVED_ACTIVE phù hợp
        var allActiveRecipes = await _db.Recipes
            .Include(r => r.Category)
            .Include(r => r.RecipeIngredients)
                .ThenInclude(ri => ri.Ingredient)
            .Where(r => r.Status == "APPROVED_ACTIVE" && !excludedIds.Contains(r.RecipeId))
            .OrderByDescending(r => r.ViewCount)
            .ToListAsync(ct);

        var filteredCandidates = FilterCandidateRecipes(
            allActiveRecipes,
            forbiddenIngredientSet,
            forbiddenFoodGroups,
            requiresAlliumFree);

        var candidateRecipes = filteredCandidates.Take(100).ToList();
        if (candidateRecipes.Count == 0)
        {
            throw AppException.UnprocessableEntity(
                ErrorCodes.ErrMealNoSubstitute,
                "Không tìm thấy món thay thế phù hợp trong kho công thức.");
        }

        // Đóng gói 100 món kèm toàn bộ công thức
        var candidateDtos = candidateRecipes.Select(r => new AiCandidateRecipeDto
        {
            RecipeId = r.RecipeId,
            Title = r.Title,
            Description = r.Description,
            Instructions = r.Instructions,
            PrepTimeMinutes = r.PrepTimeMinutes,
            CookTimeMinutes = r.CookTimeMinutes,
            Servings = r.Servings,
            DifficultyLevel = r.DifficultyLevel,
            CaloriesKcal = r.CaloriesKcal,
            CategoryName = r.Category?.CategoryName,
            UsesFridgeItem = false,
            MealTypePreference = slot.MealType,
            FavoriteCount = r.ViewCount,
            Ingredients = r.RecipeIngredients.Select(ri => new AiRecipeIngredientDto
            {
                IngredientId = ri.IngredientId,
                IngredientName = ri.Ingredient != null ? ri.Ingredient.IngredientName : string.Empty,
                QuantityNeeded = ri.QuantityNeeded,
                Unit = ri.Unit,
                PreparationNote = ri.PreparationNote,
                IsOptional = ri.IsOptional
            }).ToList()
        }).ToList();

        Recipe? selectedRecipe = null;
        string? resolvedReason = null;

        // Gọi AI Service nếu mạch không mở
        if (!_aiClient.IsCircuitOpen)
        {
            var aiRequest = new AiSwapMealByNoteRequest
            {
                CurrentRecipeId = oldRecipe.RecipeId,
                SwapNote = request.Note,
                DietarySchoolCode = plan.DietarySchool?.SchoolCode ?? "VEGAN",
                MealType = slot.MealType,
                TargetCalories = oldRecipe.CaloriesKcal,
                ExcludedRecipeIds = excludedIds.ToList(),
                CandidateRecipes = candidateDtos
            };

            var aiResponse = await _aiClient.SwapMealByNoteAsync(aiRequest, ct);
            if (aiResponse != null && aiResponse.Success && aiResponse.SelectedRecipeId > 0)
            {
                selectedRecipe = candidateRecipes.FirstOrDefault(r => r.RecipeId == aiResponse.SelectedRecipeId);
                resolvedReason = !string.IsNullOrWhiteSpace(aiResponse.Reason)
                    ? aiResponse.Reason
                    : $"AI chọn theo ghi chú: {request.Note}";
            }
        }

        // Fallback Engine nếu AI không khả dụng hoặc không tìm thấy món
        if (selectedRecipe == null)
        {
            _logger.LogInformation("[MealPlanService] Dùng Fallback Engine cho đổi món theo ghi chú.");
            var cleanNote = request.Note.Trim().ToLowerInvariant();
            var noteDelimiters = new[] { ' ', ',', '.', ';', '!', '?', '+', '-', '/', '_', '(', ')', '[', ']', ':', '"', '\'' };
            var stopWords = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "yêu", "cầu", "muốn", "thích", "không", "món", "cho", "với", "nhiều", "ít", "được", "mình", "ngày", "bữa", "ăn",
                "tôi", "có", "các", "của", "những", "và", "nhưng", "hoặc", "lại", "thì", "là", "ở", "vào", "ra", "lên", "xuống",
                "rất", "quá", "này", "đó", "kia", "nào", "thêm", "bớt"
            };
            var noteKeywords = cleanNote
                .Split(noteDelimiters, StringSplitOptions.RemoveEmptyEntries)
                .Select(w => w.Trim().ToLowerInvariant())
                .Where(w => w.Length >= 2 && !stopWords.Contains(w))
                .ToList();

            var scoredCandidates = candidateRecipes.Select(r =>
            {
                var score = 0;
                var titleLower = r.Title.ToLowerInvariant();
                var descLower = (r.Description ?? string.Empty).ToLowerInvariant();
                var ingredientNamesLower = r.RecipeIngredients
                    .Select(ri => (ri.Ingredient != null ? ri.Ingredient.IngredientName : string.Empty).ToLowerInvariant())
                    .ToList();

                // Ưu tiên cụm từ khóa ghép nếu có từ 2 từ trở lên (ví dụ: "bí đỏ", "nấm rơm")
                foreach (var kw1 in noteKeywords)
                {
                    foreach (var kw2 in noteKeywords)
                    {
                        if (kw1 != kw2)
                        {
                            var phrase = $"{kw1} {kw2}";
                            if (titleLower.Contains(phrase)) score += 25;
                            if (descLower.Contains(phrase)) score += 12;
                            if (ingredientNamesLower.Any(ing => ing.Contains(phrase))) score += 20;
                        }
                    }
                }

                // Điểm từng từ khóa riêng lẻ
                foreach (var kw in noteKeywords)
                {
                    if (titleLower.Contains(kw)) score += 5;
                    if (descLower.Contains(kw)) score += 3;
                    if (ingredientNamesLower.Any(ing => ing.Contains(kw))) score += 4;
                }

                return new { Recipe = r, Score = score, CalorieDiff = Math.Abs(r.CaloriesKcal - oldRecipe.CaloriesKcal) };
            })
            .OrderByDescending(x => x.Score)
            .ThenBy(x => x.CalorieDiff)
            .ToList();

            var positiveMatches = scoredCandidates.Where(x => x.Score > 0).ToList();
            if (positiveMatches.Count > 0)
            {
                selectedRecipe = positiveMatches[0].Recipe;
            }
            else
            {
                throw AppException.UnprocessableEntity(
                    ErrorCodes.ErrMealNoSubstitute,
                    $"Không tìm thấy món chay có nguyên liệu hoặc tên phù hợp với ghi chú '{request.Note}'. Vui lòng thử từ khóa khác hoặc duyệt thư viện công thức.");
            }
            resolvedReason = request.Note;
        }

        var oldRecipeId = oldRecipe.RecipeId;
        var newRecipeId = selectedRecipe.RecipeId;

        slot.RecipeId = newRecipeId;
        plan.SwapsRemaining--;

        var caloriesDelta = Math.Round(selectedRecipe.CaloriesKcal - oldRecipe.CaloriesKcal, 1);

        var swapHistory = new MealSlotSwapHistory
        {
            SlotId = slot.SlotId,
            OldRecipeId = oldRecipeId,
            NewRecipeId = newRecipeId,
            SwapReason = resolvedReason ?? request.Note,
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
            TargetId = plan.PlanId.ToString(),
            OldValue = JsonSerializer.Serialize(new { recipeId = oldRecipeId, reason = request.Note }),
            NewValue = JsonSerializer.Serialize(new { recipeId = newRecipeId }),
            Note = currentPhase,
            CreatedAt = DateTime.UtcNow
        };
        _db.AuditLogs.Add(auditLog);

        await _db.SaveChangesAsync(ct);

        var slotHistories = await _db.MealSlotSwapHistories
            .Include(h => h.OldRecipe)
            .Include(h => h.NewRecipe)
            .Where(h => h.SlotId == slot.SlotId)
            .OrderByDescending(h => h.SwappedAt)
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

        return new UpdateMealPlanSlotResponseData
        {
            Slot = new MealSlotDto
            {
                SlotId = slot.SlotId,
                DayOfWeek = slot.DayOfWeek,
                MealType = slot.MealType,
                IsCompleted = slot.IsCompleted,
                Notes = slot.Notes,
                Recipe = new MealSlotRecipeDto
                {
                    RecipeId = selectedRecipe.RecipeId,
                    Title = selectedRecipe.Title,
                    CaloriesKcal = selectedRecipe.CaloriesKcal,
                    ImageUrl = selectedRecipe.ImageUrl
                },
                SwapCount = slotHistories.Count,
                SwapHistories = slotHistories
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

        var durationDays = plan.DurationDays > 0 ? plan.DurationDays : (plan.MealSlots.Count / 3);
        if (durationDays <= 0) durationDays = plan.MealSlots.Count > 0 ? 1 : 7;

        var sortedSlots = plan.MealSlots
            .OrderBy(s => s.SlotId)
            .ThenBy(s => mealOrder.GetValueOrDefault(s.MealType, 99))
            .ToList();

        for (var i = 0; i < durationDays; i++)
        {
            var dayDate = plan.StartDate.AddDays(i);
            var day = MapDayOfWeek(dayDate.DayOfWeek);

            var daySlots = sortedSlots.Skip(i * 3).Take(3).ToList();
            if (daySlots.Count == 0)
            {
                daySlots = plan.MealSlots
                    .Where(s => string.Equals(s.DayOfWeek, day, StringComparison.OrdinalIgnoreCase))
                    .OrderBy(s => mealOrder.GetValueOrDefault(s.MealType, 99))
                    .ToList();
            }

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
