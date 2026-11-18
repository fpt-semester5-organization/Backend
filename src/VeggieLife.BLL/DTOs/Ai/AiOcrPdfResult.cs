namespace VeggieLife.BLL.DTOs.Ai;

/// <summary>
/// Kết quả POST /rag/ocr-pdf của AI service: văn bản nhận dạng từ PDF scan (Gemini) và usage cộng dồn mọi lô trang.
/// </summary>
public class AiOcrPdfResult
{
    public string Text { get; set; } = string.Empty;
    public int Pages { get; set; }
    public AiOcrUsage Usage { get; set; } = new();
}

public class AiOcrUsage
{
    public string Model { get; set; } = string.Empty;
    public int PromptTokens { get; set; }
    public int CompletionTokens { get; set; }
    public int LatencyMs { get; set; }
}
