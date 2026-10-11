using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace VeggieLife.DAL.External.OpenFoodFacts
{
    public class OpenFoodFactsClient : IOpenFoodFactsClient
    {
        private readonly HttpClient _httpClient;

        // Các từ khóa chỉ nguồn gốc động vật phổ biến trong thành phần tiếng Việt & tiếng Anh
        private static readonly string[] NonVeganKeywords = new[]
        {
            "sữa", "whey", "casein", "trứng", "mật ong", "gelatin", "thịt", "cá",
            "tôm", "mực", "mỡ heo", "mỡ bò", "mỡ động vật", "nước mắm", "chiết xuất thịt",
            "carmine", "cochineal", "e120", "shellac", "e904", "rennet", "tallow", "lard"
        };

        public OpenFoodFactsClient(HttpClient httpClient)
        {
            _httpClient = httpClient;
            _httpClient.Timeout = TimeSpan.FromSeconds(8);
            if (!_httpClient.DefaultRequestHeaders.Contains("User-Agent"))
            {
                _httpClient.DefaultRequestHeaders.Add("User-Agent", "VeggieLife/1.0 (Web/Mobile; https://veggielife.vn; support@veggielife.vn)");
            }
        }

        public async Task<OpenFoodFactsProductDto?> GetProductByBarcodeAsync(string barcode, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(barcode))
            {
                return null;
            }

            try
            {
                var cleanBarcode = barcode.Trim();
                var url = $"https://world.openfoodfacts.org/api/v2/product/{cleanBarcode}.json";
                using var response = await _httpClient.GetAsync(url, cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    // Thử fallback v0 endpoint nếu v2 trả về lỗi
                    var v0Url = $"https://world.openfoodfacts.org/api/v0/product/{cleanBarcode}.json";
                    using var fallbackResponse = await _httpClient.GetAsync(v0Url, cancellationToken);
                    if (!fallbackResponse.IsSuccessStatusCode)
                    {
                        return null;
                    }

                    var fallbackJson = await fallbackResponse.Content.ReadAsStringAsync(cancellationToken);
                    return ParseOpenFoodFactsResponse(cleanBarcode, fallbackJson);
                }

                var json = await response.Content.ReadAsStringAsync(cancellationToken);
                return ParseOpenFoodFactsResponse(cleanBarcode, json);
            }
            catch (Exception)
            {
                // Khi không có kết nối internet hoặc API OpenFoodFacts timeout, trả về null
                return null;
            }
        }

        private static OpenFoodFactsProductDto? ParseOpenFoodFactsResponse(string barcode, string json)
        {
            try
            {
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                if (!root.TryGetProperty("status", out var statusProp) || statusProp.GetInt32() != 1)
                {
                    return null;
                }

                if (!root.TryGetProperty("product", out var product))
                {
                    return null;
                }

                // Tên sản phẩm (ưu tiên vi, en, hoặc tên mặc định)
                string productName = string.Empty;
                if (product.TryGetProperty("product_name_vi", out var pVi) && !string.IsNullOrWhiteSpace(pVi.GetString()))
                    productName = pVi.GetString()!;
                else if (product.TryGetProperty("product_name", out var pDef) && !string.IsNullOrWhiteSpace(pDef.GetString()))
                    productName = pDef.GetString()!;
                else if (product.TryGetProperty("product_name_en", out var pEn) && !string.IsNullOrWhiteSpace(pEn.GetString()))
                    productName = pEn.GetString()!;
                else
                    productName = $"Sản phẩm #{barcode}";

                // Thương hiệu
                string? brand = null;
                if (product.TryGetProperty("brands", out var bProp))
                {
                    brand = bProp.GetString();
                }

                // Thành phần nguyên liệu
                string? ingredientsText = null;
                if (product.TryGetProperty("ingredients_text_vi", out var ingVi) && !string.IsNullOrWhiteSpace(ingVi.GetString()))
                    ingredientsText = ingVi.GetString();
                else if (product.TryGetProperty("ingredients_text", out var ingDef) && !string.IsNullOrWhiteSpace(ingDef.GetString()))
                    ingredientsText = ingDef.GetString();
                else if (product.TryGetProperty("ingredients_text_en", out var ingEn) && !string.IsNullOrWhiteSpace(ingEn.GetString()))
                    ingredientsText = ingEn.GetString();

                // Ảnh sản phẩm
                string? imageUrl = null;
                if (product.TryGetProperty("image_url", out var imgProp))
                    imageUrl = imgProp.GetString();
                else if (product.TryGetProperty("image_front_url", out var imgFront))
                    imageUrl = imgFront.GetString();

                // Danh sách phụ gia E-number
                var additives = new List<string>();
                if (product.TryGetProperty("additives_tags", out var addTags) && addTags.ValueKind == JsonValueKind.Array)
                {
                    foreach (var tag in addTags.EnumerateArray())
                    {
                        var raw = tag.GetString();
                        if (!string.IsNullOrWhiteSpace(raw))
                        {
                            var clean = raw.Replace("en:", "", StringComparison.OrdinalIgnoreCase).ToUpperInvariant();
                            additives.Add(clean);
                        }
                    }
                }

                // Dị ứng allergens
                var allergens = new List<string>();
                if (product.TryGetProperty("allergens_tags", out var allTags) && allTags.ValueKind == JsonValueKind.Array)
                {
                    foreach (var tag in allTags.EnumerateArray())
                    {
                        var raw = tag.GetString();
                        if (!string.IsNullOrWhiteSpace(raw))
                        {
                            var clean = raw.Replace("en:", "", StringComparison.OrdinalIgnoreCase);
                            allergens.Add(clean);
                        }
                    }
                }

                // Phân tích Vegan
                string veganVerdict = "DOUBTFUL";
                string verdictText = "Chưa đủ dữ liệu để xác nhận thuần chay.";

                var analysisTags = new List<string>();
                if (product.TryGetProperty("ingredients_analysis_tags", out var anaTags) && anaTags.ValueKind == JsonValueKind.Array)
                {
                    foreach (var tag in anaTags.EnumerateArray())
                    {
                        var str = tag.GetString()?.ToLowerInvariant();
                        if (!string.IsNullOrEmpty(str))
                        {
                            analysisTags.Add(str);
                        }
                    }
                }

                if (analysisTags.Contains("en:vegan"))
                {
                    veganVerdict = "VEGAN";
                    verdictText = "Sản phẩm thuần chay (Được xác nhận bởi Open Food Facts).";
                }
                else if (analysisTags.Contains("en:non-vegan"))
                {
                    veganVerdict = "NON_VEGAN";
                    verdictText = "Sản phẩm có chứa thành phần động vật.";
                }
                else if (analysisTags.Contains("en:maybe-vegan") || analysisTags.Contains("en:vegan-status-unknown"))
                {
                    veganVerdict = "DOUBTFUL";
                    verdictText = "Có thể chứa thành phần không thuần chay hoặc chất phụ gia cần kiểm tra thêm.";
                }

                // Kiểm tra chéo với danh sách từ khóa phi chay trong ingredients_text nếu có
                if (!string.IsNullOrWhiteSpace(ingredientsText))
                {
                    var lowerIng = ingredientsText.ToLowerInvariant();
                    foreach (var kw in NonVeganKeywords)
                    {
                        if (lowerIng.Contains(kw))
                        {
                            veganVerdict = "NON_VEGAN";
                            verdictText = $"Phát hiện thành phần không thuần chay: '{kw}'.";
                            break;
                        }
                    }
                }

                if (veganVerdict == "VEGAN" && string.IsNullOrEmpty(verdictText))
                {
                    verdictText = "Sản phẩm thuần chay.";
                }

                return new OpenFoodFactsProductDto
                {
                    Barcode = barcode,
                    ProductName = productName,
                    Brand = brand,
                    IngredientsText = ingredientsText,
                    VeganVerdict = veganVerdict,
                    VerdictText = verdictText,
                    Additives = additives.Distinct().ToList(),
                    Allergens = allergens.Distinct().ToList(),
                    ImageUrl = imageUrl
                };
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}
