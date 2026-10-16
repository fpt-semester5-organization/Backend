using System;

namespace VeggieLife.BLL.DTOs.Subscriptions.Responses;

/// <summary>
/// Nhật ký giao dịch tài chính mua gói VIP của người dùng (FN-SUB-04).
/// </summary>
public class SubscriptionTransactionDto
{
    public long TransactionId { get; set; }
    public string Gateway { get; set; } = string.Empty;
    public long AmountVnd { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
