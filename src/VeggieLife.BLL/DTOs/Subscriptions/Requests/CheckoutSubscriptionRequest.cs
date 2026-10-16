using System.ComponentModel.DataAnnotations;

namespace VeggieLife.BLL.DTOs.Subscriptions.Requests;

/// <summary>
/// Yêu cầu khởi tạo đơn thanh toán gói VIP (FN-SUB-02).
/// </summary>
public class CheckoutSubscriptionRequest
{
    /// <summary>
    /// ID gói VIP cần mua (> 0).
    /// </summary>
    [Required(ErrorMessage = "Mã gói VIP (planId) là bắt buộc.")]
    [Range(1, int.MaxValue, ErrorMessage = "Mã gói VIP (planId) phải lớn hơn 0.")]
    public int PlanId { get; set; }

    /// <summary>
    /// Cổng thanh toán lựa chọn: MOMO hoặc VIETQR.
    /// </summary>
    [Required(ErrorMessage = "Cổng thanh toán (gateway) là bắt buộc (MOMO hoặc VIETQR).")]
    public string Gateway { get; set; } = string.Empty;
}
