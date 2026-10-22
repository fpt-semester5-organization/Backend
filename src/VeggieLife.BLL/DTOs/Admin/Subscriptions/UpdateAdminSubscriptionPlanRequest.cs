using System.ComponentModel.DataAnnotations;

namespace VeggieLife.BLL.DTOs.Admin.Subscriptions;

/// <summary>
/// Request cập nhật giá hoặc trạng thái gói VIP dành cho Quản trị viên (FN-ADM-13, BR-ADM-PAY-001).
/// </summary>
public class UpdateAdminSubscriptionPlanRequest
{
    [Range(1, long.MaxValue, ErrorMessage = "Giá gói phải lớn hơn 0.")]
    public long? PriceVnd { get; set; }

    public bool? IsActive { get; set; }
}
