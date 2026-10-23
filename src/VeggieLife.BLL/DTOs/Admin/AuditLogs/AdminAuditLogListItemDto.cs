using System;
using System.Text.Json.Serialization;

namespace VeggieLife.BLL.DTOs.Admin.AuditLogs;

/// <summary>
/// DTO thông tin bản ghi nhật ký kiểm toán trả về cho Quản trị viên (FN-ADM-17).
/// Tuân thủ BR-ADM-SYS-002: Các trường nhạy cảm trong OldValue/NewValue được ẩn hoàn toàn.
/// </summary>
public class AdminAuditLogListItemDto
{
    public long AuditId { get; set; }

    public int? ActorUserId { get; set; }

    public string ActorRole { get; set; } = string.Empty;

    public string Action { get; set; } = string.Empty;

    public string TargetTable { get; set; } = string.Empty;

    public string TargetId { get; set; } = string.Empty;

    public string? Note { get; set; }

    public DateTime CreatedAt { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? OldValue { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? NewValue { get; set; }
}
