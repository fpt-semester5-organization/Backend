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
using VeggieLife.BLL.DTOs.Admin.Ingredients;
using VeggieLife.BLL.Services.Cache;
using VeggieLife.BLL.Services.Knowledge;
using VeggieLife.BLL.Validators.Admin;
using VeggieLife.DAL.Context;
using VeggieLife.DAL.Entities;

namespace VeggieLife.BLL.Services.Admin;

/// <summary>
/// Triển khai dịch vụ quản trị nguyên liệu thực phẩm và nhóm dị ứng (FN-ADM-12).
/// </summary>
public class AdminIngredientService : IAdminIngredientService
{
    private readonly AppDbContext _db;
    private readonly IRedisService _redis;
    private readonly IKnowledgeSyncQueue _knowledgeSyncQueue;
    private readonly ILogger<AdminIngredientService> _logger;

    public AdminIngredientService(
        AppDbContext db,
        IRedisService redis,
        IKnowledgeSyncQueue knowledgeSyncQueue,
        ILogger<AdminIngredientService> logger)
    {
        _db = db;
        _redis = redis;
        _knowledgeSyncQueue = knowledgeSyncQueue;
        _logger = logger;
    }

    /// <summary>
    /// Lấy danh sách nguyên liệu theo bộ lọc và phân trang dành cho Quản trị viên.
    /// </summary>
    public async Task<(IReadOnlyList<AdminIngredientListItemDto> Items, PagedMeta Meta)> GetIngredientsAsync(
        AdminIngredientFilterRequest request,
        CancellationToken ct = default)
    {
        AdminIngredientValidator.ValidateFilter(request);

        var query = _db.Ingredients
            .AsNoTracking()
            .Include(i => i.IngredientAllergens)
                .ThenInclude(ia => ia.Allergen)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.Keyword))
        {
            var keywordTrimmed = request.Keyword.Trim();
            query = query.Where(i => i.IngredientName.Contains(keywordTrimmed));
        }

        if (request.AllergenId.HasValue)
        {
            query = query.Where(i => i.IngredientAllergens.Any(ia => ia.AllergenId == request.AllergenId.Value));
        }

        var total = await query.CountAsync(ct);

        var entities = await query
            .OrderBy(i => i.IngredientId)
            .Skip((request.Page - 1) * request.Size)
            .Take(request.Size)
            .ToListAsync(ct);

        var items = entities.Select(i => new AdminIngredientListItemDto
        {
            IngredientId = i.IngredientId,
            Name = i.IngredientName,
            CaloriesPer100g = i.CaloriesPer100g,
            IsAllium = i.IsAllium,
            DefaultStallType = i.DefaultStallType,
            FoodGroup = i.FoodGroup,
            Allergens = i.IngredientAllergens
                .OrderBy(ia => ia.AllergenId)
                .Select(ia => new AdminIngredientAllergenItemDto
                {
                    AllergenId = ia.AllergenId,
                    Name = ia.Allergen.AllergenName
                }).ToList()
        }).ToList();

        var meta = new PagedMeta(request.Page, request.Size, total);
        return (items, meta);
    }

    /// <summary>
    /// Thêm mới nguyên liệu thực phẩm (FN-ADM-12).
    /// </summary>
    public async Task<CreateAdminIngredientResponseData> CreateIngredientAsync(
        CreateAdminIngredientRequest request,
        int adminId,
        CancellationToken ct = default)
    {
        AdminIngredientValidator.ValidateCreate(request);

        var trimmedName = request.Name.Trim();
        var isDuplicate = await _db.Ingredients.AnyAsync(
            i => i.IngredientName.ToLower() == trimmedName.ToLower(), ct);

        if (isDuplicate)
        {
            throw AppException.Conflict(ErrorCodes.ErrIngredientDuplicate, "Tên nguyên liệu đã tồn tại.");
        }

        var stallType = request.DefaultStallType.Trim().ToUpperInvariant();
        var foodGroup = string.IsNullOrWhiteSpace(request.FoodGroup) ? "PLANT" : request.FoodGroup.Trim().ToUpperInvariant();
        var unit = string.IsNullOrWhiteSpace(request.StandardUnit) ? "g" : request.StandardUnit.Trim();

        var ingredient = new Ingredient
        {
            IngredientName = trimmedName,
            StandardUnit = unit,
            DefaultStallType = stallType,
            FoodGroup = foodGroup,
            CaloriesPer100g = Math.Round(request.CaloriesPer100g, 1),
            IsAllium = request.IsAllium,
            CreatedAt = DateTime.UtcNow
        };

        _db.Ingredients.Add(ingredient);
        await _db.SaveChangesAsync(ct);

        // Ghi AuditLogs
        _db.AuditLogs.Add(new AuditLog
        {
            ActorUserId = adminId,
            ActorRole = UserRoles.Admin,
            Action = "CREATE_INGREDIENT",
            TargetTable = "Ingredients",
            TargetId = ingredient.IngredientId.ToString(),
            NewValue = JsonSerializer.Serialize(new
            {
                ingredient.IngredientId,
                ingredient.IngredientName,
                ingredient.StandardUnit,
                ingredient.DefaultStallType,
                ingredient.FoodGroup,
                ingredient.CaloriesPer100g,
                ingredient.IsAllium
            }),
            CreatedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync(ct);

        // Xóa bộ nhớ đệm
        await InvalidateIngredientCachesAsync(ct);

        // Đồng bộ kho tri thức AI
        try
        {
            await _knowledgeSyncQueue.EnqueueUpsertAsync("INGREDIENT", ingredient.IngredientId, ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Lỗi khi đưa nguyên liệu ID {Id} vào hàng đợi đồng bộ kho tri thức.", ingredient.IngredientId);
        }

        return new CreateAdminIngredientResponseData
        {
            IngredientId = ingredient.IngredientId
        };
    }

    /// <summary>
    /// Cập nhật thông tin nguyên liệu thực phẩm (FN-ADM-12).
    /// </summary>
    public async Task<UpdateAdminIngredientResponseData> UpdateIngredientAsync(
        int id,
        UpdateAdminIngredientRequest request,
        int adminId,
        CancellationToken ct = default)
    {
        AdminIngredientValidator.ValidateUpdate(id, request);

        var ingredient = await _db.Ingredients.FirstOrDefaultAsync(i => i.IngredientId == id, ct);
        if (ingredient == null)
        {
            throw AppException.NotFound(ErrorCodes.ErrAdmNotFound, "Không tìm thấy nguyên liệu.");
        }

        var oldSnapshot = JsonSerializer.Serialize(new
        {
            ingredient.IngredientId,
            ingredient.IngredientName,
            ingredient.StandardUnit,
            ingredient.DefaultStallType,
            ingredient.FoodGroup,
            ingredient.CaloriesPer100g,
            ingredient.IsAllium
        });

        if (request.Name != null)
        {
            var trimmedName = request.Name.Trim();
            var isDuplicate = await _db.Ingredients.AnyAsync(
                i => i.IngredientId != id && i.IngredientName.ToLower() == trimmedName.ToLower(), ct);

            if (isDuplicate)
            {
                throw AppException.Conflict(ErrorCodes.ErrIngredientDuplicate, "Tên nguyên liệu đã tồn tại.");
            }

            ingredient.IngredientName = trimmedName;
        }

        if (request.StandardUnit != null)
        {
            ingredient.StandardUnit = string.IsNullOrWhiteSpace(request.StandardUnit) ? "g" : request.StandardUnit.Trim();
        }

        if (!string.IsNullOrWhiteSpace(request.DefaultStallType))
        {
            ingredient.DefaultStallType = request.DefaultStallType.Trim().ToUpperInvariant();
        }

        if (!string.IsNullOrWhiteSpace(request.FoodGroup))
        {
            ingredient.FoodGroup = request.FoodGroup.Trim().ToUpperInvariant();
        }

        if (request.CaloriesPer100g.HasValue)
        {
            ingredient.CaloriesPer100g = Math.Round(request.CaloriesPer100g.Value, 1);
        }

        if (request.IsAllium.HasValue)
        {
            ingredient.IsAllium = request.IsAllium.Value;
        }

        await _db.SaveChangesAsync(ct);

        // Ghi AuditLogs
        _db.AuditLogs.Add(new AuditLog
        {
            ActorUserId = adminId,
            ActorRole = UserRoles.Admin,
            Action = "UPDATE_INGREDIENT",
            TargetTable = "Ingredients",
            TargetId = ingredient.IngredientId.ToString(),
            OldValue = oldSnapshot,
            NewValue = JsonSerializer.Serialize(new
            {
                ingredient.IngredientId,
                ingredient.IngredientName,
                ingredient.StandardUnit,
                ingredient.DefaultStallType,
                ingredient.FoodGroup,
                ingredient.CaloriesPer100g,
                ingredient.IsAllium
            }),
            CreatedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync(ct);

        // Xóa bộ nhớ đệm
        await InvalidateIngredientCachesAsync(ct);

        // Đồng bộ kho tri thức AI
        try
        {
            await _knowledgeSyncQueue.EnqueueUpsertAsync("INGREDIENT", ingredient.IngredientId, ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Lỗi khi đồng bộ kho tri thức sau cập nhật nguyên liệu ID {Id}.", ingredient.IngredientId);
        }

        return new UpdateAdminIngredientResponseData
        {
            IngredientId = ingredient.IngredientId,
            Name = ingredient.IngredientName,
            StandardUnit = ingredient.StandardUnit,
            DefaultStallType = ingredient.DefaultStallType,
            FoodGroup = ingredient.FoodGroup,
            CaloriesPer100g = ingredient.CaloriesPer100g,
            IsAllium = ingredient.IsAllium
        };
    }

    /// <summary>
    /// Thay thế toàn bộ nhóm dị ứng gán cho nguyên liệu (FN-ADM-12).
    /// </summary>
    public async Task<ReplaceAdminIngredientAllergensResponseData> ReplaceIngredientAllergensAsync(
        int id,
        ReplaceAdminIngredientAllergensRequest request,
        int adminId,
        CancellationToken ct = default)
    {
        AdminIngredientValidator.ValidateReplaceAllergens(id, request);

        var ingredient = await _db.Ingredients.FirstOrDefaultAsync(i => i.IngredientId == id, ct);
        if (ingredient == null)
        {
            throw AppException.NotFound(ErrorCodes.ErrAdmNotFound, "Không tìm thấy nguyên liệu.");
        }

        var distinctAllergenIds = request.AllergenIds.Distinct().ToList();

        if (distinctAllergenIds.Count > 0)
        {
            var existingCount = await _db.Allergens
                .CountAsync(a => distinctAllergenIds.Contains(a.AllergenId), ct);

            if (existingCount != distinctAllergenIds.Count)
            {
                throw AppException.BadRequest(ErrorCodes.ErrValidation, "Một hoặc nhiều mã nhóm dị ứng không tồn tại.");
            }
        }

        // Đọc danh sách cũ để ghi audit log
        var currentLinks = await _db.IngredientAllergens
            .Where(ia => ia.IngredientId == id)
            .ToListAsync(ct);

        var oldAllergenIds = currentLinks.Select(ia => ia.AllergenId).ToList();

        // Xóa liên kết cũ
        _db.IngredientAllergens.RemoveRange(currentLinks);

        // Thêm liên kết mới
        var newLinks = distinctAllergenIds.Select(allergenId => new IngredientAllergen
        {
            IngredientId = id,
            AllergenId = allergenId,
            CreatedAt = DateTime.UtcNow
        }).ToList();

        _db.IngredientAllergens.AddRange(newLinks);
        await _db.SaveChangesAsync(ct);

        // Ghi AuditLogs
        _db.AuditLogs.Add(new AuditLog
        {
            ActorUserId = adminId,
            ActorRole = UserRoles.Admin,
            Action = "REPLACE_INGREDIENT_ALLERGENS",
            TargetTable = "IngredientAllergens",
            TargetId = id.ToString(),
            OldValue = JsonSerializer.Serialize(new { ingredientId = id, allergenIds = oldAllergenIds }),
            NewValue = JsonSerializer.Serialize(new { ingredientId = id, allergenIds = distinctAllergenIds }),
            CreatedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync(ct);

        // Xóa bộ nhớ đệm
        await InvalidateIngredientCachesAsync(ct);

        // Cập nhật kho tri thức AI
        try
        {
            await _knowledgeSyncQueue.EnqueueUpsertAsync("INGREDIENT", id, ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Lỗi khi đồng bộ kho tri thức sau cập nhật dị ứng nguyên liệu ID {Id}.", id);
        }

        return new ReplaceAdminIngredientAllergensResponseData
        {
            IngredientId = id,
            AllergenIds = distinctAllergenIds
        };
    }

    /// <summary>
    /// Thêm mới nhóm dị ứng thực phẩm (FN-ADM-12).
    /// </summary>
    public async Task<CreateAdminAllergenResponseData> CreateAllergenAsync(
        CreateAdminAllergenRequest request,
        int adminId,
        CancellationToken ct = default)
    {
        AdminIngredientValidator.ValidateCreateAllergen(request);

        var trimmedName = request.AllergenName.Trim();
        var trimmedCode = request.Code.Trim().ToUpperInvariant();

        var isDuplicate = await _db.Allergens.AnyAsync(
            a => a.AllergenName.ToLower() == trimmedName.ToLower() ||
                 a.Code.ToLower() == trimmedCode.ToLower(), ct);

        if (isDuplicate)
        {
            throw AppException.Conflict(ErrorCodes.ErrAllergenDuplicate, "Tên hoặc mã nhóm dị ứng đã tồn tại.");
        }

        var allergen = new Allergen
        {
            AllergenName = trimmedName,
            Code = trimmedCode,
            Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _db.Allergens.Add(allergen);
        await _db.SaveChangesAsync(ct);

        // Ghi AuditLogs
        _db.AuditLogs.Add(new AuditLog
        {
            ActorUserId = adminId,
            ActorRole = UserRoles.Admin,
            Action = "CREATE_ALLERGEN",
            TargetTable = "Allergens",
            TargetId = allergen.AllergenId.ToString(),
            NewValue = JsonSerializer.Serialize(new
            {
                allergen.AllergenId,
                allergen.AllergenName,
                allergen.Code,
                allergen.Description
            }),
            CreatedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync(ct);

        // Xóa bộ nhớ đệm allergens
        try
        {
            await _redis.RemoveAsync("allergens_cache", ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Lỗi khi xóa bộ nhớ đệm nhóm dị ứng.");
        }

        // Cập nhật kho tri thức AI
        try
        {
            await _knowledgeSyncQueue.EnqueueUpsertAsync("ALLERGEN", allergen.AllergenId, ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Lỗi khi đồng bộ kho tri thức sau tạo nhóm dị ứng ID {Id}.", allergen.AllergenId);
        }

        return new CreateAdminAllergenResponseData
        {
            AllergenId = allergen.AllergenId
        };
    }

    private async Task InvalidateIngredientCachesAsync(CancellationToken ct)
    {
        try
        {
            await _redis.RemoveAsync("ingredients_cache:*", ct);
            await _redis.RemoveAsync("recipe_search:*", ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Lỗi khi xóa bộ nhớ đệm nguyên liệu.");
        }
    }
}
