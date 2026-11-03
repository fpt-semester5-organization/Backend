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
using AiCostCalculator = VeggieLife.BLL.Services.AI.AiCostCalculator;
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
    private readonly VeggieLife.BLL.Services.Settings.ISystemSettingService? _settingService;

    public const int GuestAiTrialLimit = 3;
    public const int MemberDailyLimit = 10;
    public const int VipDailyLimit = 30;
    public const long MaxImageFileBytes = 10 * 1024 * 1024; // 10 MB theo BR-AI-CV-01

    public FridgeService(
        AppDbContext db,
        IAiServiceClient aiClient,
        IRedisService redisService,
        IMediaService mediaService,
        ILogger<FridgeService> logger,
        VeggieLife.BLL.Services.Settings.ISystemSettingService? settingService = null)
    {
        _db = db;
        _aiClient = aiClient;
        _redisService = redisService;
        _mediaService = mediaService;
        _logger = logger;
        _settingService = settingService;
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
            quotaLimit = _settingService != null
                ? await _settingService.GetIntSettingAsync("CHAT_LIMIT_DAILY_GUEST", GuestAiTrialLimit, ct)
                : GuestAiTrialLimit;

            if (currentUsed >= quotaLimit)
            {
                _logger.LogInformation("Khách {GuestId} đã dùng hết hạn mức AI dùng thử ({Used}/{Limit})", guestUuid, currentUsed, quotaLimit);
                throw AppException.Forbidden(
                    ErrorCodes.ErrQuotaAiExceeded,
                    $"Khách đã dùng hết {quotaLimit:D2} lượt AI dùng thử (FN-AUTH-02, BR-GUEST-02). Vui lòng đăng ký tài khoản để tiếp tục nhận diện nguyên liệu tủ lạnh.");
            }
        }
        else
        {
            var memberId = userId!.Value;
            quotaLimit = _settingService != null
                ? (isVip
                    ? await _settingService.GetIntSettingAsync("FRIDGE_SCAN_LIMIT_DAILY_VIP", VipDailyLimit, ct)
                    : await _settingService.GetIntSettingAsync("FRIDGE_SCAN_LIMIT_DAILY_FREE", MemberDailyLimit, ct))
                : (isVip ? VipDailyLimit : MemberDailyLimit);

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

        using var imageBufferStream = new MemoryStream();
        if (input.ImageStream.CanSeek)
        {
            input.ImageStream.Position = 0;
        }
        await input.ImageStream.CopyToAsync(imageBufferStream, ct);
        var imageBytes = imageBufferStream.ToArray();

        var headerBytes = imageBytes.AsSpan(0, Math.Min(imageBytes.Length, 16)).ToArray();

        if (headerBytes.Length < 12 || !ImageMetadataHelper.IsValidImageSignature(headerBytes, extension))
        {
            throw AppException.BadRequest(
                ErrorCodes.ErrValidation,
                "Định dạng tệp ảnh không hợp lệ. Chỉ chấp nhận định dạng JPEG, PNG hoặc WEBP với chữ ký tệp hợp lệ (BR-AI-CV-01, BR-USR-MEDIA-01).");
        }

        // 2.3 Kiểm tra độ phân giải ảnh >= 640x480 pixels
        using var dimStream = new MemoryStream(imageBytes);
        var dimSuccess = ImageMetadataHelper.TryGetImageDimensions(dimStream, out var width, out var height);

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

        using var aiStream = new MemoryStream(imageBytes);
        var aiResult = await _aiClient.DetectFridgeIngredientsAsync(aiStream, mimeType, ct);
        sw.Stop();

        // Nếu AI trả về null và Circuit Breaker vừa bị bật mở
        if (_aiClient.IsCircuitOpen)
        {
            throw AppException.ServiceUnavailable(
                ErrorCodes.ErrAiCircuitOpen,
                "AI tạm ngắt mạch (BR-ADM-AI-002). Vui lòng thử lại sau.");
        }

        if (aiResult == null)
        {
            throw AppException.ServiceUnavailable(
                ErrorCodes.ErrAiCircuitOpen,
                "Không thể kết nối đến AI-Service hoặc AI xử lý quá thời gian. Vui lòng thử lại sau.");
        }

        // Danh sách nguyên liệu AI thô
        var rawAiItems = aiResult.Items;

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
            var isMeat = (rawItem.IsRedMeat || IsMeatOrSeafood(rawName, rawItem.FoodGroup)) && !IsVegetarianOverride(rawName);

            if (isMeat)
            {
                warnings.Add($"Phát hiện '{rawName}' là đồ ăn mặn / thịt cá (không được lưu vào tủ lạnh chay VeggieLife).");
            }
            else if (rawItem.FoodGroup != null && forbiddenFoodGroups.Contains(rawItem.FoodGroup))
            {
                warnings.Add($"Lưu ý: '{rawName}' thuộc nhóm '{rawItem.FoodGroup}', có thể không phù hợp với trường phái {userSchoolName ?? "ăn chay"} của bạn (BR-ADM-CNT-003).");
            }
        }

        // =========================================================================
        // BƯỚC 5: ĐỐI CHIẾU KHỚP TÊN VỚI DANH MỤC INGREDIENTS HOẶC TỰ ĐỘNG TẠO MỚI
        // =========================================================================
        var allIngredients = await _db.Ingredients.ToListAsync(ct);
        var detectedList = new List<DetectedFridgeIngredientDto>();
        var matchedItemsForSave = new List<(DetectedFridgeIngredientDto Dto, Ingredient MatchedIngredient)>();
        var newIngredientsToPersist = new List<Ingredient>();

        foreach (var rawItem in rawAiItems)
        {
            var rawName = rawItem.Name.Trim();
            var freshness = NormalizeFreshness(rawItem.FreshnessStatus);
            var isMeat = (rawItem.IsRedMeat || IsMeatOrSeafood(rawName, rawItem.FoodGroup)) && !IsVegetarianOverride(rawName);

            // 1. Nếu là đồ ăn mặn / thịt cá -> BỎ QUA KHÔNG LƯU VÀO TỦ LẠNH CHAY
            if (isMeat)
            {
                detectedList.Add(new DetectedFridgeIngredientDto
                {
                    InventoryId = null,
                    IngredientId = null,
                    Name = rawName,
                    FreshnessStatus = freshness,
                    Box2D = rawItem.Box2D,
                    Quantity = rawItem.Quantity,
                    Amount = rawItem.Amount,
                    Unit = rawItem.Unit,
                    FoodGroup = rawItem.FoodGroup ?? "MEAT",
                    IsAnimalProduct = true,
                    IsRedMeat = true,
                    Confidence = 0.95
                });
                continue;
            }

            // 2. Tìm trong danh mục nguyên liệu hiện có (>= 80% tương đồng)
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

            Ingredient targetIngredient;
            string targetName;

            if (highestSim >= 0.80 && bestMatch != null)
            {
                targetIngredient = bestMatch;
                targetName = bestMatch.IngredientName;
            }
            else
            {
                // 3. Thực phẩm của người dùng (chưa có trong danh mục mẫu): TỰ ĐỘNG TẠO ĐỂ LƯU VÀO TỦ LẠNH
                var parenIdx = rawName.IndexOf('(');
                var cleanName = parenIdx > 0 ? rawName.Substring(0, parenIdx).Trim() : rawName;
                if (string.IsNullOrWhiteSpace(cleanName)) cleanName = rawName;
                if (cleanName.Length > 100) cleanName = cleanName[..100];

                var existingDbIng = allIngredients.FirstOrDefault(i => i.IngredientName.Equals(cleanName, StringComparison.OrdinalIgnoreCase));
                if (existingDbIng == null)
                {
                    var unitStr = !string.IsNullOrWhiteSpace(rawItem.Unit) ? rawItem.Unit.Trim() : "phần";
                    if (unitStr.Length > 20) unitStr = unitStr[..20];

                    var fg = NormalizeFoodGroup(rawItem.FoodGroup, cleanName);

                    existingDbIng = new Ingredient
                    {
                        IngredientName = cleanName,
                        StandardUnit = unitStr,
                        DefaultStallType = "PRODUCE",
                        FoodGroup = fg,
                        IsAllium = false,
                        CaloriesPer100g = 50m,
                        CreatedAt = DateTime.UtcNow
                    };
                    _db.Ingredients.Add(existingDbIng);
                    allIngredients.Add(existingDbIng);
                    newIngredientsToPersist.Add(existingDbIng);
                }

                targetIngredient = existingDbIng;
                targetName = cleanName;
            }

            var dto = new DetectedFridgeIngredientDto
            {
                InventoryId = null,
                IngredientId = targetIngredient.IngredientId > 0 ? targetIngredient.IngredientId : null,
                Name = targetName,
                FreshnessStatus = freshness,
                Box2D = rawItem.Box2D,
                Quantity = rawItem.Quantity,
                Amount = rawItem.Amount,
                Unit = rawItem.Unit,
                FoodGroup = rawItem.FoodGroup,
                IsAnimalProduct = rawItem.IsAnimalProduct,
                IsRedMeat = false,
                Confidence = 0.95
            };
            detectedList.Add(dto);
            matchedItemsForSave.Add((dto, targetIngredient));
        }

        // Lưu các nguyên liệu mới tạo vào CSDL trước khi gán vào FridgeInventory
        if (newIngredientsToPersist.Count > 0 && !isGuest && userId.HasValue)
        {
            await _db.SaveChangesAsync(ct);
            foreach (var (dto, ing) in matchedItemsForSave)
            {
                dto.IngredientId = ing.IngredientId;
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
                using var uploadStream = new MemoryStream(imageBytes);
                var uploadResult = await _mediaService.UploadImageAsync(
                    uploadStream,
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

            if (!string.IsNullOrWhiteSpace(imageCdnUrl) && imageCdnUrl.Length > 255)
            {
                imageCdnUrl = imageCdnUrl[..255];
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
                var parsed = ParseAmountAndUnit(dto.Quantity, dto.Amount, dto.Unit, matchedIng.StandardUnit);
                if (existingItem != null)
                {
                    existingItem.FreshnessStatus = dto.FreshnessStatus;
                    existingItem.SessionId = fridgeSession.SessionId;
                    if (!string.IsNullOrWhiteSpace(dto.Quantity))
                    {
                        var displayName = $"{matchedIng.IngredientName} ({dto.Quantity})";
                        existingItem.CustomItemName = displayName.Length > 100 ? displayName[..100] : displayName;
                    }
                    existingItem.Quantity = parsed.Amount;
                    existingItem.Unit = parsed.Unit.Length > 30 ? parsed.Unit[..30] : parsed.Unit;
                    dto.InventoryId = existingItem.InventoryId;
                }
                else
                {
                    var customName = !string.IsNullOrWhiteSpace(dto.Quantity)
                        ? $"{matchedIng.IngredientName} ({dto.Quantity})"
                        : matchedIng.IngredientName;

                    if (customName.Length > 100) customName = customName[..100];

                    var newItem = new FridgeInventory
                    {
                        UserId = memberId,
                        IngredientId = matchedIng.IngredientId,
                        SessionId = fridgeSession.SessionId,
                        CustomItemName = customName,
                        FreshnessStatus = dto.FreshnessStatus,
                        Quantity = parsed.Amount,
                        Unit = parsed.Unit.Length > 30 ? parsed.Unit[..30] : parsed.Unit
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
                if (dto.InventoryId == null || dto.InventoryId == 0)
                {
                    var savedItem = existingInventoryList.FirstOrDefault(f => f.IngredientId == matchedIng.IngredientId);
                    if (savedItem != null && savedItem.InventoryId > 0)
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
                CostEstimateUsd = AiCostCalculator.CalculateFromTotal(tokensConsumed),
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
            .AsNoTracking()
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
                FreshnessStatus = f.FreshnessStatus,
                Quantity = f.Quantity,
                Unit = string.IsNullOrWhiteSpace(f.Unit) ? f.Ingredient.StandardUnit : f.Unit
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
        if (request == null)
        {
            throw AppException.BadRequest(ErrorCodes.ErrValidation, "Dữ liệu yêu cầu không hợp lệ.");
        }

        if (!string.IsNullOrWhiteSpace(request.CustomItemName) && request.CustomItemName.Length > 100)
        {
            throw AppException.BadRequest(ErrorCodes.ErrValidation, "Tên tùy chỉnh nguyên liệu không được vượt quá 100 ký tự.");
        }

        if (request.Quantity < 0)
        {
            throw AppException.BadRequest(ErrorCodes.ErrValidation, "Số lượng nguyên liệu (quantity) không được âm.");
        }

        // 1. Tìm hoặc tự động tạo nguyên liệu trong danh mục nếu chưa có
        Ingredient? ingredient = null;
        if (request.IngredientId.HasValue && request.IngredientId.Value > 0)
        {
            ingredient = await _db.Ingredients.FindAsync(new object[] { request.IngredientId.Value }, ct);
        }

        if (ingredient == null && !string.IsNullOrWhiteSpace(request.CustomItemName))
        {
            var rawName = request.CustomItemName.Trim();
            var parenIdx = rawName.IndexOf('(');
            var cleanName = parenIdx > 0 ? rawName.Substring(0, parenIdx).Trim() : rawName;

            var existingDbIng = await _db.Ingredients.FirstOrDefaultAsync(i => i.IngredientName.ToLower() == cleanName.ToLower(), ct);
            if (existingDbIng == null)
            {
                var standardUnit = !string.IsNullOrWhiteSpace(request.Unit) ? (request.Unit.Length > 20 ? request.Unit[..20] : request.Unit) : "g";
                existingDbIng = new Ingredient
                {
                    IngredientName = cleanName.Length > 100 ? cleanName[..100] : cleanName,
                    StandardUnit = standardUnit,
                    DefaultStallType = "PRODUCE",
                    FoodGroup = NormalizeFoodGroup(null, cleanName),
                    IsAllium = false,
                    CaloriesPer100g = 50m,
                    CreatedAt = DateTime.UtcNow
                };
                _db.Ingredients.Add(existingDbIng);
                await _db.SaveChangesAsync(ct);
            }
            ingredient = existingDbIng;
            request.IngredientId = ingredient.IngredientId;
        }

        if (ingredient == null)
        {
            throw AppException.BadRequest(ErrorCodes.ErrValidation, "Vui lòng chọn hoặc nhập tên nguyên liệu hợp lệ.");
        }

        // Mỗi nguyên liệu chỉ một dòng cho mỗi người dùng (BR-USR-08)
        var existing = await _db.FridgeInventories.AnyAsync(f => f.UserId == userId && f.IngredientId == ingredient.IngredientId, ct);
        if (existing)
        {
            throw AppException.Conflict(ErrorCodes.ErrFridgeItemExists, "Nguyên liệu đã có trong tủ.");
        }

        var parsed = ParseAmountAndUnit(request.CustomItemName, request.Quantity, request.Unit, ingredient.StandardUnit);
        var customName = string.IsNullOrWhiteSpace(request.CustomItemName) ? ingredient.IngredientName : request.CustomItemName.Trim();
        var freshness = string.Equals(request.FreshnessStatus, "NON-FRESH", StringComparison.OrdinalIgnoreCase) ? "NON-FRESH" : "FRESH";
        var quantity = parsed.Amount > 0 ? parsed.Amount : (request.Quantity > 0 ? request.Quantity : 1.0m);
        var unit = !string.IsNullOrWhiteSpace(parsed.Unit) ? parsed.Unit : (!string.IsNullOrWhiteSpace(request.Unit) ? request.Unit.Trim() : ingredient.StandardUnit);

        var newItem = new FridgeInventory
        {
            UserId = userId,
            IngredientId = ingredient.IngredientId,
            CustomItemName = customName.Length > 100 ? customName[..100] : customName,
            FreshnessStatus = freshness,
            Quantity = quantity,
            Unit = unit.Length > 30 ? unit[..30] : unit
        };

        _db.FridgeInventories.Add(newItem);
        await _db.SaveChangesAsync(ct);

        return new FridgeItemDto
        {
            InventoryId = newItem.InventoryId,
            IngredientId = newItem.IngredientId,
            Name = newItem.CustomItemName,
            FreshnessStatus = newItem.FreshnessStatus,
            Quantity = newItem.Quantity,
            Unit = newItem.Unit
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

            if (request.Quantity.HasValue)
            {
                if (request.Quantity.Value <= 0)
                {
                    throw AppException.BadRequest(ErrorCodes.ErrValidation, "Số lượng nguyên liệu (quantity) phải lớn hơn 0.");
                }
                item.Quantity = request.Quantity.Value;
            }

            if (!string.IsNullOrWhiteSpace(request.Unit))
            {
                item.Unit = request.Unit.Trim();
            }
        }

        await _db.SaveChangesAsync(ct);

        return new FridgeItemDto
        {
            InventoryId = item.InventoryId,
            IngredientId = item.IngredientId,
            Name = string.IsNullOrWhiteSpace(item.CustomItemName) ? item.Ingredient.IngredientName : item.CustomItemName,
            FreshnessStatus = item.FreshnessStatus,
            Quantity = item.Quantity,
            Unit = string.IsNullOrWhiteSpace(item.Unit) ? item.Ingredient.StandardUnit : item.Unit
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

    private static string NormalizeFoodGroup(string? rawGroup, string name)
    {
        var lower = (name ?? string.Empty).ToLowerInvariant();
        if (lower.Contains("trứng") || lower.Contains("hột gà") || lower.Contains("hột vịt"))
        {
            return "EGG";
        }
        if (lower.Contains("sữa") || lower.Contains("kem") || lower.Contains("phô mai") || lower.Contains("bơ") ||
            lower.Contains("cheese") || lower.Contains("milk") || lower.Contains("yogurt"))
        {
            return "DAIRY";
        }

        if (!string.IsNullOrWhiteSpace(rawGroup))
        {
            var upper = rawGroup.Trim().ToUpperInvariant();
            if (upper.Contains("DAIRY") || upper.Contains("SỮA")) return "DAIRY";
            if (upper.Contains("EGG") || upper.Contains("TRỨNG")) return "EGG";
            if (upper.Contains("POULTRY") || upper.Contains("GIA CẦM")) return "POULTRY";
            if (upper.Contains("SEAFOOD") || upper.Contains("HẢI SẢN") || upper.Contains("CÁ")) return "SEAFOOD";
        }

        return "PLANT";
    }

    private static (decimal Amount, string Unit) ParseAmountAndUnit(string? quantityText, decimal? rawAmount, string? rawUnit, string defaultUnit)
    {
        // 1. Ưu tiên phân tích khối lượng (g, kg) hoặc thể tích (ml, l) rõ ràng trong văn bản (ví dụ: "1/2 bắp (~300g)", "1 quả (~150g)", "2 chai 500ml", "3 chai 1L", "300 g", "(500 g)")
        if (!string.IsNullOrWhiteSpace(quantityText))
        {
            // Trường hợp 1a: Đa món kèm thể tích/khối lượng từng món: ví dụ: "2 chai 500ml", "3 chai 1L"
            var multiMatch = System.Text.RegularExpressions.Regex.Match(
                quantityText,
                @"(\d+(?:[\.,]\d+)?)\s*(?:chai|hũ|hộp|lon|gói|túi)\s*(\d+(?:[\.,]\d+)?)\s*(ml|l|lít|lit|g|gam|gram|kg|kilo)\b",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);

            if (multiMatch.Success)
            {
                var countStr = multiMatch.Groups[1].Value.Replace(',', '.');
                var volStr = multiMatch.Groups[2].Value.Replace(',', '.');
                if (decimal.TryParse(countStr, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var cnt) &&
                    decimal.TryParse(volStr, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var vol) &&
                    cnt > 0 && vol > 0)
                {
                    var u = multiMatch.Groups[3].Value.ToLowerInvariant();
                    var total = cnt * vol;
                    if (u is "kg" or "kilo" or "kilogram" or "l" or "lít" or "lit")
                    {
                        total *= 1000m;
                    }
                    var stdUnit = (u is "l" or "lít" or "lit" or "ml") ? "ml" : "g";
                    return (total, stdUnit);
                }
            }

            // Trường hợp 1b: Khối lượng hoặc thể tích có đơn vị rõ ràng: ví dụ "(~300g)", "300 g", "500g", "1.5 kg", "500ml"
            var match = System.Text.RegularExpressions.Regex.Match(
                quantityText,
                @"[~]?\s*(\d+(?:[\.,]\d+)?)\s*(kg|kilo|kilogram|g|gam|gram|ml|l|lít|lit)\b",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);

            if (match.Success)
            {
                var numStr = match.Groups[1].Value.Replace(',', '.');
                if (decimal.TryParse(numStr, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var parsedNum) && parsedNum > 0)
                {
                    var u = match.Groups[2].Value.ToLowerInvariant();
                    if (u is "kg" or "kilo" or "kilogram")
                    {
                        return (parsedNum * 1000m, "g");
                    }
                    if (u is "l" or "lít" or "lit")
                    {
                        return (parsedNum * 1000m, "ml");
                    }
                    if (u is "gam" or "gram")
                    {
                        return (parsedNum, "g");
                    }
                    return (parsedNum, u);
                }
            }

            // Trường hợp 1c: Hậu tố số trong ngoặc: ví dụ "Đậu hũ trắng (Tofu) (300)"
            var parenNumMatch = System.Text.RegularExpressions.Regex.Match(
                quantityText,
                @"\(\s*(\d+(?:[\.,]\d+)?)\s*\)",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);

            if (parenNumMatch.Success)
            {
                var numStr = parenNumMatch.Groups[1].Value.Replace(',', '.');
                if (decimal.TryParse(numStr, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var parsedNum) && parsedNum > 0)
                {
                    return (parsedNum, defaultUnit == "ml" ? "ml" : "g");
                }
            }
        }

        // 2. Sử dụng rawAmount nếu có (ví dụ: đã lưu trong database hoặc truyền thủ công)
        if (rawAmount.HasValue && rawAmount.Value > 0)
        {
            var u = !string.IsNullOrWhiteSpace(rawUnit) ? rawUnit.Trim() : defaultUnit;
            if (u.Equals("kg", StringComparison.OrdinalIgnoreCase) || u.Equals("kilo", StringComparison.OrdinalIgnoreCase))
            {
                return (rawAmount.Value * 1000m, "g");
            }
            if (u.Equals("l", StringComparison.OrdinalIgnoreCase) || u.Equals("lít", StringComparison.OrdinalIgnoreCase) || u.Equals("lit", StringComparison.OrdinalIgnoreCase))
            {
                return (rawAmount.Value * 1000m, "ml");
            }
            return (rawAmount.Value, u);
        }

        // 3. Đếm số lượng đếm được: ví dụ "3 quả", "2 củ", "1 bắp"
        if (!string.IsNullOrWhiteSpace(quantityText))
        {
            var countMatch = System.Text.RegularExpressions.Regex.Match(
                quantityText,
                @"(\d+(?:[\.,]\d+)?)\s*([a-zA-Zà-ỹÀ-Ỹ]+)",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);

            if (countMatch.Success)
            {
                var numStr = countMatch.Groups[1].Value.Replace(',', '.');
                if (decimal.TryParse(numStr, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var parsedNum) && parsedNum > 0)
                {
                    var u = countMatch.Groups[2].Value.Trim();
                    return (parsedNum, u);
                }
            }
        }

        return (1.0m, defaultUnit);
    }

    private static bool IsQuantitySufficient(
        decimal neededQty,
        string? neededUnit,
        decimal availableQty,
        string? availableUnit)
    {
        if (neededQty <= 0) return true;
        if (availableQty <= 0) return false;

        var uNeed = (neededUnit ?? string.Empty).Trim().ToLowerInvariant();
        var uAvail = (availableUnit ?? string.Empty).Trim().ToLowerInvariant();

        if (uNeed == uAvail)
        {
            return availableQty >= neededQty;
        }

        var needGrams = ToGrams(neededQty, uNeed);
        var availGrams = ToGrams(availableQty, uAvail);

        if (needGrams.HasValue && availGrams.HasValue)
        {
            return availGrams.Value >= needGrams.Value;
        }

        var needMl = ToMilliliters(neededQty, uNeed);
        var availMl = ToMilliliters(availableQty, uAvail);

        if (needMl.HasValue && availMl.HasValue)
        {
            return availMl.Value >= needMl.Value;
        }

        // Nếu công thức cần định lượng gram (ví dụ: 100g) nhưng tủ lạnh đếm theo quả/cây/bắp/củ (ví dụ: 1 bắp, 2 quả)
        if (needGrams.HasValue && IsCountUnit(uAvail))
        {
            // Trung bình 1 quả, 1 bắp, 1 cây rau củ nặng khoảng 150g - 250g
            var estimatedGrams = availableQty * 150m;
            return estimatedGrams >= needGrams.Value;
        }

        // Nếu tủ lạnh có gram (ví dụ: 300g) nhưng công thức cần miếng/lát
        if (availGrams.HasValue && (uNeed is "m,ieng" or "miếng" or "lát"))
        {
            return availGrams.Value >= (neededQty * 20m);
        }

        if (string.IsNullOrWhiteSpace(uNeed) || string.IsNullOrWhiteSpace(uAvail) || uAvail == "phần" || uNeed == "phần")
        {
            return availableQty >= neededQty;
        }

        if (IsCountUnit(uNeed) && IsCountUnit(uAvail))
        {
            return availableQty >= neededQty;
        }

        return availableQty >= neededQty;
    }

    private static decimal? ToGrams(decimal qty, string unit)
    {
        return unit switch
        {
            "g" or "gam" or "gram" or "grams" => qty,
            "kg" or "kilo" or "kilogram" => qty * 1000m,
            "lạng" or "lang" => qty * 100m,
            "mg" or "miligram" => qty / 1000m,
            _ => null
        };
    }

    private static decimal? ToMilliliters(decimal qty, string unit)
    {
        return unit switch
        {
            "ml" or "mililit" or "mililiters" => qty,
            "l" or "lit" or "lít" or "liter" or "liters" => qty * 1000m,
            "thìa cà phê" or "tsp" or "muỗng cà phê" => qty * 5m,
            "thìa canh" or "tbsp" or "muỗng canh" => qty * 15m,
            "chén" or "bát" or "cup" => qty * 240m,
            _ => null
        };
    }

    private static bool IsCountUnit(string unit)
    {
        var countUnits = new[] { "quả", "trái", "củ", "cây", "bắp", "miếng", "lát", "tép", "nhánh", "bìa", "hộp", "gói", "túi", "lon", "chai" };
        return countUnits.Any(u => unit.Contains(u));
    }

    private static bool IsVegetarianOverride(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return false;
        var lower = name.Trim().ToLowerInvariant();
        return lower.Contains("chay") ||
               lower.Contains("thực vật") ||
               lower.Contains("thuần chay") ||
               lower.Contains("vegan") ||
               lower.Contains("plant-based") ||
               lower.Contains("non-dairy");
    }

    private static bool IsMeatOrSeafood(string name, string? foodGroup)
    {
        if (string.IsNullOrWhiteSpace(name)) return false;
        if (IsVegetarianOverride(name)) return false;

        var lower = name.Trim().ToLowerInvariant();

        // Trứng và các món từ trứng, sữa, nước uống, bánh ngọt KHÔNG PHẢI là đồ ăn mặn / thịt cá
        if (lower.Contains("trứng") || lower.Contains("hột gà") || lower.Contains("hột vịt") ||
            lower.Contains("sữa") || lower.Contains("kem") || lower.Contains("phô mai") || lower.Contains("bơ") ||
            lower.Contains("nước") || lower.Contains("trà") || lower.Contains("bánh"))
        {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(foodGroup))
        {
            var fg = foodGroup.Trim().ToUpperInvariant();
            if (fg.Contains("RED_MEAT") || fg.Contains("THỊT ĐỎ") ||
                fg.Contains("MEAT") || fg.Contains("THỊT") ||
                fg.Contains("POULTRY") || fg.Contains("GIA CẦM") ||
                fg.Contains("SEAFOOD") || fg.Contains("HẢI SẢN") ||
                fg.Contains("THỦY HẢI SẢN") || fg.Contains("FISH") || fg.Contains("CÁ"))
            {
                return true;
            }
        }

        var meatKeywords = new[]
        {
            "thịt", "bò", "heo", "lợn", "gà", "vịt", "ngan", "ngỗng", "cừu", "dê", "bê",
            "ba rọi", "ba chỉ", "sườn non", "sườn heo", "sườn bò", "xương heo", "xương bò",
            "bò bít tết", "beefsteak", "beef", "pork", "chicken",
            "cá ", "cá hồi", "cá thu", "cá chép", "cá lóc", "cá tra", "cá basa", "cá ngừ", "cá đuối", "cá trích",
            "tôm", "cua", "ghẹ", "mực", "bạch tuộc", "hàu", "sò", "nghêu", "ốc", "sứa", "hải sản",
            "chả cá", "giò lụa", "chả lụa", "xúc xích heo", "xúc xích bò", "lạp xưởng", "pate gan", "mỡ heo"
        };

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
            .AsNoTracking()
            .Include(f => f.Ingredient)
            .Where(f => f.UserId == userId)
            .ToListAsync(ct);

        if (fridgeItems.Count == 0)
        {
            throw AppException.Conflict(
                ErrorCodes.ErrFridgeEmpty,
                "Tủ lạnh ảo chưa có nguyên liệu.");
        }

        // Gom nhóm nguyên liệu trong tủ theo IngredientId và theo tên chuẩn hóa để đối chiếu linh hoạt
        var fridgeInventoryMap = new Dictionary<int, (decimal TotalQty, string Unit, bool IsNonFresh)>();
        var fridgeByName = new Dictionary<string, (decimal TotalQty, string Unit, bool IsNonFresh)>(StringComparer.OrdinalIgnoreCase);

        foreach (var item in fridgeItems)
        {
            var (amount, unit) = ParseAmountAndUnit(item.CustomItemName, item.Quantity, item.Unit, item.Ingredient?.StandardUnit ?? "g");
            var isNonFresh = string.Equals(item.FreshnessStatus, "NON-FRESH", StringComparison.OrdinalIgnoreCase);

            // 1. Gom nhóm theo IngredientId
            if (fridgeInventoryMap.TryGetValue(item.IngredientId, out var existing))
            {
                var added = amount;
                if (unit.Equals("kg", StringComparison.OrdinalIgnoreCase) && existing.Unit.Equals("g", StringComparison.OrdinalIgnoreCase))
                {
                    added = amount * 1000m;
                }
                else if (unit.Equals("g", StringComparison.OrdinalIgnoreCase) && existing.Unit.Equals("kg", StringComparison.OrdinalIgnoreCase))
                {
                    added = amount / 1000m;
                }
                fridgeInventoryMap[item.IngredientId] = (existing.TotalQty + added, existing.Unit, existing.IsNonFresh || isNonFresh);
            }
            else
            {
                fridgeInventoryMap[item.IngredientId] = (amount, unit, isNonFresh);
            }

            // 2. Gom nhóm theo tên chuẩn hóa (để hỗ trợ các nguyên liệu đồng nghĩa như súp lơ / bông cải, đậu phụ / đậu hũ)
            var rawNames = new List<string>();
            if (!string.IsNullOrWhiteSpace(item.CustomItemName)) rawNames.Add(item.CustomItemName);
            if (!string.IsNullOrWhiteSpace(item.Ingredient?.IngredientName)) rawNames.Add(item.Ingredient.IngredientName);

            foreach (var rName in rawNames)
            {
                var clean = NormalizeIngName(rName);
                if (!string.IsNullOrWhiteSpace(clean))
                {
                    if (fridgeByName.TryGetValue(clean, out var existingByName))
                    {
                        var added = amount;
                        if (unit.Equals("kg", StringComparison.OrdinalIgnoreCase) && existingByName.Unit.Equals("g", StringComparison.OrdinalIgnoreCase))
                        {
                            added = amount * 1000m;
                        }
                        else if (unit.Equals("g", StringComparison.OrdinalIgnoreCase) && existingByName.Unit.Equals("kg", StringComparison.OrdinalIgnoreCase))
                        {
                            added = amount / 1000m;
                        }
                        fridgeByName[clean] = (existingByName.TotalQty + added, existingByName.Unit, existingByName.IsNonFresh || isNonFresh);
                    }
                    else
                    {
                        fridgeByName[clean] = (amount, unit, isNonFresh);
                    }
                }
            }
        }

        // 2. Đọc hồ sơ sức khỏe & danh mục dị ứng của người dùng (FN-PROF-04, BR-SCI-04)
        var profile = await _db.HealthProfiles
            .AsNoTracking()
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
            .AsNoTracking()
            .Where(ua => ua.UserId == userId)
            .Select(ua => ua.AllergenId)
            .ToListAsync(ct);

        var allergicIngredientIds = await _db.IngredientAllergens
            .AsNoTracking()
            .Where(ia => userAllergenIds.Contains(ia.AllergenId))
            .Select(ia => ia.IngredientId)
            .Distinct()
            .ToListAsync(ct);

        var forbiddenIngredientSet = new HashSet<int>(allergicIngredientIds);

        // 3. Lấy kho công thức đã duyệt (APPROVED_ACTIVE) kèm danh sách nguyên liệu
        var recipes = await _db.Recipes
            .AsNoTracking()
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

            // Tính toán mức độ đáp ứng nguyên liệu & định lượng
            var requiredIngredients = recipeIngredients.Where(ri => !ri.IsOptional).ToList();
            if (requiredIngredients.Count == 0)
            {
                requiredIngredients = recipeIngredients;
            }

            var hasInsufficientQuantity = false;
            var matchedCount = 0;
            var missing = new List<MissingIngredientDto>();

            foreach (var ri in requiredIngredients)
            {
                if (TryMatchFridgeIngredient(ri, fridgeInventoryMap, fridgeByName, out var avail))
                {
                    // Tủ lạnh CÓ nguyên liệu này -> Kiểm tra định lượng:
                    // Công thức cần (ri.QuantityNeeded) phải <= Tủ lạnh đang có (avail.TotalQty)
                    if (IsQuantitySufficient(ri.QuantityNeeded, ri.Unit, avail.TotalQty, avail.Unit))
                    {
                        matchedCount++;
                    }
                    else
                    {
                        // Định lượng công thức cần LỚN HƠN định lượng trong tủ đang có (ví dụ: cần 500g > có 300g)
                        hasInsufficientQuantity = true;
                        missing.Add(new MissingIngredientDto
                        {
                            IngredientId = ri.IngredientId,
                            Name = $"{ri.Ingredient?.IngredientName ?? "Nguyên liệu"} (cần {ri.QuantityNeeded}{ri.Unit}, tủ có {avail.TotalQty}{avail.Unit})"
                        });
                    }
                }
                else
                {
                    // Tủ lạnh KHÔNG có nguyên liệu này
                    missing.Add(new MissingIngredientDto
                    {
                        IngredientId = ri.IngredientId,
                        Name = ri.Ingredient?.IngredientName ?? $"Nguyên liệu #{ri.IngredientId}"
                    });
                }
            }

            // Yêu cầu người dùng: Món ăn được gợi ý phải khớp với số lượng:
            // Nếu nguyên liệu trong tủ KHÔNG ĐỦ định lượng yêu cầu (ri.QuantityNeeded > availableQty) -> LOẠI BỎ MÓN NÀY!
            if (hasInsufficientQuantity)
            {
                continue;
            }

            var similarity = (double)matchedCount / requiredIngredients.Count;
            similarity = Math.Round(similarity, 2);

            if (similarity < minSimilarity)
            {
                continue;
            }

            // Kiểm tra món có tận dụng nguyên liệu NON-FRESH không
            var usesNonFresh = requiredIngredients.Any(ri => fridgeInventoryMap.TryGetValue(ri.IngredientId, out var item) && item.IsNonFresh);

            suggestions.Add(new FridgeRecipeSuggestionDto
            {
                RecipeId = recipe.RecipeId,
                Title = recipe.Title,
                ImageUrl = recipe.ImageUrl,
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

    private static bool TryMatchFridgeIngredient(
        RecipeIngredient ri,
        Dictionary<int, (decimal TotalQty, string Unit, bool IsNonFresh)> fridgeInventoryMap,
        Dictionary<string, (decimal TotalQty, string Unit, bool IsNonFresh)> fridgeByName,
        out (decimal TotalQty, string Unit, bool IsNonFresh) avail)
    {
        // 1. Khớp chính xác qua IngredientId
        if (fridgeInventoryMap.TryGetValue(ri.IngredientId, out avail))
        {
            return true;
        }

        // 2. Khớp qua tên chuẩn hóa hoặc tên đồng nghĩa
        var riName = ri.Ingredient?.IngredientName;
        if (!string.IsNullOrWhiteSpace(riName))
        {
            var cleanRi = NormalizeIngName(riName);
            if (fridgeByName.TryGetValue(cleanRi, out avail))
            {
                return true;
            }

            // Súp lơ / Bông cải xanh
            if ((cleanRi.Contains("súp lơ xanh") || cleanRi.Contains("bông cải xanh") || cleanRi.Contains("broccoli")) &&
                (fridgeByName.TryGetValue("súp lơ xanh", out avail) || fridgeByName.TryGetValue("bông cải xanh", out avail)))
            {
                return true;
            }
            // Súp lơ / Bông cải trắng
            if ((cleanRi.Contains("súp lơ trắng") || cleanRi.Contains("bông cải trắng") || cleanRi.Contains("cauliflower")) &&
                (fridgeByName.TryGetValue("súp lơ trắng", out avail) || fridgeByName.TryGetValue("bông cải trắng", out avail)))
            {
                return true;
            }
            // Đậu hũ / Tofu / Đậu phụ
            if ((cleanRi.Contains("đậu hũ") || cleanRi.Contains("đậu phụ") || cleanRi.Contains("tofu")) &&
                (fridgeByName.TryGetValue("đậu hũ trắng", out avail) || fridgeByName.TryGetValue("đậu hũ non", out avail) || fridgeByName.TryGetValue("đậu hũ", out avail) || fridgeByName.TryGetValue("tofu", out avail)))
            {
                return true;
            }
            // Bắp ngọt / Ngô ngọt
            if ((cleanRi.Contains("bắp ngọt") || cleanRi.Contains("ngô ngọt") || cleanRi.Contains("bắp tươi")) &&
                (fridgeByName.TryGetValue("bắp ngọt", out avail) || fridgeByName.TryGetValue("ngô ngọt", out avail) || fridgeByName.TryGetValue("bắp ngọt tươi", out avail)))
            {
                return true;
            }
            // Nấm đùi gà
            if (cleanRi.Contains("nấm đùi gà") && fridgeByName.TryGetValue("nấm đùi gà", out avail))
            {
                return true;
            }
            // Khoai môn
            if (cleanRi.Contains("khoai môn") && fridgeByName.TryGetValue("khoai môn", out avail))
            {
                return true;
            }
            // Cải xanh / Cải cay
            if ((cleanRi.Contains("cải xanh") || cleanRi.Contains("cải cay")) &&
                (fridgeByName.TryGetValue("cải xanh", out avail) || fridgeByName.TryGetValue("cải xanh cay", out avail)))
            {
                return true;
            }
        }

        avail = default;
        return false;
    }

    private static string NormalizeIngName(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return string.Empty;
        var lower = name.Trim().ToLowerInvariant();
        var parenIdx = lower.IndexOf('(');
        if (parenIdx > 0)
        {
            lower = lower[..parenIdx].Trim();
        }
        return lower;
    }

    private static List<AiDetectedItemDto> GetDefaultSimulatedItems()
    {
        return new List<AiDetectedItemDto>
        {
            new() { Name = "Nấm rơm", FreshnessStatus = "FRESH", Box2D = new List<int> { 145, 230, 420, 510 } },
            new() { Name = "Rau lạ", FreshnessStatus = "NON-FRESH", Box2D = new List<int> { 510, 120, 780, 390 } }
        };
    }

    /// <summary>
    /// Lấy hạn mức quét ảnh tủ lạnh hôm nay của người dùng đồng bộ từ SystemSettings.
    /// </summary>
    public async Task<FridgeDetectionQuotaDto> GetFridgeQuotaAsync(
        int? userId,
        string? guestUuid,
        string userRole,
        CancellationToken ct = default)
    {
        var isGuest = string.Equals(userRole, UserRoles.Guest, StringComparison.OrdinalIgnoreCase) || userId == null;
        var isVip = string.Equals(userRole, UserRoles.Vip, StringComparison.OrdinalIgnoreCase);

        if (isGuest)
        {
            var guestLimit = _settingService != null
                ? await _settingService.GetIntSettingAsync("CHAT_LIMIT_DAILY_GUEST", GuestAiTrialLimit, ct)
                : GuestAiTrialLimit;

            long guestUsed = 0;
            if (!string.IsNullOrEmpty(guestUuid))
            {
                var redisVal = await _redisService.GetAsync<long?>($"guest_quota:ai:{guestUuid}", ct);
                guestUsed = redisVal ?? 0;
            }

            return new FridgeDetectionQuotaDto
            {
                Used = (int)guestUsed,
                Limit = guestLimit,
                Remaining = Math.Max(0, guestLimit - (int)guestUsed)
            };
        }

        var memberId = userId!.Value;
        var quotaLimit = _settingService != null
            ? (isVip
                ? await _settingService.GetIntSettingAsync("FRIDGE_SCAN_LIMIT_DAILY_VIP", VipDailyLimit, ct)
                : await _settingService.GetIntSettingAsync("FRIDGE_SCAN_LIMIT_DAILY_FREE", MemberDailyLimit, ct))
            : (isVip ? VipDailyLimit : MemberDailyLimit);

        var todayUtc = DateTime.UtcNow.Date;
        var currentUsed = await _db.AiquotaLogs.CountAsync(l =>
            l.UserId == memberId &&
            l.FeatureCode == "AI_FRIDGE" &&
            l.RequestStatus == "SUCCESS" &&
            l.CreatedAt >= todayUtc, ct);

        return new FridgeDetectionQuotaDto
        {
            Used = currentUsed,
            Limit = quotaLimit,
            Remaining = Math.Max(0, quotaLimit - currentUsed)
        };
    }
}

