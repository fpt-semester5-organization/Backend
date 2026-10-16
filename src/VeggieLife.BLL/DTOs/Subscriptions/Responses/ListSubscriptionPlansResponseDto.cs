using System.Collections.Generic;

namespace VeggieLife.BLL.DTOs.Subscriptions.Responses;

/// <summary>
/// Phản hồi danh sách gói VIP và bảng so sánh đặc quyền (FN-SUB-01).
/// </summary>
public class ListSubscriptionPlansResponseDto
{
    public List<SubscriptionPlanItemDto> Plans { get; set; } = new();
    public SubscriptionBenefitsDto Benefits { get; set; } = new();
}
