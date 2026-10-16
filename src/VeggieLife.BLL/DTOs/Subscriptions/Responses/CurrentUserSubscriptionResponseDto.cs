using System;
using System.Collections.Generic;

namespace VeggieLife.BLL.DTOs.Subscriptions.Responses;

/// <summary>
/// Trạng thái gói VIP hiện tại và lịch sử giao dịch của người dùng (FN-SUB-04).
/// </summary>
public class CurrentUserSubscriptionResponseDto
{
    public bool IsVip { get; set; }
    public SubscriptionPlanItemDto? Plan { get; set; }
    public DateTime? EndsAt { get; set; }
    public List<SubscriptionTransactionDto> Transactions { get; set; } = new();
}
