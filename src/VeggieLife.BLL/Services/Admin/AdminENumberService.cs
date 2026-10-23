using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using VeggieLife.BLL.Common.Constants;
using VeggieLife.BLL.Common.Exceptions;
using VeggieLife.BLL.Common.Responses;
using VeggieLife.BLL.DTOs.Admin.ENumbers;
using VeggieLife.BLL.Services.Cache;
using VeggieLife.BLL.Services.Knowledge;
using VeggieLife.BLL.Validators.Admin;
using VeggieLife.DAL.Context;
using VeggieLife.DAL.Entities;

namespace VeggieLife.BLL.Services.Admin;

/// <summary>
/// Triển khai dịch vụ Quản trị từ điển mã phụ gia E-number (FN-ADM-11, BR-ADM-MAS-002, BR-ADM-AI-005, BR-SYS-MAS-01).
/// </summary>
public class AdminENumberService : IAdminENumberService
{
    private const string CacheKeyENumbers = "enumber_cache";

    private static readonly Regex AnimalKeywordRegex = new(
        @"(?i)(?:^|[^\p{L}\p{N}])(sữa|whey|casein|trứng|mật\s+ong|gelatin|thịt|cá|tôm|mực|mỡ\s+heo|mỡ\s+bò|mỡ\s+động\s+vật|nước\s+mắm|chiết\s+xuất\s+thịt|carmine|cochineal|e120|shellac|e904|rennet|tallow|lard)(?:$|[^\p{L}\p{N}])",
        RegexOptions.Compiled);

    private readonly AppDbContext _db;
    private readonly IRedisService _redis;
    private readonly IKnowledgeSyncQueue _knowledgeSyncQueue;
    private readonly ILogger<AdminENumberService> _logger;

    public AdminENumberService(
        AppDbContext db,
        IRedisService redis,
        IKnowledgeSyncQueue knowledgeSyncQueue,
        ILogger<AdminENumberService> logger)
    {
        _db = db;
        _redis = redis;
        _knowledgeSyncQueue = knowledgeSyncQueue;
        _logger = logger;
    }

    /// <summary>
    /// FN-ADM-11: Lấy danh sách từ điển mã phụ gia E-number có phân trang và bộ lọc.
    /// </summary>
    public async Task<(IReadOnlyList<AdminENumberListItemDto> Items, PagedMeta Meta)> GetENumbersAsync(
        AdminENumberFilterRequest request,
        CancellationToken ct = default)
    {
        AdminENumberValidator.ValidateFilter(request);

        var query = _db.EnumberDictionaries.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.VeganStatus))
        {
            var upperStatus = request.VeganStatus.Trim().ToUpperInvariant();
            query = query.Where(e => e.VeganStatus == upperStatus);
        }

        if (!string.IsNullOrWhiteSpace(request.CategoryType))
        {
            var categoryTrimmed = request.CategoryType.Trim();
            query = query.Where(e => e.CategoryType != null && e.CategoryType.Contains(categoryTrimmed));
        }

        if (!string.IsNullOrWhiteSpace(request.Keyword))
        {
            var keywordTrimmed = request.Keyword.Trim();
            query = query.Where(e =>
                e.Code.Contains(keywordTrimmed) ||
                e.Name.Contains(keywordTrimmed) ||
                e.OriginSource.Contains(keywordTrimmed));
        }

        var total = await query.CountAsync(ct);

        var entities = await query
            .OrderBy(e => e.Code)
            .Skip((request.Page - 1) * request.Size)
            .Take(request.Size)
            .ToListAsync(ct);

        var items = entities.Select(e => new AdminENumberListItemDto
        {
            Code = e.Code,
            Name = e.Name,
            CategoryType = e.CategoryType,
            OriginSource = e.OriginSource,
            VeganStatus = e.VeganStatus,
            HealthRiskNotes = e.HealthRiskNotes,
            CreatedAt = e.CreatedAt,
            UpdatedAt = e.UpdatedAt
        }).ToList();

        var meta = new PagedMeta(request.Page, request.Size, total);
        return (items, meta);
    }

    /// <summary>
    /// FN-ADM-11: Lấy chi tiết thông tin một mã phụ gia E-number theo mã Code.
    /// </summary>
    public async Task<AdminENumberListItemDto> GetENumberByCodeAsync(
        string code,
        CancellationToken ct = default)
    {
        AdminENumberValidator.ValidateCode(code);
        var cleanCode = code.Trim().ToUpperInvariant();

        var entity = await _db.EnumberDictionaries
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Code == cleanCode, ct);

        if (entity == null)
        {
            throw AppException.NotFound(ErrorCodes.ErrAdmNotFound, $"Không tìm thấy mã E-number '{cleanCode}'.");
        }

        return new AdminENumberListItemDto
        {
            Code = entity.Code,
            Name = entity.Name,
            CategoryType = entity.CategoryType,
            OriginSource = entity.OriginSource,
            VeganStatus = entity.VeganStatus,
            HealthRiskNotes = entity.HealthRiskNotes,
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt
        };
    }

    /// <summary>
    /// FN-ADM-11: Thêm mới mã E-number vào từ điển, ghi nhận AuditLog và nạp vào kho tri thức chatbot.
    /// </summary>
    public async Task<CreateAdminENumberResponseData> CreateENumberAsync(
        CreateAdminENumberRequest request,
        int adminId,
        CancellationToken ct = default)
    {
        AdminENumberValidator.ValidateCreate(request);

        var cleanCode = request.Code.Trim().ToUpperInvariant();

        // Kiểm tra mã Code duy nhất
        var isDuplicate = await _db.EnumberDictionaries.AnyAsync(
            e => e.Code.ToLower() == cleanCode.ToLower(), ct);

        if (isDuplicate)
        {
            throw AppException.Conflict(
                ErrorCodes.ErrEnumberDuplicate,
                $"Mã E-number '{cleanCode}' đã tồn tại trong từ điển.");
        }

        var entity = new EnumberDictionary
        {
            Code = cleanCode,
            Name = request.Name.Trim(),
            CategoryType = string.IsNullOrWhiteSpace(request.CategoryType) ? null : request.CategoryType.Trim(),
            OriginSource = request.OriginSource.Trim(),
            VeganStatus = request.VeganStatus.Trim().ToUpperInvariant(),
            HealthRiskNotes = string.IsNullOrWhiteSpace(request.HealthRiskNotes) ? null : request.HealthRiskNotes.Trim(),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _db.EnumberDictionaries.Add(entity);
        await _db.SaveChangesAsync(ct);

        // Ghi AuditLogs
        _db.AuditLogs.Add(new AuditLog
        {
            ActorUserId = adminId,
            ActorRole = UserRoles.Admin,
            Action = "CREATE_ENUMBER",
            TargetTable = "ENumberDictionary",
            TargetId = cleanCode,
            NewValue = JsonSerializer.Serialize(new
            {
                entity.Code,
                entity.Name,
                entity.CategoryType,
                entity.OriginSource,
                entity.VeganStatus,
                entity.HealthRiskNotes
            }),
            CreatedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync(ct);

        // Xóa bộ nhớ đệm
        await InvalidateCacheAsync(ct);

        // Đẩy mục mới vào kho tri thức chatbot RAG
        try
        {
            await _knowledgeSyncQueue.EnqueueUpsertAsync("E_NUMBER", cleanCode, ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Lỗi khi đưa mã E-number {Code} vào hàng đợi đồng bộ kho tri thức.", cleanCode);
        }

        return new CreateAdminENumberResponseData
        {
            Code = cleanCode
        };
    }

    /// <summary>
    /// FN-ADM-11: Cập nhật thông tin mã phụ gia E-number. Nếu đổi trạng thái chay, tính lại kết luận sản phẩm liên quan (bỏ qua Admin_Verified = 1) và cập nhật kho tri thức chatbot.
    /// </summary>
    public async Task<UpdateAdminENumberResponseData> UpdateENumberAsync(
        string code,
        UpdateAdminENumberRequest request,
        int adminId,
        CancellationToken ct = default)
    {
        AdminENumberValidator.ValidateCode(code);
        AdminENumberValidator.ValidateUpdate(request);

        var cleanCode = code.Trim().ToUpperInvariant();

        var entity = await _db.EnumberDictionaries.FirstOrDefaultAsync(e => e.Code == cleanCode, ct);
        if (entity == null)
        {
            throw AppException.NotFound(ErrorCodes.ErrAdmNotFound, $"Không tìm thấy mã E-number '{cleanCode}'.");
        }

        var oldValue = JsonSerializer.Serialize(new
        {
            entity.Code,
            entity.Name,
            entity.CategoryType,
            entity.OriginSource,
            entity.VeganStatus,
            entity.HealthRiskNotes
        });

        if (request.Name != null)
        {
            entity.Name = request.Name.Trim();
        }

        if (request.OriginSource != null)
        {
            entity.OriginSource = request.OriginSource.Trim();
        }

        if (request.CategoryType != null)
        {
            entity.CategoryType = string.IsNullOrWhiteSpace(request.CategoryType) ? null : request.CategoryType.Trim();
        }

        if (request.HealthRiskNotes != null)
        {
            entity.HealthRiskNotes = string.IsNullOrWhiteSpace(request.HealthRiskNotes) ? null : request.HealthRiskNotes.Trim();
        }

        var statusChanged = false;
        if (request.VeganStatus != null)
        {
            var newStatus = request.VeganStatus.Trim().ToUpperInvariant();
            if (newStatus != entity.VeganStatus)
            {
                statusChanged = true;
                entity.VeganStatus = newStatus;
            }
        }

        entity.UpdatedAt = DateTime.UtcNow;

        // BR-ADM-AI-005: Đổi Vegan_Status -> đưa job/xử lý tính lại Products.Vegan_Verdict (bỏ qua sản phẩm Admin_Verified = 1)
        if (statusChanged)
        {
            await RecalculateAffectedProductsAsync(cleanCode, entity.VeganStatus, ct);
        }

        await _db.SaveChangesAsync(ct);

        var newValue = JsonSerializer.Serialize(new
        {
            entity.Code,
            entity.Name,
            entity.CategoryType,
            entity.OriginSource,
            entity.VeganStatus,
            entity.HealthRiskNotes
        });

        // Ghi AuditLogs
        _db.AuditLogs.Add(new AuditLog
        {
            ActorUserId = adminId,
            ActorRole = UserRoles.Admin,
            Action = "UPDATE_ENUMBER",
            TargetTable = "ENumberDictionary",
            TargetId = cleanCode,
            OldValue = oldValue,
            NewValue = newValue,
            CreatedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync(ct);

        // Xóa bộ nhớ đệm
        await InvalidateCacheAsync(ct);

        // Cập nhật mục vào kho tri thức chatbot RAG
        try
        {
            await _knowledgeSyncQueue.EnqueueUpsertAsync("E_NUMBER", cleanCode, ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Lỗi khi đưa mã E-number {Code} vào hàng đợi đồng bộ kho tri thức.", cleanCode);
        }

        return new UpdateAdminENumberResponseData
        {
            Code = cleanCode,
            VeganStatus = entity.VeganStatus
        };
    }

    /// <summary>
    /// Tính lại Vegan_Verdict cho toàn bộ các sản phẩm chứa mã phụ gia vừa thay đổi trạng thái chay (BR-ADM-AI-005).
    /// Bỏ qua sản phẩm đã được Admin thẩm định thủ công (Admin_Verified = 1).
    /// </summary>
    private async Task RecalculateAffectedProductsAsync(
        string changedCode,
        string newStatusForChangedCode,
        CancellationToken ct)
    {
        // Lấy danh sách sản phẩm liên quan chưa qua Admin thẩm định thủ công
        var affectedProducts = await _db.Products
            .Include(p => p.ProductEnumbers)
            .Where(p => !p.AdminVerified && p.ProductEnumbers.Any(pe => pe.Code == changedCode))
            .ToListAsync(ct);

        if (!affectedProducts.Any())
        {
            return;
        }

        // Lấy tất cả mã E-number khác thuộc các sản phẩm này để tra cứu trạng thái
        var allRelatedCodes = affectedProducts
            .SelectMany(p => p.ProductEnumbers.Select(pe => pe.Code))
            .Distinct()
            .ToList();

        var dictMap = await _db.EnumberDictionaries
            .AsNoTracking()
            .Where(e => allRelatedCodes.Contains(e.Code))
            .ToDictionaryAsync(e => e.Code, e => e.VeganStatus, ct);

        // Đảm bảo mã vừa đổi có giá trị mới nhất
        dictMap[changedCode] = newStatusForChangedCode;

        foreach (var product in affectedProducts)
        {
            var productCodes = product.ProductEnumbers.Select(pe => pe.Code).ToList();
            var hasNonVegan = false;
            var hasDoubtful = false;

            foreach (var c in productCodes)
            {
                if (dictMap.TryGetValue(c, out var st))
                {
                    if (string.Equals(st, "NON_VEGAN", StringComparison.OrdinalIgnoreCase))
                    {
                        hasNonVegan = true;
                        break;
                    }

                    if (string.Equals(st, "DOUBTFUL", StringComparison.OrdinalIgnoreCase))
                    {
                        hasDoubtful = true;
                    }
                }
            }

            var hasAnimalKeyword = false;
            if (!string.IsNullOrWhiteSpace(product.IngredientsText) && AnimalKeywordRegex.IsMatch(product.IngredientsText))
            {
                hasAnimalKeyword = true;
            }

            string newVerdict;
            if (hasNonVegan || hasAnimalKeyword)
            {
                newVerdict = "NON_VEGAN";
            }
            else if (hasDoubtful)
            {
                newVerdict = "DOUBTFUL";
            }
            else
            {
                newVerdict = "VEGAN";
            }

            if (product.VeganVerdict != newVerdict)
            {
                _logger.LogInformation(
                    "Tính lại kết luận cho sản phẩm ID {ProductId} do mã {Code} đổi trạng thái: {OldVerdict} -> {NewVerdict}",
                    product.ProductId, changedCode, product.VeganVerdict, newVerdict);
                product.VeganVerdict = newVerdict;
            }
        }
    }

    private async Task InvalidateCacheAsync(CancellationToken ct)
    {
        try
        {
            await _redis.RemoveAsync(CacheKeyENumbers, ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Lỗi khi xóa bộ nhớ đệm {CacheKey}", CacheKeyENumbers);
        }
    }
}
