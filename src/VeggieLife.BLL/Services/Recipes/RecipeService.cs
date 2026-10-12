using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using VeggieLife.BLL.Common.Constants;
using VeggieLife.BLL.Common.Exceptions;
using VeggieLife.BLL.Common.Responses;
using VeggieLife.BLL.DTOs.Recipes.Requests;
using VeggieLife.BLL.DTOs.Recipes.Responses;
using VeggieLife.BLL.Services.Cache;
using VeggieLife.DAL.Context;
using VeggieLife.DAL.Entities;

using VeggieLife.BLL.DTOs.Ai;
using VeggieLife.BLL.Services.Knowledge;
using VeggieLife.BLL.Services.Moderation;

namespace VeggieLife.BLL.Services.Recipes;

/// <summary>
/// Dịch vụ xử lý tìm kiếm, lọc, xem chi tiết và quản lý công thức nấu ăn món chay (FN-POST-01, FN-POST-02, FN-POST-03, FN-POST-04).
/// Tuân thủ các quy tắc: BR-GUEST-01, BR-ADM-CNT-001, BR-ADM-CNT-002, BR-ADM-CNT-003, BR-REC-MOD-01, BR-USR-REC-01, BR-USR-REC-02, BR-USR-04, BR-USR-05.
/// </summary>
public class RecipeService : IRecipeService
{
    private readonly AppDbContext _db;
    private readonly IRedisService _redis;
    private readonly ILogger<RecipeService> _logger;
    private readonly IContentModerationService _moderationService;
    private readonly IKnowledgeSyncQueue _knowledgeSyncQueue;

    private static readonly TimeSpan RecipeSearchCacheTtl = TimeSpan.FromHours(24); // BR-SYS-CACHE-01
    private static readonly TimeSpan RecipeDetailCacheTtl = TimeSpan.FromHours(24); // BR-SYS-CACHE-01
    private static readonly TimeSpan GuestHistoryTtl = TimeSpan.FromHours(24);      // BR-GUEST-AUTH-01

    public RecipeService(
        AppDbContext db,
        IRedisService redis,
        ILogger<RecipeService> logger,
        IContentModerationService? moderationService = null,
        IKnowledgeSyncQueue? knowledgeSyncQueue = null)
    {
        _db = db;
        _redis = redis;
        _logger = logger;
        _moderationService = moderationService ?? new SimulatedContentModerationService(new Microsoft.Extensions.Logging.Abstractions.NullLogger<SimulatedContentModerationService>());
        _knowledgeSyncQueue = knowledgeSyncQueue ?? new KnowledgeSyncQueueStub(new Microsoft.Extensions.Logging.Abstractions.NullLogger<KnowledgeSyncQueueStub>());
    }

    /// <summary>
    /// Tìm kiếm và lọc công thức, video nấu ăn công khai đã được phê duyệt (FN-POST-01).
    /// </summary>
    public async Task<(IReadOnlyList<RecipeSummaryDto> Items, PagedMeta Meta)> ListRecipesAsync(
        RecipeFilterRequest filter,
        bool isGuest,
        string? guestId,
        CancellationToken ct = default)
    {
        // 1. Kiểm tra tham số hợp lệ (Validation)
        ValidateFilterRequest(filter);

        var normalizedKeyword = filter.Keyword?.Trim();
        var normalizedIngredient = filter.Ingredient?.Trim();
        var normalizedContentType = (filter.ContentType ?? "ALL").ToUpperInvariant();
        var normalizedSort = (filter.Sort ?? "POPULAR").ToUpperInvariant();

        // 2. Tra cứu bộ nhớ đệm Redis: recipe_search:{hash} (BR-SYS-CACHE-01)
        var cacheHash = ComputeSearchHash(
            normalizedKeyword,
            filter.CategoryId,
            filter.DietarySchoolId,
            normalizedIngredient,
            filter.MaxCookTime,
            normalizedContentType,
            normalizedSort,
            filter.Page,
            filter.Size);

        var cacheKey = $"recipe_search:{cacheHash}";
        var cachedData = await _redis.GetAsync<RecipeSearchResultCache>(cacheKey, ct);
        if (cachedData != null)
        {
            _logger.LogInformation("Lấy danh sách công thức từ cache: {CacheKey}", cacheKey);
            var cachedMeta = new PagedMeta(
                filter.Page,
                filter.Size,
                cachedData.Total,
                isGuest ? 5 : null); // BR-GUEST-01: signUpPromptAtItem = 5 cho Khách

            // Ghi nhận lịch sử tìm kiếm cho Khách nếu có từ khóa (BR-GUEST-AUTH-01)
            if (isGuest && !string.IsNullOrWhiteSpace(normalizedKeyword) && !string.IsNullOrWhiteSpace(guestId))
            {
                await SaveGuestSearchHistoryAsync(guestId, normalizedKeyword, ct);
            }

            return (cachedData.Items, cachedMeta);
        }

        // 3. Khách: Lưu từ khóa vào Redis guest_search_history:{guestId} (≤ 20 mục, TTL 24h) (BR-GUEST-AUTH-01)
        if (isGuest && !string.IsNullOrWhiteSpace(normalizedKeyword) && !string.IsNullOrWhiteSpace(guestId))
        {
            await SaveGuestSearchHistoryAsync(guestId, normalizedKeyword, ct);
        }

        // 4. Xây dựng truy vấn CSDL: Chỉ lấy Status = APPROVED_ACTIVE (BR-ADM-CNT-001) và Category.IsActive (BR-ADM-MAS-001)
        var query = _db.Recipes
            .AsNoTracking()
            .Where(r => r.Status == "APPROVED_ACTIVE" && r.Category.IsActive);

        // Lọc theo Danh mục
        if (filter.CategoryId.HasValue)
        {
            query = query.Where(r => r.CategoryId == filter.CategoryId.Value);
        }

        // Lọc theo Trường phái chay
        if (filter.DietarySchoolId.HasValue)
        {
            query = query.Where(r => r.DietarySchoolId == filter.DietarySchoolId.Value);
        }

        // Lọc theo Từ khóa tên món / mô tả
        if (!string.IsNullOrWhiteSpace(normalizedKeyword))
        {
            query = query.Where(r => r.Title.Contains(normalizedKeyword) || (r.Description != null && r.Description.Contains(normalizedKeyword)));
        }

        // Lọc theo Nguyên liệu
        if (!string.IsNullOrWhiteSpace(normalizedIngredient))
        {
            query = query.Where(r => r.RecipeIngredients.Any(ri => ri.Ingredient.IngredientName.Contains(normalizedIngredient)));
        }

        // Lọc theo Thời gian nấu tối đa
        if (filter.MaxCookTime.HasValue)
        {
            query = query.Where(r => r.CookTimeMinutes <= filter.MaxCookTime.Value);
        }

        // Lọc theo Loại nội dung (RECIPE | VIDEO | ALL)
        if (normalizedContentType == "RECIPE")
        {
            query = query.Where(r => !r.Posts.Any(p => p.Videos.Any(v => v.Status == "READY")));
        }
        else if (normalizedContentType == "VIDEO")
        {
            query = query.Where(r => r.Posts.Any(p => p.Videos.Any(v => v.Status == "READY")));
        }

        // Đếm tổng số bản ghi thỏa mãn điều kiện
        var total = await query.CountAsync(ct);

        // Sắp xếp
        IQueryable<Recipe> orderedQuery;
        if (normalizedSort == "NEWEST")
        {
            orderedQuery = query.OrderByDescending(r => r.CreatedAt);
        }
        else
        {
            // POPULAR: Sắp xếp theo số vote giảm dần, sau đó theo lượt xem (ViewCount)
            orderedQuery = query
                .OrderByDescending(r => r.Posts.SelectMany(p => p.Votes).Count())
                .ThenByDescending(r => r.ViewCount);
        }

        // Phân trang và chiếu DTO
        var items = await orderedQuery
            .Skip((filter.Page - 1) * filter.Size)
            .Take(filter.Size)
            .Select(r => new RecipeSummaryDto
            {
                RecipeId = r.RecipeId,
                Type = r.Posts.Any(p => p.Videos.Any(v => v.Status == "READY")) ? "VIDEO" : "RECIPE",
                Title = r.Title,
                ImageUrl = r.ImageUrl,
                DietarySchool = r.DietarySchool.SchoolCode,
                CookTimeMinutes = r.CookTimeMinutes,
                CaloriesKcal = r.CaloriesKcal,
                VoteCount = r.Posts.SelectMany(p => p.Votes).Count(),
                IsVerified = r.IsVerified
            })
            .ToListAsync(ct);

        // 5. Lưu kết quả vào bộ nhớ đệm Redis: recipe_search:{hash} (BR-SYS-CACHE-01)
        var cacheToStore = new RecipeSearchResultCache
        {
            Items = items,
            Total = total
        };
        await _redis.SetAsync(cacheKey, cacheToStore, RecipeSearchCacheTtl, ct);

        // 6. Trả về kết quả kèm PagedMeta (BR-GUEST-01)
        var meta = new PagedMeta(filter.Page, filter.Size, total, isGuest ? 5 : null);
        return (items, meta);
    }

    /// <summary>
    /// Xem chi tiết đầy đủ công thức nấu ăn đã được duyệt (FN-POST-02).
    /// </summary>
    public async Task<RecipeDetailDto> GetRecipeByIdAsync(
        int id,
        bool isGuest,
        int? currentUserId = null,
        CancellationToken ct = default)
    {
        // 1. Kiểm tra mã id > 0
        if (id <= 0)
        {
            throw AppException.BadRequest(ErrorCodes.ErrValidation, "Mã công thức phải lớn hơn 0.");
        }

        var detailCacheKey = $"recipe_detail:{id}";

        // 2. Tra cứu cache Redis: recipe_detail:{id} (BR-SYS-CACHE-01)
        var cachedRecipe = await _redis.GetAsync<RecipeDetailDto>(detailCacheKey, ct);
        if (cachedRecipe != null)
        {
            _logger.LogInformation("Lấy chi tiết công thức từ cache: {CacheKey}", detailCacheKey);

            // Cập nhật lại quyền thao tác tương tác theo người gọi hiện tại (BR-AUTH-03)
            cachedRecipe.MemberActions = new MemberActionsDto
            {
                CanVote = !isGuest,
                CanComment = !isGuest,
                CanReport = !isGuest
            };

            // Tăng View_Count qua Redis counter (FN-POST-02)
            await IncrementViewCountAsync(id, ct);

            return cachedRecipe;
        }

        // 3. Truy vấn CSDL: SELECT Recipes + RecipeIngredients + Ingredients + FoodCategories + DietarySchools
        var recipe = await _db.Recipes
            .Include(r => r.Category)
            .Include(r => r.DietarySchool)
            .Include(r => r.RecipeIngredients)
                .ThenInclude(ri => ri.Ingredient)
            .Include(r => r.VideoTranscripts)
            .Include(r => r.Posts)
                .ThenInclude(p => p.Videos)
                    .ThenInclude(v => v.VideoTranscript)
            .Include(r => r.Posts)
                .ThenInclude(p => p.Votes)
            .Include(r => r.Posts)
                .ThenInclude(p => p.Comments)
            .FirstOrDefaultAsync(r => r.RecipeId == id, ct);

        // 4. Kiểm tra điều kiện: Không tồn tại hoặc chưa APPROVED_ACTIVE thì báo 404 (BR-ADM-CNT-001)
        // Ngoại lệ: tác giả có thể xem trước nội dung nháp của chính mình
        if (recipe == null)
        {
            throw AppException.NotFound(ErrorCodes.ErrContentNotFound, "Nội dung không tồn tại hoặc chưa ở trạng thái APPROVED_ACTIVE.");
        }

        var isAuthor = currentUserId.HasValue && recipe.UserId == currentUserId.Value;
        if (recipe.Status != "APPROVED_ACTIVE" && !isAuthor)
        {
            throw AppException.NotFound(ErrorCodes.ErrContentNotFound, "Nội dung không tồn tại hoặc chưa ở trạng thái APPROVED_ACTIVE.");
        }

        // 5. Xác định hasTranscript: VideoTranscripts COMPLETED hoặc Video gắn Post có Transcript COMPLETED
        var hasTranscript = recipe.VideoTranscripts.Any(vt => vt.Status == "COMPLETED") ||
                            recipe.Posts.Any(p => p.Videos.Any(v => v.VideoTranscript != null && v.VideoTranscript.Status == "COMPLETED"));

        // 6. Tính số bình luận và lượt vote
        var voteCount = recipe.Posts.SelectMany(p => p.Votes).Count();
        var commentCount = recipe.Posts.SelectMany(p => p.Comments).Count(c => !c.IsSoftDeleted);

        // 7. Xác định Type (VIDEO nếu có video READY, ngược lại là RECIPE)
        var hasReadyVideo = recipe.Posts.Any(p => p.Videos.Any(v => v.Status == "READY"));
        var contentType = hasReadyVideo ? "VIDEO" : "RECIPE";

        // 8. Chuyển đổi toàn văn các bước nấu ăn (Instructions) thành danh sách chuỗi
        var parsedInstructions = ParseInstructions(recipe.Instructions);

        // 9. Chuyển đổi danh sách nguyên liệu
        var ingredients = recipe.RecipeIngredients
            .OrderBy(ri => ri.IngredientId)
            .Select(ri => new RecipeIngredientDetailDto
            {
                IngredientId = ri.IngredientId,
                Name = ri.Ingredient.IngredientName,
                Quantity = ri.QuantityNeeded,
                Unit = ri.Unit
            })
            .ToList();

        // 10. Quyền tương tác: Khách canVote/canComment/canReport = false (BR-AUTH-03)
        var memberActions = new MemberActionsDto
        {
            CanVote = !isGuest,
            CanComment = !isGuest,
            CanReport = !isGuest
        };

        var detailDto = new RecipeDetailDto
        {
            RecipeId = recipe.RecipeId,
            Type = contentType,
            Title = recipe.Title,
            ImageUrl = recipe.ImageUrl,
            DietarySchool = recipe.DietarySchool.SchoolCode,
            CookTimeMinutes = recipe.CookTimeMinutes,
            CaloriesKcal = recipe.CaloriesKcal,
            VoteCount = voteCount,
            IsVerified = recipe.IsVerified,
            Description = recipe.Description,
            Servings = recipe.Servings,
            PrepTimeMinutes = recipe.PrepTimeMinutes,
            Instructions = parsedInstructions,
            Ingredients = ingredients,
            HasTranscript = hasTranscript,
            CommentCount = commentCount,
            MemberActions = memberActions
        };

        // 11. Lưu vào cache Redis: recipe_detail:{id} (TTL 24h)
        await _redis.SetAsync(detailCacheKey, detailDto, RecipeDetailCacheTtl, ct);

        // 12. Tăng View_Count trong CSDL & Redis counter
        await IncrementViewCountAsync(id, ct);

        return detailDto;
    }

    /// <summary>
    /// Kiểm tra và xác thực các tham số bộ lọc tìm kiếm theo đúng ràng buộc đặc tả.
    /// </summary>
    private static void ValidateFilterRequest(RecipeFilterRequest filter)
    {
        if (filter.Keyword != null && filter.Keyword.Length > 100)
        {
            throw AppException.BadRequest(ErrorCodes.ErrValidation, "Từ khóa tìm kiếm tối đa 100 ký tự.");
        }

        if (filter.CategoryId.HasValue && filter.CategoryId.Value <= 0)
        {
            throw AppException.BadRequest(ErrorCodes.ErrValidation, "Mã danh mục không hợp lệ (phải > 0).");
        }

        if (filter.DietarySchoolId.HasValue && (filter.DietarySchoolId.Value < 1 || filter.DietarySchoolId.Value > 7))
        {
            throw AppException.BadRequest(ErrorCodes.ErrValidation, "Mã trường phái không hợp lệ (phải từ 1 đến 7).");
        }

        if (filter.Ingredient != null && filter.Ingredient.Length > 50)
        {
            throw AppException.BadRequest(ErrorCodes.ErrValidation, "Tên nguyên liệu tìm kiếm tối đa 50 ký tự.");
        }

        if (filter.MaxCookTime.HasValue && (filter.MaxCookTime.Value < 1 || filter.MaxCookTime.Value > 600))
        {
            throw AppException.BadRequest(ErrorCodes.ErrValidation, "Thời gian nấu phải trong khoảng 1 đến 600 phút.");
        }

        var ct = (filter.ContentType ?? "ALL").ToUpperInvariant();
        if (ct != "ALL" && ct != "RECIPE" && ct != "VIDEO")
        {
            throw AppException.BadRequest(ErrorCodes.ErrValidation, "Loại nội dung không hợp lệ (chỉ chấp nhận: ALL, RECIPE, VIDEO).");
        }

        var sort = (filter.Sort ?? "POPULAR").ToUpperInvariant();
        if (sort != "POPULAR" && sort != "NEWEST")
        {
            throw AppException.BadRequest(ErrorCodes.ErrValidation, "Tiêu chí sắp xếp không hợp lệ (chỉ chấp nhận: POPULAR, NEWEST).");
        }

        if (filter.Page < 1)
        {
            throw AppException.BadRequest(ErrorCodes.ErrValidation, "Số trang (page) phải lớn hơn hoặc bằng 1.");
        }

        if (filter.Size < 1 || filter.Size > 20)
        {
            throw AppException.BadRequest(ErrorCodes.ErrValidation, "Kích thước trang (size) phải từ 1 đến 20.");
        }
    }

    /// <summary>
    /// Tính toán chuỗi mã băm MD5 duy nhất cho bộ tham số tìm kiếm nhằm lưu cache Redis.
    /// </summary>
    private static string ComputeSearchHash(
        string? keyword,
        int? categoryId,
        int? dietarySchoolId,
        string? ingredient,
        int? maxCookTime,
        string contentType,
        string sort,
        int page,
        int size)
    {
        var rawKey = $"{keyword?.ToLowerInvariant()}|{categoryId}|{dietarySchoolId}|{ingredient?.ToLowerInvariant()}|{maxCookTime}|{contentType}|{sort}|{page}|{size}";
        using var md5 = MD5.Create();
        var bytes = md5.ComputeHash(Encoding.UTF8.GetBytes(rawKey));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    /// <summary>
    /// Lưu từ khóa tìm kiếm của Khách vào Redis guest_search_history:{guestId} (tối đa 20 mục, TTL 24 giờ).
    /// </summary>
    private async Task SaveGuestSearchHistoryAsync(string guestId, string keyword, CancellationToken ct)
    {
        try
        {
            var key = $"guest_search_history:{guestId}";
            var history = await _redis.GetAsync<List<string>>(key, ct) ?? new List<string>();

            // Loại bỏ từ khóa trùng (không phân biệt hoa thường)
            history.RemoveAll(k => string.Equals(k, keyword, StringComparison.OrdinalIgnoreCase));
            // Đưa từ khóa mới lên đầu
            history.Insert(0, keyword);

            // Giữ tối đa 20 mục gần nhất (BR-GUEST-AUTH-01)
            if (history.Count > 20)
            {
                history = history.Take(20).ToList();
            }

            await _redis.SetAsync(key, history, GuestHistoryTtl, ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Không thể lưu lịch sử tìm kiếm khách cho guestId '{GuestId}'.", guestId);
        }
    }

    /// <summary>
    /// Tăng lượt xem của công thức cả trên CSDL và bộ đếm phân tán Redis.
    /// </summary>
    private async Task IncrementViewCountAsync(int recipeId, CancellationToken ct)
    {
        try
        {
            // Tăng bộ đếm Redis
            await _redis.IncrementAsync($"view_counter:recipe:{recipeId}", 1, TimeSpan.FromDays(7), ct);

            // Cập nhật ViewCount trực tiếp trong CSDL
            var entity = await _db.Recipes.FindAsync(new object[] { recipeId }, ct);
            if (entity != null)
            {
                entity.ViewCount += 1;
                await _db.SaveChangesAsync(ct);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Lỗi khi tăng lượt xem công thức ID {RecipeId}.", recipeId);
        }
    }

    /// <summary>
    /// Phân tích toàn văn các bước nấu ăn thành mảng chuỗi (hỗ trợ cả JSON array hoặc phân tách theo dòng).
    /// </summary>
    private static List<string> ParseInstructions(string rawInstructions)
    {
        if (string.IsNullOrWhiteSpace(rawInstructions))
        {
            return new List<string>();
        }

        var trimmed = rawInstructions.Trim();
        if (trimmed.StartsWith("[") && trimmed.EndsWith("]"))
        {
            try
            {
                var list = JsonSerializer.Deserialize<List<string>>(trimmed);
                if (list != null && list.Count > 0)
                {
                    return list;
                }
            }
            catch
            {
                // Fallback nếu không phải mảng JSON hợp lệ
            }
        }

        return trimmed
            .Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToList();
    }

    /// <summary>
    /// Đăng công thức nấu ăn món chay mới: lưu nháp hoặc gửi duyệt kèm kiểm duyệt AI và Red Alert (FN-POST-03).
    /// </summary>
    public async Task<CreateRecipeResponseDto> CreateRecipeAsync(
        int userId,
        CreateRecipeRequest request,
        CancellationToken ct = default)
    {
        if (request == null)
        {
            throw AppException.BadRequest(ErrorCodes.ErrValidation, "Dữ liệu yêu cầu không được để trống.");
        }

        // 1. Kiểm tra giới hạn đăng bài theo ngày (BR-USR-04) khi submit = true
        if (request.Submit)
        {
            await CheckDailyLimitAsync(userId, ct);
            ValidateRecipeStructure(request);
        }

        // 2. Kiểm tra tồn tại của Danh mục và Trường phái
        var school = await _db.DietarySchools.AsNoTracking().FirstOrDefaultAsync(s => s.DietarySchoolId == request.DietarySchoolId, ct);
        if (school == null && request.Submit)
        {
            throw new AppException(422, ErrorCodes.ErrRecipeStructureInvalid, "Trường phái ăn chay không tồn tại.");
        }

        var categoryExists = await _db.FoodCategories.AnyAsync(c => c.CategoryId == request.CategoryId, ct);
        if (!categoryExists && request.Submit)
        {
            throw new AppException(422, ErrorCodes.ErrRecipeStructureInvalid, "Danh mục món ăn không tồn tại.");
        }

        // 3. Xử lý chế độ Lưu nháp (submit = false)
        if (!request.Submit)
        {
            var draftCalories = await CalculateCaloriesAsync(request.Ingredients, ct);
            var draftRecipe = new Recipe
            {
                UserId = userId,
                CategoryId = request.CategoryId > 0 ? request.CategoryId : 1,
                DietarySchoolId = request.DietarySchoolId > 0 ? request.DietarySchoolId : 1,
                Title = !string.IsNullOrWhiteSpace(request.Title) ? request.Title.Trim() : "Công thức chưa đặt tên",
                Description = request.Description?.Trim(),
                Instructions = request.Instructions != null && request.Instructions.Count > 0
                    ? JsonSerializer.Serialize(request.Instructions)
                    : "[]",
                PrepTimeMinutes = request.PrepTimeMinutes >= 0 ? request.PrepTimeMinutes : 15,
                CookTimeMinutes = request.CookTimeMinutes > 0 ? request.CookTimeMinutes : 20,
                Servings = request.Servings >= 1 ? request.Servings : 2,
                CaloriesKcal = draftCalories,
                DifficultyLevel = !string.IsNullOrWhiteSpace(request.DifficultyLevel) ? request.DifficultyLevel.ToUpperInvariant() : "MEDIUM",
                IsVerified = false,
                ImageUrl = request.ImageUrl?.Trim(),
                Status = "DRAFT",
                ViewCount = 0,
                Source = "USER",
                CreatedAt = DateTime.UtcNow
            };

            _db.Recipes.Add(draftRecipe);
            await _db.SaveChangesAsync(ct);

            // Thêm nguyên liệu định lượng nếu có
            if (request.Ingredients != null && request.Ingredients.Count > 0)
            {
                foreach (var ing in request.Ingredients.Where(i => i.IngredientId > 0))
                {
                    _db.RecipeIngredients.Add(new RecipeIngredient
                    {
                        RecipeId = draftRecipe.RecipeId,
                        IngredientId = ing.IngredientId,
                        QuantityNeeded = ing.Quantity > 0 ? ing.Quantity : 1,
                        Unit = !string.IsNullOrWhiteSpace(ing.Unit) ? ing.Unit.Trim() : "phần",
                        IsOptional = ing.IsOptional
                    });
                }
                await _db.SaveChangesAsync(ct);
            }

            return new CreateRecipeResponseDto
            {
                RecipeId = draftRecipe.RecipeId,
                Status = "DRAFT",
                AiConfidence = 0.0
            };
        }

        // 4. Kiểm tra tất định Red Alert theo trường phái trước khi gọi AI (BR-ADM-CNT-003, D109)
        var forbiddenCheck = await CheckForbiddenIngredientsAsync(request.DietarySchoolId, request.Ingredients, ct);
        if (forbiddenCheck.IsForbidden)
        {
            var quarantinedCalories = await CalculateCaloriesAsync(request.Ingredients, ct);
            var quarantinedRecipe = new Recipe
            {
                UserId = userId,
                CategoryId = request.CategoryId,
                DietarySchoolId = request.DietarySchoolId,
                Title = request.Title.Trim(),
                Description = request.Description?.Trim(),
                Instructions = JsonSerializer.Serialize(request.Instructions),
                PrepTimeMinutes = request.PrepTimeMinutes,
                CookTimeMinutes = request.CookTimeMinutes,
                Servings = request.Servings,
                CaloriesKcal = quarantinedCalories,
                DifficultyLevel = request.DifficultyLevel?.ToUpperInvariant() ?? "MEDIUM",
                IsVerified = false,
                ImageUrl = request.ImageUrl?.Trim(),
                Status = "QUARANTINED_HIDDEN",
                ViewCount = 0,
                Source = "USER",
                CreatedAt = DateTime.UtcNow
            };

            _db.Recipes.Add(quarantinedRecipe);
            await _db.SaveChangesAsync(ct);

            foreach (var ing in request.Ingredients)
            {
                _db.RecipeIngredients.Add(new RecipeIngredient
                {
                    RecipeId = quarantinedRecipe.RecipeId,
                    IngredientId = ing.IngredientId,
                    QuantityNeeded = ing.Quantity,
                    Unit = ing.Unit.Trim(),
                    IsOptional = ing.IsOptional
                });
            }

            _db.ViolationReports.Add(new ViolationReport
            {
                RecipeId = quarantinedRecipe.RecipeId,
                ReporterUserId = null,
                ViolationType = forbiddenCheck.ViolationType,
                EvidenceSummary = forbiddenCheck.Reason.Length > 500 ? forbiddenCheck.Reason[..500] : forbiddenCheck.Reason,
                ReportSource = "AI_SCAN",
                Status = "PENDING",
                CreatedAt = DateTime.UtcNow
            });

            await _db.SaveChangesAsync(ct);
            throw new AppException(422, ErrorCodes.ErrRecipeRedAlert, forbiddenCheck.Reason);
        }

        // 5. Quét kiểm duyệt nội dung qua AI Service (NLP + LLM Multimodal - BR-USR-03, D118)
        var ingredientNames = await _db.Ingredients.AsNoTracking()
            .Where(i => request.Ingredients.Select(x => x.IngredientId).Contains(i.IngredientId))
            .Select(i => i.IngredientName)
            .ToListAsync(ct);

        var moderationReq = new ModerationCheckRequest
        {
            Title = request.Title.Trim(),
            Description = request.Description?.Trim(),
            IngredientNames = ingredientNames,
            Steps = request.Instructions,
            DietarySchoolCode = school?.SchoolCode ?? "VEGAN",
            ImageUrls = !string.IsNullOrWhiteSpace(request.ImageUrl) ? new List<string> { request.ImageUrl.Trim() } : new List<string>()
        };

        var aiResult = await _moderationService.CheckContentAsync(moderationReq, ct);
        await LogAiQuotaAsync(userId, aiResult, ct);

        var isMeatViolation = aiResult.ViolationType == "VIO_MEAT";
        var isLabelViolation = aiResult.ViolationType == "VIO_LABEL";
        var isAiQuarantined = isMeatViolation || isLabelViolation || aiResult.ViolationScore >= 0.85 || aiResult.ViolationType != "NONE";

        var finalStatus = isAiQuarantined ? "QUARANTINED_HIDDEN" : "APPROVED_ACTIVE";
        var calculatedCalories = await CalculateCaloriesAsync(request.Ingredients, ct);

        var recipe = new Recipe
        {
            UserId = userId,
            CategoryId = request.CategoryId,
            DietarySchoolId = request.DietarySchoolId,
            Title = request.Title.Trim(),
            Description = request.Description?.Trim(),
            Instructions = JsonSerializer.Serialize(request.Instructions),
            PrepTimeMinutes = request.PrepTimeMinutes,
            CookTimeMinutes = request.CookTimeMinutes,
            Servings = request.Servings,
            CaloriesKcal = calculatedCalories,
            DifficultyLevel = request.DifficultyLevel?.ToUpperInvariant() ?? "MEDIUM",
            IsVerified = false,
            ImageUrl = request.ImageUrl?.Trim(),
            Status = finalStatus,
            ViewCount = 0,
            Source = "USER",
            CreatedAt = DateTime.UtcNow
        };

        _db.Recipes.Add(recipe);
        await _db.SaveChangesAsync(ct);

        foreach (var ing in request.Ingredients)
        {
            _db.RecipeIngredients.Add(new RecipeIngredient
            {
                RecipeId = recipe.RecipeId,
                IngredientId = ing.IngredientId,
                QuantityNeeded = ing.Quantity,
                Unit = ing.Unit.Trim(),
                IsOptional = ing.IsOptional
            });
        }
        await _db.SaveChangesAsync(ct);

        if (isAiQuarantined)
        {
            var evidence = aiResult.Reasons.Count > 0
                ? string.Join("; ", aiResult.Reasons)
                : $"AI phát hiện vi phạm loại {aiResult.ViolationType} (Điểm vi phạm: {aiResult.ViolationScore:0.00}).";
            if (evidence.Length > 500) evidence = evidence[..500];

            _db.ViolationReports.Add(new ViolationReport
            {
                RecipeId = recipe.RecipeId,
                ReporterUserId = null,
                ViolationType = aiResult.ViolationType != "NONE" ? aiResult.ViolationType : "VIO_HATE",
                EvidenceSummary = evidence,
                ReportSource = "AI_SCAN",
                Status = "PENDING",
                CreatedAt = DateTime.UtcNow
            });
            await _db.SaveChangesAsync(ct);

            if (isMeatViolation)
            {
                throw new AppException(422, ErrorCodes.ErrContentViolationVegan,
                    "Công thức chứa nguyên liệu hoặc nội dung thịt động vật — vi phạm chuẩn món chay (D109).");
            }

            if (isLabelViolation)
            {
                throw new AppException(422, ErrorCodes.ErrRecipeRedAlert,
                    "Nguyên liệu hoặc nội dung trái với nhãn trường phái đã khai báo (Red Alert, D109).");
            }
        }
        else
        {
            var repPost = new Post
            {
                UserId = userId,
                RecipeId = recipe.RecipeId,
                Title = recipe.Title,
                Content = recipe.Description ?? recipe.Title,
                ImageUrl = recipe.ImageUrl,
                Visibility = "PUBLIC",
                ModerationStatus = "APPROVED_ACTIVE",
                CreatedAt = DateTime.UtcNow
            };
            _db.Posts.Add(repPost);
            await _db.SaveChangesAsync(ct);

            await _knowledgeSyncQueue.EnqueueUpsertAsync("RECIPE", recipe.RecipeId, ct);
        }

        await InvalidateRecipeCachesAsync(recipe.RecipeId, ct);

        return new CreateRecipeResponseDto
        {
            RecipeId = recipe.RecipeId,
            Status = recipe.Status,
            AiConfidence = Math.Round(aiResult.ViolationScore, 2)
        };
    }

    /// <summary>
    /// Cập nhật công thức nấu ăn của mình (FN-POST-04, BR-USR-REC-01).
    /// </summary>
    public async Task<UpdateRecipeResponseDto> UpdateRecipeAsync(
        int id,
        int userId,
        string userRole,
        UpdateRecipeRequest request,
        CancellationToken ct = default)
    {
        if (id <= 0)
        {
            throw AppException.BadRequest(ErrorCodes.ErrValidation, "Mã công thức không hợp lệ (phải > 0).");
        }
        if (request == null)
        {
            throw AppException.BadRequest(ErrorCodes.ErrValidation, "Dữ liệu cập nhật không được để trống.");
        }

        var recipe = await _db.Recipes
            .Include(r => r.RecipeIngredients)
            .FirstOrDefaultAsync(r => r.RecipeId == id, ct);

        if (recipe == null || recipe.Status == "DELETED" || recipe.Status == "ARCHIVED")
        {
            throw AppException.NotFound(ErrorCodes.ErrContentNotFound, "Công thức không tồn tại hoặc đã bị gỡ/xóa.");
        }

        if (recipe.UserId != userId)
        {
            throw AppException.Forbidden(ErrorCodes.ErrContentNotOwner, "Không phải tác giả của nội dung.");
        }

        if (recipe.Status == "QUARANTINED_HIDDEN")
        {
            throw new AppException(409, ErrorCodes.ErrRecipeLocked, "Công thức đang bị cách ly chờ Quản trị viên duyệt, không thể tự chỉnh sửa.");
        }

        var newTitle = !string.IsNullOrWhiteSpace(request.Title) ? request.Title.Trim() : recipe.Title;
        var newDesc = request.Description != null ? request.Description.Trim() : recipe.Description;
        var newCategoryId = request.CategoryId.HasValue && request.CategoryId.Value > 0 ? request.CategoryId.Value : recipe.CategoryId;
        var newDietarySchoolId = request.DietarySchoolId.HasValue && request.DietarySchoolId.Value > 0 ? request.DietarySchoolId.Value : recipe.DietarySchoolId;
        var newPrepTime = request.PrepTimeMinutes.HasValue ? request.PrepTimeMinutes.Value : recipe.PrepTimeMinutes;
        var newCookTime = request.CookTimeMinutes.HasValue ? request.CookTimeMinutes.Value : recipe.CookTimeMinutes;
        var newServings = request.Servings.HasValue ? request.Servings.Value : recipe.Servings;
        var newDifficulty = !string.IsNullOrWhiteSpace(request.DifficultyLevel) ? request.DifficultyLevel.ToUpperInvariant() : recipe.DifficultyLevel;
        var newImageUrl = request.ImageUrl != null ? request.ImageUrl.Trim() : recipe.ImageUrl;

        var newInstructions = request.Instructions != null && request.Instructions.Count > 0
            ? request.Instructions
            : ParseInstructions(recipe.Instructions);

        var newIngredients = request.Ingredients != null && request.Ingredients.Count > 0
            ? request.Ingredients
            : recipe.RecipeIngredients.Select(ri => new RecipeIngredientItemRequest
            {
                IngredientId = ri.IngredientId,
                Quantity = ri.QuantityNeeded,
                Unit = ri.Unit,
                IsOptional = ri.IsOptional
            }).ToList();

        var isContentChanged = !string.Equals(recipe.Title, newTitle, StringComparison.Ordinal)
            || !string.Equals(recipe.Description, newDesc, StringComparison.Ordinal)
            || recipe.DietarySchoolId != newDietarySchoolId
            || !string.Equals(recipe.ImageUrl, newImageUrl, StringComparison.Ordinal)
            || request.Instructions != null
            || request.Ingredients != null;

        // 1. Nhánh Recipe hiện tại là DRAFT hoặc REJECTED:
        if (recipe.Status == "DRAFT" || recipe.Status == "REJECTED")
        {
            if (!request.Submit)
            {
                recipe.Title = newTitle;
                recipe.Description = newDesc;
                recipe.CategoryId = newCategoryId;
                recipe.DietarySchoolId = newDietarySchoolId;
                recipe.PrepTimeMinutes = newPrepTime;
                recipe.CookTimeMinutes = newCookTime;
                recipe.Servings = newServings;
                recipe.DifficultyLevel = newDifficulty;
                recipe.ImageUrl = newImageUrl;
                recipe.Instructions = JsonSerializer.Serialize(newInstructions);
                recipe.Status = "DRAFT";
                recipe.UpdatedAt = DateTime.UtcNow;

                if (request.Ingredients != null)
                {
                    _db.RecipeIngredients.RemoveRange(recipe.RecipeIngredients);
                    foreach (var ing in newIngredients)
                    {
                        _db.RecipeIngredients.Add(new RecipeIngredient
                        {
                            RecipeId = recipe.RecipeId,
                            IngredientId = ing.IngredientId,
                            QuantityNeeded = ing.Quantity,
                            Unit = ing.Unit.Trim(),
                            IsOptional = ing.IsOptional
                        });
                    }
                }

                recipe.CaloriesKcal = await CalculateCaloriesAsync(newIngredients, ct);
                await _db.SaveChangesAsync(ct);
                await InvalidateRecipeCachesAsync(recipe.RecipeId, ct);

                return new UpdateRecipeResponseDto
                {
                    RecipeId = recipe.RecipeId,
                    Status = "DRAFT",
                    Moderated = false
                };
            }
            else
            {
                await CheckDailyLimitAsync(userId, ct);
                ValidateCandidateStructure(newTitle, newInstructions, newIngredients, newCookTime, newDietarySchoolId);

                var forbiddenCheck = await CheckForbiddenIngredientsAsync(newDietarySchoolId, newIngredients, ct);
                if (forbiddenCheck.IsForbidden)
                {
                    recipe.Status = "QUARANTINED_HIDDEN";
                    recipe.UpdatedAt = DateTime.UtcNow;
                    _db.ViolationReports.Add(new ViolationReport
                    {
                        RecipeId = recipe.RecipeId,
                        ReporterUserId = null,
                        ViolationType = forbiddenCheck.ViolationType,
                        EvidenceSummary = forbiddenCheck.Reason.Length > 500 ? forbiddenCheck.Reason[..500] : forbiddenCheck.Reason,
                        ReportSource = "AI_SCAN",
                        Status = "PENDING",
                        CreatedAt = DateTime.UtcNow
                    });
                    await _db.SaveChangesAsync(ct);
                    throw new AppException(422, ErrorCodes.ErrRecipeRedAlert, forbiddenCheck.Reason);
                }

                var school = await _db.DietarySchools.AsNoTracking().FirstOrDefaultAsync(s => s.DietarySchoolId == newDietarySchoolId, ct);
                var ingNames = await _db.Ingredients.AsNoTracking()
                    .Where(i => newIngredients.Select(x => x.IngredientId).Contains(i.IngredientId))
                    .Select(i => i.IngredientName)
                    .ToListAsync(ct);

                var modReq = new ModerationCheckRequest
                {
                    Title = newTitle,
                    Description = newDesc,
                    IngredientNames = ingNames,
                    Steps = newInstructions,
                    DietarySchoolCode = school?.SchoolCode ?? "VEGAN",
                    ImageUrls = !string.IsNullOrWhiteSpace(newImageUrl) ? new List<string> { newImageUrl } : new List<string>()
                };

                var aiRes = await _moderationService.CheckContentAsync(modReq, ct);
                await LogAiQuotaAsync(userId, aiRes, ct);

                var isMeat = aiRes.ViolationType == "VIO_MEAT";
                var isLabel = aiRes.ViolationType == "VIO_LABEL";
                var isQuar = isMeat || isLabel || aiRes.ViolationScore >= 0.85 || aiRes.ViolationType != "NONE";

                recipe.Status = isQuar ? "QUARANTINED_HIDDEN" : "APPROVED_ACTIVE";
                recipe.Title = newTitle;
                recipe.Description = newDesc;
                recipe.CategoryId = newCategoryId;
                recipe.DietarySchoolId = newDietarySchoolId;
                recipe.PrepTimeMinutes = newPrepTime;
                recipe.CookTimeMinutes = newCookTime;
                recipe.Servings = newServings;
                recipe.DifficultyLevel = newDifficulty;
                recipe.ImageUrl = newImageUrl;
                recipe.Instructions = JsonSerializer.Serialize(newInstructions);
                recipe.UpdatedAt = DateTime.UtcNow;
                recipe.CaloriesKcal = await CalculateCaloriesAsync(newIngredients, ct);

                if (request.Ingredients != null)
                {
                    _db.RecipeIngredients.RemoveRange(recipe.RecipeIngredients);
                    foreach (var ing in newIngredients)
                    {
                        _db.RecipeIngredients.Add(new RecipeIngredient
                        {
                            RecipeId = recipe.RecipeId,
                            IngredientId = ing.IngredientId,
                            QuantityNeeded = ing.Quantity,
                            Unit = ing.Unit.Trim(),
                            IsOptional = ing.IsOptional
                        });
                    }
                }

                if (isQuar)
                {
                    var ev = aiRes.Reasons.Count > 0 ? string.Join("; ", aiRes.Reasons) : "AI phát hiện vi phạm.";
                    if (ev.Length > 500) ev = ev[..500];

                    _db.ViolationReports.Add(new ViolationReport
                    {
                        RecipeId = recipe.RecipeId,
                        ReporterUserId = null,
                        ViolationType = aiRes.ViolationType != "NONE" ? aiRes.ViolationType : "VIO_HATE",
                        EvidenceSummary = ev,
                        ReportSource = "AI_SCAN",
                        Status = "PENDING",
                        CreatedAt = DateTime.UtcNow
                    });
                    await _db.SaveChangesAsync(ct);

                    if (isMeat) throw new AppException(422, ErrorCodes.ErrContentViolationVegan, "Công thức chứa thịt động vật (D109).");
                    if (isLabel) throw new AppException(422, ErrorCodes.ErrRecipeRedAlert, "Nội dung trái với nhãn trường phái (D109).");
                }
                else
                {
                    var repPost = await _db.Posts.FirstOrDefaultAsync(p => p.RecipeId == recipe.RecipeId, ct);
                    if (repPost == null)
                    {
                        repPost = new Post
                        {
                            UserId = userId,
                            RecipeId = recipe.RecipeId,
                            Title = recipe.Title,
                            Content = recipe.Description ?? recipe.Title,
                            ImageUrl = recipe.ImageUrl,
                            Visibility = "PUBLIC",
                            ModerationStatus = "APPROVED_ACTIVE",
                            CreatedAt = DateTime.UtcNow
                        };
                        _db.Posts.Add(repPost);
                    }
                    else
                    {
                        repPost.Title = recipe.Title;
                        repPost.Content = recipe.Description ?? recipe.Title;
                        repPost.ImageUrl = recipe.ImageUrl;
                        repPost.ModerationStatus = "APPROVED_ACTIVE";
                        repPost.UpdatedAt = DateTime.UtcNow;
                    }
                    await _db.SaveChangesAsync(ct);
                    await _knowledgeSyncQueue.EnqueueUpsertAsync("RECIPE", recipe.RecipeId, ct);
                }

                await _db.SaveChangesAsync(ct);
                await InvalidateRecipeCachesAsync(recipe.RecipeId, ct);

                return new UpdateRecipeResponseDto
                {
                    RecipeId = recipe.RecipeId,
                    Status = recipe.Status,
                    Moderated = true
                };
            }
        }

        // 2. Nhánh Recipe hiện tại là APPROVED_ACTIVE:
        if (!isContentChanged)
        {
            recipe.CategoryId = newCategoryId;
            recipe.PrepTimeMinutes = newPrepTime;
            recipe.CookTimeMinutes = newCookTime;
            recipe.Servings = newServings;
            recipe.DifficultyLevel = newDifficulty;
            recipe.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync(ct);
            await InvalidateRecipeCachesAsync(recipe.RecipeId, ct);

            return new UpdateRecipeResponseDto
            {
                RecipeId = recipe.RecipeId,
                Status = "APPROVED_ACTIVE",
                Moderated = false
            };
        }

        // CÓ ĐỔI NỘI DUNG: KIỂM DUYỆT ĐỒNG BỘ TRÊN BẢN MỚI TRƯỚC KHI GHI (BR-USR-REC-01, D107)
        ValidateCandidateStructure(newTitle, newInstructions, newIngredients, newCookTime, newDietarySchoolId);

        var candForbidden = await CheckForbiddenIngredientsAsync(newDietarySchoolId, newIngredients, ct);
        if (candForbidden.IsForbidden)
        {
            // KHÔNG GHI CSDL! Bản công khai giữ nguyên
            throw new AppException(422, ErrorCodes.ErrRecipeRedAlert,
                $"Bản sửa mới chứa nguyên liệu vi phạm trường phái (D109). Bản công khai giữ nguyên: {candForbidden.Reason}");
        }

        var candSchool = await _db.DietarySchools.AsNoTracking().FirstOrDefaultAsync(s => s.DietarySchoolId == newDietarySchoolId, ct);
        var candIngNames = await _db.Ingredients.AsNoTracking()
            .Where(i => newIngredients.Select(x => x.IngredientId).Contains(i.IngredientId))
            .Select(i => i.IngredientName)
            .ToListAsync(ct);

        var candModReq = new ModerationCheckRequest
        {
            Title = newTitle,
            Description = newDesc,
            IngredientNames = candIngNames,
            Steps = newInstructions,
            DietarySchoolCode = candSchool?.SchoolCode ?? "VEGAN",
            ImageUrls = !string.IsNullOrWhiteSpace(newImageUrl) ? new List<string> { newImageUrl } : new List<string>()
        };

        var candAiResult = await _moderationService.CheckContentAsync(candModReq, ct);
        await LogAiQuotaAsync(userId, candAiResult, ct);

        if (candAiResult.ViolationType == "VIO_MEAT")
        {
            throw new AppException(422, ErrorCodes.ErrContentViolationVegan,
                "Bản sửa mới chứa thịt động vật, vi phạm chuẩn món chay. Bản công khai giữ nguyên.");
        }

        if (candAiResult.ViolationType == "VIO_LABEL")
        {
            throw new AppException(422, ErrorCodes.ErrRecipeRedAlert,
                "Bản sửa mới trái với nhãn trường phái đã khai báo (Red Alert, D109). Bản công khai giữ nguyên.");
        }

        if (candAiResult.ViolationScore >= 0.85 || candAiResult.ViolationType != "NONE")
        {
            throw new AppException(422, ErrorCodes.ErrRecipeEditRejected,
                "Bản sửa mới không qua được kiểm duyệt tự động bằng AI (Điểm vi phạm >= 85%). Bản công khai giữ nguyên (D107).");
        }

        // ĐẠT CHUẨN: TIẾN HÀNH GHI VÀO CSDL
        var oldValuesJson = JsonSerializer.Serialize(new
        {
            recipe.Title,
            recipe.Description,
            recipe.DietarySchoolId,
            recipe.Instructions,
            recipe.ImageUrl
        });

        recipe.Title = newTitle;
        recipe.Description = newDesc;
        recipe.CategoryId = newCategoryId;
        recipe.DietarySchoolId = newDietarySchoolId;
        recipe.PrepTimeMinutes = newPrepTime;
        recipe.CookTimeMinutes = newCookTime;
        recipe.Servings = newServings;
        recipe.DifficultyLevel = newDifficulty;
        recipe.ImageUrl = newImageUrl;
        recipe.Instructions = JsonSerializer.Serialize(newInstructions);
        recipe.UpdatedAt = DateTime.UtcNow;
        recipe.CaloriesKcal = await CalculateCaloriesAsync(newIngredients, ct);

        if (request.Ingredients != null)
        {
            _db.RecipeIngredients.RemoveRange(recipe.RecipeIngredients);
            foreach (var ing in newIngredients)
            {
                _db.RecipeIngredients.Add(new RecipeIngredient
                {
                    RecipeId = recipe.RecipeId,
                    IngredientId = ing.IngredientId,
                    QuantityNeeded = ing.Quantity,
                    Unit = ing.Unit.Trim(),
                    IsOptional = ing.IsOptional
                });
            }
        }

        var repPostActive = await _db.Posts.FirstOrDefaultAsync(p => p.RecipeId == recipe.RecipeId, ct);
        if (repPostActive != null)
        {
            repPostActive.Title = recipe.Title;
            repPostActive.Content = recipe.Description ?? recipe.Title;
            repPostActive.ImageUrl = recipe.ImageUrl;
            repPostActive.UpdatedAt = DateTime.UtcNow;
        }

        _db.AuditLogs.Add(new AuditLog
        {
            ActorUserId = userId,
            ActorRole = userRole,
            Action = "UPDATE_RECIPE",
            TargetTable = "Recipes",
            TargetId = recipe.RecipeId.ToString(),
            OldValue = oldValuesJson,
            NewValue = JsonSerializer.Serialize(new { recipe.Title, recipe.Description, recipe.DietarySchoolId, recipe.ImageUrl }),
            Note = "Cập nhật công thức đã duyệt (đã qua kiểm duyệt AI)",
            CreatedAt = DateTime.UtcNow
        });

        await _db.SaveChangesAsync(ct);
        await _knowledgeSyncQueue.EnqueueUpsertAsync("RECIPE", recipe.RecipeId, ct);
        await InvalidateRecipeCachesAsync(recipe.RecipeId, ct);

        return new UpdateRecipeResponseDto
        {
            RecipeId = recipe.RecipeId,
            Status = "APPROVED_ACTIVE",
            Moderated = true
        };
    }

    /// <summary>
    /// Gỡ hoặc xóa mềm công thức của mình (FN-POST-04, BR-USR-REC-02).
    /// </summary>
    public async Task<DeleteRecipeResponseDto> DeleteRecipeAsync(
        int id,
        int userId,
        string userRole,
        string mode,
        CancellationToken ct = default)
    {
        if (id <= 0)
        {
            throw AppException.BadRequest(ErrorCodes.ErrValidation, "Mã công thức không hợp lệ.");
        }

        var recipe = await _db.Recipes.FirstOrDefaultAsync(r => r.RecipeId == id, ct);
        if (recipe == null || recipe.Status == "DELETED")
        {
            throw AppException.NotFound(ErrorCodes.ErrContentNotFound, "Công thức không tồn tại hoặc đã bị xóa.");
        }

        if (recipe.UserId != userId)
        {
            throw AppException.Forbidden(ErrorCodes.ErrContentNotOwner, "Không phải tác giả của nội dung.");
        }

        var normalizedMode = (mode ?? "archive").Trim().ToLowerInvariant();
        if (normalizedMode != "archive" && normalizedMode != "all")
        {
            throw AppException.BadRequest(ErrorCodes.ErrValidation, "Chế độ xóa không hợp lệ (chỉ chấp nhận 'archive' hoặc 'all').");
        }

        var oldStatus = recipe.Status;
        var oldUserId = recipe.UserId;
        var repPost = await _db.Posts.FirstOrDefaultAsync(p => p.RecipeId == recipe.RecipeId, ct);

        if (normalizedMode == "archive")
        {
            recipe.Status = "ARCHIVED";
            recipe.UserId = null;
            recipe.UpdatedAt = DateTime.UtcNow;

            if (repPost != null)
            {
                repPost.Visibility = "PRIVATE";
                repPost.ModerationStatus = "QUARANTINED_HIDDEN";
                repPost.UpdatedAt = DateTime.UtcNow;
            }
        }
        else
        {
            recipe.Status = "DELETED";
            recipe.UpdatedAt = DateTime.UtcNow;

            if (repPost != null)
            {
                repPost.ModerationStatus = "DELETED";
                repPost.UpdatedAt = DateTime.UtcNow;
            }

            await _knowledgeSyncQueue.EnqueueDeleteAsync("RECIPE", recipe.RecipeId, ct);
        }

        _db.AuditLogs.Add(new AuditLog
        {
            ActorUserId = userId,
            ActorRole = userRole,
            Action = "DELETE_RECIPE",
            TargetTable = "Recipes",
            TargetId = recipe.RecipeId.ToString(),
            OldValue = JsonSerializer.Serialize(new { status = oldStatus, userId = oldUserId }),
            NewValue = JsonSerializer.Serialize(new { status = recipe.Status, userId = recipe.UserId }),
            Note = $"Gỡ/Xóa công thức (chế độ: {normalizedMode})",
            CreatedAt = DateTime.UtcNow
        });

        await _db.SaveChangesAsync(ct);
        await InvalidateRecipeCachesAsync(recipe.RecipeId, ct);

        return new DeleteRecipeResponseDto
        {
            RecipeId = recipe.RecipeId,
            Status = recipe.Status
        };
    }

    private async Task CheckDailyLimitAsync(int userId, CancellationToken ct)
    {
        var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.UserId == userId, ct);
        if (user == null)
        {
            throw AppException.Unauthorized(ErrorCodes.ErrAuthInvalidToken, "Không tìm thấy người dùng.");
        }

        var todayUtc = DateTime.UtcNow.Date;
        var accountAgeDays = (DateTime.UtcNow - user.CreatedAt).TotalDays;
        var maxLimit = accountAgeDays < 3.0 ? 2 : 10;

        var postsCount = await _db.Posts
            .CountAsync(p => p.UserId == userId && p.CreatedAt >= todayUtc && p.ModerationStatus != "DRAFT", ct);

        var recipesCount = await _db.Recipes
            .CountAsync(r => r.UserId == userId && r.CreatedAt >= todayUtc && r.Status != "DRAFT", ct);

        if (postsCount + recipesCount >= maxLimit)
        {
            throw new AppException(429, ErrorCodes.ErrPostDailyLimitExceeded,
                $"Tài khoản đã đạt giới hạn tối đa {maxLimit} nội dung đăng tải trong ngày (BR-USR-04).");
        }
    }

    private static void ValidateRecipeStructure(CreateRecipeRequest request)
    {
        ValidateCandidateStructure(request.Title, request.Instructions, request.Ingredients, request.CookTimeMinutes, request.DietarySchoolId);
    }

    private static void ValidateCandidateStructure(
        string title,
        List<string>? instructions,
        List<RecipeIngredientItemRequest>? ingredients,
        int cookTimeMinutes,
        int dietarySchoolId)
    {
        if (string.IsNullOrWhiteSpace(title) || title.Trim().Length < 10 || title.Trim().Length > 100)
        {
            throw new AppException(422, ErrorCodes.ErrRecipeStructureInvalid, "Tiêu đề công thức phải từ 10 đến 100 ký tự (BR-USR-05).");
        }

        if (instructions == null || instructions.Count < 2 || instructions.Any(string.IsNullOrWhiteSpace))
        {
            throw new AppException(422, ErrorCodes.ErrRecipeStructureInvalid, "Công thức phải có ít nhất 2 bước hướng dẫn thực hiện (BR-USR-05).");
        }

        if (ingredients == null || ingredients.Count < 1 || ingredients.Any(i => i.Quantity <= 0 || string.IsNullOrWhiteSpace(i.Unit)))
        {
            throw new AppException(422, ErrorCodes.ErrRecipeStructureInvalid, "Công thức phải có ít nhất 1 nguyên liệu có định lượng và đơn vị hợp lệ (BR-USR-05).");
        }

        if (cookTimeMinutes <= 0)
        {
            throw new AppException(422, ErrorCodes.ErrRecipeStructureInvalid, "Thời gian nấu phải lớn hơn 0 phút (BR-USR-05).");
        }

        if (dietarySchoolId <= 0)
        {
            throw new AppException(422, ErrorCodes.ErrRecipeStructureInvalid, "Trường phái ăn chay không hợp lệ (BR-USR-05).");
        }
    }

    private async Task<(bool IsForbidden, string Reason, string ViolationType)> CheckForbiddenIngredientsAsync(
        int dietarySchoolId,
        IEnumerable<RecipeIngredientItemRequest> ingredients,
        CancellationToken ct)
    {
        var school = await _db.DietarySchools.AsNoTracking().FirstOrDefaultAsync(s => s.DietarySchoolId == dietarySchoolId, ct);
        if (school == null)
        {
            throw AppException.BadRequest(ErrorCodes.ErrDietSchoolNotFound, "Trường phái ăn chay không tồn tại.");
        }

        ForbiddenFoodRulesDto rules = new();
        if (!string.IsNullOrWhiteSpace(school.ForbiddenFoodRules))
        {
            try
            {
                rules = JsonSerializer.Deserialize<ForbiddenFoodRulesDto>(
                    school.ForbiddenFoodRules,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new();
            }
            catch
            {
                // Fallback nếu JSON không hợp lệ
            }
        }

        var ingredientIds = ingredients.Select(i => i.IngredientId).Distinct().ToList();
        var dbIngredients = await _db.Ingredients.AsNoTracking()
            .Where(i => ingredientIds.Contains(i.IngredientId))
            .ToListAsync(ct);

        foreach (var ing in dbIngredients)
        {
            if (rules.ForbiddenFoodGroups != null && rules.ForbiddenFoodGroups.Any(g => string.Equals(g, ing.FoodGroup, StringComparison.OrdinalIgnoreCase)))
            {
                var reason = $"Nguyên liệu '{ing.IngredientName}' thuộc nhóm '{ing.FoodGroup}' bị cấm trong trường phái '{school.SchoolName}' (D109).";
                return (true, reason, "VIO_LABEL");
            }

            if (rules.ForbidAllium && (ing.IsAllium || string.Equals(ing.FoodGroup, "ALLIUM", StringComparison.OrdinalIgnoreCase)))
            {
                var reason = $"Nguyên liệu '{ing.IngredientName}' chứa ngũ vị tân (Allium) bị cấm trong trường phái '{school.SchoolName}' (D109).";
                return (true, reason, "VIO_LABEL");
            }
        }

        return (false, string.Empty, "NONE");
    }

    private async Task<decimal> CalculateCaloriesAsync(IEnumerable<RecipeIngredientItemRequest>? ingredients, CancellationToken ct)
    {
        if (ingredients == null) return 150.0m;

        var items = ingredients.Where(i => i.IngredientId > 0 && i.Quantity > 0).ToList();
        if (items.Count == 0) return 150.0m;

        var ingIds = items.Select(i => i.IngredientId).Distinct().ToList();
        var dbIngredients = await _db.Ingredients.AsNoTracking()
            .Where(i => ingIds.Contains(i.IngredientId))
            .ToDictionaryAsync(i => i.IngredientId, ct);

        decimal totalCalories = 0m;
        foreach (var item in items)
        {
            if (dbIngredients.TryGetValue(item.IngredientId, out var ing))
            {
                totalCalories += (item.Quantity / 100m) * ing.CaloriesPer100g;
            }
        }

        return totalCalories > 0 ? Math.Round(totalCalories, 1) : 150.0m;
    }

    private async Task LogAiQuotaAsync(int userId, ModerationCheckResponse modResult, CancellationToken ct)
    {
        try
        {
            var quotaLog = new AiquotaLog
            {
                UserId = userId,
                FeatureCode = "CONTENT_MODERATION",
                TokensConsumed = modResult.Usage.PromptTokens + modResult.Usage.CompletionTokens,
                CostEstimateUsd = 0.0001m,
                RequestStatus = "SUCCESS",
                ExecutionTimeMs = modResult.Usage.LatencyMs,
                CreatedAt = DateTime.UtcNow
            };
            _db.AiquotaLogs.Add(quotaLog);
            await _db.SaveChangesAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Lỗi khi ghi nhật ký AIQuotaLogs cho người dùng {UserId}.", userId);
        }
    }

    private async Task InvalidateRecipeCachesAsync(int recipeId, CancellationToken ct)
    {
        try
        {
            await _redis.RemoveAsync($"recipe_detail:{recipeId}", ct);
            await _redis.RemoveAsync("recipe_search:*", ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Lỗi khi xóa bộ nhớ đệm công thức {RecipeId}.", recipeId);
        }
    }

    private class ForbiddenFoodRulesDto
    {
        public List<string> ForbiddenFoodGroups { get; set; } = new();
        public bool ForbidAllium { get; set; }
        public string? Description { get; set; }
    }

    /// <summary>
    /// Lớp bọc nội bộ phục vụ việc lưu trữ kết quả phân trang tìm kiếm vào cache Redis.
    /// </summary>
    private class RecipeSearchResultCache
    {
        public List<RecipeSummaryDto> Items { get; set; } = new();
        public int Total { get; set; }
    }
}

