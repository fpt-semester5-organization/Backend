namespace VeggieLife.DAL.External.AiService;

/// <summary>
/// Cấu hình kết nối tới microservice ai-service (FastAPI trên VPS).
/// </summary>
public class AiServiceSettings
{
    public const string SectionName = "AiService";

    public string BaseUrl { get; set; } = "http://localhost:8080";
    public string InternalApiKey { get; set; } = string.Empty;
    public int TimeoutSeconds { get; set; } = 0; // 0 = Không giới hạn timeout
    public int BarcodeTimeoutSeconds { get; set; } = 0; // 0 = Không giới hạn timeout
}
