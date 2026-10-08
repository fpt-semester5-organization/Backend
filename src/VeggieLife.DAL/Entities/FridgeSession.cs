using System;
using System.Collections.Generic;

namespace VeggieLife.DAL.Entities;

public partial class FridgeSession
{
    public int SessionId { get; set; }

    public int UserId { get; set; }

    public string ImageCdnUrl { get; set; } = null!;

    public int DetectedItemsCount { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual ICollection<FridgeInventory> FridgeInventories { get; set; } = new List<FridgeInventory>();

    public virtual User User { get; set; } = null!;
}
