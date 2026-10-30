namespace VeggieLife.BLL.DTOs.Fridge.Requests;

/// <summary>
/// DTO yêu cầu thêm thủ công một nguyên liệu vào tủ lạnh ảo (FN-VIS-02).
/// </summary>
public class CreateFridgeItemRequest
{
    /// <summary>
    /// Mã nguyên liệu trong hệ thống (tùy chọn, nếu không truyền sẽ tự động tìm/tạo từ CustomItemName).
    /// </summary>
    public int? IngredientId { get; set; }

    /// <summary>
    /// Tên tùy chỉnh do người dùng đặt (tối đa 100 ký tự). Mặc định là tên nguyên liệu gốc.
    /// </summary>
    public string? CustomItemName { get; set; }

    /// <summary>
    /// Trạng thái độ tươi của nguyên liệu: FRESH | NON-FRESH (mặc định FRESH).
    /// </summary>
    public string? FreshnessStatus { get; set; }
}
