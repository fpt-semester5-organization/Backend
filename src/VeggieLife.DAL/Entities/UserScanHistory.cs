using System;
using System.Collections.Generic;

namespace VeggieLife.DAL.Entities;

public partial class UserScanHistory
{
    public long ScanId { get; set; }

    public int? UserId { get; set; }

    public string? GuestUuid { get; set; }

    public int ProductId { get; set; }

    public string ScanMethod { get; set; } = null!;

    public bool IsNonVeganTriggered { get; set; }

    public DateTime ScannedAt { get; set; }

    public virtual Product Product { get; set; } = null!;

    public virtual User? User { get; set; }
}
