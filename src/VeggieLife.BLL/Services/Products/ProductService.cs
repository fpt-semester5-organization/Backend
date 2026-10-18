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
using VeggieLife.BLL.DTOs.Products.Responses;
using VeggieLife.BLL.Services.Cache;
using VeggieLife.DAL.Context;
using VeggieLife.DAL.Entities;
using VeggieLife.DAL.External.AiService;
using VeggieLife.DAL.External.OpenFoodFacts;

namespace VeggieLife.BLL.Services.Products;

/// <summary>
/// Dịch vụ tra cứu mã vạch sản phẩm đóng gói và quản lý lịch sử quét (FN-BAR-01, FN-BAR-02).
/// Tuân thủ: BR-GUEST-07 (Khách 3 lần/ngày qua Redis), BR-USR-09, BR-ADM-MAS-002, BR-ADM-AI-005.
/// </summary>
public class ProductService : IProductService
{
    private readonly AppDbContext _context;
    private readonly IOpenFoodFactsClient _openFoodFactsClient;
    private readonly IAiServiceClient? _aiClient;
    private readonly IRedisService _redis;
    private readonly ILogger<ProductService> _logger;

    private static readonly Regex BarcodeRegex = new(@"^\d{8,14}$", RegexOptions.Compiled);
    private static readonly Regex AdditiveRegex = new(@"(?i)\b(E|INS)\s*(\d{3,4}[a-z]?)\b", RegexOptions.Compiled);

    private static readonly Regex AnimalKeywordRegex = new(
        @"(?i)(?:^|[^\p{L}\p{N}])(sữa|whey|casein|trứng|mật\s+ong|gelatin|thịt|cá|tôm|mực|mỡ\s+heo|mỡ\s+bò|mỡ\s+động\s+vật|nước\s+mắm|chiết\s+xuất\s+thịt|carmine|cochineal|e120|shellac|e904|rennet|tallow|lard)(?:$|[^\p{L}\p{N}])",
        RegexOptions.Compiled);

    public ProductService(
        AppDbContext context,
        IOpenFoodFactsClient openFoodFactsClient,
        IAiServiceClient aiClient,
        IRedisService redis,
        ILogger<ProductService> logger)
    {
        _context = context;
        _openFoodFactsClient = openFoodFactsClient;
        _aiClient = aiClient;
        _redis = redis;
        _logger = logger;
    }

    public ProductService(
        AppDbContext context,
        IOpenFoodFactsClient openFoodFactsClient,
        IRedisService redis,
        ILogger<ProductService> logger)
        : this(context, openFoodFactsClient, null!, redis, logger)
    {
    }

    public async Task<BarcodeScanResponse> ScanBarcodeAsync(
        string barcode,
        int? userId,
        string? guestId = null,
        string method = "CAMERA_BARCODE",
        CancellationToken ct = default)
    {
        // 1. Kiểm tra định dạng mã vạch EAN-8/13 hoặc UPC-A (^\d{8,14}$) -> 400 ERR_VALIDATION
        if (string.IsNullOrWhiteSpace(barcode) || !BarcodeRegex.IsMatch(barcode.Trim()))
        {
            throw AppException.BadRequest(
                ErrorCodes.ErrValidation,
                "Mã vạch không đúng định dạng. Chỉ chấp nhận mã EAN-8, EAN-13 hoặc UPC-A gồm từ 8 đến 14 chữ số.");
        }

        var cleanBarcode = barcode.Trim();
        var scanMethod = string.Equals(method, "MANUAL_INPUT", StringComparison.OrdinalIgnoreCase)
            ? "MANUAL_INPUT"
            : "CAMERA_BARCODE";

        // 2. Kiểm tra hạn mức sử dụng của Khách vãng lai qua Redis (BR-GUEST-07) -> 403 ERR_QUOTA_BARCODE_EXCEEDED
        string? guestQuotaKey = null;
        string? effectiveGuestId = null;
        if (!userId.HasValue)
        {
            effectiveGuestId = !string.IsNullOrWhiteSpace(guestId) ? guestId.Trim() : "anonymous";
            guestQuotaKey = $"guest_quota:{effectiveGuestId}:barcode";

            var currentQuota = await _redis.GetAsync<int?>(guestQuotaKey, ct) ?? 0;
            if (currentQuota == 0)
            {
                var dateKey = $"guest_quota:barcode:{effectiveGuestId}:{DateTime.UtcNow:yyyyMMdd}";
                currentQuota = (int)(await _redis.GetAsync<long?>(dateKey, ct) ?? 0);
            }

            if (currentQuota >= 3)
            {
                throw AppException.Forbidden(
                    ErrorCodes.ErrQuotaBarcodeExceeded,
                    "Khách đã sử dụng hết hạn mức quét mã vạch trong ngày (tối đa 03 lần/ngày). Vui lòng đăng ký tài khoản để tiếp tục sử dụng.");
            }
        }

        // 3. TRA DB TRƯỚC: có Products theo Barcode và Last_Synced_At trong 30 ngày thì dùng luôn kết quả đã lưu (mục tiêu <= 400 ms)
        var product = await _context.Products
            .Include(p => p.ProductEnumbers)
                .ThenInclude(pe => pe.CodeNavigation)
            .Include(p => p.ProductAllergens)
                .ThenInclude(pa => pa.Allergen)
            .FirstOrDefaultAsync(p => p.Barcode == cleanBarcode, ct);

        bool isFresh = product != null
            && product.LastSyncedAt >= DateTime.UtcNow.AddDays(-30)
            && (product.AdminVerified || !string.IsNullOrWhiteSpace(product.ImageUrl));

        if (isFresh)
        {
            // Tận dụng dữ liệu đã lưu, KHÔNG gọi Open Food Facts và KHÔNG gọi AI service
            var cachedApiVerdict = MapToApiVerdict(product!.VeganVerdict);
            bool cachedPersonalAllergyCheck = false;
            var cachedAllergenWarnings = new List<string>();

            if (userId.HasValue)
            {
                var userAllergies = await _context.UserAllergies
                    .Include(ua => ua.Allergen)
                    .Where(ua => ua.UserId == userId.Value)
                    .ToListAsync(ct);

                if (userAllergies.Count > 0)
                {
                    var lowerIng = product.IngredientsText?.ToLowerInvariant() ?? string.Empty;
                    var productAllergenIds = product.ProductAllergens.Select(pa => pa.AllergenId).ToHashSet();

                    foreach (var ua in userAllergies)
                    {
                        var aName = ua.Allergen?.AllergenName;
                        bool matched = productAllergenIds.Contains(ua.AllergenId) ||
                                       (!string.IsNullOrWhiteSpace(aName) && lowerIng.Contains(aName.ToLowerInvariant()));

                        if (matched && !string.IsNullOrWhiteSpace(aName))
                        {
                            cachedPersonalAllergyCheck = true;
                            cachedAllergenWarnings.Add($"Chứa dị ứng cá nhân: {aName}");
                        }
                    }
                }
            }

            // Ghi nhận lịch sử quét mã vạch
            try
            {
                var safeGuestUuid = !userId.HasValue
                    ? (effectiveGuestId?.Length > 36 ? effectiveGuestId[..36] : effectiveGuestId)
                    : null;

                var cachedHistory = new UserScanHistory
                {
                    UserId = userId,
                    GuestUuid = safeGuestUuid,
                    ProductId = product.ProductId,
                    ScanMethod = scanMethod,
                    IsNonVeganTriggered = cachedApiVerdict != "GREEN" || cachedPersonalAllergyCheck,
                    ScannedAt = DateTime.UtcNow
                };
                _context.UserScanHistories.Add(cachedHistory);
                await _context.SaveChangesAsync(ct);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Lỗi khi ghi lịch sử UserScanHistory cho barcode {Barcode}.", cleanBarcode);
            }

            // Khấu trừ hạn mức Khách sau khi commit thành công
            ScanQuotaDto cachedQuota;
            if (!userId.HasValue && guestQuotaKey != null)
            {
                var newUsed = await _redis.IncrementAsync(guestQuotaKey, 1, TimeSpan.FromHours(24), ct);
                try
                {
                    var dateKey = $"guest_quota:barcode:{effectiveGuestId}:{DateTime.UtcNow:yyyyMMdd}";
                    await _redis.IncrementAsync(dateKey, 1, TimeSpan.FromHours(24), ct);
                }
                catch
                {
                    // ignored
                }

                cachedQuota = new ScanQuotaDto
                {
                    Used = (int)newUsed,
                    Limit = 3,
                    Remaining = Math.Max(0, 3 - (int)newUsed)
                };
            }
            else
            {
                cachedQuota = new ScanQuotaDto
                {
                    Used = 1,
                    Limit = 100,
                    Remaining = 99
                };
            }

            var cachedAdditives = product.ProductEnumbers
                .Select(pe => new DetectedAdditiveDto
                {
                    Code = pe.Code,
                    Name = pe.CodeNavigation?.Name ?? $"Phụ gia {pe.Code}",
                    VeganStatus = pe.CodeNavigation?.VeganStatus ?? "DOUBTFUL"
                })
                .ToList();

            var cachedVerdictText = cachedApiVerdict switch
            {
                "RED" => "Chứa phụ gia hoặc thành phần có nguồn gốc động vật.",
                "YELLOW" => "Có chứa thành phần nghi vấn hoặc phụ gia cần lưu ý kiểm tra.",
                _ => "Sản phẩm thuần chay, không phát hiện thành phần động vật."
            };

            return new BarcodeScanResponse
            {
                Barcode = product.Barcode,
                ProductName = product.ProductName,
                Brand = product.Brand ?? "Không rõ thương hiệu",
                ImageUrl = product.ImageUrl,
                VeganVerdict = cachedApiVerdict,
                VerdictText = cachedVerdictText,
                DetectedAdditives = cachedAdditives,
                PersonalAllergyCheck = cachedPersonalAllergyCheck,
                AllergenWarnings = cachedAllergenWarnings.Distinct().ToList(),
                Quota = cachedQuota
            };
        }

        // 4. Chưa có hoặc quá 30 ngày: gọi Open Food Facts (HTTPS GET, timeout 3s)
        OpenFoodFactsProductDto? offProduct = null;
        try
        {
            offProduct = await _openFoodFactsClient.GetProductByBarcodeAsync(cleanBarcode, ct);
        }
        catch (OpenFoodFactsException ex)
        {
            _logger.LogError(ex, "Open Food Facts gặp sự cố khi tra cứu barcode {Barcode}.", cleanBarcode);
            if (product == null)
            {
                throw AppException.ServiceUnavailable(
                    ErrorCodes.ErrExtOpenfoodfactsFailed,
                    "Dịch vụ tra cứu mã vạch Open Food Facts tạm thời gián đoạn. Vui lòng thử lại sau.");
            }
        }

        // Không có sản phẩm -> 404 ERR_PRODUCT_NOT_FOUND và KHÔNG trừ lượt
        if (offProduct == null && product == null)
        {
            throw AppException.NotFound(
                ErrorCodes.ErrProductNotFound,
                $"Không tìm thấy thông tin sản phẩm có mã vạch '{cleanBarcode}' trên hệ thống hoặc Open Food Facts.");
        }

        // 5. Trích mã phụ gia bằng regex (E|INS)\s*\d{3,4}[a-z]? + additives_tags
        var detectedCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        var ingredientsForRegex = offProduct?.IngredientsText ?? product?.IngredientsText;
        if (!string.IsNullOrWhiteSpace(ingredientsForRegex))
        {
            var matches = AdditiveRegex.Matches(ingredientsForRegex);
            foreach (Match m in matches)
            {
                var num = m.Groups[2].Value.ToUpperInvariant();
                detectedCodes.Add("E" + num);
            }
        }

        if (offProduct?.Additives != null)
        {
            foreach (var code in offProduct.Additives)
            {
                detectedCodes.Add(code.ToUpperInvariant());
            }
        }

        if (offProduct?.AdditivesTags != null)
        {
            foreach (var tag in offProduct.AdditivesTags)
            {
                var cleanTag = tag.Replace("en:", "", StringComparison.OrdinalIgnoreCase).ToUpperInvariant();
                detectedCodes.Add(cleanTag);
            }
        }

        if (product != null)
        {
            foreach (var pe in product.ProductEnumbers)
            {
                detectedCodes.Add(pe.Code.ToUpperInvariant());
            }
        }

        // Tra các mục khớp trong ENumberDictionary (nguồn chuẩn BR-SYS-MAS-01)
        var knownDictionary = await _context.EnumberDictionaries
            .Where(e => detectedCodes.Contains(e.Code))
            .ToDictionaryAsync(e => e.Code, StringComparer.OrdinalIgnoreCase, ct);

        // 6. Gọi AI service qua IAiServiceClient tới POST /barcode/classify
        AiBarcodeClassifyResponse? aiResponse = null;
        bool aiCalled = false;
        bool usedFallback = false;

        if (_aiClient != null && !_aiClient.IsCircuitOpen)
        {
            var aiRequest = new AiBarcodeClassifyRequest
            {
                Barcode = cleanBarcode,
                Product = new AiBarcodeProductDto
                {
                    ProductName = offProduct?.ProductName ?? product?.ProductName ?? $"Sản phẩm #{cleanBarcode}",
                    Brand = offProduct?.Brand ?? product?.Brand,
                    IngredientsText = offProduct?.IngredientsText ?? product?.IngredientsText,
                    AdditivesTags = offProduct?.AdditivesTags ?? new(),
                    AllergensTags = offProduct?.AllergensTags ?? new(),
                    TracesTags = offProduct?.TracesTags ?? new(),
                    LabelsTags = offProduct?.LabelsTags ?? new(),
                    IngredientsAnalysisTags = offProduct?.IngredientsAnalysisTags ?? new()
                },
                KnownAdditives = knownDictionary.Values.Select(d => new AiKnownAdditiveDto
                {
                    Code = d.Code,
                    Name = d.Name,
                    VeganStatus = d.VeganStatus
                }).ToList()
            };

            try
            {
                aiCalled = true;
                aiResponse = await _aiClient.ClassifyBarcodeAsync(aiRequest, ct);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Gọi AI service phân loại barcode {Barcode} gặp lỗi. Dùng Fallback.", cleanBarcode);
                aiResponse = null;
            }
        }

        if (aiResponse == null)
        {
            usedFallback = true;
            _logger.LogWarning("Kích hoạt Rule-based Fallback (regex + ENumberDictionary) cho barcode {Barcode}.", cleanBarcode);
        }

        // 7. KIỂM TRA LẠI Ở TẦNG MÃ trước khi lưu (Admin thắng AI, BR-ADM-AI-005; kết luận tính lại theo BR-USR-09)
        var detectedAdditives = new List<DetectedAdditiveDto>();
        bool hasNonVeganAdditive = false;
        bool hasDoubtfulAdditive = false;

        if (aiResponse != null && !usedFallback)
        {
            foreach (var aiAdd in aiResponse.DetectedAdditives)
            {
                var code = aiAdd.Code.ToUpperInvariant();
                var status = aiAdd.VeganStatus.ToUpperInvariant();
                var name = aiAdd.Name;

                // BR-ADM-AI-005: Mã phụ gia có trong ENumberDictionary thì dùng Vegan_Status của từ điển
                if (knownDictionary.TryGetValue(code, out var dict))
                {
                    status = dict.VeganStatus.ToUpperInvariant();
                    name = dict.Name;
                }

                if (status == "NON_VEGAN") hasNonVeganAdditive = true;
                else if (status == "DOUBTFUL") hasDoubtfulAdditive = true;

                detectedAdditives.Add(new DetectedAdditiveDto
                {
                    Code = code,
                    Name = name,
                    VeganStatus = status
                });
            }

            // Bổ sung các mã phụ gia có trong từ điển mà AI có thể sót
            foreach (var kvp in knownDictionary)
            {
                if (!detectedAdditives.Any(a => string.Equals(a.Code, kvp.Key, StringComparison.OrdinalIgnoreCase)))
                {
                    var dict = kvp.Value;
                    var status = dict.VeganStatus.ToUpperInvariant();
                    if (status == "NON_VEGAN") hasNonVeganAdditive = true;
                    else if (status == "DOUBTFUL") hasDoubtfulAdditive = true;

                    detectedAdditives.Add(new DetectedAdditiveDto
                    {
                        Code = dict.Code,
                        Name = dict.Name,
                        VeganStatus = status
                    });
                }
            }
        }
        else
        {
            // Bộ phân loại luật dự phòng
            foreach (var code in detectedCodes)
            {
                if (knownDictionary.TryGetValue(code, out var dict))
                {
                    var status = dict.VeganStatus.ToUpperInvariant();
                    if (status == "NON_VEGAN") hasNonVeganAdditive = true;
                    else if (status == "DOUBTFUL") hasDoubtfulAdditive = true;

                    detectedAdditives.Add(new DetectedAdditiveDto
                    {
                        Code = dict.Code,
                        Name = dict.Name,
                        VeganStatus = status
                    });
                }
                else
                {
                    hasDoubtfulAdditive = true;
                    detectedAdditives.Add(new DetectedAdditiveDto
                    {
                        Code = code,
                        Name = $"Phụ gia {code}",
                        VeganStatus = "DOUBTFUL"
                    });
                }
            }
        }

        // Kiểm tra nguyên liệu động vật từ ingredientsText
        bool hasAnimalIngredients = false;
        var textToCheck = offProduct?.IngredientsText ?? product?.IngredientsText;
        if (!string.IsNullOrWhiteSpace(textToCheck))
        {
            if (AnimalKeywordRegex.IsMatch(textToCheck))
            {
                hasAnimalIngredients = true;
            }
        }

        bool offNonVegan = offProduct?.IngredientsAnalysisTags?.Contains("en:non-vegan") == true ||
                           string.Equals(offProduct?.VeganVerdict, "NON_VEGAN", StringComparison.OrdinalIgnoreCase);

        string calculatedVerdict;
        if (hasNonVeganAdditive || hasAnimalIngredients || offNonVegan)
        {
            calculatedVerdict = "NON_VEGAN";
        }
        else if (hasDoubtfulAdditive || (aiResponse != null && aiResponse.VeganVerdict == "DOUBTFUL") ||
                 string.Equals(offProduct?.VeganVerdict, "DOUBTFUL", StringComparison.OrdinalIgnoreCase))
        {
            calculatedVerdict = "DOUBTFUL";
        }
        else
        {
            calculatedVerdict = "VEGAN";
        }

        // Nếu OFF không có ingredients_text và không có additives_tags thì kết luận DOUBTFUL, không được đoán VEGAN
        bool hasNoIngredientsAndNoAdditives = string.IsNullOrWhiteSpace(offProduct?.IngredientsText) &&
                                              (offProduct?.Additives == null || offProduct.Additives.Count == 0) &&
                                              (offProduct?.AdditivesTags == null || offProduct.AdditivesTags.Count == 0);
        if (hasNoIngredientsAndNoAdditives && calculatedVerdict == "VEGAN")
        {
            calculatedVerdict = "DOUBTFUL";
        }

        // Sản phẩm Admin_Verified = 1 thì không bao giờ bị ghi đè
        if (product != null && product.AdminVerified)
        {
            calculatedVerdict = product.VeganVerdict;
        }

        string apiVerdict = MapToApiVerdict(calculatedVerdict);
        bool personalAllergyCheck = false;
        var allergenWarnings = new List<string>();

        // 8. Transaction lưu DB: UPSERT Products, REPLACE Product_ENumbers, REPLACE ProductAllergens, INSERT UserScanHistory, AIQuotaLogs & AuditLogs
        var strategy = _context.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            using var transaction = _context.Database.IsRelational() ? await _context.Database.BeginTransactionAsync(ct) : null;

            var rawName = !string.IsNullOrWhiteSpace(offProduct?.ProductName) ? offProduct.ProductName : $"Sản phẩm #{cleanBarcode}";
            var safeName = rawName.Length > 150 ? rawName[..150] : rawName;
            var safeBrand = offProduct?.Brand != null && offProduct.Brand.Length > 100 ? offProduct.Brand[..100] : offProduct?.Brand;
            var safeImageUrl = offProduct?.ImageUrl != null && offProduct.ImageUrl.Length > 500 ? offProduct.ImageUrl[..500] : (!string.IsNullOrWhiteSpace(offProduct?.ImageUrl) ? offProduct.ImageUrl : null);

            if (product == null)
            {
                product = new Product
                {
                    Barcode = cleanBarcode,
                    ProductName = safeName,
                    Brand = safeBrand,
                    ImageUrl = safeImageUrl,
                    IngredientsText = offProduct?.IngredientsText,
                    VeganVerdict = calculatedVerdict,
                    AdminVerified = false,
                    CreatedAt = DateTime.UtcNow,
                    LastSyncedAt = DateTime.UtcNow
                };
                _context.Products.Add(product);
                await _context.SaveChangesAsync(ct);
            }
            else
            {
                if (!product.AdminVerified)
                {
                    product.ProductName = safeName;
                    product.Brand = safeBrand ?? product.Brand;
                    if (!string.IsNullOrWhiteSpace(safeImageUrl))
                    {
                        product.ImageUrl = safeImageUrl;
                    }
                    product.IngredientsText = offProduct?.IngredientsText ?? product.IngredientsText;
                    product.VeganVerdict = calculatedVerdict;
                }
                product.LastSyncedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync(ct);
            }

            // REPLACE Product_ENumbers (chỉ mã có trong ENumberDictionary vì có FK)
            _context.ProductEnumbers.RemoveRange(product.ProductEnumbers);
            foreach (var additive in detectedAdditives)
            {
                if (knownDictionary.ContainsKey(additive.Code))
                {
                    _context.ProductEnumbers.Add(new ProductEnumber
                    {
                        ProductId = product.ProductId,
                        Code = additive.Code,
                        CreatedAt = DateTime.UtcNow
                    });
                }
            }

            // REPLACE ProductAllergens (map tên dị ứng sang Allergens.Allergen_ID, Presence_Type CONTAINS/MAY_CONTAIN)
            _context.ProductAllergens.RemoveRange(product.ProductAllergens);
            var allAllergens = await _context.Allergens.ToListAsync(ct);
            var allergenItems = new List<(string Name, string PresenceType)>();
            if (aiResponse?.Allergens != null && aiResponse.Allergens.Count > 0)
            {
                foreach (var a in aiResponse.Allergens)
                {
                    allergenItems.Add((a.Name, a.PresenceType));
                }
            }
            else if (offProduct?.Allergens != null)
            {
                foreach (var a in offProduct.Allergens)
                {
                    allergenItems.Add((a, "CONTAINS"));
                }
            }

            var mappedAllergenIds = new HashSet<int>();
            foreach (var item in allergenItems)
            {
                var cleanName = item.Name.Trim();
                var matchedAllergen = allAllergens.FirstOrDefault(a =>
                    string.Equals(a.AllergenName, cleanName, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(a.Code, cleanName, StringComparison.OrdinalIgnoreCase) ||
                    cleanName.Contains(a.AllergenName, StringComparison.OrdinalIgnoreCase) ||
                    cleanName.Contains(a.Code, StringComparison.OrdinalIgnoreCase));

                if (matchedAllergen != null && mappedAllergenIds.Add(matchedAllergen.AllergenId))
                {
                    _context.ProductAllergens.Add(new ProductAllergen
                    {
                        ProductId = product.ProductId,
                        AllergenId = matchedAllergen.AllergenId,
                        PresenceType = string.Equals(item.PresenceType, "MAY_CONTAIN", StringComparison.OrdinalIgnoreCase) ? "MAY_CONTAIN" : "CONTAINS"
                    });
                }
            }

            // 9. Thành viên: đối chiếu ProductAllergens với UserAllergies để điền personalAllergyCheck và allergenWarnings
            if (userId.HasValue)
            {
                var userAllergies = await _context.UserAllergies
                    .Include(ua => ua.Allergen)
                    .Where(ua => ua.UserId == userId.Value)
                    .ToListAsync(ct);

                if (userAllergies.Count > 0)
                {
                    var lowerIng = product.IngredientsText?.ToLowerInvariant() ?? string.Empty;
                    var productAllergenIds = mappedAllergenIds;

                    foreach (var ua in userAllergies)
                    {
                        var aName = ua.Allergen?.AllergenName;
                        bool matched = productAllergenIds.Contains(ua.AllergenId) ||
                                       (!string.IsNullOrWhiteSpace(aName) && lowerIng.Contains(aName.ToLowerInvariant()));

                        if (matched && !string.IsNullOrWhiteSpace(aName))
                        {
                            personalAllergyCheck = true;
                            allergenWarnings.Add($"Chứa dị ứng cá nhân: {aName}");
                        }
                    }
                }
            }

            apiVerdict = MapToApiVerdict(product.VeganVerdict);
            bool isNonVeganTriggered = apiVerdict != "GREEN" || personalAllergyCheck;

            // INSERT UserScanHistory (User_ID hoặc Guest_UUID, Scan_Method, Is_Non_Vegan_Triggered)
            var safeGuestUuid = !userId.HasValue
                ? (effectiveGuestId?.Length > 36 ? effectiveGuestId[..36] : effectiveGuestId)
                : null;

            var history = new UserScanHistory
            {
                UserId = userId,
                GuestUuid = safeGuestUuid,
                ProductId = product.ProductId,
                ScanMethod = scanMethod,
                IsNonVeganTriggered = isNonVeganTriggered,
                ScannedAt = DateTime.UtcNow
            };
            _context.UserScanHistories.Add(history);

            // INSERT AIQuotaLogs và AuditLogs khi thực sự có gọi AI
            // Lưu ý: CK_AuditLogs_Actor_Role chỉ cho phép ('SYSTEM', 'ADMIN', 'VIP', 'MEMBER')
            // Khi là Khách vãng lai, ActorRole phải ghi 'SYSTEM' để tránh vi phạm CHECK constraint
            if (aiCalled && aiResponse != null)
            {
                if (userId.HasValue)
                {
                    _context.AiquotaLogs.Add(new AiquotaLog
                    {
                        UserId = userId.Value,
                        FeatureCode = "AI_BARCODE",
                        TokensConsumed = aiResponse.Usage?.TotalTokens ?? 0,
                        CostEstimateUsd = 0.0002m,
                        RequestStatus = "SUCCESS",
                        ExecutionTimeMs = aiResponse.Usage?.LatencyMs ?? 0,
                        CreatedAt = DateTime.UtcNow
                    });
                }

                _context.AuditLogs.Add(new AuditLog
                {
                    ActorUserId = userId,
                    ActorRole = userId.HasValue ? "MEMBER" : "SYSTEM",
                    Action = "AI_BARCODE_CLASSIFY",
                    TargetTable = "Products",
                    TargetId = product.ProductId.ToString(),
                    NewValue = JsonSerializer.Serialize(new
                    {
                        model = aiResponse.Usage?.Model,
                        prompt = aiResponse.Audit?.Prompt,
                        temperature = aiResponse.Usage?.Temperature,
                        rawResult = aiResponse.Audit?.RawResult,
                        guestUuid = safeGuestUuid
                    }),
                    CreatedAt = DateTime.UtcNow
                });
            }
            else if (usedFallback)
            {
                _context.AuditLogs.Add(new AuditLog
                {
                    ActorUserId = userId,
                    ActorRole = userId.HasValue ? "MEMBER" : "SYSTEM",
                    Action = "AI_BARCODE_FALLBACK",
                    TargetTable = "Products",
                    TargetId = product.ProductId.ToString(),
                    Note = "Kích hoạt Rule-based Fallback do AI service không phản hồi hoặc mạch ngắt.",
                    CreatedAt = DateTime.UtcNow
                });
            }

            await _context.SaveChangesAsync(ct);
            if (transaction != null)
            {
                await transaction.CommitAsync(ct);
            }
        });

        // 10. Khách: INCR guest_quota:{guestId}:barcode (TTL 24 giờ) SAU KHI commit thành công
        ScanQuotaDto quota;
        if (!userId.HasValue && guestQuotaKey != null)
        {
            var newUsed = await _redis.IncrementAsync(guestQuotaKey, 1, TimeSpan.FromHours(24), ct);
            try
            {
                var dateKey = $"guest_quota:barcode:{effectiveGuestId}:{DateTime.UtcNow:yyyyMMdd}";
                await _redis.IncrementAsync(dateKey, 1, TimeSpan.FromHours(24), ct);
            }
            catch
            {
                // ignored
            }

            quota = new ScanQuotaDto
            {
                Used = (int)newUsed,
                Limit = 3,
                Remaining = Math.Max(0, 3 - (int)newUsed)
            };
        }
        else
        {
            quota = new ScanQuotaDto
            {
                Used = 1,
                Limit = 100,
                Remaining = 99
            };
        }

        // 11. Trả HTTP 200 đúng envelope mẫu
        var verdictText = !string.IsNullOrWhiteSpace(aiResponse?.VerdictText)
            ? aiResponse.VerdictText
            : (apiVerdict switch
            {
                "RED" => "Chứa phụ gia hoặc thành phần có nguồn gốc động vật.",
                "YELLOW" => "Có chứa thành phần nghi vấn hoặc phụ gia cần lưu ý kiểm tra.",
                _ => "Sản phẩm thuần chay, không phát hiện thành phần động vật."
            });

        return new BarcodeScanResponse
        {
            Barcode = product.Barcode,
            ProductName = product.ProductName,
            Brand = product.Brand ?? "Không rõ thương hiệu",
            ImageUrl = product.ImageUrl,
            VeganVerdict = apiVerdict,
            VerdictText = verdictText,
            DetectedAdditives = detectedAdditives,
            PersonalAllergyCheck = personalAllergyCheck,
            AllergenWarnings = allergenWarnings.Distinct().ToList(),
            Quota = quota
        };
    }

    public async Task<PagedResult<UserScanHistoryResponse>> GetUserScanHistoryAsync(
        int userId,
        string? verdict = null,
        bool? allergenOnly = null,
        int page = 1,
        int size = 20,
        CancellationToken ct = default)
    {
        if (page < 1) page = 1;
        if (size < 1) size = 20;
        if (size > 50) size = 50;

        var query = _context.UserScanHistories
            .Include(h => h.Product)
            .Where(h => h.UserId == userId);

        if (!string.IsNullOrWhiteSpace(verdict))
        {
            var dbVerdict = MapToDbVerdict(verdict.Trim());
            query = query.Where(h => h.Product.VeganVerdict == dbVerdict);
        }

        if (allergenOnly == true)
        {
            query = query.Where(h => h.IsNonVeganTriggered);
        }

        var totalItems = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(h => h.ScannedAt)
            .Skip((page - 1) * size)
            .Take(size)
            .Select(h => new UserScanHistoryResponse
            {
                ScanId = (int)h.ScanId,
                Barcode = h.Product.Barcode,
                ProductName = h.Product.ProductName,
                VeganVerdict = MapToApiVerdict(h.Product.VeganVerdict),
                IsAllergenTriggered = h.IsNonVeganTriggered,
                ScannedAt = h.ScannedAt,
                ImageUrl = h.Product.ImageUrl
            })
            .ToListAsync(ct);

        return new PagedResult<UserScanHistoryResponse>(items, totalItems, page, size);
    }

    private static string MapToApiVerdict(string? dbVerdict) => dbVerdict?.ToUpperInvariant() switch
    {
        "NON_VEGAN" => "RED",
        "DOUBTFUL" => "YELLOW",
        "VEGAN" => "GREEN",
        "RED" => "RED",
        "YELLOW" => "YELLOW",
        "GREEN" => "GREEN",
        _ => "YELLOW"
    };

    private static string MapToDbVerdict(string? apiVerdict) => apiVerdict?.ToUpperInvariant() switch
    {
        "RED" => "NON_VEGAN",
        "YELLOW" => "DOUBTFUL",
        "GREEN" => "VEGAN",
        "NON_VEGAN" => "NON_VEGAN",
        "DOUBTFUL" => "DOUBTFUL",
        "VEGAN" => "VEGAN",
        _ => "DOUBTFUL"
    };
}
