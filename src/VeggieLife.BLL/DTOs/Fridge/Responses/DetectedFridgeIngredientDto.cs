using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace VeggieLife.BLL.DTOs.Fridge.Responses;

/// <summary>
/// DTO đại diện cho một nguyên liệu được phát hiện từ ảnh tủ lạnh (FN-VIS-01).
/// Nếu không khớp nguyên liệu hệ thống: inventoryId và ingredientId sẽ mang giá trị null.
/// </summary>
public class DetectedFridgeIngredientDto
{
    /// <summary>
    /// Mã bản ghi trong tủ lạnh ảo (null nếu là khách hoặc nguyên liệu chưa khớp).
    /// </summary>
    public int? InventoryId { get; set; }

    /// <summary>
    /// Mã nguyên liệu chuẩn trong hệ thống (null nếu chưa khớp >= 80%).
    /// </summary>
    public int? IngredientId { get; set; }

    /// <summary>
    /// Tên nguyên liệu (tên chuẩn nếu khớp hoặc tên thô do AI nhận diện nếu chưa khớp).
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Ước lượng độ tươi: FRESH | NON-FRESH.
    /// </summary>
    public string FreshnessStatus { get; set; } = "FRESH";

    /// <summary>
    /// Định lượng hoặc số lượng ước tính từ ảnh (ví dụ: "nửa trái", "3 quả", "500g", "1 bó", "3kg").
    /// </summary>
    [JsonPropertyName("quantity")]
    public string? Quantity { get; set; }

    /// <summary>
    /// Giá trị số học của định lượng (ví dụ: 0.5, 3.0, 500.0).
    /// </summary>
    [JsonPropertyName("amount")]
    public decimal? Amount { get; set; }

    /// <summary>
    /// Đơn vị đo lường tương ứng (ví dụ: "trái", "quả", "kg", "g", "bó", "củ").
    /// </summary>
    [JsonPropertyName("unit")]
    public string? Unit { get; set; }

    /// <summary>
    /// Tọa độ hộp bao [ymin, xmin, ymax, xmax] trong thang đo 0..1000 chuẩn hóa từ Gemini Vision.
    /// </summary>
    [JsonPropertyName("box2d")]
    public List<int>? Box2D { get; set; }
}
