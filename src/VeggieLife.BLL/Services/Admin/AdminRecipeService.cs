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
using VeggieLife.BLL.DTOs.Admin.Recipes;
using VeggieLife.BLL.Services.Cache;
using VeggieLife.DAL.Context;
using VeggieLife.DAL.Entities;

namespace VeggieLife.BLL.Services.Admin;

/// <summary>
/// Triển khai dịch vụ Quản trị xác minh công thức chuẩn chay (FN-ADM-09, BR-ADM-MAS-003, BR-ADM-AI-005).
/// </summary>
public class AdminRecipeService : IAdminRecipeService
{
    private readonly AppDbContext _db;
    private readonly IRedisService _redis;
    private readonly ILogger<AdminRecipeService> _logger;

    public AdminRecipeService(
        AppDbContext db,
        IRedisService redis,
        ILogger<AdminRecipeService> logger)
    {
        _db = db;
        _redis = redis;
        _logger = logger;
    }

    /// <summary>
    /// FN-ADM-09: Gắn huy hiệu "Công thức chay chuẩn" khi đủ 4 tiêu chí của BR-ADM-MAS-003.
    /// Tiêu chí 1: Đủ định lượng gia vị và thời gian chế biến rõ ràng từng bước.
    /// Tiêu chí 2: Đã trải qua kiểm duyệt thành phần, không vi phạm tiêu chuẩn ăn chay của trường phái đã chọn (BR-ADM-MAS-001).
    /// Tiêu chí 3: Đã nhận được tối thiểu 10 lượt thả tim từ cộng đồng (Votes).
    /// Tiêu chí 4: Không còn báo cáo vi phạm nào đang chờ xử lý (PENDING).
    /// </summary>
    public async Task<VerifyAdminRecipeResponseData> VerifyRecipeAsync(
        int id,
        VerifyAdminRecipeRequest? request,
        int adminId,
        CancellationToken ct = default)
    {
        if (id <= 0)
        {
            throw AppException.BadRequest(ErrorCodes.ErrValidation, "Mã công thức không hợp lệ.");
        }

        if (request?.Note != null && request.Note.Trim().Length > 500)
        {
            throw AppException.BadRequest(ErrorCodes.ErrValidation, "Ghi chú tối đa 500 ký tự.");
        }

        var recipe = await _db.Recipes
            .Include(r => r.RecipeIngredients)
                .ThenInclude(ri => ri.Ingredient)
            .Include(r => r.DietarySchool)
            .FirstOrDefaultAsync(r => r.RecipeId == id, ct);

        if (recipe == null)
        {
            throw AppException.NotFound(ErrorCodes.ErrAdmNotFound, "Đối tượng không tồn tại.");
        }

        var failedCriteria = new List<string>();

        // 1. Tiêu chí 1: Đủ định lượng gia vị và thời gian chế biến rõ ràng từng bước
        bool hasValidCookTime = recipe.CookTimeMinutes > 0;
        var parsedSteps = ParseInstructions(recipe.Instructions);
        bool hasValidSteps = parsedSteps.Count >= 2 && parsedSteps.All(s => !string.IsNullOrWhiteSpace(s));
        bool hasValidIngredients = recipe.RecipeIngredients.Count > 0 &&
                                   recipe.RecipeIngredients.All(ri => ri.QuantityNeeded > 0 && !string.IsNullOrWhiteSpace(ri.Unit));

        if (!hasValidCookTime || !hasValidSteps || !hasValidIngredients)
        {
            failedCriteria.Add("Chưa cấu hình đầy đủ định lượng gia vị và thời gian chế biến rõ ràng từng bước.");
        }

        // 2. Tiêu chí 2: Đã trải qua kiểm duyệt thành phần, không vi phạm tiêu chuẩn ăn chay của trường phái đã chọn (BR-ADM-MAS-001)
        bool isApprovedActive = string.Equals(recipe.Status, "APPROVED_ACTIVE", StringComparison.OrdinalIgnoreCase);
        bool hasForbiddenIngredient = false;

        if (recipe.DietarySchool != null && !string.IsNullOrWhiteSpace(recipe.DietarySchool.ForbiddenFoodRules))
        {
            try
            {
                using var doc = JsonDocument.Parse(recipe.DietarySchool.ForbiddenFoodRules);
                var forbiddenGroups = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                if (doc.RootElement.TryGetProperty("forbiddenFoodGroups", out var groupsElem) && groupsElem.ValueKind == JsonValueKind.Array)
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

                var forbidAllium = false;
                if (doc.RootElement.TryGetProperty("forbidAllium", out var alliumElem) &&
                    (alliumElem.ValueKind == JsonValueKind.True || alliumElem.ValueKind == JsonValueKind.False))
                {
                    forbidAllium = alliumElem.GetBoolean();
                }

                foreach (var ri in recipe.RecipeIngredients)
                {
                    if (ri.Ingredient != null)
                    {
                        if (forbiddenGroups.Contains(ri.Ingredient.FoodGroup))
                        {
                            hasForbiddenIngredient = true;
                            break;
                        }

                        if (forbidAllium && (ri.Ingredient.IsAllium || string.Equals(ri.Ingredient.FoodGroup, "ALLIUM", StringComparison.OrdinalIgnoreCase)))
                        {
                            hasForbiddenIngredient = true;
                            break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Lỗi phân tích cú pháp ForbiddenFoodRules của trường phái {DietarySchoolId}", recipe.DietarySchoolId);
            }
        }

        if (!isApprovedActive || hasForbiddenIngredient)
        {
            failedCriteria.Add("Chưa qua kiểm duyệt phê duyệt (phải ở trạng thái APPROVED_ACTIVE) hoặc vi phạm tiêu chuẩn ăn chay của trường phái đã chọn.");
        }

        // 3. Tiêu chí 3: Đã nhận được tối thiểu 10 lượt thả tim từ cộng đồng (Votes)
        var voteCount = await _db.Votes.CountAsync(v => v.Post != null && v.Post.RecipeId == id, ct);
        if (voteCount < 10)
        {
            failedCriteria.Add($"Chưa nhận được tối thiểu 10 lượt thả tim từ cộng đồng (hiện có: {voteCount}/10).");
        }

        // 4. Tiêu chí 4: Không còn báo cáo vi phạm nào đang chờ xử lý (PENDING)
        var hasPendingReports = await _db.ViolationReports.AnyAsync(
            vr => (vr.RecipeId == id || (vr.Post != null && vr.Post.RecipeId == id)) && vr.Status == "PENDING", ct);
        if (hasPendingReports)
        {
            failedCriteria.Add("Còn báo cáo vi phạm đang chờ xử lý.");
        }

        // Nếu không đủ 4 tiêu chí -> Trả về lỗi 422 ERR_RECIPE_NOT_ELIGIBLE
        if (failedCriteria.Count > 0)
        {
            throw AppException.UnprocessableEntity(
                ErrorCodes.ErrRecipeNotEligible,
                "Chưa đủ 4 tiêu chí; data.failedCriteria liệt kê tiêu chí chưa đạt.",
                new { failedCriteria }
            );
        }

        // Cập nhật trạng thái xác minh
        recipe.IsVerified = true;
        recipe.UpdatedAt = DateTime.UtcNow;

        // Ghi AuditLogs
        _db.AuditLogs.Add(new AuditLog
        {
            ActorUserId = adminId,
            ActorRole = UserRoles.Admin,
            Action = "RECIPE_VERIFY",
            TargetTable = "Recipes",
            TargetId = id.ToString(),
            Note = string.IsNullOrWhiteSpace(request?.Note) ? null : request.Note.Trim(),
            CreatedAt = DateTime.UtcNow
        });

        await _db.SaveChangesAsync(ct);

        // Xóa bộ nhớ đệm
        await InvalidateRecipeDetailCacheAsync(id, ct);

        return new VerifyAdminRecipeResponseData
        {
            RecipeId = recipe.RecipeId,
            IsVerified = true
        };
    }

    /// <summary>
    /// FN-ADM-09: Gỡ huy hiệu "Công thức chay chuẩn" khỏi công thức.
    /// </summary>
    public async Task<UnverifyAdminRecipeResponseData> UnverifyRecipeAsync(
        int id,
        int adminId,
        CancellationToken ct = default)
    {
        if (id <= 0)
        {
            throw AppException.BadRequest(ErrorCodes.ErrValidation, "Mã công thức không hợp lệ.");
        }

        var recipe = await _db.Recipes.FirstOrDefaultAsync(r => r.RecipeId == id, ct);
        if (recipe == null)
        {
            throw AppException.NotFound(ErrorCodes.ErrAdmNotFound, "Đối tượng không tồn tại.");
        }

        recipe.IsVerified = false;
        recipe.UpdatedAt = DateTime.UtcNow;

        // Ghi AuditLogs
        _db.AuditLogs.Add(new AuditLog
        {
            ActorUserId = adminId,
            ActorRole = UserRoles.Admin,
            Action = "RECIPE_UNVERIFY",
            TargetTable = "Recipes",
            TargetId = id.ToString(),
            CreatedAt = DateTime.UtcNow
        });

        await _db.SaveChangesAsync(ct);

        // Xóa bộ nhớ đệm
        await InvalidateRecipeDetailCacheAsync(id, ct);

        return new UnverifyAdminRecipeResponseData
        {
            RecipeId = recipe.RecipeId,
            IsVerified = false
        };
    }

    private static List<string> ParseInstructions(string? rawInstructions)
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

    private async Task InvalidateRecipeDetailCacheAsync(int recipeId, CancellationToken ct)
    {
        try
        {
            await _redis.RemoveAsync($"recipe_detail:{recipeId}", ct);
            await _redis.RemoveAsync($"recipe_related:{recipeId}*", ct);
            await _redis.RemoveAsync("recipe_search:*", ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Lỗi khi xóa bộ nhớ đệm công thức {RecipeId}.", recipeId);
        }
    }
}
