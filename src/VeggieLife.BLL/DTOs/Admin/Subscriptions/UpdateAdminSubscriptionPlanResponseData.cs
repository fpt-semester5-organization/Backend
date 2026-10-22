namespace VeggieLife.BLL.DTOs.Admin.Subscriptions;

/// <summary>
/// Response payload khi cập nhật gói VIP thành công (FN-ADM-13).
/// </summary>
public class UpdateAdminSubscriptionPlanResponseData
{
    public int PlanId { get; set; }
    public long PriceVnd { get; set; }
}
