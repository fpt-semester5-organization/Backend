using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace VeggieLife.DAL.External.AiService;

/// <summary>
/// Kết quả phản hồi nhận diện nguyên liệu tủ lạnh từ dịch vụ AI (Gemini Vision) (FN-VIS-01).
/// </summary>
public class AiFridgeDetectionResult
{
    public bool Success { get; set; } = true;
    public List<AiDetectedItemDto> Items { get; set; } = new();
    public int TokensConsumed { get; set; } = 250;
    public string? ModelVersion { get; set; } = "gemini-2.5-flash-vision";
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
    public bool IsAnimalProduct { get; set; }
    public bool IsRedMeat { get; set; }

    /// <summary>
    /// Tọa độ hộp bao [ymin, xmin, ymax, xmax] trong thang đo 0..1000 từ Gemini Vision.
    /// </summary>
    [JsonPropertyName("box2d")]
    public List<int>? Box2D { get; set; }

    [JsonPropertyName("box_2d")]
    public List<int>? Box_2D
    {
        get => Box2D;
        set => Box2D ??= value;
    }
}
