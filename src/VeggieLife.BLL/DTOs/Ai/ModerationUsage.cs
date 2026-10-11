namespace VeggieLife.BLL.DTOs.Ai;

/// <summary>
/// Thông số hiệu năng và định mức token tiêu thụ từ mô hình AI (gemini-2.5-flash).
/// </summary>
public class ModerationUsage
{
    public string Model { get; set; } = string.Empty;
    public int PromptTokens { get; set; }
    public int CompletionTokens { get; set; }
    public int LatencyMs { get; set; }
    public double Temperature { get; set; }
}
