using System;

namespace VeggieLife.BLL.DTOs.Admin.Moderation;

/// <summary>
/// DTO đại diện cho một phần tử trong hàng đợi kiểm duyệt nội dung (FN-ADM-04).
/// </summary>
public class AdminModerationQueueItemDto
{
    public int ReportId { get; set; }
    public string TargetType { get; set; } = string.Empty;
    public int TargetId { get; set; }
    public int? ParentPostId { get; set; }
    public string? TargetTitle { get; set; }
    public string? TargetContent { get; set; }
    public string? AuthorName { get; set; }
    public string ViolationType { get; set; } = string.Empty;
    public string ReportSource { get; set; } = string.Empty;
    public string EvidenceSummary { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
