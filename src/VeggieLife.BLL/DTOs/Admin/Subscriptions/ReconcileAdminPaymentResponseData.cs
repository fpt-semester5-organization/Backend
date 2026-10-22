using System;

namespace VeggieLife.BLL.DTOs.Admin.Subscriptions;

/// <summary>
/// Response payload khi đối soát thành công (FN-ADM-13).
/// </summary>
public class ReconcileAdminPaymentResponseData
{
    public long TransactionId { get; set; }
    public string Status { get; set; } = null!;
    public DateTime VipEndsAt { get; set; }
}
