namespace VeggieLife.BLL.DTOs.Admin.AuditLogs;

/// <summary>
/// DTO chứa các tham số bộ lọc và phân trang tra cứu nhật ký kiểm toán (FN-ADM-17).
/// </summary>
public class AdminAuditLogFilterRequest
{
    public int? ActorUserId { get; set; }

    public string? Action { get; set; }

    public string? TargetTable { get; set; }

    public string? TargetId { get; set; }

    public string? From { get; set; }

    public string? To { get; set; }

    public int Page { get; set; } = 1;

    public int Size { get; set; } = 20;
}
