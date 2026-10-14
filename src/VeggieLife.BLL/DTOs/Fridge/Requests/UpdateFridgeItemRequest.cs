namespace VeggieLife.BLL.DTOs.Fridge.Requests;

/// <summary>
/// DTO yêu cầu cập nhật tên hiển thị hoặc độ tươi của một mục trong tủ lạnh ảo (FN-VIS-02).
/// </summary>
public class UpdateFridgeItemRequest
{
    /// <summary>
    /// Tên tùy chỉnh do người dùng đặt (tùy chọn, tối đa 100 ký tự).
    /// </summary>
    public string? CustomItemName { get; set; }

    /// <summary>
    /// Trạng thái độ tươi của nguyên liệu (tùy chọn: FRESH | NON-FRESH).
    /// </summary>
    public string? FreshnessStatus { get; set; }
}
