namespace VeggieLife.BLL.DTOs.Admin.Moderation;

/// <summary>
/// DTO dữ liệu trả về khi quyết định xử lý báo cáo thành công (FN-ADM-05).
/// </summary>
public class DecideAdminModerationReportResponseData
{
    public int ReportId { get; set; }
    public string Status { get; set; } = string.Empty;
    public string TargetStatus { get; set; } = string.Empty;
}
