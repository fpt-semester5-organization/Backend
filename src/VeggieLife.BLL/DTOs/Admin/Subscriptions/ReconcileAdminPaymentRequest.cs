using System.ComponentModel.DataAnnotations;

namespace VeggieLife.BLL.DTOs.Admin.Subscriptions;

/// <summary>
/// Request đối soát hoặc hoàn tiền giao dịch thanh toán (FN-ADM-13, BR-ADM-PAY-002).
/// Action: COMPENSATE | REFUND.
/// </summary>
public class ReconcileAdminPaymentRequest
{
    [Required(ErrorMessage = "Hành động đối soát không được để trống.")]
    public string Action { get; set; } = null!;

    [Required(ErrorMessage = "Lý do đối soát không được để trống.")]
    [StringLength(500, MinimumLength = 10, ErrorMessage = "Lý do đối soát phải từ 10 đến 500 ký tự.")]
    public string Reason { get; set; } = null!;
}
