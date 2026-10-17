using System;
using System.Collections.Generic;
using System.Linq;
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
    private readonly IRedisService _redis;
    private readonly ILogger<ProductService> _logger;

    private static readonly Regex BarcodeRegex = new(@"^\d{8,14}$", RegexOptions.Compiled);
    private static readonly Regex AdditiveRegex = new(@"(?i)\b(E|INS)\s*(\d{3,4}[a-z]?)\b", RegexOptions.Compiled);

    private static readonly string[] NonVeganKeywords = new[]
    {
        "sữa", "whey", "casein", "trứng", "mật ong", "gelatin", "thịt", "cá",
        "tôm", "mực", "mỡ heo", "mỡ bò", "mỡ động vật", "nước mắm", "chiết xuất thịt",
        "carmine", "cochineal", "e120", "shellac", "e904", "rennet", "tallow", "lard"
    };

    public ProductService(
        AppDbContext context,
        IOpenFoodFactsClient openFoodFactsClient,
        IRedisService redis,
        ILogger<ProductService> logger)
    {
        _context = context;
        _openFoodFactsClient = openFoodFactsClient;
        _redis = redis;
        _logger = logger;
    }

    public async Task<BarcodeScanResponse> ScanBarcodeAsync(
        string barcode,
        int? userId,
        string? guestId = null,
        string method = "CAMERA_BARCODE",
        CancellationToken ct = default)
    {
        // 1. Kiểm tra định dạng mã vạch EAN-8/13 hoặc UPC-A (^\d{8,14}$)
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

        // 2. Kiểm tra hạn mức sử dụng của Khách vãng lai qua Redis (BR-GUEST-07)
        string? guestQuotaKey = null;
        if (!userId.HasValue)
        {
            var effectiveGuestId = !string.IsNullOrWhiteSpace(guestId) ? guestId.Trim() : "anonymous";
            guestQuotaKey = $"guest_quota:{effectiveGuestId}:barcode";

            var currentQuota = await _redis.GetAsync<int?>(guestQuotaKey, ct) ?? 0;
            if (currentQuota >= 3)
            {
                throw AppException.Forbidden(
                    ErrorCodes.ErrQuotaBarcodeExceeded,
                    "Khách đã sử dụng hết hạn mức quét mã vạch trong ngày (tối đa 03 lần/ngày). Vui lòng đăng ký tài khoản để tiếp tục sử dụng.");
            }
        }

        // 3. Tra cứu sản phẩm trong CSDL cục bộ (bảng Products)
        var product = await _context.Products
            .Include(p => p.ProductEnumbers)
                .ThenInclude(pe => pe.CodeNavigation)
            .Include(p => p.ProductAllergens)
                .ThenInclude(pa => pa.Allergen)
            .FirstOrDefaultAsync(p => p.Barcode == cleanBarcode, ct);

        // Kiểm tra tính mới của dữ liệu: có và LastSyncedAt trong vòng 30 ngày (FN-BAR-01 Logic 3).
        // Nếu sản phẩm trong CSDL chưa có ảnh (ImageUrl null/rỗng) và chưa được Admin kiểm định,
        // thì cần truy vấn Open Food Facts để lấy ảnh sản phẩm phục vụ hiển thị.
        bool isFresh = product != null
            && product.LastSyncedAt >= DateTime.UtcNow.AddDays(-30)
            && (product.AdminVerified || !string.IsNullOrWhiteSpace(product.ImageUrl));

        OpenFoodFactsProductDto? offProduct = null;
        if (!isFresh)
        {
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

            // Nếu Open Food Facts không tìm thấy sản phẩm và CSDL cũng chưa có: trả 404 và KHÔNG trừ lượt
            if (offProduct == null && product == null)
            {
                throw AppException.NotFound(
                    ErrorCodes.ErrProductNotFound,
                    $"Không tìm thấy thông tin sản phẩm có mã vạch '{cleanBarcode}' trên hệ thống hoặc Open Food Facts.");
            }

            if (offProduct != null)
            {
                if (product == null)
                {
                    product = new Product
                    {
                        Barcode = cleanBarcode,
                        ProductName = string.IsNullOrWhiteSpace(offProduct.ProductName)
                            ? $"Sản phẩm #{cleanBarcode}"
                            : offProduct.ProductName,
                        Brand = offProduct.Brand,
                        ImageUrl = !string.IsNullOrWhiteSpace(offProduct.ImageUrl) ? offProduct.ImageUrl : null,
                        IngredientsText = offProduct.IngredientsText,
                        VeganVerdict = offProduct.VeganVerdict,
                        AdminVerified = false,
                        CreatedAt = DateTime.UtcNow,
                        LastSyncedAt = DateTime.UtcNow
                    };
                    _context.Products.Add(product);
                    await _context.SaveChangesAsync(ct);
                }
                else if (!product.AdminVerified)
                {
                    product.ProductName = string.IsNullOrWhiteSpace(offProduct.ProductName) ? product.ProductName : offProduct.ProductName;
                    product.Brand = offProduct.Brand ?? product.Brand;
                    if (!string.IsNullOrWhiteSpace(offProduct.ImageUrl))
                    {
                        product.ImageUrl = offProduct.ImageUrl;
                    }
                    product.IngredientsText = offProduct.IngredientsText ?? product.IngredientsText;
                    product.VeganVerdict = offProduct.VeganVerdict;
                    product.LastSyncedAt = DateTime.UtcNow;
                }
            }
        }

        // 5. Trích xuất mã phụ gia E-Numbers bằng Regex và đối chiếu từ điển EnumberDictionary (FN-BAR-01 Logic 5)
        var detectedCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // Trích xuất từ IngredientsText bằng regex (E|INS)\s*\d{3,4}[a-z]?
        if (!string.IsNullOrWhiteSpace(product!.IngredientsText))
        {
            var matches = AdditiveRegex.Matches(product.IngredientsText);
            foreach (Match m in matches)
            {
                var num = m.Groups[2].Value.ToUpperInvariant();
                detectedCodes.Add("E" + num);
            }
        }

        // Bổ sung các mã phụ gia có sẵn từ Open Food Facts
        if (offProduct?.Additives != null)
        {
            foreach (var code in offProduct.Additives)
            {
                detectedCodes.Add(code.ToUpperInvariant());
            }
        }

        // Bổ sung các mã phụ gia đã lưu từ trước trong CSDL
        foreach (var pe in product.ProductEnumbers)
        {
            detectedCodes.Add(pe.Code.ToUpperInvariant());
        }

        var knownDictionary = await _context.EnumberDictionaries
            .Where(e => detectedCodes.Contains(e.Code))
            .ToDictionaryAsync(e => e.Code, StringComparer.OrdinalIgnoreCase, ct);

        var detectedAdditives = new List<DetectedAdditiveDto>();
        bool hasNonVeganAdditive = false;
        bool hasDoubtfulAdditive = false;

        foreach (var code in detectedCodes)
        {
            if (knownDictionary.TryGetValue(code, out var dict))
            {
                detectedAdditives.Add(new DetectedAdditiveDto
                {
                    Code = dict.Code,
                    Name = dict.Name,
                    VeganStatus = dict.VeganStatus
                });

                if (string.Equals(dict.VeganStatus, "NON_VEGAN", StringComparison.OrdinalIgnoreCase))
                    hasNonVeganAdditive = true;
                else if (string.Equals(dict.VeganStatus, "DOUBTFUL", StringComparison.OrdinalIgnoreCase))
                    hasDoubtfulAdditive = true;

                // Lưu liên kết ProductEnumber nếu chưa có
                if (!product.ProductEnumbers.Any(pe => string.Equals(pe.Code, dict.Code, StringComparison.OrdinalIgnoreCase)))
                {
                    _context.ProductEnumbers.Add(new ProductEnumber
                    {
                        ProductId = product.ProductId,
                        Code = dict.Code,
                        CreatedAt = DateTime.UtcNow
                    });
                }
            }
            else
            {
                detectedAdditives.Add(new DetectedAdditiveDto
                {
                    Code = code,
                    Name = $"Phụ gia {code}",
                    VeganStatus = "DOUBTFUL"
                });
                hasDoubtfulAdditive = true;
            }
        }

        // 6. Kết luận thuần chay (GREEN / YELLOW / RED) - BR-USR-09, BR-ADM-MAS-002, BR-ADM-AI-005
        if (!product.AdminVerified)
        {
            bool hasAnimalIngredients = false;
            if (!string.IsNullOrWhiteSpace(product.IngredientsText))
            {
                var lowerIng = product.IngredientsText.ToLowerInvariant();
                if (NonVeganKeywords.Any(k => lowerIng.Contains(k)))
                {
                    hasAnimalIngredients = true;
                }
            }

            if (hasNonVeganAdditive || hasAnimalIngredients || string.Equals(product.VeganVerdict, "NON_VEGAN", StringComparison.OrdinalIgnoreCase))
            {
                product.VeganVerdict = "NON_VEGAN";
            }
            else if (hasDoubtfulAdditive || string.Equals(product.VeganVerdict, "DOUBTFUL", StringComparison.OrdinalIgnoreCase))
            {
                product.VeganVerdict = "DOUBTFUL";
            }
            else
            {
                product.VeganVerdict = "VEGAN";
            }
        }

        await _context.SaveChangesAsync(ct);

        // Chuyển đổi mã kết luận sang chuẩn API: GREEN, YELLOW, RED
        var apiVerdict = MapToApiVerdict(product.VeganVerdict);
        var verdictText = apiVerdict switch
        {
            "RED" => "Chứa phụ gia hoặc thành phần có nguồn gốc động vật.",
            "YELLOW" => "Có chứa thành phần nghi vấn hoặc phụ gia cần lưu ý kiểm tra.",
            _ => "Sản phẩm thuần chay, không phát hiện thành phần động vật."
        };

        // 7. Kiểm tra dị ứng cá nhân cho Thành viên đã đăng nhập (FN-BAR-01 Logic 7)
        bool personalAllergyCheck = false;
        var allergenWarnings = new List<string>();

        if (userId.HasValue)
        {
            var userAllergies = await _context.UserAllergies
                .Include(ua => ua.Allergen)
                .Where(ua => ua.UserId == userId.Value)
                .ToListAsync(ct);

            if (userAllergies.Count > 0 && !string.IsNullOrWhiteSpace(product.IngredientsText))
            {
                var lowerIng = product.IngredientsText.ToLowerInvariant();
                foreach (var ua in userAllergies)
                {
                    var aName = ua.Allergen?.AllergenName;
                    if (!string.IsNullOrWhiteSpace(aName) && lowerIng.Contains(aName.ToLowerInvariant()))
                    {
                        personalAllergyCheck = true;
                        allergenWarnings.Add($"Chứa dị ứng cá nhân: {aName}");
                    }
                }
            }

            // Ghi nhận lịch sử quét mã vạch cho Thành viên
            try
            {
                var history = new UserScanHistory
                {
                    UserId = userId.Value,
                    ProductId = product.ProductId,
                    ScanMethod = scanMethod,
                    IsNonVeganTriggered = apiVerdict != "GREEN" || personalAllergyCheck,
                    ScannedAt = DateTime.UtcNow
                };
                _context.UserScanHistories.Add(history);
                await _context.SaveChangesAsync(ct);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Lỗi khi ghi lịch sử UserScanHistory cho UserId {UserId}.", userId.Value);
            }
        }

        // 8. Khấu trừ hạn mức Khách vãng lai sau khi thành công (FN-BAR-01 Logic 9)
        ScanQuotaDto quota;
        if (!userId.HasValue && guestQuotaKey != null)
        {
            var newUsed = await _redis.IncrementAsync(guestQuotaKey, 1, TimeSpan.FromHours(24), ct);
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
