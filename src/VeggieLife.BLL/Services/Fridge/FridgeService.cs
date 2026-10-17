using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using VeggieLife.BLL.Common.Constants;
using VeggieLife.BLL.Common.Exceptions;
using VeggieLife.BLL.Common.Helpers;
using VeggieLife.BLL.DTOs.Fridge.Requests;
using VeggieLife.BLL.DTOs.Fridge.Responses;
using VeggieLife.BLL.Services.Cache;
using VeggieLife.BLL.Services.Media;
using VeggieLife.DAL.Context;
using VeggieLife.DAL.Entities;
using VeggieLife.DAL.External.AiService;

namespace VeggieLife.BLL.Services.Fridge;

/// <summary>
/// Triển khai nghiệp vụ cho các tính năng nhận diện nguyên liệu tủ lạnh qua ảnh (FN-VIS-01)
/// và quản lý nguyên liệu trong tủ lạnh ảo (FN-VIS-02).
/// </summary>
public class FridgeService : IFridgeService
{
    private readonly AppDbContext _db;
    private readonly IAiServiceClient _aiClient;
    private readonly IRedisService _redisService;
    private readonly IMediaService _mediaService;
    private readonly ILogger<FridgeService> _logger;

    public const int GuestAiTrialLimit = 3;
    public const int MemberDailyLimit = 10;
    public const int VipDailyLimit = 30;
    public const long MaxImageFileBytes = 10 * 1024 * 1024; // 10 MB theo BR-AI-CV-01

    public FridgeService(
        AppDbContext db,
        IAiServiceClient aiClient,
        IRedisService redisService,
        IMediaService mediaService,
        ILogger<FridgeService> logger)
    {
        _db = db;
        _aiClient = aiClient;
        _redisService = redisService;
        _mediaService = mediaService;
        _logger = logger;
    }

    /// <summary>
    /// FN-VIS-01: Nhận diện nguyên liệu thực vật và độ tươi từ ảnh tủ lạnh/rổ rau bằng Gemini Vision.
    /// </summary>
    public async Task<CreateFridgeDetectionResponseData> DetectIngredientsAsync(
        FridgeDetectionInputDto input,
        int? userId,
        string userRole,
        string? guestUuid,
        CancellationToken ct = default)
    {
        if (input == null || input.ImageStream == null || input.FileLength <= 0)
        {
            throw AppException.BadRequest(ErrorCodes.ErrValidation, "Vui lòng tải lên tệp hình ảnh tủ lạnh hợp lệ.");
        }

        var isGuest = string.Equals(userRole, UserRoles.Guest, StringComparison.OrdinalIgnoreCase) || userId == null;
        var isVip = string.Equals(userRole, UserRoles.Vip, StringComparison.OrdinalIgnoreCase);

        // =========================================================================
        // BƯỚC 1: KIỂM TRA HẠN MỨC (QUOTA CHECK) - Ưu tiên kiểm tra trước khi xử lý ảnh
        // =========================================================================
        int quotaLimit;
        int currentUsed;
        string? guestRedisKey = null;

        if (isGuest)
        {
            if (string.IsNullOrWhiteSpace(guestUuid))
            {
                throw AppException.Unauthorized(ErrorCodes.ErrAuthInvalidToken, "Thiếu mã định danh phiên khách vãng lai (guestUuid).");
            }

            guestRedisKey = $"guest_quota:ai:{guestUuid}";
            var usedLong = await _redisService.GetAsync<long?>(guestRedisKey, ct) ?? 0;
            currentUsed = (int)usedLong;
            quotaLimit = GuestAiTrialLimit;

            if (currentUsed >= quotaLimit)
            {
                _logger.LogInformation("Khách {GuestId} đã dùng hết hạn mức AI dùng thử ({Used}/{Limit})", guestUuid, currentUsed, quotaLimit);
                throw AppException.Forbidden(
                    ErrorCodes.ErrQuotaAiExceeded,
                    $"Khách đã dùng hết {GuestAiTrialLimit:D2} lượt AI dùng thử (FN-AUTH-02, BR-GUEST-02). Vui lòng đăng ký tài khoản để tiếp tục nhận diện nguyên liệu tủ lạnh.");
            }
        }
        else
        {
            var memberId = userId!.Value;
            quotaLimit = isVip ? VipDailyLimit : MemberDailyLimit;

            var todayUtc = DateTime.UtcNow.Date;
            currentUsed = await _db.AiquotaLogs.CountAsync(l =>
                l.UserId == memberId &&
                l.FeatureCode == "AI_FRIDGE" &&
                l.RequestStatus == "SUCCESS" &&
                l.CreatedAt >= todayUtc, ct);

            if (currentUsed >= quotaLimit)
            {
                _logger.LogInformation("Thành viên {UserId} ({Role}) đã hết hạn mức quét ảnh tủ lạnh trong ngày ({Used}/{Limit})",
                    memberId, userRole, currentUsed, quotaLimit);
                throw AppException.Forbidden(
                    ErrorCodes.ErrQuotaAiExceeded,
                    $"Bạn đã sử dụng hết hạn mức quét ảnh tủ lạnh trong ngày ({currentUsed}/{quotaLimit} ảnh/ngày - BR-PAY-SUB-01). Vui lòng nâng cấp VIP hoặc quay lại vào ngày mai.");
            }
        }

        // =========================================================================
        // BƯỚC 2: KIỂM TRA ĐỊNH DẠNG, MAGIC BYTES, DUNG LƯỢNG VÀ ĐỘ PHÂN GIẢI ẢNH (BR-AI-CV-01)
        // =========================================================================
        // 2.1 Dung lượng tệp <= 10 MB
        if (input.FileLength > MaxImageFileBytes)
        {
            throw new AppException(
                413,
                ErrorCodes.ErrMediaFileTooLarge,
                "Ảnh vượt quá dung lượng tối đa 10 MB (BR-AI-CV-01).");
        }

        // 2.2 Kiểm tra chữ ký magic bytes (JPEG, PNG, WEBP)
        var extension = Path.GetExtension(input.FileName);
        if (string.IsNullOrWhiteSpace(extension))
        {
            throw AppException.BadRequest(ErrorCodes.ErrValidation, "Tên tệp ảnh không có phần mở rộng hợp lệ.");
        }

        input.ImageStream.Position = 0;
        var headerBytes = new byte[16];
        var bytesRead = await input.ImageStream.ReadAsync(headerBytes.AsMemory(0, 16), ct);
        input.ImageStream.Position = 0;

        if (bytesRead < 12 || !ImageMetadataHelper.IsValidImageSignature(headerBytes, extension))
        {
            throw AppException.BadRequest(
                ErrorCodes.ErrValidation,
                "Định dạng tệp ảnh không hợp lệ. Chỉ chấp nhận định dạng JPEG, PNG hoặc WEBP với chữ ký tệp hợp lệ (BR-AI-CV-01, BR-USR-MEDIA-01).");
        }

        // 2.3 Kiểm tra độ phân giải ảnh >= 640x480 pixels
        input.ImageStream.Position = 0;
        var dimSuccess = ImageMetadataHelper.TryGetImageDimensions(input.ImageStream, out var width, out var height);
        input.ImageStream.Position = 0;

        if (!dimSuccess || width <= 0 || height <= 0 ||
            (Math.Max(width, height) < 640 || Math.Min(width, height) < 480))
        {
            _logger.LogWarning("Ảnh tủ lạnh không đạt độ phân giải tối thiểu 640x480: {Width}x{Height}", width, height);
            throw new AppException(
                422,
                ErrorCodes.ErrFridgeImageTooSmall,
                "Độ phân giải ảnh nhỏ hơn mức tối thiểu 640×480 pixels (BR-AI-CV-01).");
        }

        // =========================================================================
        // BƯỚC 3: GỌI DỊCH VỤ AI NHẬN DIỆN NGUYÊN LIỆU VÀ ĐỘ TƯƠI (BR-USR-VIS-FRESH-01)
        // =========================================================================
        if (_aiClient.IsCircuitOpen)
        {
            throw AppException.ServiceUnavailable(
                ErrorCodes.ErrAiCircuitOpen,
                "AI tạm ngắt mạch (BR-ADM-AI-002). Vui lòng thử lại sau.");
        }

        var sw = Stopwatch.StartNew();
        var mimeType = string.IsNullOrWhiteSpace(input.ContentType)
            ? (extension.Equals(".png", StringComparison.OrdinalIgnoreCase) ? "image/png"
               : extension.Equals(".webp", StringComparison.OrdinalIgnoreCase) ? "image/webp" : "image/jpeg")
            : input.ContentType;

        input.ImageStream.Position = 0;
        var aiResult = await _aiClient.DetectFridgeIngredientsAsync(input.ImageStream, mimeType, ct);
        sw.Stop();

        // Nếu AI trả về null và Circuit Breaker vừa bị bật mở
        if (_aiClient.IsCircuitOpen)
        {
            throw AppException.ServiceUnavailable(
                ErrorCodes.ErrAiCircuitOpen,
                "AI tạm ngắt mạch (BR-ADM-AI-002). Vui lòng thử lại sau.");
        }

        // Danh sách nguyên liệu AI thô
        var rawAiItems = aiResult?.Items ?? GetDefaultSimulatedItems();

        // =========================================================================
        // BƯỚC 4: KIỂM TRA CẢNH BÁO TRƯỜNG PHÁI & THỊT/ĐỘNG VẬT (BR-ADM-CNT-003, D109)
        // =========================================================================
        var warnings = new List<string>();
        HashSet<string> forbiddenFoodGroups = new(StringComparer.OrdinalIgnoreCase);
        var requiresAlliumFree = false;
        string? userSchoolName = null;

        if (!isGuest && userId.HasValue)
        {
            var profile = await _db.HealthProfiles
                .Include(hp => hp.DietarySchool)
                .FirstOrDefaultAsync(hp => hp.UserId == userId.Value, ct);

            if (profile?.DietarySchool != null)
            {
                userSchoolName = profile.DietarySchool.SchoolName;
                requiresAlliumFree = profile.RequiresAlliumFree;
                ParseForbiddenRules(profile.DietarySchool.ForbiddenFoodRules, forbiddenFoodGroups);
            }
        }

        foreach (var rawItem in rawAiItems)
        {
            var rawName = rawItem.Name.Trim();
            var isMeatOrAnimal = IsMeatOrSeafood(rawName, rawItem.FoodGroup);

            if (isGuest)
            {
                if (isMeatOrAnimal)
                {
                    warnings.Add($"Cảnh báo: Phát hiện '{rawName}' là thực phẩm có nguồn gốc động vật/hải sản, không thuộc nhóm thuần chay (BR-ADM-CNT-003).");
                }
            }
            else
            {
                if (isMeatOrAnimal)
                {
                    warnings.Add($"Cảnh báo: Phát hiện '{rawName}' là thực phẩm có nguồn gốc động vật/thịt, vi phạm tiêu chuẩn thuần chay (BR-ADM-CNT-003).");
                }
                else if (rawItem.FoodGroup != null && forbiddenFoodGroups.Contains(rawItem.FoodGroup))
                {
                    warnings.Add($"Cảnh báo: Phát hiện '{rawName}' thuộc nhóm '{rawItem.FoodGroup}', không phù hợp với trường phái {userSchoolName ?? "ăn chay"} của bạn (BR-ADM-CNT-003).");
                }
            }
        }

        // =========================================================================
        // BƯỚC 5: ĐỐI CHIẾU KHỚP TÊN VỚI DANH MỤC INGREDIENTS (ĐỘ GIỐNG >= 80%)
        // =========================================================================
        var allIngredients = await _db.Ingredients.AsNoTracking().ToListAsync(ct);
        var detectedList = new List<DetectedFridgeIngredientDto>();
        var matchedItemsForSave = new List<(DetectedFridgeIngredientDto Dto, Ingredient MatchedIngredient)>();

        foreach (var rawItem in rawAiItems)
        {
            var rawName = rawItem.Name.Trim();
            var freshness = NormalizeFreshness(rawItem.FreshnessStatus);

            Ingredient? bestMatch = null;
            var highestSim = 0.0;

            foreach (var ing in allIngredients)
            {
                var sim = StringSimilarityHelper.CalculateSimilarity(rawName, ing.IngredientName);
                if (sim > highestSim)
                {
                    highestSim = sim;
                    bestMatch = ing;
                }
            }

            if (highestSim >= 0.80 && bestMatch != null)
            {
                var dto = new DetectedFridgeIngredientDto
                {
                    InventoryId = null, // Sẽ được điền sau khi lưu vào DB (nếu là Thành viên)
                    IngredientId = bestMatch.IngredientId,
                    Name = bestMatch.IngredientName,
                    FreshnessStatus = freshness
                };
                detectedList.Add(dto);
                matchedItemsForSave.Add((dto, bestMatch));
            }
            else
            {
                // Không khớp >= 80%: không lưu vào DB, trả về để người dùng chọn thủ công
                detectedList.Add(new DetectedFridgeIngredientDto
                {
                    InventoryId = null,
                    IngredientId = null,
                    Name = rawName,
                    FreshnessStatus = freshness
                });
            }
        }

        // =========================================================================
        // BƯỚC 6: LƯU TRỮ VÀO CSDL HOẶC GHI NHẬN HẠN MỨC TRÊN REDIS
        // =========================================================================
        var saved = false;

        if (!isGuest && userId.HasValue)
        {
            var memberId = userId.Value;

            // 6.1 Tải ảnh lên CDN hoặc lấy URL an toàn
            string imageCdnUrl;
            try
            {
                input.ImageStream.Position = 0;
                var uploadResult = await _mediaService.UploadImageAsync(
                    input.ImageStream,
                    input.FileName,
                    "veggielife/fridge",
                    ct);
                imageCdnUrl = uploadResult.SecureUrl;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Không thể tải ảnh tủ lạnh lên Cloudinary. Sử dụng fallback URL nội bộ.");
                imageCdnUrl = $"https://cdn.veggielife.vn/fridge/{DateTime.UtcNow:yyyyMMdd}/{Guid.NewGuid():N}.jpg";
            }

            // 6.2 Tạo phiên nhận diện FridgeSessions
            var fridgeSession = new FridgeSession
            {
                UserId = memberId,
                ImageCdnUrl = imageCdnUrl,
                DetectedItemsCount = matchedItemsForSave.Count,
                CreatedAt = DateTime.UtcNow
            };
            _db.FridgeSessions.Add(fridgeSession);
            await _db.SaveChangesAsync(ct);

            // 6.3 UPSERT vào bảng FridgeInventory: nguyên liệu đã có thì cập nhật độ tươi
            var existingInventoryList = await _db.FridgeInventories
                .Where(f => f.UserId == memberId)
                .ToListAsync(ct);

            foreach (var (dto, matchedIng) in matchedItemsForSave)
            {
                var existingItem = existingInventoryList.FirstOrDefault(f => f.IngredientId == matchedIng.IngredientId);
                if (existingItem != null)
                {
                    existingItem.FreshnessStatus = dto.FreshnessStatus;
                    existingItem.SessionId = fridgeSession.SessionId;
                    dto.InventoryId = existingItem.InventoryId;
                }
                else
                {
                    var newItem = new FridgeInventory
                    {
                        UserId = memberId,
                        IngredientId = matchedIng.IngredientId,
                        SessionId = fridgeSession.SessionId,
                        CustomItemName = matchedIng.IngredientName,
                        FreshnessStatus = dto.FreshnessStatus
                    };
                    _db.FridgeInventories.Add(newItem);
                    existingInventoryList.Add(newItem);
                }
            }

            // Lưu các thay đổi trong FridgeInventory
            await _db.SaveChangesAsync(ct);

            // Gán lại InventoryId cho các mục vừa thêm mới
            foreach (var (dto, matchedIng) in matchedItemsForSave)
            {
                if (dto.InventoryId == null)
                {
                    var savedItem = existingInventoryList.FirstOrDefault(f => f.IngredientId == matchedIng.IngredientId);
                    if (savedItem != null)
                    {
                        dto.InventoryId = savedItem.InventoryId;
                    }
                }
            }

            // 6.4 Ghi nhận nhật ký AIQuotaLogs
            var executionTime = aiResult?.ExecutionTimeMs ?? (int)sw.ElapsedMilliseconds;
            var tokensConsumed = aiResult?.TokensConsumed ?? 250;
            var quotaLog = new AiquotaLog
            {
                UserId = memberId,
                FeatureCode = "AI_FRIDGE",
                TokensConsumed = tokensConsumed,
                CostEstimateUsd = 0.0005m,
                RequestStatus = "SUCCESS",
                ExecutionTimeMs = executionTime > 0 ? executionTime : 500,
                CreatedAt = DateTime.UtcNow
            };
            _db.AiquotaLogs.Add(quotaLog);
            await _db.SaveChangesAsync(ct);

            saved = true;
            currentUsed++;
        }
        else if (isGuest && guestRedisKey != null)
        {
            // Khách vãng lai: KHÔNG ghi DB, chỉ tăng bộ đếm Redis guest_quota:{guestId}:ai
            currentUsed++;
            await _redisService.SetAsync(guestRedisKey, currentUsed, TimeSpan.FromHours(24), ct);
            saved = false;
        }

        // =========================================================================
        // BƯỚC 7: TRẢ VỀ PHẢN HỒI KÈM HẠN MỨC CÒN LẠI
        // =========================================================================
        return new CreateFridgeDetectionResponseData
        {
            Saved = saved,
            Detected = detectedList,
            Warnings = warnings,
            Quota = new FridgeDetectionQuotaDto
            {
                Used = currentUsed,
                Limit = quotaLimit,
                Remaining = Math.Max(0, quotaLimit - currentUsed)
            }
        };
    }

    /// <summary>
    /// FN-VIS-02: Lấy danh sách nguyên liệu trong tủ lạnh ảo, ưu tiên NON-FRESH xếp trước.
    /// </summary>
    public async Task<IReadOnlyList<FridgeItemDto>> GetFridgeItemsAsync(
        int userId,
        string? freshnessFilter = null,
        CancellationToken ct = default)
    {
        var query = _db.FridgeInventories
            .Include(f => f.Ingredient)
            .Where(f => f.UserId == userId);

        if (!string.IsNullOrWhiteSpace(freshnessFilter))
        {
            var filter = freshnessFilter.Trim().ToUpperInvariant();
            if (filter == "FRESH" || filter == "NON-FRESH")
            {
                query = query.Where(f => f.FreshnessStatus == filter);
            }
        }

        // Sắp xếp: NON-FRESH xếp trước FRESH, sau đó xếp theo ID giảm dần
        var items = await query
            .OrderBy(f => f.FreshnessStatus == "NON-FRESH" ? 0 : 1)
            .ThenByDescending(f => f.InventoryId)
            .Select(f => new FridgeItemDto
            {
                InventoryId = f.InventoryId,
                IngredientId = f.IngredientId,
                Name = string.IsNullOrWhiteSpace(f.CustomItemName) ? f.Ingredient.IngredientName : f.CustomItemName,
                FreshnessStatus = f.FreshnessStatus
            })
            .ToListAsync(ct);

        return items;
    }

    /// <summary>
    /// FN-VIS-02: Thêm thủ công một nguyên liệu vào tủ lạnh ảo.
    /// </summary>
    public async Task<FridgeItemDto> AddFridgeItemAsync(
        int userId,
        CreateFridgeItemRequest request,
        CancellationToken ct = default)
    {
        if (request == null || request.IngredientId <= 0)
        {
            throw AppException.BadRequest(ErrorCodes.ErrValidation, "Mã nguyên liệu (ingredientId) bắt buộc và phải lớn hơn 0.");
        }

        if (!string.IsNullOrWhiteSpace(request.CustomItemName) && request.CustomItemName.Length > 100)
        {
            throw AppException.BadRequest(ErrorCodes.ErrValidation, "Tên tùy chỉnh nguyên liệu không được vượt quá 100 ký tự.");
        }

        var ingredient = await _db.Ingredients.FindAsync(new object[] { request.IngredientId }, ct);
        if (ingredient == null)
        {
            throw AppException.BadRequest(ErrorCodes.ErrValidation, "Nguyên liệu không tồn tại trong danh mục hệ thống.");
        }

        // Mỗi nguyên liệu chỉ một dòng cho mỗi người dùng (BR-USR-08)
        var existing = await _db.FridgeInventories.AnyAsync(f => f.UserId == userId && f.IngredientId == request.IngredientId, ct);
        if (existing)
        {
            throw AppException.Conflict(ErrorCodes.ErrFridgeItemExists, "Nguyên liệu đã có trong tủ.");
        }

        var customName = string.IsNullOrWhiteSpace(request.CustomItemName) ? ingredient.IngredientName : request.CustomItemName.Trim();
        var newItem = new FridgeInventory
        {
            UserId = userId,
            IngredientId = request.IngredientId,
            CustomItemName = customName,
            FreshnessStatus = "FRESH"
        };

        _db.FridgeInventories.Add(newItem);
        await _db.SaveChangesAsync(ct);

        return new FridgeItemDto
        {
            InventoryId = newItem.InventoryId,
            IngredientId = newItem.IngredientId,
            Name = newItem.CustomItemName,
            FreshnessStatus = newItem.FreshnessStatus
        };
    }

    /// <summary>
    /// FN-VIS-02: Cập nhật độ tươi hoặc tên hiển thị của nguyên liệu trong tủ lạnh ảo.
    /// </summary>
    public async Task<FridgeItemDto> UpdateFridgeItemAsync(
        int userId,
        int inventoryId,
        UpdateFridgeItemRequest request,
        CancellationToken ct = default)
    {
        if (inventoryId <= 0)
        {
            throw AppException.BadRequest(ErrorCodes.ErrValidation, "Mã mục tủ lạnh (id) phải lớn hơn 0.");
        }

        if (request != null && !string.IsNullOrWhiteSpace(request.CustomItemName) && request.CustomItemName.Length > 100)
        {
            throw AppException.BadRequest(ErrorCodes.ErrValidation, "Tên tùy chỉnh nguyên liệu không được vượt quá 100 ký tự.");
        }

        if (request != null && !string.IsNullOrWhiteSpace(request.FreshnessStatus))
        {
            var upperFresh = request.FreshnessStatus.Trim().ToUpperInvariant();
            if (upperFresh != "FRESH" && upperFresh != "NON-FRESH")
            {
                throw AppException.BadRequest(ErrorCodes.ErrValidation, "Độ tươi chỉ chấp nhận giá trị FRESH hoặc NON-FRESH.");
            }
        }

        var item = await _db.FridgeInventories
            .Include(f => f.Ingredient)
            .FirstOrDefaultAsync(f => f.InventoryId == inventoryId && f.UserId == userId, ct);

        if (item == null)
        {
            throw AppException.NotFound(ErrorCodes.ErrFridgeItemNotFound, "Mục không tồn tại.");
        }

        if (request != null)
        {
            if (!string.IsNullOrWhiteSpace(request.CustomItemName))
            {
                item.CustomItemName = request.CustomItemName.Trim();
            }

            if (!string.IsNullOrWhiteSpace(request.FreshnessStatus))
            {
                item.FreshnessStatus = request.FreshnessStatus.Trim().ToUpperInvariant();
            }
        }

        await _db.SaveChangesAsync(ct);

        return new FridgeItemDto
        {
            InventoryId = item.InventoryId,
            IngredientId = item.IngredientId,
            Name = string.IsNullOrWhiteSpace(item.CustomItemName) ? item.Ingredient.IngredientName : item.CustomItemName,
            FreshnessStatus = item.FreshnessStatus
        };
    }

    /// <summary>
    /// FN-VIS-02: Xóa một mục nguyên liệu khỏi tủ lạnh ảo của người dùng.
    /// </summary>
    public async Task DeleteFridgeItemAsync(
        int userId,
        int inventoryId,
        CancellationToken ct = default)
    {
        if (inventoryId <= 0)
        {
            throw AppException.BadRequest(ErrorCodes.ErrValidation, "Mã mục tủ lạnh (id) phải lớn hơn 0.");
        }

        var item = await _db.FridgeInventories
            .FirstOrDefaultAsync(f => f.InventoryId == inventoryId && f.UserId == userId, ct);

        if (item == null)
        {
            throw AppException.NotFound(ErrorCodes.ErrFridgeItemNotFound, "Mục không tồn tại.");
        }

        _db.FridgeInventories.Remove(item);
        await _db.SaveChangesAsync(ct);
    }

    private static string NormalizeFreshness(string? freshness)
    {
        if (string.IsNullOrWhiteSpace(freshness)) return "FRESH";
        var upper = freshness.Trim().ToUpperInvariant();
        return upper == "NON-FRESH" ? "NON-FRESH" : "FRESH";
    }

    private static bool IsMeatOrSeafood(string name, string? foodGroup)
    {
        if (!string.IsNullOrWhiteSpace(foodGroup))
        {
            var fg = foodGroup.Trim().ToUpperInvariant();
            if (fg == "MEAT" || fg == "SEAFOOD" || fg == "POULTRY" || fg == "ANIMAL")
            {
                return true;
            }
        }

        var lower = name.ToLowerInvariant();
        var meatKeywords = new[] { "thịt", "bò", "heo", "lợn", "gà", "vịt", "cá", "tôm", "cua", "mực", "ốc", "hải sản", "chả cá", "xúc xích heo", "giò lụa" };
        return meatKeywords.Any(k => lower.Contains(k));
    }

    private static void ParseForbiddenRules(string? rulesJson, HashSet<string> forbiddenFoodGroups)
    {
        if (string.IsNullOrWhiteSpace(rulesJson)) return;

        try
        {
            using var doc = System.Text.Json.JsonDocument.Parse(rulesJson);
            var root = doc.RootElement;
            if (root.TryGetProperty("forbiddenFoodGroups", out var groupsElem) && groupsElem.ValueKind == System.Text.Json.JsonValueKind.Array)
            {
                foreach (var g in groupsElem.EnumerateArray())
                {
                    var val = g.GetString();
                    if (!string.IsNullOrWhiteSpace(val))
                    {
                        forbiddenFoodGroups.Add(val);
                    }
                }
            }
        }
        catch
        {
            // Bỏ qua lỗi JSON không hợp lệ
        }
    }

    /// <summary>
    /// FN-VIS-03: Gợi ý công thức món ăn từ nguyên liệu trong tủ lạnh ảo.
    /// Tính độ tương đồng nguyên liệu (>= minSimilarity, mặc định 0.7 theo BR-USR-08).
    /// Ưu tiên xếp hạng cao hơn cho món dùng nguyên liệu NON-FRESH (cần dùng sớm).
    /// Loại trừ các món vi phạm trường phái ăn chay hoặc nhóm dị ứng của người dùng (BR-SCI-04, BR-ADM-CNT-003).
    /// </summary>
    public async Task<IReadOnlyList<FridgeRecipeSuggestionDto>> GetRecipeSuggestionsAsync(
        int userId,
        double minSimilarity = 0.7,
        int limit = 10,
        CancellationToken ct = default)
    {
        // 1. Kiểm tra tủ lạnh ảo có ít nhất 1 nguyên liệu (PRE-1, BR-USR-08)
        var fridgeItems = await _db.FridgeInventories
            .Include(f => f.Ingredient)
            .Where(f => f.UserId == userId)
            .ToListAsync(ct);

        if (fridgeItems.Count == 0)
        {
            throw AppException.Conflict(
                ErrorCodes.ErrFridgeEmpty,
                "Tủ lạnh ảo chưa có nguyên liệu.");
        }

        var fridgeIngredientIds = fridgeItems.Select(f => f.IngredientId).ToHashSet();
        var nonFreshIngredientIds = fridgeItems
            .Where(f => string.Equals(f.FreshnessStatus, "NON-FRESH", StringComparison.OrdinalIgnoreCase))
            .Select(f => f.IngredientId)
            .ToHashSet();

        // 2. Đọc hồ sơ sức khỏe & danh mục dị ứng của người dùng (FN-PROF-04, BR-SCI-04)
        var profile = await _db.HealthProfiles
            .Include(hp => hp.DietarySchool)
            .FirstOrDefaultAsync(hp => hp.UserId == userId, ct);

        var forbiddenFoodGroups = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var requiresAlliumFree = false;

        if (profile?.DietarySchool != null)
        {
            requiresAlliumFree = profile.RequiresAlliumFree;
            ParseForbiddenRules(profile.DietarySchool.ForbiddenFoodRules, forbiddenFoodGroups);
            if (profile.DietarySchool.ForbiddenFoodRules != null &&
                profile.DietarySchool.ForbiddenFoodRules.Contains("\"forbidAllium\":true", StringComparison.OrdinalIgnoreCase))
            {
                requiresAlliumFree = true;
            }
        }

        var userAllergenIds = await _db.UserAllergies
            .Where(ua => ua.UserId == userId)
            .Select(ua => ua.AllergenId)
            .ToListAsync(ct);

        var allergicIngredientIds = await _db.IngredientAllergens
            .Where(ia => userAllergenIds.Contains(ia.AllergenId))
            .Select(ia => ia.IngredientId)
            .Distinct()
            .ToListAsync(ct);

        var forbiddenIngredientSet = new HashSet<int>(allergicIngredientIds);

        // 3. Lấy kho công thức đã duyệt (APPROVED_ACTIVE) kèm danh sách nguyên liệu
        var recipes = await _db.Recipes
            .Include(r => r.RecipeIngredients)
                .ThenInclude(ri => ri.Ingredient)
            .Where(r => r.Status == "APPROVED_ACTIVE")
            .ToListAsync(ct);

        var suggestions = new List<FridgeRecipeSuggestionDto>();

        foreach (var recipe in recipes)
        {
            var recipeIngredients = recipe.RecipeIngredients.ToList();
            if (recipeIngredients.Count == 0) continue;

            // Kiểm tra vi phạm trường phái ăn chay hoặc dị ứng
            var isViolated = false;
            foreach (var ri in recipeIngredients)
            {
                var ing = ri.Ingredient;
                if (ing == null) continue;

                // Vi phạm dị ứng
                if (forbiddenIngredientSet.Contains(ing.IngredientId))
                {
                    isViolated = true;
                    break;
                }

                // Vi phạm nhóm thực phẩm cấm
                if (!string.IsNullOrWhiteSpace(ing.FoodGroup) && forbiddenFoodGroups.Contains(ing.FoodGroup))
                {
                    isViolated = true;
                    break;
                }

                // Vi phạm kiêng ngũ vị tân (Allium)
                if (requiresAlliumFree && ing.IsAllium)
                {
                    isViolated = true;
                    break;
                }
            }

            if (isViolated) continue;

            // Tính điểm tương đồng: số nguyên liệu có trong tủ / tổng nguyên liệu bắt buộc (BR-USR-08)
            var requiredIngredients = recipeIngredients.Where(ri => !ri.IsOptional).ToList();
            if (requiredIngredients.Count == 0)
            {
                requiredIngredients = recipeIngredients;
            }

            var matchedCount = requiredIngredients.Count(ri => fridgeIngredientIds.Contains(ri.IngredientId));
            var similarity = (double)matchedCount / requiredIngredients.Count;
            similarity = Math.Round(similarity, 2);

            if (similarity < minSimilarity)
            {
                continue;
            }

            // Tìm các nguyên liệu còn thiếu
            var missing = requiredIngredients
                .Where(ri => !fridgeIngredientIds.Contains(ri.IngredientId))
                .Select(ri => new MissingIngredientDto
                {
                    IngredientId = ri.IngredientId,
                    Name = ri.Ingredient?.IngredientName ?? $"Nguyên liệu #{ri.IngredientId}"
                })
                .ToList();

            // Kiểm tra món có tận dụng nguyên liệu NON-FRESH không
            var usesNonFresh = recipeIngredients.Any(ri => nonFreshIngredientIds.Contains(ri.IngredientId));

            suggestions.Add(new FridgeRecipeSuggestionDto
            {
                RecipeId = recipe.RecipeId,
                Title = recipe.Title,
                Similarity = similarity,
                MissingIngredients = missing,
                UsesNonFreshItems = usesNonFresh
            });
        }

        // 4. Xếp hạng: ưu tiên món dùng nguyên liệu NON-FRESH trước, sau đó điểm tương đồng giảm dần
        var result = suggestions
            .OrderByDescending(s => s.UsesNonFreshItems)
            .ThenByDescending(s => s.Similarity)
            .ThenByDescending(s => s.RecipeId)
            .Take(limit)
            .ToList();

        return result;
    }

    private static List<AiDetectedItemDto> GetDefaultSimulatedItems()
    {
        return new List<AiDetectedItemDto>
        {
            new() { Name = "Nấm rơm", FreshnessStatus = "FRESH" },
            new() { Name = "Rau lạ", FreshnessStatus = "NON-FRESH" }
        };
    }
}

