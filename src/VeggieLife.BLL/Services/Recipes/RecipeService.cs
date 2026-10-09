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

namespace VeggieLife.BLL.Services.Recipes;

/// <summary>
/// Dịch vụ xử lý tìm kiếm, lọc và xem chi tiết công thức nấu ăn món chay (FN-POST-01, FN-POST-02).
/// Tuân thủ các quy tắc: BR-GUEST-01, BR-ADM-CNT-001, BR-ADM-MAS-001, BR-SYS-CACHE-01, BR-GUEST-AUTH-01, BR-AUTH-03.
/// </summary>
public class RecipeService : IRecipeService
{
    private readonly AppDbContext _db;
    private readonly IRedisService _redis;
    private readonly ILogger<RecipeService> _logger;

    private static readonly TimeSpan RecipeSearchCacheTtl = TimeSpan.FromHours(24); // BR-SYS-CACHE-01
    private static readonly TimeSpan RecipeDetailCacheTtl = TimeSpan.FromHours(24); // BR-SYS-CACHE-01
    private static readonly TimeSpan GuestHistoryTtl = TimeSpan.FromHours(24);      // BR-GUEST-AUTH-01

    public RecipeService(AppDbContext db, IRedisService redis, ILogger<RecipeService> logger)
    {
        _db = db;
        _redis = redis;
        _logger = logger;
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
    /// Lớp bọc nội bộ phục vụ việc lưu trữ kết quả phân trang tìm kiếm vào cache Redis.
    /// </summary>
    private class RecipeSearchResultCache
    {
        public List<RecipeSummaryDto> Items { get; set; } = new();
        public int Total { get; set; }
    }
}
