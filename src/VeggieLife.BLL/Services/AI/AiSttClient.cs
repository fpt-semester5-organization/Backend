using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using VeggieLife.BLL.DTOs.Videos.Responses;

namespace VeggieLife.BLL.Services.AI;

/// <summary>
/// Client kết nối HTTP với AI STT Microservice (Whisper small-int8 + Gemini 2.5 Flash).
/// Endpoint phục vụ: POST /stt/video-to-recipe (FN-POST-10, BR-AI-STT-01, BR-AI-STT-03).
/// </summary>
public class AiSttClient : IAiSttClient
{
    private readonly HttpClient _client;
    private readonly ILogger<AiSttClient> _logger;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public AiSttClient(HttpClient client, IConfiguration config, ILogger<AiSttClient> logger)
    {
        _client = client;
        _logger = logger;

        if (_client.BaseAddress == null)
        {
            var baseUrl = config["AiService:SttBaseUrl"] ?? config["AiService:BaseUrl"] ?? "http://localhost:8081";
            _client.BaseAddress = new Uri(baseUrl);
        }

        if (!_client.DefaultRequestHeaders.Contains("X-Internal-Api-Key"))
        {
            var apiKey = config["AiService:InternalApiKey"] ?? "veggielife-secret-internal-key-2026";
            _client.DefaultRequestHeaders.Add("X-Internal-Api-Key", apiKey);
        }

        _client.Timeout = TimeSpan.FromSeconds(300);
    }

    public async Task<AiSttResultDto?> ExtractRecipeFromVideoAsync(string videoUrl, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(videoUrl))
        {
            _logger.LogWarning("VideoUrl để bóc tách công thức bị rỗng.");
            return null;
        }

        try
        {
            var payload = new { videoUrl };
            _logger.LogInformation("Gửi yêu cầu bóc tách STT tới {BaseAddress}/stt/video-to-recipe...", _client.BaseAddress);

            HttpResponseMessage response;
            try
            {
                response = await _client.PostAsJsonAsync("/stt/video-to-recipe", payload, JsonOptions, ct);
            }
            catch (HttpRequestException ex) when (_client.BaseAddress?.Port != 8081)
            {
                _logger.LogWarning(ex, "Không kết nối được tới {BaseAddress}, thử kết nối dự phòng tới http://localhost:8081/stt/video-to-recipe", _client.BaseAddress);
                using var fallbackClient = new HttpClient { BaseAddress = new Uri("http://localhost:8081"), Timeout = _client.Timeout };
                foreach (var h in _client.DefaultRequestHeaders)
                {
                    fallbackClient.DefaultRequestHeaders.TryAddWithoutValidation(h.Key, h.Value);
                }
                response = await fallbackClient.PostAsJsonAsync("/stt/video-to-recipe", payload, JsonOptions, ct);
            }

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(ct);
                _logger.LogError("AI STT Microservice trả về mã lỗi {StatusCode}: {ErrorBody}", response.StatusCode, errorBody);
                return null;
            }

            var result = await response.Content.ReadFromJsonAsync<AiSttResultDto>(JsonOptions, cancellationToken: ct);
            if (result == null)
            {
                _logger.LogWarning("AI STT Microservice trả về phản hồi rỗng.");
                return null;
            }

            _logger.LogInformation("Nhận kết quả bóc tách thành công: model={Model}, duration={Duration}s, rawLength={RawLen}",
                result.SttModelVersion, result.DurationSeconds, result.RawTranscript?.Length ?? 0);

            return result;
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            _logger.LogWarning("Tác vụ bóc tách STT bị hủy bởi caller.");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi ngoại lệ khi giao tiếp với AI STT Microservice cho video {VideoUrl}", videoUrl);
            return null;
        }
    }
}
