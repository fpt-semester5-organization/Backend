using System.Collections.Generic;

namespace VeggieLife.DAL.External.AiService;

/// <summary>
/// Kết quả phản hồi nhận diện nguyên liệu tủ lạnh từ dịch vụ AI (Gemini Vision) (FN-VIS-01).
/// </summary>
public class AiFridgeDetectionResult
{
    public bool Success { get; set; }
    public List<AiDetectedItemDto> Items { get; set; } = new();
    public int TokensConsumed { get; set; } = 250;
    public string? ModelVersion { get; set; } = "gemini-3.8-flash";
    public int ExecutionTimeMs { get; set; }
}

/// <summary>
/// Thông tin từng nguyên liệu được AI phát hiện từ hình ảnh.
/// </summary>
public class AiDetectedItemDto
{
    public string Name { get; set; } = string.Empty;
    public string FreshnessStatus { get; set; } = "FRESH";
    public string? FoodGroup { get; set; }
}
