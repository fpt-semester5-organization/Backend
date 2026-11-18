using System.Collections.Generic;

namespace VeggieLife.BLL.DTOs.Fridge.Responses;

/// <summary>
/// Kết quả lưu hàng loạt nguyên liệu vào tủ lạnh ảo (FN-VIS-02, D123).
/// </summary>
public class FridgeBatchSaveResultDto
{
    /// <summary>
    /// Số mục gửi lên trong yêu cầu.
    /// </summary>
    public int Requested { get; set; }

    /// <summary>
    /// Số dòng tủ lạnh được tạo mới.
    /// </summary>
    public int Created { get; set; }

    /// <summary>
    /// Số dòng tủ lạnh đã có sẵn và được ghi đè theo lần lưu này.
    /// </summary>
    public int Updated { get; set; }

    /// <summary>
    /// Số mục bị bỏ qua (đồ mặn, tên không hợp lệ).
    /// </summary>
    public int Skipped { get; set; }

    /// <summary>
    /// Các dòng tủ lạnh sau khi lưu (mỗi nguyên liệu một dòng).
    /// </summary>
    public List<FridgeItemDto> Items { get; set; } = new();

    /// <summary>
    /// Chi tiết các mục bị bỏ qua.
    /// </summary>
    public List<FridgeBatchSkippedItemDto> SkippedItems { get; set; } = new();
}

/// <summary>
/// Một mục bị bỏ qua khi lưu hàng loạt.
/// </summary>
public class FridgeBatchSkippedItemDto
{
    /// <summary>
    /// Tên mục gửi lên.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Lý do: NON_VEGETARIAN | INVALID_NAME.
    /// </summary>
    public string Reason { get; set; } = string.Empty;
}
