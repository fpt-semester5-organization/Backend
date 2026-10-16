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
}
