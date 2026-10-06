using System;
using System.Collections.Generic;

namespace VeggieLife.DAL.Entities;

public partial class SubscriptionPlan
{
    public int PlanId { get; set; }

    public int AdminId { get; set; }

    public string PlanName { get; set; } = null!;

    public decimal Price { get; set; }

    public int DurationDays { get; set; }

    public string? FeaturesJson { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual User Admin { get; set; } = null!;

    public virtual ICollection<PaymentTransaction> PaymentTransactions { get; set; } = new List<PaymentTransaction>();
}
