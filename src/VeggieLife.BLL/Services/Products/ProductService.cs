using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using VeggieLife.BLL.Common.Responses;
using VeggieLife.BLL.DTOs.Products.Responses;
using VeggieLife.DAL.Context;
using VeggieLife.DAL.Entities;
using VeggieLife.DAL.External.OpenFoodFacts;

namespace VeggieLife.BLL.Services.Products
{
    public class ProductService : IProductService
    {
        private readonly AppDbContext _context;
        private readonly IOpenFoodFactsClient _openFoodFactsClient;

        public ProductService(AppDbContext context, IOpenFoodFactsClient openFoodFactsClient)
        {
            _context = context;
            _openFoodFactsClient = openFoodFactsClient;
        }

        public async Task<BarcodeScanResponse> ScanBarcodeAsync(string barcode, int? userId)
        {
            if (string.IsNullOrWhiteSpace(barcode))
            {
                throw new ArgumentException("Mã vạch không được để trống.", nameof(barcode));
            }

            var cleanBarcode = barcode.Trim();

            // 1. Tìm sản phẩm trong CSDL cục bộ
            var product = await _context.Products
                .Include(p => p.ProductEnumbers)
                    .ThenInclude(pe => pe.CodeNavigation)
                .Include(p => p.ProductAllergens)
                .FirstOrDefaultAsync(p => p.Barcode == cleanBarcode);

            var detectedAdditives = new List<DetectedAdditiveDto>();
            var allergenWarnings = new List<string>();
            bool personalAllergyCheck = false;

            // 2. Nếu chưa có trong CSDL, tra cứu qua Open Food Facts API
            if (product == null)
            {
                var offProduct = await _openFoodFactsClient.GetProductByBarcodeAsync(cleanBarcode);

                if (offProduct != null)
                {
                    product = new Product
                    {
                        Barcode = cleanBarcode,
                        ProductName = string.IsNullOrWhiteSpace(offProduct.ProductName) ? $"Sản phẩm #{cleanBarcode}" : offProduct.ProductName,
                        Brand = offProduct.Brand,
                        IngredientsText = offProduct.IngredientsText,
                        VeganVerdict = offProduct.VeganVerdict,
                        AdminVerified = false,
                        CreatedAt = DateTime.UtcNow,
                        LastSyncedAt = DateTime.UtcNow
                    };

                    _context.Products.Add(product);
                    await _context.SaveChangesAsync();

                    // Lưu các phụ gia E-number nếu có sẵn trong EnumberDictionary
                    if (offProduct.Additives.Count > 0)
                    {
                        var knownCodes = await _context.EnumberDictionaries
                            .Where(e => offProduct.Additives.Contains(e.Code))
                            .ToDictionaryAsync(e => e.Code, e => e);

                        foreach (var code in offProduct.Additives)
                        {
                            if (knownCodes.TryGetValue(code, out var dict))
                            {
                                detectedAdditives.Add(new DetectedAdditiveDto
                                {
                                    Code = dict.Code,
                                    Name = dict.Name,
                                    VeganStatus = dict.VeganStatus
                                });

                                _context.ProductEnumbers.Add(new ProductEnumber
                                {
                                    ProductId = product.ProductId,
                                    Code = dict.Code,
                                    CreatedAt = DateTime.UtcNow
                                });
                            }
                            else
                            {
                                detectedAdditives.Add(new DetectedAdditiveDto
                                {
                                    Code = code,
                                    Name = $"Phụ gia {code}",
                                    VeganStatus = "DOUBTFUL"
                                });
                            }
                        }

                        await _context.SaveChangesAsync();
                    }

                    allergenWarnings.AddRange(offProduct.Allergens);
                }
            }
            else
            {
                // Sản phẩm đã có trong DB, lấy danh sách phụ gia
                foreach (var pe in product.ProductEnumbers)
                {
                    detectedAdditives.Add(new DetectedAdditiveDto
                    {
                        Code = pe.Code,
                        Name = pe.CodeNavigation?.Name ?? $"Phụ gia {pe.Code}",
                        VeganStatus = pe.CodeNavigation?.VeganStatus ?? "UNKNOWN"
                    });
                }
            }

            // 3. Nếu không tìm thấy ở cả DB và Open Food Facts
            if (product == null)
            {
                return new BarcodeScanResponse
                {
                    Barcode = cleanBarcode,
                    ProductName = $"Mã vạch #{cleanBarcode}",
                    Brand = "Chưa rõ",
                    VeganVerdict = "DOUBTFUL",
                    VerdictText = "Chưa có thông tin sản phẩm trên hệ thống hoặc Open Food Facts.",
                    DetectedAdditives = new List<DetectedAdditiveDto>(),
                    PersonalAllergyCheck = false,
                    AllergenWarnings = new List<string>(),
                    Quota = new ScanQuotaDto
                    {
                        Used = 1,
                        Limit = userId.HasValue ? 100 : 3,
                        Remaining = userId.HasValue ? 99 : 2
                    }
                };
            }

            // 4. Kiểm tra dị ứng của người dùng nếu đã đăng nhập
            if (userId.HasValue)
            {
                var userAllergies = await _context.UserAllergies
                    .Include(ua => ua.Allergen)
                    .Where(ua => ua.UserId == userId.Value)
                    .Select(ua => ua.Allergen.AllergenName.ToLower())
                    .ToListAsync();

                if (userAllergies.Count > 0 && !string.IsNullOrWhiteSpace(product.IngredientsText))
                {
                    var ing = product.IngredientsText.ToLowerInvariant();
                    foreach (var a in userAllergies)
                    {
                        if (ing.Contains(a))
                        {
                            personalAllergyCheck = true;
                            allergenWarnings.Add($"Chứa dị ứng cá nhân: {a}");
                        }
                    }
                }

                // Lưu lịch sử quét
                try
                {
                    var history = new UserScanHistory
                    {
                        UserId = userId.Value,
                        ProductId = product.ProductId,
                        ScanMethod = "CAMERA_BARCODE",
                        IsNonVeganTriggered = product.VeganVerdict != "VEGAN",
                        ScannedAt = DateTime.UtcNow
                    };
                    _context.UserScanHistories.Add(history);
                    await _context.SaveChangesAsync();
                }
                catch (Exception)
                {
                    // Lỗi ghi log lịch sử không chặn kết quả quét
                }
            }

            string verdictDesc = product.VeganVerdict switch
            {
                "VEGAN" => "Sản phẩm thuần chay an tâm sử dụng.",
                "NON_VEGAN" => "Sản phẩm có chứa thành phần nguồn gốc động vật.",
                "DOUBTFUL" => "Có thể chứa thành phần chưa xác định hoặc phụ gia cần lưu ý.",
                _ => "Chưa đủ dữ liệu để kết luận."
            };

            return new BarcodeScanResponse
            {
                Barcode = product.Barcode,
                ProductName = product.ProductName,
                Brand = product.Brand ?? "Không rõ thương hiệu",
                VeganVerdict = product.VeganVerdict,
                VerdictText = verdictDesc,
                DetectedAdditives = detectedAdditives,
                PersonalAllergyCheck = personalAllergyCheck,
                AllergenWarnings = allergenWarnings.Distinct().ToList(),
                Quota = new ScanQuotaDto
                {
                    Used = 1,
                    Limit = userId.HasValue ? 100 : 3,
                    Remaining = userId.HasValue ? 99 : 2
                }
            };
        }

        public async Task<PagedResult<UserScanHistoryResponse>> GetUserScanHistoryAsync(
            int userId,
            string? verdict,
            bool? allergenOnly,
            int page,
            int size)
        {
            if (page < 1) page = 1;
            if (size < 1) size = 20;

            var query = _context.UserScanHistories
                .Include(h => h.Product)
                .Where(h => h.UserId == userId);

            if (!string.IsNullOrWhiteSpace(verdict))
            {
                query = query.Where(h => h.Product.VeganVerdict == verdict.ToUpperInvariant());
            }

            if (allergenOnly == true)
            {
                query = query.Where(h => h.IsNonVeganTriggered);
            }

            var totalItems = await query.CountAsync();

            var items = await query
                .OrderByDescending(h => h.ScannedAt)
                .Skip((page - 1) * size)
                .Take(size)
                .Select(h => new UserScanHistoryResponse
                {
                    ScanId = (int)h.ScanId,
                    Barcode = h.Product.Barcode,
                    ProductName = h.Product.ProductName,
                    VeganVerdict = h.Product.VeganVerdict,
                    IsAllergenTriggered = h.IsNonVeganTriggered,
                    ScannedAt = h.ScannedAt
                })
                .ToListAsync();

            return new PagedResult<UserScanHistoryResponse>(items, totalItems, page, size);
        }
    }
}
