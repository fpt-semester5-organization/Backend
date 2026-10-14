namespace VeggieLife.BLL.DTOs.Fridge.Responses;

/// <summary>
/// DTO chứa thông tin hạn mức sử dụng quét ảnh tủ lạnh còn lại (FN-VIS-01).
/// </summary>
public class FridgeDetectionQuotaDto
{
    /// <summary>
    /// Số lượt đã sử dụng (trong phiên đối với Khách, trong ngày đối với Thành viên).
    /// </summary>
    public int Used { get; set; }

    /// <summary>
    /// Hạn mức tối đa (Khách: 3, Free: 10, VIP: 30).
    /// </summary>
    public int Limit { get; set; }

    /// <summary>
    /// Số lượt còn lại có thể sử dụng.
    /// </summary>
    public int Remaining { get; set; }
}
