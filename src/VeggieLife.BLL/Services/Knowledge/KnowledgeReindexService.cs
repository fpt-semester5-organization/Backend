using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using VeggieLife.BLL.Common.Constants;
using VeggieLife.BLL.Common.Exceptions;
using VeggieLife.BLL.DTOs.Admin.Knowledge;
using VeggieLife.BLL.Services.AI;
using VeggieLife.BLL.Services.Cache;
using VeggieLife.DAL.Context;

namespace VeggieLife.BLL.Services.Knowledge;

/// <summary>
/// Dịch vụ điều phối nạp lại (reindex) kho tri thức RAG theo phạm vi yêu cầu từ Quản trị viên (FN-CHAT-07).
/// </summary>
public class KnowledgeReindexService : IKnowledgeReindexService
{
    private const string LockKey = "reindex_running_lock";
    private readonly IRedisService _redis;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<KnowledgeReindexService> _logger;

    public KnowledgeReindexService(
        IRedisService redis,
        IServiceScopeFactory scopeFactory,
        ILogger<KnowledgeReindexService> logger)
    {
        _redis = redis;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public async Task<CreateAdminKnowledgeReindexResponseData> TriggerReindexAsync(string scope, CancellationToken ct = default)
    {
        var cleanScope = string.IsNullOrWhiteSpace(scope) ? "ALL" : scope.Trim().ToUpperInvariant();

        // 1. Kiểm tra khóa chống trùng lặp chạy đồng thời (BR-ADM-AI-002 / OpenAPI 409)
        var isRunning = await _redis.ExistsAsync(LockKey, ct);
        if (isRunning)
        {
            throw AppException.Conflict(
                ErrorCodes.ErrKnowledgeReindexRunning,
                "Đang có job đồng bộ kho tri thức đang chạy.");
        }

        // Đặt khóa với TTL 15 phút
        await _redis.SetAsync(LockKey, "RUNNING", TimeSpan.FromMinutes(15), ct);

        var jobId = $"kb_{DateTime.UtcNow:yyyyMMdd_HHmmss}";
        _logger.LogInformation("[KnowledgeReindexService] Bắt đầu xếp job reindex kho tri thức: {JobId}, Scope: {Scope}", jobId, cleanScope);

        // 2. Chạy tác vụ quét nền không chặn luồng trả lời HTTP 202 của Admin
        _ = Task.Run(async () =>
        {
            try
            {
                using var serviceScope = _scopeFactory.CreateScope();
                var db = serviceScope.ServiceProvider.GetRequiredService<AppDbContext>();
                var queue = serviceScope.ServiceProvider.GetRequiredService<IKnowledgeSyncQueue>();
                var aiClient = serviceScope.ServiceProvider.GetRequiredService<IAiServiceClient>();

                await ExecuteReindexBackgroundAsync(cleanScope, db, queue, aiClient);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[KnowledgeReindexService] Lỗi trong tiến trình quét Reindex nền job {JobId}", jobId);
            }
            finally
            {
                // Giải phóng khóa sau khi hoàn tất
                try
                {
                    await _redis.RemoveAsync(LockKey);
                    _logger.LogInformation("[KnowledgeReindexService] Đã giải phóng khóa reindex cho job {JobId}.", jobId);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "[KnowledgeReindexService] Lỗi khi giải phóng khóa reindex cho job {JobId}.", jobId);
                }
            }
        });

        return new CreateAdminKnowledgeReindexResponseData
        {
            JobId = jobId,
            Scope = cleanScope,
            Status = "ACCEPTED"
        };
    }

    private async Task ExecuteReindexBackgroundAsync(
        string scope,
        AppDbContext db,
        IKnowledgeSyncQueue queue,
        IAiServiceClient aiClient)
    {
        _logger.LogInformation("[KnowledgeReindexService] Bắt đầu nạp lại dữ liệu cho phạm vi: {Scope}", scope);

        // 1. Nạp công thức món ăn (RECIPE)
        if (scope == "ALL" || scope == "RECIPE")
        {
            var recipeIds = await db.Recipes
                .AsNoTracking()
                .Where(r => r.Status == "APPROVED_ACTIVE")
                .Select(r => r.RecipeId)
                .ToListAsync();

            _logger.LogInformation("[KnowledgeReindexService] Đang đẩy {Count} công thức vào hàng đợi đồng bộ.", recipeIds.Count);
            foreach (var id in recipeIds)
            {
                await queue.EnqueueUpsertAsync("RECIPE", id);
            }
        }

        // 2. Nạp bài viết cộng đồng (POST)
        if (scope == "ALL" || scope == "POST")
        {
            var postIds = await db.Posts
                .AsNoTracking()
                .Where(p => p.ModerationStatus == "APPROVED_ACTIVE" && p.Visibility == "PUBLIC")
                .Select(p => p.PostId)
                .ToListAsync();

            _logger.LogInformation("[KnowledgeReindexService] Đang đẩy {Count} bài viết vào hàng đợi đồng bộ.", postIds.Count);
            foreach (var id in postIds)
            {
                await queue.EnqueueUpsertAsync("POST", id);
            }
        }

        // 3. Nạp từ điển E-number, nguyên liệu, dị ứng (DICTIONARY)
        if (scope == "ALL" || scope == "DICTIONARY")
        {
            var eCodes = await db.EnumberDictionaries
                .AsNoTracking()
                .Select(e => e.Code)
                .ToListAsync();

            _logger.LogInformation("[KnowledgeReindexService] Đang đẩy {Count} mã E-number vào hàng đợi đồng bộ.", eCodes.Count);
            foreach (var code in eCodes)
            {
                await queue.EnqueueUpsertAsync("E_NUMBER", code);
            }

            var ingIds = await db.Ingredients
                .AsNoTracking()
                .Select(i => i.IngredientId)
                .ToListAsync();

            _logger.LogInformation("[KnowledgeReindexService] Đang đẩy {Count} nguyên liệu vào hàng đợi đồng bộ.", ingIds.Count);
            foreach (var id in ingIds)
            {
                await queue.EnqueueUpsertAsync("INGREDIENT", id);
            }

            var allergenIds = await db.Allergens
                .AsNoTracking()
                .Select(a => a.AllergenId)
                .ToListAsync();

            _logger.LogInformation("[KnowledgeReindexService] Đang đẩy {Count} nhóm dị ứng vào hàng đợi đồng bộ.", allergenIds.Count);
            foreach (var id in allergenIds)
            {
                await queue.EnqueueUpsertAsync("ALLERGEN", id);
            }
        }

        // 4. Nạp tài liệu dinh dưỡng (NUTRITION_DOC)
        if (scope == "ALL" || scope == "NUTRITION_DOC")
        {
            _logger.LogInformation("[KnowledgeReindexService] Gọi AI Service nạp lại tài liệu dinh dưỡng (NUTRITION_DOC)...");
            await aiClient.ReindexNutritionAsync();
        }

        _logger.LogInformation("[KnowledgeReindexService] Quét và xếp hàng nạp lại dữ liệu cho phạm vi {Scope} thành công.", scope);
    }
}
