using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace VeggieLife.DAL.External.Goong;

/// <summary>
/// Client hiện thực kết nối trực tiếp tới Goong Places REST API (v2) (FN-MAP-01).
/// </summary>
public class GoongPlacesClient : IGoongPlacesClient
{
    private readonly HttpClient _httpClient;
    private readonly GoongSettings _settings;
    private readonly ILogger<GoongPlacesClient> _logger;

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public GoongPlacesClient(
        HttpClient httpClient,
        IOptions<GoongSettings> options,
        ILogger<GoongPlacesClient> logger)
    {
        _httpClient = httpClient;
        _settings = options.Value ?? new GoongSettings();
        _logger = logger;

        if (_settings.TimeoutSeconds > 0)
        {
            _httpClient.Timeout = TimeSpan.FromSeconds(_settings.TimeoutSeconds);
        }
    }

    public bool IsConfigured => !string.IsNullOrWhiteSpace(_settings.ApiKey);

    public async Task<IReadOnlyList<GoongPlacePredictionDto>> SearchNearbyPlacesAsync(
        string keyword,
        double latitude,
        double longitude,
        int radiusMeters,
        int limit = 10,
        CancellationToken cancellationToken = default)
    {
        if (!IsConfigured)
        {
            _logger.LogDebug("Goong API Key chưa được thiết lập. Bỏ qua tìm kiếm trực tiếp Goong.");
            return Array.Empty<GoongPlacePredictionDto>();
        }

        var baseUrl = _settings.BaseUrl.TrimEnd('/');
        var latStr = latitude.ToString("F6", CultureInfo.InvariantCulture);
        var lngStr = longitude.ToString("F6", CultureInfo.InvariantCulture);
        var query = Uri.EscapeDataString(keyword);

        var url = $"{baseUrl}/v2/place/autocomplete?api_key={_settings.ApiKey}&input={query}&location={latStr},{lngStr}&radius={radiusMeters}&limit={limit}";

        try
        {
            using var response = await _httpClient.GetAsync(url, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Goong Autocomplete API trả về mã lỗi HTTP {StatusCode}", response.StatusCode);
                return Array.Empty<GoongPlacePredictionDto>();
            }

            var jsonStream = await response.Content.ReadAsStreamAsync(cancellationToken);
            var result = await JsonSerializer.DeserializeAsync<GoongAutocompleteResponse>(jsonStream, SerializerOptions, cancellationToken);

            if (result?.Predictions != null && result.Predictions.Count > 0)
            {
                return result.Predictions;
            }

            return Array.Empty<GoongPlacePredictionDto>();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Lỗi xảy ra khi gọi Goong Autocomplete API quanh ({Lat}, {Lng})", latStr, lngStr);
            return Array.Empty<GoongPlacePredictionDto>();
        }
    }

    public async Task<IReadOnlyList<GoongPlacePredictionDto>> SearchPlacesByTextAsync(
        string input,
        int limit = 10,
        CancellationToken cancellationToken = default)
    {
        if (!IsConfigured || string.IsNullOrWhiteSpace(input))
        {
            return Array.Empty<GoongPlacePredictionDto>();
        }

        var baseUrl = _settings.BaseUrl.TrimEnd('/');
        var query = Uri.EscapeDataString(input.Trim());
        var url = $"{baseUrl}/v2/place/autocomplete?api_key={_settings.ApiKey}&input={query}&limit={limit}";

        try
        {
            using var response = await _httpClient.GetAsync(url, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Goong Autocomplete API trả về mã lỗi HTTP {StatusCode} cho chuỗi '{Input}'", response.StatusCode, input);
                return Array.Empty<GoongPlacePredictionDto>();
            }

            var jsonStream = await response.Content.ReadAsStreamAsync(cancellationToken);
            var result = await JsonSerializer.DeserializeAsync<GoongAutocompleteResponse>(jsonStream, SerializerOptions, cancellationToken);

            return result?.Predictions ?? (IReadOnlyList<GoongPlacePredictionDto>)Array.Empty<GoongPlacePredictionDto>();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Lỗi xảy ra khi tìm kiếm địa điểm Goong với từ khóa '{Input}'", input);
            return Array.Empty<GoongPlacePredictionDto>();
        }
    }

    public async Task<GoongPlaceDetailDto?> GetPlaceDetailAsync(
        string placeId,
        CancellationToken cancellationToken = default)
    {
        if (!IsConfigured || string.IsNullOrWhiteSpace(placeId))
        {
            return null;
        }

        var baseUrl = _settings.BaseUrl.TrimEnd('/');
        var encodedId = Uri.EscapeDataString(placeId.Trim());
        var url = $"{baseUrl}/v2/place/detail?api_key={_settings.ApiKey}&place_id={encodedId}";

        try
        {
            using var response = await _httpClient.GetAsync(url, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Goong Place Detail API trả về mã lỗi HTTP {StatusCode} cho place_id '{PlaceId}'", response.StatusCode, placeId);
                return null;
            }

            var jsonStream = await response.Content.ReadAsStreamAsync(cancellationToken);
            var result = await JsonSerializer.DeserializeAsync<GoongPlaceDetailResponse>(jsonStream, SerializerOptions, cancellationToken);

            return result?.Result;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Lỗi xảy ra khi lấy chi tiết địa điểm Goong cho place_id '{PlaceId}'", placeId);
            return null;
        }
    }
}
