namespace VeggieLife.BLL.DTOs.Admin.Moderation;

/// <summary>
/// DTO yêu cầu quyết định xử lý báo cáo vi phạm nội dung (FN-ADM-05).
/// </summary>
public class DecideAdminModerationReportRequest
{
    public string Decision { get; set; } = string.Empty;
    public string Note { get; set; } = string.Empty;
    public bool ApplySanction { get; set; } = false;
}
