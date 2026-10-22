namespace VeggieLife.BLL.DTOs.Admin.Subscriptions;

/// <summary>
/// DTO thông tin gói VIP trong danh sách trả về cho Quản trị viên (FN-ADM-13).
/// </summary>
public class AdminSubscriptionPlanItemDto
{
    public int PlanId { get; set; }
    public string PlanName { get; set; } = null!;
    public int DurationDays { get; set; }
    public long PriceVnd { get; set; }
    public bool IsActive { get; set; }
}
