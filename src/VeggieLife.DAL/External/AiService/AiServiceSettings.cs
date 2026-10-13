namespace VeggieLife.DAL.External.AiService;

/// <summary>
/// Cấu hình kết nối tới microservice ai-service (FastAPI trên VPS).
/// </summary>
public class AiServiceSettings
{
    public const string SectionName = "AiService";

    public string BaseUrl { get; set; } = "http://localhost:8080";
    public string InternalApiKey { get; set; } = "dev-internal-key";
    public int TimeoutSeconds { get; set; } = 4; // Ràng buộc BR-ADM-AI-002: quá 4s ngắt sang fallback
}
