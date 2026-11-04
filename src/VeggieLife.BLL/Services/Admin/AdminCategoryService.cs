using System;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using VeggieLife.BLL.Common.Constants;
using VeggieLife.BLL.Common.Exceptions;
using VeggieLife.BLL.DTOs.Admin.Categories;
using VeggieLife.BLL.Services.Cache;
using VeggieLife.DAL.Context;
using VeggieLife.DAL.Entities;

namespace VeggieLife.BLL.Services.Admin;

/// <summary>
/// Triển khai dịch vụ Quản trị danh mục món ăn (FN-ADM-07, FN-ADM-08, BR-ADM-CAT-001, BR-SYS-CACHE-01).
/// </summary>
public class AdminCategoryService : IAdminCategoryService
{
    private readonly AppDbContext _db;
    private readonly IRedisService _redis;
    private readonly ILogger<AdminCategoryService> _logger;

    public AdminCategoryService(
        AppDbContext db,
        IRedisService redis,
        ILogger<AdminCategoryService> logger)
    {
        _db = db;
        _redis = redis;
        _logger = logger;
    }

    /// <summary>
    /// FN-ADM-07: Tạo mới danh mục món ăn. Tên danh mục duy nhất (2–100 ký tự).
    /// </summary>
    public async Task<CreateAdminCategoryResponseData> CreateCategoryAsync(
        CreateAdminCategoryRequest request,
        int adminId,
        CancellationToken ct = default)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.CategoryName))
        {
            throw AppException.BadRequest(ErrorCodes.ErrValidation, "Tên danh mục không được để trống.");
        }

        var trimmedName = request.CategoryName.Trim();
        if (trimmedName.Length < 2 || trimmedName.Length > 100)
        {
            throw AppException.BadRequest(ErrorCodes.ErrValidation, "Tên danh mục phải từ 2 đến 100 ký tự.");
        }

        if (request.Description != null && request.Description.Trim().Length > 500)
        {
            throw AppException.BadRequest(ErrorCodes.ErrValidation, "Mô tả danh mục tối đa 500 ký tự.");
        }

        // Kiểm tra tên danh mục duy nhất
        var isDuplicate = await _db.FoodCategories.AnyAsync(
            c => c.CategoryName.ToLower() == trimmedName.ToLower(), ct);
        if (isDuplicate)
        {
            throw AppException.Conflict(ErrorCodes.ErrCategoryDuplicate, "Tên danh mục đã tồn tại.");
        }

        var category = new FoodCategory
        {
            CategoryName = trimmedName,
            Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
            IsCore = false,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _db.FoodCategories.Add(category);
        await _db.SaveChangesAsync(ct);

        // Ghi AuditLogs
        _db.AuditLogs.Add(new AuditLog
        {
            ActorUserId = adminId,
            ActorRole = UserRoles.Admin,
            Action = "CREATE_CATEGORY",
            TargetTable = "FoodCategories",
            TargetId = category.CategoryId.ToString(),
            NewValue = JsonSerializer.Serialize(new
            {
                category.CategoryId,
                category.CategoryName,
                category.Description,
                category.IsCore,
                category.IsActive
            }),
            CreatedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync(ct);

        // Xóa bộ nhớ đệm
        await InvalidateCategoryCachesAsync(ct);

        return new CreateAdminCategoryResponseData
        {
            CategoryId = category.CategoryId,
            CategoryName = category.CategoryName
        };
    }

    /// <summary>
    /// FN-ADM-07: Cập nhật danh mục món ăn. Is_Core không đổi được qua API.
    /// </summary>
    public async Task<UpdateAdminCategoryResponseData> UpdateCategoryAsync(
        int id,
        UpdateAdminCategoryRequest request,
        int adminId,
        CancellationToken ct = default)
    {
        if (id <= 0)
        {
            throw AppException.BadRequest(ErrorCodes.ErrValidation, "Mã danh mục không hợp lệ.");
        }

        var category = await _db.FoodCategories.FirstOrDefaultAsync(c => c.CategoryId == id, ct);
        if (category == null)
        {
            throw AppException.NotFound(ErrorCodes.ErrAdmNotFound, "Đối tượng không tồn tại.");
        }

        if (request != null)
        {
            if (request.CategoryName != null)
            {
                var trimmedName = request.CategoryName.Trim();
                if (trimmedName.Length < 2 || trimmedName.Length > 100)
                {
                    throw AppException.BadRequest(ErrorCodes.ErrValidation, "Tên danh mục phải từ 2 đến 100 ký tự.");
                }

                var isDuplicate = await _db.FoodCategories.AnyAsync(
                    c => c.CategoryId != id && c.CategoryName.ToLower() == trimmedName.ToLower(), ct);
                if (isDuplicate)
                {
                    throw AppException.Conflict(ErrorCodes.ErrCategoryDuplicate, "Tên danh mục đã tồn tại.");
                }

                category.CategoryName = trimmedName;
            }

            if (request.Description != null)
            {
                var trimmedDesc = request.Description.Trim();
                if (trimmedDesc.Length > 500)
                {
                    throw AppException.BadRequest(ErrorCodes.ErrValidation, "Mô tả danh mục tối đa 500 ký tự.");
                }

                category.Description = string.IsNullOrWhiteSpace(trimmedDesc) ? null : trimmedDesc;
            }

            if (request.IsActive.HasValue)
            {
                category.IsActive = request.IsActive.Value;
            }
        }

        category.UpdatedAt = DateTime.UtcNow;

        // Ghi AuditLogs
        _db.AuditLogs.Add(new AuditLog
        {
            ActorUserId = adminId,
            ActorRole = UserRoles.Admin,
            Action = "UPDATE_CATEGORY",
            TargetTable = "FoodCategories",
            TargetId = category.CategoryId.ToString(),
            NewValue = JsonSerializer.Serialize(new
            {
                category.CategoryId,
                category.CategoryName,
                category.IsActive,
                category.Description
            }),
            CreatedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync(ct);

        // Xóa bộ nhớ đệm
        await InvalidateCategoryCachesAsync(ct);

        return new UpdateAdminCategoryResponseData
        {
            CategoryId = category.CategoryId,
            IsActive = category.IsActive,
            CategoryName = category.CategoryName
        };
    }

    /// <summary>
    /// FN-ADM-08: Xóa mềm một danh mục; bắt buộc chuyển toàn bộ công thức và quán đang dùng sang danh mục đích trước;
    /// danh mục cốt lõi không được xóa (BR-ADM-CAT-001).
    /// </summary>
    public async Task<DeleteAdminCategoryResponseData> DeleteCategoryAsync(
        int id,
        int? targetCategoryId,
        int adminId,
        CancellationToken ct = default)
    {
        if (id <= 0)
        {
            throw AppException.BadRequest(ErrorCodes.ErrValidation, "Mã danh mục không hợp lệ.");
        }

        var category = await _db.FoodCategories.FirstOrDefaultAsync(c => c.CategoryId == id, ct);
        if (category == null)
        {
            throw AppException.NotFound(ErrorCodes.ErrAdmNotFound, "Đối tượng không tồn tại.");
        }

        // Bước 1: Từ chối nếu Is_Core = 1 (FLOW-07 bước 26a)
        if (category.IsCore)
        {
            throw AppException.Forbidden(ErrorCodes.ErrCategoryCoreProtected, "Không được xóa danh mục cốt lõi.");
        }

        // Bước 2: Đếm Recipes và VenueCategories tham chiếu (BR-ADM-CAT-001)
        var recipesCount = await _db.Foods.CountAsync(f => f.CategoryId == id, ct);
        var venuesCount = await _db.VenueCategories.CountAsync(vc => vc.CategoryId == id, ct);

        if (recipesCount > 0 || venuesCount > 0)
        {
            if (!targetCategoryId.HasValue || targetCategoryId.Value <= 0)
            {
                throw AppException.Conflict(
                    ErrorCodes.ErrCategoryNeedsMigration,
                    "Còn nội dung phụ thuộc, cần targetCategoryId.");
            }

            if (targetCategoryId.Value == id)
            {
                throw AppException.BadRequest(ErrorCodes.ErrValidation, "Danh mục đích phải khác danh mục cần xóa.");
            }

            var targetCategory = await _db.FoodCategories.FirstOrDefaultAsync(c => c.CategoryId == targetCategoryId.Value, ct);
            if (targetCategory == null)
            {
                throw AppException.NotFound(ErrorCodes.ErrAdmNotFound, "Danh mục đích không tồn tại.");
            }
        }

        // Bước 3: Di chuyển dữ liệu sang danh mục đích (loại trùng)
        int migratedRecipes = 0;
        int migratedVenues = 0;

        if (targetCategoryId.HasValue && targetCategoryId.Value > 0)
        {
            var targetId = targetCategoryId.Value;

            // Di chuyển Foods
            var foodsToMigrate = await _db.Foods.Where(f => f.CategoryId == id).ToListAsync(ct);
            foreach (var f in foodsToMigrate)
            {
                f.CategoryId = targetId;
                f.UpdatedAt = DateTime.UtcNow;
            }
            migratedRecipes = foodsToMigrate.Count;

            // Di chuyển VenueCategories loại trùng
            var oldVenueCategories = await _db.VenueCategories.Where(vc => vc.CategoryId == id).ToListAsync(ct);
            var existingTargetVenueIds = await _db.VenueCategories
                .Where(vc => vc.CategoryId == targetId)
                .Select(vc => vc.VenueId)
                .ToListAsync(ct);
            var targetVenueIdSet = existingTargetVenueIds.ToHashSet();

            foreach (var vc in oldVenueCategories)
            {
                _db.VenueCategories.Remove(vc);
                if (!targetVenueIdSet.Contains(vc.VenueId))
                {
                    _db.VenueCategories.Add(new VenueCategory
                    {
                        CategoryId = targetId,
                        VenueId = vc.VenueId,
                        IsPrimary = vc.IsPrimary,
                        AssignedAt = DateTime.UtcNow
                    });
                    targetVenueIdSet.Add(vc.VenueId);
                }
                migratedVenues++;
            }
        }

        // Cập nhật trạng thái xóa mềm danh mục cũ
        category.IsActive = false;
        category.UpdatedAt = DateTime.UtcNow;

        // Ghi AuditLogs
        _db.AuditLogs.Add(new AuditLog
        {
            ActorUserId = adminId,
            ActorRole = UserRoles.Admin,
            Action = "DELETE_CATEGORY",
            TargetTable = "FoodCategories",
            TargetId = id.ToString(),
            OldValue = JsonSerializer.Serialize(new { category.CategoryId, category.CategoryName, IsActive = true }),
            NewValue = JsonSerializer.Serialize(new
            {
                category.CategoryId,
                IsActive = false,
                TargetCategoryId = targetCategoryId,
                MigratedRecipes = migratedRecipes,
                MigratedVenues = migratedVenues
            }),
            CreatedAt = DateTime.UtcNow
        });

        await _db.SaveChangesAsync(ct);

        // Xóa bộ nhớ đệm
        await InvalidateCategoryCachesAsync(ct);

        return new DeleteAdminCategoryResponseData
        {
            MigratedRecipes = migratedRecipes,
            MigratedVenues = migratedVenues
        };
    }

    /// <summary>
    /// FN-ADM-07: Lấy toàn bộ danh mục món ăn dành cho Admin (bao gồm cả danh mục ẩn, danh mục cốt lõi).
    /// </summary>
    public async Task<IReadOnlyList<AdminCategoryItemResponseData>> GetAllCategoriesAsync(CancellationToken ct = default)
    {
        var categories = await _db.FoodCategories
            .AsNoTracking()
            .OrderBy(c => c.CategoryId)
            .ToListAsync(ct);

        return categories.Select(c => new AdminCategoryItemResponseData
        {
            CategoryId = c.CategoryId,
            CategoryName = c.CategoryName,
            Description = c.Description,
            IsCore = c.IsCore,
            IsActive = c.IsActive,
            CreatedAt = c.CreatedAt,
            UpdatedAt = c.UpdatedAt
        }).ToList();
    }

    private async Task InvalidateCategoryCachesAsync(CancellationToken ct)
    {
        try
        {
            await _redis.RemoveAsync("categories_cache", ct);
            await _redis.RemoveAsync("recipe_search:*", ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Lỗi khi xóa bộ nhớ đệm danh mục món ăn.");
        }
    }
}
