using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace VeggieLife.DAL.External.OpenFoodFacts;

/// <summary>
/// Client kết nối ngoại vi Open Food Facts API (FN-BAR-01).
/// Tuân thủ quy định: không hardcode URL/timeout, cấu hình thông qua OpenFoodFactsSettings.
/// </summary>
public class OpenFoodFactsClient : IOpenFoodFactsClient
{
    private readonly HttpClient _httpClient;
    private readonly OpenFoodFactsSettings _settings;
    private readonly ILogger<OpenFoodFactsClient> _logger;

    // Các từ khóa chỉ nguồn gốc động vật phổ biến trong thành phần tiếng Việt & tiếng Anh
    private static readonly Regex AnimalKeywordRegex = new(
        @"(?i)(?:^|[^\p{L}\p{N}])(sữa|whey|casein|trứng|mật\s+ong|gelatin|thịt|cá|tôm|mực|mỡ\s+heo|mỡ\s+bò|mỡ\s+động\s+vật|nước\s+mắm|chiết\s+xuất\s+thịt|carmine|cochineal|e120|shellac|e904|rennet|tallow|lard)(?:$|[^\p{L}\p{N}])",
        RegexOptions.Compiled);

    public OpenFoodFactsClient(
        HttpClient httpClient,
        IOptions<OpenFoodFactsSettings> options,
        ILogger<OpenFoodFactsClient> logger)
    {
        _httpClient = httpClient;
        _settings = options.Value ?? new OpenFoodFactsSettings();
        _logger = logger;

        _httpClient.Timeout = TimeSpan.FromSeconds(Math.Max(1, _settings.TimeoutSeconds));
        if (!_httpClient.DefaultRequestHeaders.Contains("User-Agent"))
        {
            _httpClient.DefaultRequestHeaders.Add("User-Agent", _settings.UserAgent);
        }
    }

    public async Task<OpenFoodFactsProductDto?> GetProductByBarcodeAsync(string barcode, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(barcode))
        {
            return null;
        }

        var cleanBarcode = barcode.Trim();
        var baseUrl = _settings.BaseUrl.TrimEnd('/');

        try
        {
            var url = $"{baseUrl}/api/v2/product/{cleanBarcode}.json";
            using var response = await _httpClient.GetAsync(url, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync(cancellationToken);
                var parsed = ParseOpenFoodFactsResponse(cleanBarcode, json);
                if (parsed != null)
                {
                    return parsed;
                }
            }

            // Thử endpoint v0 làm giải pháp dự phòng (fallback) nếu v2 không có dữ liệu
            var v0Url = $"{baseUrl}/api/v0/product/{cleanBarcode}.json";
            using var fallbackResponse = await _httpClient.GetAsync(v0Url, cancellationToken);
            if (fallbackResponse.IsSuccessStatusCode)
            {
                var fallbackJson = await fallbackResponse.Content.ReadAsStringAsync(cancellationToken);
                var parsedFallback = ParseOpenFoodFactsResponse(cleanBarcode, fallbackJson);
                if (parsedFallback != null)
                {
                    return parsedFallback;
                }
            }

            _logger.LogWarning("Open Food Facts không tìm thấy sản phẩm barcode {Barcode}.", cleanBarcode);
            return null;
        }
        catch (OperationCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogError(ex, "Timeout khi kết nối tới Open Food Facts API cho mã vạch {Barcode} (giới hạn {Timeout}s).", cleanBarcode, _settings.TimeoutSeconds);
            throw new OpenFoodFactsException($"Timeout kết nối tới Open Food Facts API (giới hạn {_settings.TimeoutSeconds}s).", ex);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Lỗi mạng khi kết nối tới Open Food Facts API cho mã vạch {Barcode}.", cleanBarcode);
            throw new OpenFoodFactsException("Không thể kết nối tới dịch vụ tra cứu mã vạch Open Food Facts do lỗi mạng.", ex);
        }
    }

    public static OpenFoodFactsProductDto? ParseOpenFoodFactsResponse(string barcode, string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            int status = 0;
            if (root.TryGetProperty("status", out var statusProp))
            {
                if (statusProp.ValueKind == JsonValueKind.Number && statusProp.TryGetInt32(out var sInt))
                {
                    status = sInt;
                }
                else if (statusProp.ValueKind == JsonValueKind.String && int.TryParse(statusProp.GetString(), out var sParsed))
                {
                    status = sParsed;
                }
            }

            if (status != 1)
            {
                return null;
            }

            if (!root.TryGetProperty("product", out var product) || product.ValueKind != JsonValueKind.Object)
            {
                return null;
            }

            // Tên sản phẩm (ưu tiên vi, en, hoặc tên mặc định)
            string productName = string.Empty;
            if (product.TryGetProperty("product_name_vi", out var pVi) && !string.IsNullOrWhiteSpace(pVi.GetString()))
                productName = pVi.GetString()!.Trim();
            else if (product.TryGetProperty("product_name", out var pDef) && !string.IsNullOrWhiteSpace(pDef.GetString()))
                productName = pDef.GetString()!.Trim();
            else if (product.TryGetProperty("product_name_en", out var pEn) && !string.IsNullOrWhiteSpace(pEn.GetString()))
                productName = pEn.GetString()!.Trim();
            else
                productName = $"Sản phẩm #{barcode}";

            // Thương hiệu
            string? brand = null;
            if (product.TryGetProperty("brands", out var bProp) && bProp.ValueKind == JsonValueKind.String)
            {
                brand = bProp.GetString()?.Trim();
            }

            // Thành phần nguyên liệu (ưu tiên vi -> mặc định -> en; nếu bản đó bị cụt thì lấy bản ngôn ngữ khác đầy đủ hơn)
            string? ingredientsText = SelectIngredientsText(product);

            // Ảnh sản phẩm (FN-BAR-01: trích xuất toàn diện từ image_front_url, image_url, selected_images, images)
            string? imageUrl = ExtractProductImageUrl(product, barcode);

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

            // Dấu vết dị ứng (traces_tags)
            var tracesTags = new List<string>();
            if (product.TryGetProperty("traces_tags", out var trTags) && trTags.ValueKind == JsonValueKind.Array)
            {
                foreach (var tag in trTags.EnumerateArray())
                {
                    var raw = tag.GetString();
                    if (!string.IsNullOrWhiteSpace(raw))
                    {
                        tracesTags.Add(raw.Trim());
                    }
                }
            }

            // Nhãn sản phẩm (labels_tags)
            var labelsTags = new List<string>();
            if (product.TryGetProperty("labels_tags", out var lbTags) && lbTags.ValueKind == JsonValueKind.Array)
            {
                foreach (var tag in lbTags.EnumerateArray())
                {
                    var raw = tag.GetString();
                    if (!string.IsNullOrWhiteSpace(raw))
                    {
                        labelsTags.Add(raw.Trim());
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
                var match = AnimalKeywordRegex.Match(ingredientsText);
                if (match.Success)
                {
                    var kw = match.Groups[1].Value;
                    veganVerdict = "NON_VEGAN";
                    verdictText = $"Phát hiện thành phần không thuần chay: '{kw}'.";
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
                AdditivesTags = additives.Distinct().ToList(),
                AllergensTags = allergens.Distinct().ToList(),
                TracesTags = tracesTags.Distinct().ToList(),
                LabelsTags = labelsTags.Distinct().ToList(),
                IngredientsAnalysisTags = analysisTags.Distinct().ToList(),
                ImageUrl = imageUrl
            };
        }
        catch (Exception)
        {
            return null;
        }
    }

    /// <summary>
    /// Trích xuất URL ảnh sản phẩm từ phản hồi JSON của Open Food Facts theo thứ tự ưu tiên:
    /// 1. Các trường trực tiếp: image_front_url, image_url, image_front_small_url, image_small_url, image_thumb_url
    /// 2. Object selected_images: front/general -> display/small/thumb (ưu tiên vi, en hoặc bất kỳ ngôn ngữ nào)
    /// 3. Object images: tìm khóa front hoặc số và sinh URL đầy đủ theo cấu trúc Open Food Facts CDN.
    /// </summary>
    // Ngôn ngữ dùng chữ Latinh: bộ lọc từ khóa động vật ở BLL đọc được các ngôn ngữ này
    private static readonly HashSet<string> LatinScriptLanguages = new(StringComparer.OrdinalIgnoreCase)
    {
        "en", "fr", "es", "it", "de", "nl", "pt", "ro", "pl", "id", "ms", "tl", "sv", "da", "no", "fi", "cs", "hu", "tr"
    };

    private static readonly Regex LanguageIngredientsKey = new(@"^ingredients_text_([a-z]{2})$", RegexOptions.Compiled);

    /// <summary>
    /// Chọn chuỗi thành phần đầy đủ nhất. OFF đôi khi chỉ có bản tiếng Anh bị cụt (ví dụ Nutella: "sugar, palm oil")
    /// trong khi bản tiếng Pháp/Ý... đầy đủ; khi bản ưu tiên ngắn hơn một nửa bản dài nhất thì dùng bản dài nhất,
    /// ưu tiên ngôn ngữ chữ Latinh.
    /// </summary>
    private static string? SelectIngredientsText(JsonElement product)
    {
        static string? Read(JsonElement p, string key) =>
            p.TryGetProperty(key, out var v) && v.ValueKind == JsonValueKind.String && !string.IsNullOrWhiteSpace(v.GetString())
                ? v.GetString()!.Trim()
                : null;

        var preferred = Read(product, "ingredients_text_vi") ?? Read(product, "ingredients_text") ?? Read(product, "ingredients_text_en");

        string? longestLatin = null, longestAny = null;
        foreach (var prop in product.EnumerateObject())
        {
            var m = LanguageIngredientsKey.Match(prop.Name);
            if (!m.Success || prop.Value.ValueKind != JsonValueKind.String) continue;
            var text = prop.Value.GetString()?.Trim();
            if (string.IsNullOrWhiteSpace(text)) continue;
            if (longestAny == null || text.Length > longestAny.Length) longestAny = text;
            if (LatinScriptLanguages.Contains(m.Groups[1].Value) && (longestLatin == null || text.Length > longestLatin.Length))
                longestLatin = text;
        }

        var fallback = longestLatin ?? longestAny;
        if (preferred == null) return fallback;
        if (fallback != null && preferred.Length * 2 < fallback.Length) return fallback;
        return preferred;
    }

    private static string? ExtractProductImageUrl(JsonElement product, string barcode)
    {
        // 1. Kiểm tra các thuộc tính ảnh trực tiếp
        string[] directKeys = new[]
        {
            "image_front_url",
            "image_url",
            "image_front_small_url",
            "image_small_url",
            "image_thumb_url"
        };

        foreach (var key in directKeys)
        {
            if (product.TryGetProperty(key, out var prop) && prop.ValueKind == JsonValueKind.String)
            {
                var val = prop.GetString();
                if (!string.IsNullOrWhiteSpace(val))
                {
                    return NormalizeImageUrl(val);
                }
            }
        }

        // 2. Kiểm tra selected_images: front / general -> display / small / thumb
        if (product.TryGetProperty("selected_images", out var selImages) && selImages.ValueKind == JsonValueKind.Object)
        {
            string[] targetTypes = new[] { "front", "general" };
            string[] sizeTypes = new[] { "display", "small", "thumb" };

            foreach (var tType in targetTypes)
            {
                if (selImages.TryGetProperty(tType, out var typeObj) && typeObj.ValueKind == JsonValueKind.Object)
                {
                    foreach (var sType in sizeTypes)
                    {
                        if (typeObj.TryGetProperty(sType, out var sizeObj) && sizeObj.ValueKind == JsonValueKind.Object)
                        {
                            // Ưu tiên tiếng Việt (vi), rồi tiếng Anh (en)
                            if (sizeObj.TryGetProperty("vi", out var viProp) && viProp.ValueKind == JsonValueKind.String)
                            {
                                var val = viProp.GetString();
                                if (!string.IsNullOrWhiteSpace(val)) return NormalizeImageUrl(val);
                            }
                            if (sizeObj.TryGetProperty("en", out var enProp) && enProp.ValueKind == JsonValueKind.String)
                            {
                                var val = enProp.GetString();
                                if (!string.IsNullOrWhiteSpace(val)) return NormalizeImageUrl(val);
                            }
                            // Bất kỳ ngôn ngữ nào khác có URL hợp lệ
                            foreach (var item in sizeObj.EnumerateObject())
                            {
                                if (item.Value.ValueKind == JsonValueKind.String)
                                {
                                    var val = item.Value.GetString();
                                    if (!string.IsNullOrWhiteSpace(val)) return NormalizeImageUrl(val);
                                }
                            }
                        }
                    }
                }
            }
        }

        // 3. Fallback: Nếu có danh sách images trong Open Food Facts
        if (product.TryGetProperty("images", out var imagesObj) && imagesObj.ValueKind == JsonValueKind.Object)
        {
            string? frontKey = null;
            string? numericKey = null;

            foreach (var img in imagesObj.EnumerateObject())
            {
                if (img.Name.StartsWith("front", StringComparison.OrdinalIgnoreCase))
                {
                    frontKey = img.Name;
                    break;
                }
                if (int.TryParse(img.Name, out _))
                {
                    numericKey ??= img.Name;
                }
            }

            var selectedKey = frontKey ?? numericKey;
            if (!string.IsNullOrEmpty(selectedKey))
            {
                return BuildOpenFoodFactsImageUrl(barcode, selectedKey);
            }
        }

        return null;
    }

    private static string NormalizeImageUrl(string url)
    {
        var trimmed = url.Trim();
        if (trimmed.StartsWith("http://", StringComparison.OrdinalIgnoreCase))
        {
            return "https://" + trimmed.Substring(7);
        }
        return trimmed;
    }

    private static string BuildOpenFoodFactsImageUrl(string barcode, string imageKey)
    {
        var cleanBarcode = barcode.Trim();
        string path;
        if (cleanBarcode.Length <= 8)
        {
            path = cleanBarcode;
        }
        else
        {
            var m = Regex.Match(cleanBarcode, @"^(\d{3})(\d{3})(\d{3})(.*)$");
            if (m.Success)
            {
                path = $"{m.Groups[1].Value}/{m.Groups[2].Value}/{m.Groups[3].Value}/{m.Groups[4].Value}";
            }
            else
            {
                path = cleanBarcode;
            }
        }

        var suffix = imageKey.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) || imageKey.EndsWith(".png", StringComparison.OrdinalIgnoreCase)
            ? imageKey
            : $"{imageKey}.400.jpg";

        return $"https://images.openfoodfacts.org/images/products/{path}/{suffix}";
    }
}
