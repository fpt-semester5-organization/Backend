using System.ComponentModel.DataAnnotations;

namespace VeggieLife.BLL.DTOs.Subscriptions.Requests;

/// <summary>
/// Yêu cầu Webhook IPN từ cổng thanh toán MoMo / VietQR (FN-SUB-03).
/// </summary>
public class HandlePaymentIpnRequest
{
    /// <summary>
    /// Mã đơn hàng đối soát của hệ thống (<= 100 ký tự).
    /// </summary>
    [Required(ErrorMessage = "orderCode là bắt buộc.")]
    [MaxLength(100, ErrorMessage = "orderCode tối đa 100 ký tự.")]
    public string OrderCode { get; set; } = string.Empty;

    /// <summary>
    /// Mã giao dịch của cổng thanh toán (<= 100 ký tự).
    /// </summary>
    [Required(ErrorMessage = "partnerTransactionId là bắt buộc.")]
    [MaxLength(100, ErrorMessage = "partnerTransactionId tối đa 100 ký tự.")]
    public string PartnerTransactionId { get; set; } = string.Empty;

    /// <summary>
    /// Số tiền thanh toán thực tế (> 0, phải khớp AmountVND).
    /// </summary>
    [Required(ErrorMessage = "amount là bắt buộc.")]
    [Range(1, long.MaxValue, ErrorMessage = "amount phải lớn hơn 0.")]
    public long Amount { get; set; }

    /// <summary>
    /// Mã kết quả thanh toán từ cổng (0 = thành công).
    /// </summary>
    [Required(ErrorMessage = "resultCode là bắt buộc.")]
    public int ResultCode { get; set; }

    /// <summary>
    /// Chữ ký số HMAC-SHA256 (<= 256 ký tự).
    /// </summary>
    [Required(ErrorMessage = "signature là bắt buộc.")]
    [MaxLength(256, ErrorMessage = "signature tối đa 256 ký tự.")]
    public string Signature { get; set; } = string.Empty;
}
