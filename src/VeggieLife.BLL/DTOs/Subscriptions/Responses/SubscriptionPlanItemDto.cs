namespace VeggieLife.BLL.DTOs.Subscriptions.Responses;

/// <summary>
/// Thông tin chi tiết một gói hội viên VIP (FN-SUB-01).
/// </summary>
public class SubscriptionPlanItemDto
{
    public int PlanId { get; set; }
    public string PlanName { get; set; } = string.Empty;
    public int DurationDays { get; set; }
    public long PriceVnd { get; set; }
    public bool IsActive { get; set; }
}
