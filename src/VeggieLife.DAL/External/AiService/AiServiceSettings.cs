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

    /// <summary>
    /// API Key của Google Gemini dùng để gọi trực tiếp Gemini Vision (được ánh xạ từ mục 'Gemini:ApiKey' trong appsettings.json).
    /// </summary>
    public string? GeminiApiKey { get; set; }

    /// <summary>
    /// Tên model Gemini sử dụng (mặc định: gemini-1.5-flash).
    /// </summary>
    public string GeminiModel { get; set; } = "gemini-1.5-flash";
}
