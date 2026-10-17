using System;
using System.Collections.Generic;

namespace VeggieLife.DAL.Entities;

public partial class AuditLog
{
    public long AuditId { get; set; }

    public int? ActorUserId { get; set; }

    public string ActorRole { get; set; } = null!;

    public string Action { get; set; } = null!;

    public string TargetTable { get; set; } = null!;

    public string TargetId { get; set; } = null!;

    public string? OldValue { get; set; }

    public string? NewValue { get; set; }

    public string? Note { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual User? ActorUser { get; set; }
}
