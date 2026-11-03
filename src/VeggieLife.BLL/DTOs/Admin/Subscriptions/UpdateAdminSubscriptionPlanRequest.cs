using System.ComponentModel.DataAnnotations;

namespace VeggieLife.BLL.DTOs.Admin.Subscriptions;

/// <summary>
/// Request cập nhật giá, tên, thời hạn hoặc trạng thái gói VIP dành cho Quản trị viên (FN-ADM-13, BR-ADM-PAY-001).
/// </summary>
public class UpdateAdminSubscriptionPlanRequest
{
    [StringLength(100, ErrorMessage = "Tên gói tối đa 100 ký tự.")]
    public string? PlanName { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Thời hạn gói phải lớn hơn 0.")]
    public int? DurationDays { get; set; }

    [Range(1, long.MaxValue, ErrorMessage = "Giá gói phải lớn hơn 0.")]
    public long? PriceVnd { get; set; }

    public bool? IsActive { get; set; }
}
