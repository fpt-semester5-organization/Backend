using System;

namespace VeggieLife.BLL.DTOs.Admin.Subscriptions;

/// <summary>
/// DTO thông tin lịch sử phiên bản của một gói VIP (FN-ADM-13, BR-PAY-SUB-01).
/// </summary>
public class AdminSubscriptionPlanVersionDto
{
    public int VersionId { get; set; }
    public int PlanId { get; set; }
    public int Version { get; set; }
    public string PlanName { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int DurationDays { get; set; }
    public string? FeaturesJson { get; set; }
    public DateTime EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
    public int UpdatedByAdminId { get; set; }
    public string? UpdatedByAdminName { get; set; }
    public string? ChangeReason { get; set; }
    public DateTime CreatedAt { get; set; }
}
