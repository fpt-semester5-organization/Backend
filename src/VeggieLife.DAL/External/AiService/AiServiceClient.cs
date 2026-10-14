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
