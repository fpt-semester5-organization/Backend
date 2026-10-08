using System;
using System.Collections.Generic;

namespace VeggieLife.DAL.Entities;

public partial class UserWeightHistory
{
    public long LogId { get; set; }

    public int UserId { get; set; }

    public decimal WeightKg { get; set; }

    public DateOnly LoggedDate { get; set; }

    public string? Note { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual User User { get; set; } = null!;
}
