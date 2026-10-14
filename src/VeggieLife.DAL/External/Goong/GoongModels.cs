using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace VeggieLife.DAL.External.Goong;

/// <summary>
/// Cấu trúc phản hồi từ Goong Place Autocomplete API (V2).
/// </summary>
public class GoongAutocompleteResponse
{
    [JsonPropertyName("predictions")]
    public List<GoongPlacePredictionDto>? Predictions { get; set; }

    [JsonPropertyName("status")]
    public string? Status { get; set; }
}

public class GoongPlacePredictionDto
{
    [JsonPropertyName("place_id")]
    public string PlaceId { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("structured_formatting")]
    public GoongStructuredFormatting? StructuredFormatting { get; set; }
}

public class GoongStructuredFormatting
{
    [JsonPropertyName("main_text")]
    public string MainText { get; set; } = string.Empty;

    [JsonPropertyName("secondary_text")]
    public string SecondaryText { get; set; } = string.Empty;
}

/// <summary>
/// Cấu trúc phản hồi từ Goong Place Detail API (V2).
/// </summary>
public class GoongPlaceDetailResponse
{
    [JsonPropertyName("result")]
    public GoongPlaceDetailDto? Result { get; set; }

    [JsonPropertyName("status")]
    public string? Status { get; set; }
}

public class GoongPlaceDetailDto
{
    [JsonPropertyName("place_id")]
    public string PlaceId { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("formatted_address")]
    public string FormattedAddress { get; set; } = string.Empty;

    [JsonPropertyName("geometry")]
    public GoongGeometry? Geometry { get; set; }

    [JsonPropertyName("rating")]
    public decimal? Rating { get; set; }
}

public class GoongGeometry
{
    [JsonPropertyName("location")]
    public GoongLocation? Location { get; set; }
}

public class GoongLocation
{
    [JsonPropertyName("lat")]
    public double Lat { get; set; }

    [JsonPropertyName("lng")]
    public double Lng { get; set; }
}
