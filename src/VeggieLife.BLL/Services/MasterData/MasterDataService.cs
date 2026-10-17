using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using VeggieLife.BLL.Common.Constants;
using VeggieLife.BLL.Common.Exceptions;
using VeggieLife.BLL.Common.Responses;
using VeggieLife.BLL.DTOs.MasterData.Responses;
using VeggieLife.BLL.Services.Cache;
using VeggieLife.DAL.Context;

namespace VeggieLife.BLL.Services.MasterData;

/// <summary>
/// Dịch vụ tra cứu danh mục dùng chung hệ thống (FN-PROF-08):
/// 07 trường phái ăn chay, nhóm chất dị ứng, danh mục món ăn, nguyên liệu thực phẩm.
/// Bộ nhớ đệm phân tán Redis 1 giờ (D106).
/// </summary>
public class MasterDataService : IMasterDataService
{
    private readonly AppDbContext _db;
    private readonly IRedisService _redis;
    private readonly ILogger<MasterDataService> _logger;

    private static readonly TimeSpan CacheTtl = TimeSpan.FromHours(1);

    public MasterDataService(AppDbContext db, IRedisService redis, ILogger<MasterDataService> logger)
    {
        _db = db;
        _redis = redis;
        _logger = logger;
    }

    /// <summary>
    /// Lấy danh sách 07 trường phái ăn chay cốt lõi kèm quy tắc cấm JSON (BR-ADM-MAS-001, D109).
    /// </summary>
    public async Task<IReadOnlyList<DietarySchoolDto>> GetDietarySchoolsAsync(CancellationToken ct = default)
    {
        const string cacheKey = "dietary_schools_cache";
        var cached = await _redis.GetAsync<List<DietarySchoolDto>>(cacheKey, ct);
        if (cached != null)
        {
            return cached;
        }

        var entities = await _db.DietarySchools
            .AsNoTracking()
            .OrderBy(ds => ds.DietarySchoolId)
            .ToListAsync(ct);

        var result = new List<DietarySchoolDto>();
        foreach (var school in entities)
        {
            var forbiddenGroups = new List<string>();
            var forbidAllium = false;

            if (!string.IsNullOrWhiteSpace(school.ForbiddenFoodRules))
            {
                try
                {
                    using var doc = JsonDocument.Parse(school.ForbiddenFoodRules);
                    if (doc.RootElement.TryGetProperty("forbiddenFoodGroups", out var groupsElem) &&
                        groupsElem.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var item in groupsElem.EnumerateArray())
                        {
                            var str = item.GetString();
                            if (!string.IsNullOrEmpty(str))
                            {
                                forbiddenGroups.Add(str);
                            }
                        }
                    }

                    if (doc.RootElement.TryGetProperty("forbidAllium", out var alliumElem) &&
                        (alliumElem.ValueKind == JsonValueKind.True || alliumElem.ValueKind == JsonValueKind.False))
                    {
                        forbidAllium = alliumElem.GetBoolean();
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Lỗi phân tích cú pháp ForbiddenFoodRules cho trường phái {Code}", school.SchoolCode);
                }
            }

            result.Add(new DietarySchoolDto
            {
                DietarySchoolId = school.DietarySchoolId,
                SchoolCode = school.SchoolCode,
                SchoolName = school.SchoolName,
                ForbiddenFoodGroups = forbiddenGroups,
                ForbidAllium = forbidAllium
            });
        }

        await _redis.SetAsync(cacheKey, result, CacheTtl, ct);
        return result;
    }

    /// <summary>
    /// Lấy danh mục nhóm chất dị ứng thực phẩm (BR-SCI-04).
    /// </summary>
    public async Task<IReadOnlyList<AllergenDto>> GetAllergensAsync(CancellationToken ct = default)
    {
        const string cacheKey = "allergens_cache";
        var cached = await _redis.GetAsync<List<AllergenDto>>(cacheKey, ct);
        if (cached != null)
        {
            return cached;
        }

        var entities = await _db.Allergens
            .AsNoTracking()
            .OrderBy(a => a.AllergenName)
            .ToListAsync(ct);

        var result = entities.Select(a => new AllergenDto
        {
            AllergenId = a.AllergenId,
            Code = a.Code,
            AllergenName = a.AllergenName
        }).ToList();

        await _redis.SetAsync(cacheKey, result, CacheTtl, ct);
        return result;
    }

    /// <summary>
    /// Lấy danh mục món ăn thuần chay đang kích hoạt (Is_Active = 1).
    /// </summary>
    public async Task<IReadOnlyList<CategoryDto>> GetCategoriesAsync(CancellationToken ct = default)
    {
        const string cacheKey = "categories_cache";
        var cached = await _redis.GetAsync<List<CategoryDto>>(cacheKey, ct);
        if (cached != null)
        {
            return cached;
        }

        var entities = await _db.FoodCategories
            .AsNoTracking()
            .Where(c => c.IsActive)
            .OrderBy(c => c.CategoryName)
            .ToListAsync(ct);

        var result = entities.Select(c => new CategoryDto
        {
            CategoryId = c.CategoryId,
            CategoryName = c.CategoryName,
            IsCore = c.IsCore
        }).ToList();

        await _redis.SetAsync(cacheKey, result, CacheTtl, ct);
        return result;
    }

    /// <summary>
    /// Tra cứu danh sách nguyên liệu thực phẩm lọc theo từ khóa, có phân trang.
    /// </summary>
    public async Task<(IReadOnlyList<IngredientDto> Items, PagedMeta Meta)> GetIngredientsAsync(
        string? keyword, int page, int size, CancellationToken ct = default)
    {
        if (page < 1)
        {
            throw AppException.BadRequest(ErrorCodes.ErrValidation, "Chỉ số trang (page) phải từ 1 trở lên.");
        }

        if (size < 1 || size > 50)
        {
            throw AppException.BadRequest(ErrorCodes.ErrValidation, "Số lượng phần tử mỗi trang (size) phải từ 1 đến 50.");
        }

        if (keyword != null && keyword.Length > 100)
        {
            throw AppException.BadRequest(ErrorCodes.ErrValidation, "Từ khóa tìm kiếm tối đa 100 ký tự.");
        }

        var trimmedKeyword = keyword?.Trim();
        var cacheKey = $"ingredients_cache:{trimmedKeyword?.ToLowerInvariant() ?? ""}:{page}:{size}";

        var cached = await _redis.GetAsync<IngredientsCacheWrapper>(cacheKey, ct);
        if (cached != null)
        {
            return (cached.Items, cached.Meta);
        }

        var query = _db.Ingredients.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(trimmedKeyword))
        {
            query = query.Where(i => i.IngredientName.Contains(trimmedKeyword));
        }

        var total = await query.CountAsync(ct);

        var entities = await query
            .OrderBy(i => i.IngredientName)
            .Skip((page - 1) * size)
            .Take(size)
            .Select(i => new IngredientDto
            {
                IngredientId = i.IngredientId,
                Name = i.IngredientName,
                StandardUnit = i.StandardUnit,
                FoodGroup = i.FoodGroup,
                IsAllium = i.IsAllium,
                CaloriesPer100g = i.CaloriesPer100g
            })
            .ToListAsync(ct);

        var meta = new PagedMeta(page, size, total);

        var wrapper = new IngredientsCacheWrapper
        {
            Items = entities,
            Meta = meta
        };

        await _redis.SetAsync(cacheKey, wrapper, CacheTtl, ct);

        return (entities, meta);
    }

    /// <summary>
    /// Đối tượng bọc dữ liệu nguyên liệu kèm metadata phân trang để lưu bộ nhớ đệm Redis.
    /// </summary>
    public class IngredientsCacheWrapper
    {
        public List<IngredientDto> Items { get; set; } = new();
        public PagedMeta Meta { get; set; } = new();
    }
}
