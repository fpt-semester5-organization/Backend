using System.Collections.Generic;

namespace VeggieLife.BLL.DTOs.Fridge.Requests;

/// <summary>
/// DTO yêu cầu lưu hàng loạt nguyên liệu vào tủ lạnh ảo (FN-VIS-02, D123).
/// Dùng khi Thành viên bấm "Lưu vào tủ lạnh cá nhân" sau khi xem và chỉnh kết quả nhận diện.
/// </summary>
public class SaveFridgeItemsBatchRequest
{
    /// <summary>
    /// Phiên nhận diện nguồn (tùy chọn). Có giá trị thì các mục được gắn Session_ID để truy vết.
    /// </summary>
    public int? SessionId { get; set; }

    /// <summary>
    /// Danh sách nguyên liệu cần lưu (1–100 mục). Mục trùng nguyên liệu được gộp, cộng dồn số lượng.
    /// </summary>
    public List<CreateFridgeItemRequest> Items { get; set; } = new();
}
