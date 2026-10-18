using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace VeggieLife.DAL.External.AiService;

/// <summary>
/// DTO gửi yêu cầu phân loại thuần chay qua barcode sang AI service (POST /barcode/classify).
/// </summary>
public class AiBarcodeClassifyRequest
{
    [JsonPropertyName("barcode")]
    public string Barcode { get; set; } = string.Empty;

    [JsonPropertyName("product")]
    public AiBarcodeProductDto Product { get; set; } = new();

    [JsonPropertyName("knownAdditives")]
    public List<AiKnownAdditiveDto> KnownAdditives { get; set; } = new();
}

public class AiBarcodeProductDto
{
    [JsonPropertyName("productName")]
    public string ProductName { get; set; } = string.Empty;

    [JsonPropertyName("brand")]
    public string? Brand { get; set; }

    [JsonPropertyName("ingredientsText")]
    public string? IngredientsText { get; set; }

    [JsonPropertyName("additivesTags")]
    public List<string> AdditivesTags { get; set; } = new();

    [JsonPropertyName("allergensTags")]
    public List<string> AllergensTags { get; set; } = new();

    [JsonPropertyName("tracesTags")]
    public List<string> TracesTags { get; set; } = new();

    [JsonPropertyName("labelsTags")]
    public List<string> LabelsTags { get; set; } = new();

    [JsonPropertyName("ingredientsAnalysisTags")]
    public List<string> IngredientsAnalysisTags { get; set; } = new();
}

public class AiKnownAdditiveDto
{
    [JsonPropertyName("code")]
    public string Code { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("veganStatus")]
    public string VeganStatus { get; set; } = string.Empty;
}

/// <summary>
/// DTO nhận kết quả phân loại thuần chay từ AI service (POST /barcode/classify).
/// </summary>
public class AiBarcodeClassifyResponse
{
    [JsonPropertyName("veganVerdict")]
    public string VeganVerdict { get; set; } = "DOUBTFUL";

    [JsonPropertyName("verdictText")]
    public string VerdictText { get; set; } = string.Empty;

    [JsonPropertyName("detectedAdditives")]
    public List<AiDetectedAdditiveDto> DetectedAdditives { get; set; } = new();

    [JsonPropertyName("allergens")]
    public List<AiBarcodeAllergenDto> Allergens { get; set; } = new();

    [JsonPropertyName("confidence")]
    public double Confidence { get; set; }

    [JsonPropertyName("usage")]
    public AiUsageDto Usage { get; set; } = new();

    [JsonPropertyName("audit")]
    public AiAuditDto Audit { get; set; } = new();
}

public class AiDetectedAdditiveDto
{
    [JsonPropertyName("code")]
    public string Code { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("veganStatus")]
    public string VeganStatus { get; set; } = "DOUBTFUL";

    [JsonPropertyName("reason")]
    public string Reason { get; set; } = string.Empty;

    [JsonPropertyName("source")]
    public string Source { get; set; } = "MODEL";
}

public class AiBarcodeAllergenDto
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("presenceType")]
    public string PresenceType { get; set; } = "CONTAINS";
}

public class AiAuditDto
{
    [JsonPropertyName("prompt")]
    public string Prompt { get; set; } = string.Empty;

    [JsonPropertyName("rawResult")]
    public string RawResult { get; set; } = string.Empty;
}
