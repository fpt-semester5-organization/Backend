namespace VeggieLife.BLL.DTOs.Community.Responses;

/// <summary>
/// Dữ liệu phản hồi sau khi ghi nhận báo cáo vi phạm thành công (FN-COMM-05).
/// </summary>
public class ReportResponseData
{
    /// <summary>
    /// Mã phiếu báo cáo vi phạm vừa được khởi tạo trong CSDL.
    /// </summary>
    public int ReportId { get; set; }

    /// <summary>
    /// Trạng thái ban đầu của phiếu báo cáo (luôn là "PENDING" chờ Quản trị viên duyệt).
    /// </summary>
    public string Status { get; set; } = "PENDING";
}
