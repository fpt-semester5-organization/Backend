using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using VeggieLife.BLL.Common.Constants;
using VeggieLife.BLL.Common.Exceptions;
using VeggieLife.BLL.Common.Responses;
using VeggieLife.BLL.DTOs.Foods.Responses;
using VeggieLife.DAL.Context;
using VeggieLife.DAL.Entities;

namespace VeggieLife.BLL.Services.Foods;

/// <summary>
/// Dịch vụ xử lý tra cứu và tự động tạo mới thực thể món ăn (Food) theo pattern "Combobox with Create".
/// </summary>
public class FoodService : IFoodService
{
    private readonly AppDbContext _db;
    private readonly ILogger<FoodService> _logger;

    /// <summary>
    /// Khởi tạo FoodService với AppDbContext và ILogger.
    /// </summary>
    /// <param name="db">DbContext của ứng dụng.</param>
    /// <param name="logger">Logger cho FoodService.</param>
    public FoodService(AppDbContext db, ILogger<FoodService> logger)
    {
        _db = db;
        _logger = logger;
    }

    /// <summary>
    /// Lấy danh sách tất cả món ăn có phân trang và tìm kiếm tùy chọn.
    /// </summary>
    public async Task<(IReadOnlyList<FoodSummaryDto> Items, PagedMeta Meta)> ListFoodsAsync(string? query, int page = 1, int size = 20, CancellationToken ct = default)
    {
        page = Math.Max(1, page);
        size = Math.Clamp(size, 1, 100);

        var q = _db.Foods.AsNoTracking();

        var trimmed = (query ?? string.Empty).Trim();
        if (!string.IsNullOrWhiteSpace(trimmed))
        {
            q = q.Where(f => f.FoodName.Contains(trimmed));
        }

        var total = await q.CountAsync(ct);

        var items = await q
            .OrderBy(f => f.FoodName)
            .Skip((page - 1) * size)
            .Take(size)
            .Select(f => new FoodSummaryDto
            {
                FoodId = f.FoodId,
                FoodName = f.FoodName,
                CategoryId = f.CategoryId,
                CategoryName = f.Category != null ? f.Category.CategoryName : null,
                // Ảnh món: Foods.Image_URL; chưa có thì lấy ảnh công thức (ưu tiên đã duyệt, rồi SYSTEM từ sách, rồi cũ nhất)
                ImageUrl = f.ImageUrl ?? f.Recipes
                    .Where(r => r.ImageUrl != null && r.ImageUrl != "")
                    .OrderBy(r => r.Status == "APPROVED_ACTIVE" ? 0 : 1)
                    .ThenBy(r => r.Source == "SYSTEM" ? 0 : 1)
                    .ThenBy(r => r.RecipeId)
                    .Select(r => r.ImageUrl)
                    .FirstOrDefault(),
                RecipeCount = f.Recipes.Count(r => r.Status == "APPROVED_ACTIVE"),
                HasHighCalorie = f.Recipes.Any(r => r.Status == "APPROVED_ACTIVE" && r.CaloriesKcal >= 550),
                HasNoSpicy = f.Recipes.Any(r => r.Status == "APPROVED_ACTIVE" && r.Title.Contains("không cay")),
                HasAlliumFree = f.Recipes.Any(r => r.Status == "APPROVED_ACTIVE"
                    && (r.DietarySchool.SchoolCode == "BUDDHIST" || r.Title.Contains("không hành tỏi") || r.Title.Contains("ngũ vị tân")))
            })
            .ToListAsync(ct);

        var meta = new PagedMeta(page, size, total);
        return (items, meta);
    }

    /// <summary>
    /// Tìm kiếm Food theo tên (LIKE '%query%'), sắp xếp theo tên, trả tối đa 10 kết quả.
    /// </summary>
    /// <param name="query">Từ khóa tìm kiếm.</param>
    /// <param name="ct">CancellationToken.</param>
    /// <returns>Danh sách tối đa 10 FoodSummaryDto khớp với từ khóa.</returns>
    public async Task<IReadOnlyList<FoodSummaryDto>> SearchFoodsAsync(string query, CancellationToken ct = default)
    {
        var trimmed = (query ?? string.Empty).Trim();

        var q = _db.Foods.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(trimmed))
        {
            q = q.Where(f => f.FoodName.Contains(trimmed));
        }

        var results = await q
            .OrderBy(f => f.FoodName)
            .Take(10)
            .Select(f => new FoodSummaryDto
            {
                FoodId = f.FoodId,
                FoodName = f.FoodName,
                CategoryId = f.CategoryId,
                CategoryName = f.Category != null ? f.Category.CategoryName : null,
                ImageUrl = f.ImageUrl,
                RecipeCount = f.Recipes.Count(r => r.Status == "APPROVED_ACTIVE")
            })
            .ToListAsync(ct);

        return results;
    }

    /// <summary>
    /// Tìm Food theo tên (case-insensitive). Nếu chưa có → INSERT mới rồi trả về entity.
    /// </summary>
    public Task<Food> GetOrCreateFoodAsync(string foodName, int categoryId, CancellationToken ct = default)
        => GetOrCreateFoodAsync(foodName, categoryId, null, ct);

    /// <summary>
    /// Tìm Food theo tên (case-insensitive) kèm ảnh minh họa. Nếu chưa có → INSERT mới rồi trả về entity.
    /// </summary>
    /// <param name="foodName">Tên món ăn cần lấy hoặc tạo.</param>
    /// <param name="categoryId">Danh mục sẽ gán khi tạo mới.</param>
    /// <param name="imageUrl">Đường dẫn hình ảnh minh họa (tùy chọn).</param>
    /// <param name="ct">CancellationToken.</param>
    /// <returns>Entity Food đã tồn tại hoặc vừa được tạo mới.</returns>
    public async Task<Food> GetOrCreateFoodAsync(string foodName, int categoryId, string? imageUrl, CancellationToken ct = default)
    {
        var normalizedName = (foodName ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(normalizedName))
        {
            throw AppException.BadRequest(ErrorCodes.ErrValidation, "Tên món ăn không được để trống.");
        }

        var existing = await _db.Foods
            .FirstOrDefaultAsync(f => f.FoodName.ToLower() == normalizedName.ToLower(), ct);

        if (existing is not null)
        {
            if (string.IsNullOrWhiteSpace(existing.ImageUrl) && !string.IsNullOrWhiteSpace(imageUrl))
            {
                existing.ImageUrl = imageUrl.Trim();
                existing.UpdatedAt = DateTime.UtcNow;
                await _db.SaveChangesAsync(ct);
            }
            return existing;
        }

        var newFood = new Food
        {
            FoodName = normalizedName,
            CategoryId = categoryId > 0 ? categoryId : 1,
            ImageUrl = !string.IsNullOrWhiteSpace(imageUrl) ? imageUrl.Trim() : null,
            CreatedAt = DateTime.UtcNow,
        };

        _db.Foods.Add(newFood);
        await _db.SaveChangesAsync(ct);

        _logger.LogInformation("Created new Food: Id={FoodId}, Name={FoodName}, CategoryId={CategoryId}, ImageUrl={ImageUrl}",
            newFood.FoodId, newFood.FoodName, newFood.CategoryId, newFood.ImageUrl);

        return newFood;
    }

    /// <summary>
    /// Lấy thông tin chi tiết món ăn theo mã định danh (FoodId).
    /// </summary>
    /// <param name="foodId">Mã định danh món ăn.</param>
    /// <param name="ct">CancellationToken.</param>
    /// <returns>FoodSummaryDto hoặc null nếu không tìm thấy.</returns>
    public async Task<FoodSummaryDto?> GetFoodByIdAsync(int foodId, CancellationToken ct = default)
    {
        if (foodId <= 0)
        {
            return null;
        }

        return await _db.Foods.AsNoTracking()
            .Where(f => f.FoodId == foodId)
            .Select(f => new FoodSummaryDto
            {
                FoodId = f.FoodId,
                FoodName = f.FoodName,
                CategoryId = f.CategoryId,
                CategoryName = f.Category != null ? f.Category.CategoryName : null,
                ImageUrl = f.ImageUrl,
                RecipeCount = f.Recipes.Count(r => r.Status == "APPROVED_ACTIVE")
            })
            .FirstOrDefaultAsync(ct);
    }
}
