namespace VeggieLife.DAL.External.Google;

/// <summary>
/// Cấu hình tích hợp Google OAuth 2.0 (Client ID, Client Secret).
/// </summary>
public class GoogleAuthSettings
{
    public const string SectionName = "Google";

    public string? ClientId { get; set; }

    public string? ClientSecret { get; set; }
}
