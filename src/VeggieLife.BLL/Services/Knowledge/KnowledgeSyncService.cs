using System;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using VeggieLife.BLL.Services.AI;
using VeggieLife.DAL.Context;

namespace VeggieLife.BLL.Services.Knowledge;

/// <summary>
/// Dịch vụ bóc tách thực thể CSDL và đồng bộ văn bản ngữ nghĩa vào kho tri thức RAG Qdrant (FN-CHAT-07).
/// </summary>
public class KnowledgeSyncService : IKnowledgeSyncService
{
    private readonly AppDbContext _db;
    private readonly IAiServiceClient _aiClient;
    private readonly ILogger<KnowledgeSyncService> _logger;

    public KnowledgeSyncService(
        AppDbContext db,
        IAiServiceClient aiClient,
        ILogger<KnowledgeSyncService> logger)
    {
        _db = db;
        _aiClient = aiClient;
        _logger = logger;
    }

    /// <summary>
    /// Đồng bộ công thức món ăn đã duyệt (RECIPE) sang Qdrant.
    /// </summary>
    public async Task<bool> SyncRecipeAsync(int recipeId, CancellationToken ct = default)
    {
        var recipe = await _db.Recipes
            .AsNoTracking()
            .Include(r => r.Category)
            .Include(r => r.DietarySchool)
            .Include(r => r.RecipeIngredients)
                .ThenInclude(ri => ri.Ingredient)
            .FirstOrDefaultAsync(r => r.RecipeId == recipeId, ct);

        if (recipe == null || recipe.Status != "APPROVED_ACTIVE")
        {
            _logger.LogInformation("[KnowledgeSyncService] Recipe #{RecipeId} không tồn tại hoặc không ở trạng thái APPROVED_ACTIVE, tiến hành xóa khỏi Qdrant nếu có.", recipeId);
            await _aiClient.DeleteKnowledgeAsync("RECIPE", recipeId.ToString(), ct);
            return false;
        }

        var sb = new StringBuilder();
        sb.AppendLine($"Món ăn chay: {recipe.Title}");
        if (!string.IsNullOrWhiteSpace(recipe.Description))
        {
            sb.AppendLine($"Mô tả: {recipe.Description.Trim()}");
        }
        sb.AppendLine($"Thể loại: {recipe.Category?.CategoryName ?? "Món chay"} | Trường phái: {recipe.DietarySchool?.SchoolName ?? "Thuần chay"}");
        sb.AppendLine($"Khẩu phần: {recipe.Servings} người | Thời gian chế biến: {recipe.PrepTimeMinutes + recipe.CookTimeMinutes} phút | Năng lượng: {recipe.CaloriesKcal} kcal | Độ khó: {recipe.DifficultyLevel}");

        if (recipe.RecipeIngredients != null && recipe.RecipeIngredients.Any())
        {
            sb.AppendLine("Nguyên liệu chuẩn bị:");
            foreach (var ri in recipe.RecipeIngredients)
            {
                var opt = ri.IsOptional ? " (tùy chọn)" : "";
                var note = !string.IsNullOrWhiteSpace(ri.PreparationNote) ? $" - {ri.PreparationNote.Trim()}" : "";
                sb.AppendLine($"- {ri.Ingredient?.IngredientName ?? "Nguyên liệu"}: {ri.QuantityNeeded} {ri.Unit}{opt}{note}");
            }
        }

        sb.AppendLine("Hướng dẫn các bước thực hiện:");
        sb.AppendLine(recipe.Instructions);

        var title = recipe.Title.Trim();
        var text = sb.ToString();

        return await _aiClient.UpsertKnowledgeAsync("RECIPE", recipeId.ToString(), title, text, recipe.UpdatedAt ?? recipe.CreatedAt, ct);
    }

    /// <summary>
    /// Đồng bộ bài viết cộng đồng đã duyệt (POST) sang Qdrant.
    /// </summary>
    public async Task<bool> SyncPostAsync(int postId, CancellationToken ct = default)
    {
        var post = await _db.Posts
            .AsNoTracking()
            .Include(p => p.User)
            .FirstOrDefaultAsync(p => p.PostId == postId, ct);

        if (post == null || post.ModerationStatus != "APPROVED_ACTIVE" || post.Visibility != "PUBLIC")
        {
            _logger.LogInformation("[KnowledgeSyncService] Post #{PostId} không tồn tại hoặc không ở trạng thái hiển thị công khai, tiến hành xóa khỏi Qdrant nếu có.", postId);
            await _aiClient.DeleteKnowledgeAsync("POST", postId.ToString(), ct);
            return false;
        }

        var sb = new StringBuilder();
        sb.AppendLine($"Bài viết cộng đồng: {post.Title.Trim()}");
        sb.AppendLine($"Tác giả: {post.User?.FullName ?? "Thành viên VeggieLife"}");
        sb.AppendLine("Nội dung chia sẻ:");
        sb.AppendLine(post.Content.Trim());

        return await _aiClient.UpsertKnowledgeAsync("POST", postId.ToString(), post.Title.Trim(), sb.ToString(), post.UpdatedAt ?? post.CreatedAt, ct);
    }

    /// <summary>
    /// Đồng bộ mã phụ gia thực phẩm từ điển (E_NUMBER) sang Qdrant.
    /// </summary>
    public async Task<bool> SyncENumberAsync(string code, CancellationToken ct = default)
    {
        var cleanCode = code.Trim().ToUpperInvariant();
        var entity = await _db.EnumberDictionaries
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Code == cleanCode, ct);

        if (entity == null)
        {
            _logger.LogInformation("[KnowledgeSyncService] Mã E-number {Code} không tìm thấy, xóa khỏi Qdrant nếu có.", cleanCode);
            await _aiClient.DeleteKnowledgeAsync("E_NUMBER", cleanCode, ct);
            return false;
        }

        var sb = new StringBuilder();
        sb.AppendLine($"Mã số phụ gia: {entity.Code}");
        sb.AppendLine($"Tên gọi phổ biến: {entity.Name}");
        if (!string.IsNullOrWhiteSpace(entity.CategoryType))
        {
            sb.AppendLine($"Phân loại công dụng: {entity.CategoryType}");
        }
        sb.AppendLine($"Nguồn gốc xuất xứ: {entity.OriginSource}");
        sb.AppendLine($"Đánh giá ăn chay: {entity.VeganStatus} (VEGAN: Thuần chay, DOUBTFUL: Nghi vấn nguồn gốc động vật, NON_VEGAN: Chứa thành phần động vật)");
        if (!string.IsNullOrWhiteSpace(entity.HealthRiskNotes))
        {
            sb.AppendLine($"Cảnh báo và lưu ý sức khỏe: {entity.HealthRiskNotes.Trim()}");
        }

        var title = $"Mã phụ gia {entity.Code} - {entity.Name}";
        return await _aiClient.UpsertKnowledgeAsync("E_NUMBER", cleanCode, title, sb.ToString(), entity.UpdatedAt, ct);
    }

    /// <summary>
    /// Đồng bộ nguyên liệu đã duyệt (INGREDIENT) sang Qdrant.
    /// </summary>
    public async Task<bool> SyncIngredientAsync(int ingredientId, CancellationToken ct = default)
    {
        var entity = await _db.Ingredients
            .AsNoTracking()
            .Include(i => i.IngredientAllergens)
                .ThenInclude(ia => ia.Allergen)
            .FirstOrDefaultAsync(i => i.IngredientId == ingredientId, ct);

        if (entity == null)
        {
            await _aiClient.DeleteKnowledgeAsync("INGREDIENT", ingredientId.ToString(), ct);
            return false;
        }

        var sb = new StringBuilder();
        sb.AppendLine($"Nguyên liệu thực vật: {entity.IngredientName}");
        sb.AppendLine($"Nhóm thực phẩm: {entity.FoodGroup} | Gian hàng: {entity.DefaultStallType}");
        sb.AppendLine($"Đơn vị chuẩn: {entity.StandardUnit} | Năng lượng ước tính: {entity.CaloriesPer100g} kcal/100g");
        sb.AppendLine($"Thuộc nhóm Ngũ vị tân (hành, tỏi, hẹ, kiệu...): {(entity.IsAllium ? "Có (Người ăn chay ngũ vị tân cần tránh)" : "Không")}");

        if (entity.IngredientAllergens != null && entity.IngredientAllergens.Any())
        {
            var allergens = string.Join(", ", entity.IngredientAllergens.Select(ia => ia.Allergen?.AllergenName).Where(n => !string.IsNullOrEmpty(n)));
            sb.AppendLine($"Cảnh báo dị ứng: Chứa thành phần {allergens}");
        }

        var title = $"Nguyên liệu thực vật: {entity.IngredientName}";
        return await _aiClient.UpsertKnowledgeAsync("INGREDIENT", ingredientId.ToString(), title, sb.ToString(), entity.CreatedAt, ct);
    }

    /// <summary>
    /// Đồng bộ nhóm dị ứng (ALLERGEN) sang Qdrant.
    /// </summary>
    public async Task<bool> SyncAllergenAsync(int allergenId, CancellationToken ct = default)
    {
        var entity = await _db.Allergens
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.AllergenId == allergenId, ct);

        if (entity == null)
        {
            await _aiClient.DeleteKnowledgeAsync("ALLERGEN", allergenId.ToString(), ct);
            return false;
        }

        var sb = new StringBuilder();
        sb.AppendLine($"Nhóm chất gây dị ứng: {entity.AllergenName} ({entity.Code})");
        if (!string.IsNullOrWhiteSpace(entity.Description))
        {
            sb.AppendLine($"Mô tả và triệu chứng: {entity.Description.Trim()}");
        }

        var title = $"Nhóm dị ứng: {entity.AllergenName}";
        return await _aiClient.UpsertKnowledgeAsync("ALLERGEN", allergenId.ToString(), title, sb.ToString(), entity.UpdatedAt, ct);
    }

    /// <summary>
    /// Xóa một mẩu tri thức khỏi Qdrant theo loại nguồn và mã định danh.
    /// </summary>
    public async Task<int> DeleteAsync(string sourceType, string sourceId, CancellationToken ct = default)
    {
        return await _aiClient.DeleteKnowledgeAsync(sourceType, sourceId, ct);
    }
}
