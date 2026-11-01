using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace VeggieLife.BLL.DTOs.Fridge.Responses;

/// <summary>
/// DTO đại diện cho một nguyên liệu được phát hiện từ ảnh tủ lạnh (FN-VIS-01).
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
    /// Định lượng/số lượng hiển thị bằng tiếng Việt do AI ước tính (ví dụ: "3 quả (~450g)", "1 bó (~500g)").
    /// </summary>
    public string? Quantity { get; set; }

    /// <summary>
    /// Số lượng ước tính dạng số.
    /// </summary>
    public decimal? Amount { get; set; }

    /// <summary>
    /// Đơn vị tính (quả, củ, bó, miếng, g, kg...).
    /// </summary>
    public string? Unit { get; set; }

    /// <summary>
    /// Tọa độ hộp bao [ymin, xmin, ymax, xmax] trong thang đo 0..1000 chuẩn hóa từ Gemini Vision.
    /// </summary>
    [JsonPropertyName("box2d")]
    public List<int>? Box2D { get; set; }

    /// <summary>
    /// Nhóm thực phẩm (Rau củ, Trái cây, Sữa & Chế phẩm, Nước uống...).
    /// </summary>
    public string? FoodGroup { get; set; }

    /// <summary>
    /// Có nguồn gốc động vật không (thịt, cá, trứng, sữa...).
    /// </summary>
    public bool IsAnimalProduct { get; set; }

    /// <summary>
    /// Có phải là thịt đỏ (bò, heo, cừu...) không.
    /// </summary>
    public bool IsRedMeat { get; set; }

    /// <summary>
    /// Độ tin cậy từ mô hình AI (0..1).
    /// </summary>
    public double? Confidence { get; set; }
}
