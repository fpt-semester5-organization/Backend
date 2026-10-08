using System;
using System.Collections.Generic;

namespace VeggieLife.DAL.Entities;

public partial class PaymentTransaction
{
    public long TransactionId { get; set; }

    public int PlanId { get; set; }

    public int UserId { get; set; }

    public string GatewayProvider { get; set; } = null!;

    public string PartnerTransactionCode { get; set; } = null!;

    public decimal AmountVnd { get; set; }

    public string TransactionStatus { get; set; } = null!;

    public string SignatureHash { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public DateTime? CompletedAt { get; set; }

    public string? ReceiptUrl { get; set; }

    public DateTime StartsAt { get; set; }

    public DateTime EndsAt { get; set; }

    public virtual SubscriptionPlan Plan { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
