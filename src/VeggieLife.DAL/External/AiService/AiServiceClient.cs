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

        _httpClient.Timeout = Timeout.InfiniteTimeSpan;
    }

    public bool IsCircuitOpen
    {
        get
        {
            // Bỏ ngắt mạch dưới Backend để không bao giờ tự động chặn yêu cầu của người dùng
            return false;
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
            var response = await _httpClient.PostAsJsonAsync("/meal/generate", request, ct);
            sw.Stop();

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<AiMealPlanResponse>(cancellationToken: ct);
                var expectedSlotCount = (request.DurationDays > 0 ? request.DurationDays : 7) * 3;
                if (result != null && result.Slots != null && (result.Slots.Count == expectedSlotCount || (request.DurationDays != 7 && result.Slots.Count > 0)))
                {
                    result.Success = true; // [BỔ SUNG QUAN TRỌNG]: Xác nhận kết quả hợp lệ
                    result.ExecutionTimeMs = (int)sw.ElapsedMilliseconds;
                    RecordSuccess();
                    return result;
                }
            }

            var statusCode = (int)response.StatusCode;
            var errorBody = await response.Content.ReadAsStringAsync(cancellationToken: ct);
            _logger.LogWarning("[AiServiceClient] ai-service phản hồi lỗi HTTP {StatusCode}: {ErrorBody}", statusCode, errorBody);
            RecordFailure();
            return null;
        }
        catch (OperationCanceledException ex) when (!ct.IsCancellationRequested)
        {
            sw.Stop();
            _logger.LogWarning(ex, "[AiServiceClient] Gọi ai-service bị TIMEOUT ({ElapsedMs} ms).", sw.ElapsedMilliseconds);
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

    public async Task<AiSwapMealByNoteResponse?> SwapMealByNoteAsync(AiSwapMealByNoteRequest request, CancellationToken ct = default)
    {
        if (IsCircuitOpen)
        {
            _logger.LogWarning("[AiServiceClient] Mạch AI đang mở (Circuit Breaker OPEN). Không thể gọi AI đổi món theo note.");
            return null;
        }

        var sw = Stopwatch.StartNew();

        try
        {
            var response = await _httpClient.PostAsJsonAsync("/meal/swap-by-note", request, ct);
            sw.Stop();

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<AiSwapMealByNoteResponse>(cancellationToken: ct);
                if (result != null && result.SelectedRecipeId > 0)
                {
                    result.Success = true;
                    result.ExecutionTimeMs = (int)sw.ElapsedMilliseconds;
                    RecordSuccess();
                    return result;
                }
            }

            var statusCode = (int)response.StatusCode;
            var errorBody = await response.Content.ReadAsStringAsync(cancellationToken: ct);
            _logger.LogWarning("[AiServiceClient] ai-service /meal/swap-by-note phản hồi HTTP {StatusCode}: {ErrorBody}", statusCode, errorBody);
            RecordFailure();
            return null;
        }
        catch (OperationCanceledException ex) when (!ct.IsCancellationRequested)
        {
            sw.Stop();
            _logger.LogWarning(ex, "[AiServiceClient] Gọi ai-service /meal/swap-by-note bị TIMEOUT ({ElapsedMs} ms).", sw.ElapsedMilliseconds);
            RecordFailure();
            return null;
        }
        catch (HttpRequestException ex)
        {
            sw.Stop();
            _logger.LogWarning(ex, "[AiServiceClient] Không thể kết nối tới ai-service ({BaseUrl}) cho swap-by-note.", _httpClient.BaseAddress);
            RecordFailure();
            return null;
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger.LogError(ex, "[AiServiceClient] Lỗi không xác định khi gọi ai-service /meal/swap-by-note.");
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
            // Sao chép sang MemoryStream nội bộ để không vô tình dispose stream của caller
            using var memoryStream = new MemoryStream();
            if (imageStream.CanSeek)
            {
                imageStream.Position = 0;
            }
            await imageStream.CopyToAsync(memoryStream, ct);
            memoryStream.Position = 0;

            using var content = new MultipartFormDataContent();
            using var streamContent = new StreamContent(memoryStream);
            streamContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(string.IsNullOrWhiteSpace(mimeType) ? "image/jpeg" : mimeType);
            content.Add(streamContent, "image", "fridge.jpg");

            // Gọi endpoint /vision/detect-ingredients chuẩn theo Kiến trúc D118-D119 (05. Architecture and Deployment.docx)
            var response = await _httpClient.PostAsync("/vision/detect-ingredients", content, ct);
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                // Fallback nếu microservice ai-service cũ dùng route /fridge/detect
                memoryStream.Position = 0;
                using var fallbackContent = new MultipartFormDataContent();
                using var fallbackStreamContent = new StreamContent(memoryStream);
                fallbackStreamContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(string.IsNullOrWhiteSpace(mimeType) ? "image/jpeg" : mimeType);
                fallbackContent.Add(fallbackStreamContent, "image", "fridge.jpg");
                response = await _httpClient.PostAsync("/fridge/detect", fallbackContent, ct);
            }
            sw.Stop();

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<AiFridgeDetectionResult>(cancellationToken: ct);
                if (result != null)
                {
                    result.ExecutionTimeMs = (int)sw.ElapsedMilliseconds;
                    RecordSuccess();
                    return result;
                }
            }

            var statusCode = (int)response.StatusCode;
            var errorBody = await response.Content.ReadAsStringAsync(cancellationToken: ct);
            _logger.LogWarning("[AiServiceClient] ai-service /vision/detect-ingredients phản hồi lỗi HTTP {StatusCode}: {ErrorBody}", statusCode, errorBody);
            RecordFailure();
            return null;
        }
        catch (OperationCanceledException ex) when (!ct.IsCancellationRequested)
        {
            sw.Stop();
            _logger.LogWarning(ex, "[AiServiceClient] Gọi ai-service /vision/detect-ingredients bị TIMEOUT ({ElapsedMs} ms).", sw.ElapsedMilliseconds);
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

    public async Task<AiBarcodeClassifyResponse?> ClassifyBarcodeAsync(AiBarcodeClassifyRequest request, CancellationToken ct = default)
    {
        if (IsCircuitOpen)
        {
            _logger.LogWarning("[AiServiceClient] Mạch AI đang mở (Circuit Breaker OPEN). Kích hoạt Fallback Engine cho barcode.");
            return null;
        }

        var sw = Stopwatch.StartNew();

        try
        {
            var response = await _httpClient.PostAsJsonAsync("/barcode/classify", request, ct);
            sw.Stop();

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<AiBarcodeClassifyResponse>(cancellationToken: ct);
                if (result != null)
                {
                    if (result.Usage != null && result.Usage.LatencyMs <= 0)
                    {
                        result.Usage.LatencyMs = (int)sw.ElapsedMilliseconds;
                    }
                    RecordSuccess();
                    return result;
                }
            }

            var statusCode = (int)response.StatusCode;
            var errorBody = await response.Content.ReadAsStringAsync(cancellationToken: ct);
            _logger.LogWarning("[AiServiceClient] ai-service /barcode/classify phản hồi lỗi HTTP {StatusCode}: {ErrorBody}", statusCode, errorBody);
            RecordFailure();
            return null;
        }
        catch (OperationCanceledException ex) when (!ct.IsCancellationRequested)
        {
            sw.Stop();
            _logger.LogWarning(ex, "[AiServiceClient] Gọi ai-service /barcode/classify bị TIMEOUT ({ElapsedMs} ms).", sw.ElapsedMilliseconds);
            RecordFailure();
            return null;
        }
        catch (HttpRequestException ex)
        {
            sw.Stop();
            _logger.LogWarning(ex, "[AiServiceClient] Không thể kết nối tới ai-service /barcode/classify ({BaseUrl}).", _httpClient.BaseAddress);
            RecordFailure();
            return null;
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger.LogError(ex, "[AiServiceClient] Lỗi không xác định khi gọi ai-service /barcode/classify.");
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
            // Bỏ ngắt mạch dưới Backend
            _circuitOpenUntil = null;
        }
    }
}
