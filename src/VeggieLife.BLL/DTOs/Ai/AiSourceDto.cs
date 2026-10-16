using System.Text.Json.Serialization;

namespace VeggieLife.BLL.DTOs.Ai;

/// <summary>
/// Nguồn tham chiếu do AI Service truy xuất từ Qdrant (D118, D119).
/// Hỗ trợ cả 2 chuẩn: sourceType/sourceId và type/id.
/// </summary>
public class AiSourceDto
{
    [JsonPropertyName("sourceType")]
    public string SourceType { get; set; } = string.Empty;

    [JsonPropertyName("sourceId")]
    public string SourceId { get; set; } = string.Empty;

    [JsonPropertyName("type")]
    public string? LegacyType { get; set; }

    [JsonPropertyName("id")]
    public object? LegacyId { get; set; }

    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("score")]
    public double? Score { get; set; }

    [JsonIgnore]
    public string Type
    {
        get => !string.IsNullOrEmpty(SourceType) ? SourceType : (LegacyType ?? string.Empty);
        set => SourceType = value;
    }

    [JsonIgnore]
    public int? Id
    {
        get
        {
            if (int.TryParse(SourceId, out var parsed)) return parsed;
            if (LegacyId is int intVal) return intVal;
            if (LegacyId != null && int.TryParse(LegacyId.ToString(), out var parsedLegacy)) return parsedLegacy;
            return null;
        }
        set => SourceId = value?.ToString() ?? string.Empty;
    }
}
