using System;
using System.Diagnostics;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace VeggieLife.DAL.External.AiService;

/// <summary>
/// Triển khai HTTP Client gọi sang microservice ai-service (FastAPI trên VPS).
/// Hiện thực Circuit Breaker và Timeout 4 giây theo BR-ADM-AI-002, D118–D119.
/// </summary>
public class AiServiceClient : IAiServiceClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<AiServiceClient> _logger;
    private readonly AiServiceSettings _settings;

    // Circuit Breaker State (Thread-safe)
    private static int _consecutiveFailures = 0;
    private static DateTime? _circuitOpenUntil = null;
    private static readonly object _lock = new();

    public AiServiceClient(
        HttpClient httpClient,
        ILogger<AiServiceClient> logger,
        IOptions<AiServiceSettings> settings)
    {
        _httpClient = httpClient;
        _logger = logger;
        _settings = settings.Value;

        if (_httpClient.BaseAddress == null && !string.IsNullOrWhiteSpace(_settings.BaseUrl))
        {
            _httpClient.BaseAddress = new Uri(_settings.BaseUrl);
        }

        if (!_httpClient.DefaultRequestHeaders.Contains("X-Internal-Api-Key") && !string.IsNullOrWhiteSpace(_settings.InternalApiKey))
        {
            _httpClient.DefaultRequestHeaders.Add("X-Internal-Api-Key", _settings.InternalApiKey);
        }

        _httpClient.Timeout = TimeSpan.FromSeconds(_settings.TimeoutSeconds > 0 ? _settings.TimeoutSeconds : 4);
    }

    public bool IsCircuitOpen
    {
        get
        {
            lock (_lock)
            {
                if (_circuitOpenUntil.HasValue)
                {
                    if (DateTime.UtcNow < _circuitOpenUntil.Value)
                    {
                        return true;
                    }

                    // Hết thời gian ngắt mạch (5 phút) -> Đóng mạch chuyển sang Half-Open
                    _circuitOpenUntil = null;
                    _consecutiveFailures = 0;
                }
                return false;
            }
        }
    }

    public async Task<AiMealPlanResponse?> GenerateMealPlanAsync(AiMealPlanRequest request, CancellationToken ct = default)
    {
        // 1. Kiểm tra Circuit Breaker (ngắt mạch 5 phút nếu lỗi liên tục)
        if (IsCircuitOpen)
        {
            _logger.LogWarning("[AiServiceClient] Mạch AI đang mở (Circuit Breaker OPEN). Kích hoạt Fallback Engine ngay lập tức.");
            return null;
        }

        var sw = Stopwatch.StartNew();

        try
        {
            // Thiết lập timeout 4 giây bằng CancellationTokenSource kết hợp (BR-ADM-AI-002)
            using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(4));
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct, timeoutCts.Token);

            var response = await _httpClient.PostAsJsonAsync("/meal/generate", request, linkedCts.Token);
            sw.Stop();

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<AiMealPlanResponse>(cancellationToken: linkedCts.Token);
                if (result != null)
                {
                    result.ExecutionTimeMs = (int)sw.ElapsedMilliseconds;
                    RecordSuccess();
                    return result;
                }
            }

            var statusCode = (int)response.StatusCode;
            _logger.LogWarning("[AiServiceClient] ai-service phản hồi lỗi HTTP {StatusCode}", statusCode);
            RecordFailure();
            return null;
        }
        catch (OperationCanceledException ex) when (!ct.IsCancellationRequested)
        {
            // Quá 4 giây timeout (BR-ADM-AI-002)
            sw.Stop();
            _logger.LogWarning(ex, "[AiServiceClient] Gọi ai-service bị TIMEOUT quá 4 giây ({ElapsedMs} ms).", sw.ElapsedMilliseconds);
            RecordFailure();
            return null;
        }
        catch (HttpRequestException ex)
        {
            // Mất kết nối hoặc dịch vụ AI chưa bật
            sw.Stop();
            _logger.LogWarning(ex, "[AiServiceClient] Không thể kết nối tới ai-service ({BaseUrl}).", _httpClient.BaseAddress);
            RecordFailure();
            return null;
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger.LogError(ex, "[AiServiceClient] Lỗi không xác định khi gọi ai-service.");
            RecordFailure();
            return null;
        }
    }

    public async Task<AiFridgeDetectionResult?> DetectFridgeIngredientsAsync(System.IO.Stream imageStream, string mimeType, CancellationToken ct = default)
    {
        if (IsCircuitOpen)
        {
            _logger.LogWarning("[AiServiceClient] Mạch AI đang mở (Circuit Breaker OPEN). Không thể gọi AI Vision.");
            return null;
        }

        // 1. ƯU TIÊN: Nếu người dùng đã cấu hình Gemini API Key thực tế (không phải placeholder), gọi thẳng Google Gemini Vision API
        if (IsGeminiApiKeyValid(_settings.GeminiApiKey))
        {
            _logger.LogInformation("[AiServiceClient] Phát hiện Gemini API Key hợp lệ. Gọi trực tiếp Google Gemini Vision API ({Model}).", _settings.GeminiModel);
            var directResult = await CallGoogleGeminiVisionDirectAsync(imageStream, mimeType, _settings.GeminiApiKey!.Trim(), _settings.GeminiModel, ct);
            if (directResult != null)
            {
                RecordSuccess();
                return directResult;
            }

            _logger.LogWarning("[AiServiceClient] Gọi trực tiếp Google Gemini thất bại hoặc bị lỗi. Thử kết nối sang microservice ai-service nội bộ.");
        }

        var sw = Stopwatch.StartNew();

        try
        {
            using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(4));
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct, timeoutCts.Token);

            using var content = new MultipartFormDataContent();
            using var streamContent = new StreamContent(imageStream);
            streamContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(string.IsNullOrWhiteSpace(mimeType) ? "image/jpeg" : mimeType);
            content.Add(streamContent, "image", "fridge.jpg");

            // Gọi endpoint /vision/detect-ingredients chuẩn theo Kiến trúc D118-D119 (05. Architecture and Deployment.docx)
            var response = await _httpClient.PostAsync("/vision/detect-ingredients", content, linkedCts.Token);
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                // Fallback nếu microservice ai-service cũ dùng route /fridge/detect
                using var fallbackContent = new MultipartFormDataContent();
                using var fallbackStreamContent = new StreamContent(imageStream);
                fallbackStreamContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(string.IsNullOrWhiteSpace(mimeType) ? "image/jpeg" : mimeType);
                fallbackContent.Add(fallbackStreamContent, "image", "fridge.jpg");
                response = await _httpClient.PostAsync("/fridge/detect", fallbackContent, linkedCts.Token);
            }
            sw.Stop();

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<AiFridgeDetectionResult>(cancellationToken: linkedCts.Token);
                if (result != null)
                {
                    result.ExecutionTimeMs = (int)sw.ElapsedMilliseconds;
                    RecordSuccess();
                    return result;
                }
            }

            var statusCode = (int)response.StatusCode;
            _logger.LogWarning("[AiServiceClient] ai-service /vision/detect-ingredients phản hồi lỗi HTTP {StatusCode}", statusCode);
            RecordFailure();
            return null;
        }
        catch (OperationCanceledException ex) when (!ct.IsCancellationRequested)
        {
            sw.Stop();
            _logger.LogWarning(ex, "[AiServiceClient] Gọi ai-service /fridge/detect bị TIMEOUT quá 4 giây ({ElapsedMs} ms).", sw.ElapsedMilliseconds);
            RecordFailure();
            return null;
        }
        catch (HttpRequestException ex)
        {
            sw.Stop();
            _logger.LogWarning(ex, "[AiServiceClient] Không thể kết nối tới ai-service /fridge/detect ({BaseUrl}).", _httpClient.BaseAddress);
            RecordFailure();
            return null;
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger.LogError(ex, "[AiServiceClient] Lỗi không xác định khi gọi ai-service /fridge/detect.");
            RecordFailure();
            return null;
        }
    }

    private static bool IsGeminiApiKeyValid(string? key)
    {
        if (string.IsNullOrWhiteSpace(key)) return false;
        var trimmed = key.Trim();
        return !trimmed.Equals("YOUR_GEMINI_API_KEY_HERE", StringComparison.OrdinalIgnoreCase)
               && !trimmed.Equals("YOUR_API_KEY", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Gọi trực tiếp Google Gemini 1.5 Flash Vision API qua REST endpoint chính thức:
    /// POST https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent?key={apiKey}
    /// Nhận diện nguyên liệu trong ảnh tủ lạnh và trích xuất JSON theo định dạng chuẩn (FN-VIS-01).
    /// </summary>
    private async Task<AiFridgeDetectionResult?> CallGoogleGeminiVisionDirectAsync(
        System.IO.Stream imageStream,
        string mimeType,
        string apiKey,
        string modelName,
        CancellationToken ct)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            using var ms = new System.IO.MemoryStream();
            if (imageStream.CanSeek)
            {
                imageStream.Position = 0;
            }
            await imageStream.CopyToAsync(ms, ct);
            var base64Data = Convert.ToBase64String(ms.ToArray());

            var prompt = "Bạn là trợ lý AI chuyên nhận diện nguyên liệu thực phẩm tủ lạnh cho ứng dụng ăn chay VeggieLife (FN-VIS-01). " +
                         "Hãy phân tích ảnh này và trích xuất danh sách tất cả các nguyên liệu thực vật/thực phẩm nhìn thấy trong ảnh. " +
                         "Chỉ trả về JSON thuần túy (không bọc trong markdown code fence) theo cấu trúc mảng: " +
                         "[{\"name\": \"Tên nguyên liệu tiếng Việt chuẩn\", \"freshnessStatus\": \"FRESH\" hoặc \"NON-FRESH\", \"foodGroup\": \"VEGETABLE\" hoặc \"MUSHROOM\" hoặc \"SOY\" hoặc \"FRUIT\" hoặc \"ALLIUM\" hoặc \"MEAT\"}]";

            var payload = new
            {
                contents = new[]
                {
                    new
                    {
                        parts = new object[]
                        {
                            new { text = prompt },
                            new
                            {
                                inline_data = new
                                {
                                    mime_type = string.IsNullOrWhiteSpace(mimeType) ? "image/jpeg" : mimeType,
                                    data = base64Data
                                }
                            }
                        }
                    }
                },
                generationConfig = new
                {
                    responseMimeType = "application/json",
                    temperature = 0.2
                }
            };

            using var directClient = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
            var targetModel = string.IsNullOrWhiteSpace(modelName) ? "gemini-1.5-flash" : modelName;
            var url = $"https://generativelanguage.googleapis.com/v1beta/models/{targetModel}:generateContent?key={apiKey}";

            var response = await directClient.PostAsJsonAsync(url, payload, ct);
            sw.Stop();

            if (!response.IsSuccessStatusCode)
            {
                var errorText = await response.Content.ReadAsStringAsync(ct);
                _logger.LogWarning("[AiServiceClient] Google Gemini API phản hồi lỗi HTTP {StatusCode}: {Error}", response.StatusCode, errorText);
                return null;
            }

            var jsonDoc = await response.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>(cancellationToken: ct);
            if (jsonDoc.TryGetProperty("candidates", out var candidates) &&
                candidates.GetArrayLength() > 0 &&
                candidates[0].TryGetProperty("content", out var contentElem) &&
                contentElem.TryGetProperty("parts", out var partsElem) &&
                partsElem.GetArrayLength() > 0 &&
                partsElem[0].TryGetProperty("text", out var textElem))
            {
                var rawJsonText = textElem.GetString()?.Trim();
                if (!string.IsNullOrWhiteSpace(rawJsonText))
                {
                    if (rawJsonText.StartsWith("```json", StringComparison.OrdinalIgnoreCase))
                    {
                        rawJsonText = rawJsonText.Substring(7);
                    }
                    else if (rawJsonText.StartsWith("```"))
                    {
                        rawJsonText = rawJsonText.Substring(3);
                    }
                    if (rawJsonText.EndsWith("```"))
                    {
                        rawJsonText = rawJsonText.Substring(0, rawJsonText.Length - 3);
                    }
                    rawJsonText = rawJsonText.Trim();

                    var items = System.Text.Json.JsonSerializer.Deserialize<List<AiDetectedItemDto>>(rawJsonText, new System.Text.Json.JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    }) ?? new List<AiDetectedItemDto>();

                    var tokens = 250;
                    if (jsonDoc.TryGetProperty("usageMetadata", out var usageElem) &&
                        usageElem.TryGetProperty("totalTokenCount", out var totalTokensElem))
                    {
                        tokens = totalTokensElem.GetInt32();
                    }

                    _logger.LogInformation("[AiServiceClient] Nhận diện thành công {Count} nguyên liệu qua Google Gemini Vision ({ElapsedMs} ms).", items.Count, sw.ElapsedMilliseconds);

                    return new AiFridgeDetectionResult
                    {
                        Success = true,
                        Items = items,
                        TokensConsumed = tokens,
                        ModelVersion = targetModel,
                        ExecutionTimeMs = (int)sw.ElapsedMilliseconds
                    };
                }
            }

            return null;
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger.LogError(ex, "[AiServiceClient] Lỗi khi gọi trực tiếp Google Gemini Vision API.");
            return null;
        }
    }

    private void RecordSuccess()
    {
        lock (_lock)
        {
            _consecutiveFailures = 0;
            _circuitOpenUntil = null;
        }
    }

    private void RecordFailure()
    {
        lock (_lock)
        {
            _consecutiveFailures++;
            if (_consecutiveFailures >= 3)
            {
                // Ngắt mạch trong 5 phút theo quy chuẩn D118 / BR-ADM-AI-002
                _circuitOpenUntil = DateTime.UtcNow.AddMinutes(5);
                _logger.LogError("[AiServiceClient] Phát hiện 3 lần lỗi liên tiếp. MỞ MẠCH Circuit Breaker trong 5 phút tới {Until:u}.", _circuitOpenUntil);
            }
        }
    }
}
