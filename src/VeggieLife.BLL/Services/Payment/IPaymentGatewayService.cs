using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace VeggieLife.BLL.Services.Payment;

/// <summary>
/// Kết quả sinh liên kết thanh toán từ cổng trung gian.
/// </summary>
public class PaymentCheckoutResult
{
    public string PaymentUrl { get; set; } = string.Empty;
    public string QrCodeUrl { get; set; } = string.Empty;
    public string PartnerTransactionId { get; set; } = string.Empty;
}

/// <summary>
/// Interface dịch vụ tích hợp cổng thanh toán số (MoMo, VietQR) theo BR-PAY-TXN-01.
/// </summary>
public interface IPaymentGatewayService
{
    /// <summary>
    /// Khởi tạo yêu cầu thanh toán qua cổng MoMo.
    /// </summary>
    Task<PaymentCheckoutResult> CreateMoMoPaymentAsync(
        long transactionId,
        decimal amountVnd,
        string orderInfo,
        string returnUrl,
        CancellationToken ct = default);

    /// <summary>
    /// Khởi tạo yêu cầu thanh toán VietQR động kèm mã giao dịch đối soát.
    /// </summary>
    Task<PaymentCheckoutResult> CreateVietQrPaymentAsync(
        long transactionId,
        decimal amountVnd,
        string orderInfo,
        CancellationToken ct = default);

    /// <summary>
    /// Xác thực chữ ký số HMAC-SHA256 trên webhook IPN từ cổng thanh toán.
    /// </summary>
    bool VerifySignature(IDictionary<string, string> requestData, string secureHash);

    /// <summary>
    /// Xác thực chữ ký số HMAC-SHA256 theo từng cổng thanh toán cụ thể (MoMo, VietQR).
    /// </summary>
    bool VerifySignature(string gateway, IDictionary<string, string> requestData, string secureHash);

    /// <summary>
    /// Tạo chữ ký số HMAC-SHA256 từ dữ liệu yêu cầu IPN theo khóa bí mật của cổng.
    /// </summary>
    string CreateSignature(string gateway, IDictionary<string, string> requestData);
}
