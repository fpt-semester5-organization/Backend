using System;

namespace VeggieLife.DAL.Entities;

/// <summary>
/// Thực thể lưu trữ lịch sử các phiên bản của gói hội viên VIP (Audit & Historical Plan Versioning).
/// Giúp phân biệt quyền lợi và đối soát đúng cam kết biên lai của người đăng ký trước và sau khi Admin cập nhật gói.
/// </summary>
public partial class SubscriptionPlanVersion
{
    public int VersionId { get; set; }

    public int PlanId { get; set; }

    public int Version { get; set; }

    public string PlanName { get; set; } = null!;

    public decimal Price { get; set; }

    public int DurationDays { get; set; }

    public string? FeaturesJson { get; set; }

    public DateTime EffectiveFrom { get; set; }

    public DateTime? EffectiveTo { get; set; }

    public int UpdatedByAdminId { get; set; }

    public string? ChangeReason { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual SubscriptionPlan Plan { get; set; } = null!;

    public virtual User UpdatedByAdmin { get; set; } = null!;
}
