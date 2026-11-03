namespace VeggieLife.BLL.DTOs.Fridge.Responses;

/// <summary>
/// DTO đại diện cho một nguyên liệu đang có trong tủ lạnh ảo của người dùng (FN-VIS-02).
/// </summary>
public class FridgeItemDto
{
    /// <summary>
    /// Mã bản ghi lưu trữ trong tủ lạnh ảo (FridgeInventory.Inventory_ID).
    /// </summary>
    public int InventoryId { get; set; }

    /// <summary>
    /// Mã nguyên liệu chuẩn trong danh mục hệ thống (Ingredients.Ingredient_ID).
    /// </summary>
    public int IngredientId { get; set; }

    /// <summary>
    /// Tên nguyên liệu hiển thị (Custom_Item_Name hoặc Ingredient_Name).
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Trạng thái độ tươi: FRESH | NON-FRESH.
    /// </summary>
    public string FreshnessStatus { get; set; } = "FRESH";

    /// <summary>
    /// Số lượng nguyên liệu có trong tủ lạnh ảo.
    /// </summary>
    public decimal Quantity { get; set; } = 1;

    /// <summary>
    /// Đơn vị đong đo của nguyên liệu (VD: "quả", "g", "củ", "miếng").
    /// </summary>
    public string Unit { get; set; } = "phần";

    /// <summary>
    /// Đường dẫn ảnh thu nhỏ hoặc ảnh cắt cận cảnh của nguyên liệu từ phiên quét AI.
    /// </summary>
    public string? ThumbnailUrl { get; set; }
}
