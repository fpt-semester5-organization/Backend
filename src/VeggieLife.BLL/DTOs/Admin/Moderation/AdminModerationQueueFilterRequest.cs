namespace VeggieLife.BLL.DTOs.Admin.Moderation;

/// <summary>
/// DTO tham số lọc danh sách hàng đợi kiểm duyệt dành cho Quản trị viên (FN-ADM-04).
/// </summary>
public class AdminModerationQueueFilterRequest
{
    public string? ReportSource { get; set; }
    public string? ViolationType { get; set; }
    public string? TargetType { get; set; }
    public int Page { get; set; } = 1;
    public int Size { get; set; } = 20;
}
