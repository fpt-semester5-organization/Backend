using System.ComponentModel.DataAnnotations;

namespace VeggieLife.BLL.DTOs.Admin.Subscriptions;

/// <summary>
/// Request tạo mới gói VIP dành cho Quản trị viên (FN-ADM-13).
/// </summary>
public class CreateAdminSubscriptionPlanRequest
{
    [Required(ErrorMessage = "Tên gói không được để trống.")]
    [StringLength(100, ErrorMessage = "Tên gói tối đa 100 ký tự.")]
    public string PlanName { get; set; } = null!;

    [Range(1, int.MaxValue, ErrorMessage = "Thời hạn gói phải lớn hơn 0.")]
    public int DurationDays { get; set; }

    [Range(1, long.MaxValue, ErrorMessage = "Giá gói phải lớn hơn 0.")]
    public long PriceVnd { get; set; }
}
