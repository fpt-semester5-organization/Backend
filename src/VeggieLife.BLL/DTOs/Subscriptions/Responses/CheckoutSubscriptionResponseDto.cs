using System;

namespace VeggieLife.BLL.DTOs.Subscriptions.Responses;

/// <summary>
/// Phản hồi khởi tạo đơn thanh toán gói VIP (FN-SUB-02).
/// </summary>
public class CheckoutSubscriptionResponseDto
{
    public long TransactionId { get; set; }
    public string OrderCode { get; set; } = string.Empty;
    public long AmountVnd { get; set; }
    public string PayUrl { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
}
