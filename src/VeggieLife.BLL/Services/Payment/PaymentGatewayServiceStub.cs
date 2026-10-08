using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace VeggieLife.BLL.Services.Payment;

/// <summary>
/// Triển khai bản rỗng (Stub/Mock) của IPaymentGatewayService để thành viên khác hoàn thiện cổng MoMo / VietQR.
/// </summary>
public class PaymentGatewayServiceStub : IPaymentGatewayService
{
    private readonly ILogger<PaymentGatewayServiceStub> _logger;

    public PaymentGatewayServiceStub(ILogger<PaymentGatewayServiceStub> logger)
    {
        _logger = logger;
    }

    public Task<PaymentCheckoutResult> CreateMoMoPaymentAsync(
        long transactionId, 
        decimal amountVnd, 
        string orderInfo, 
        string returnUrl, 
        CancellationToken ct = default)
    {
        _logger.LogInformation("[PaymentGatewayServiceStub] Tạo yêu cầu thanh toán MoMo cho giao dịch #{TransactionId}, số tiền: {Amount:N0} VND", transactionId, amountVnd);
        // TODO: Gọi MoMo All-in-one payment gateway API
        var result = new PaymentCheckoutResult
        {
            PartnerTransactionId = $"MOMO_{DateTime.UtcNow.Ticks}",
            PaymentUrl = $"https://test-payment.momo.vn/v2/gateway/pay?orderId={transactionId}",
            QrCodeUrl = $"https://api.qrserver.com/v1/create-qr-code/?size=250x250&data=momo_demo_{transactionId}"
        };

        return Task.FromResult(result);
    }

    public Task<PaymentCheckoutResult> CreateVietQrPaymentAsync(
        long transactionId, 
        decimal amountVnd, 
        string orderInfo, 
        CancellationToken ct = default)
    {
        _logger.LogInformation("[PaymentGatewayServiceStub] Tạo yêu cầu VietQR động cho giao dịch #{TransactionId}, số tiền: {Amount:N0} VND", transactionId, amountVnd);
        // TODO: Sinh VietQR theo chuẩn Napas 247
        var result = new PaymentCheckoutResult
        {
            PartnerTransactionId = $"VQR_{transactionId}_{DateTime.UtcNow.Ticks}",
            PaymentUrl = string.Empty,
            QrCodeUrl = $"https://img.vietqr.io/image/MB-0987654321-compact2.png?amount={amountVnd}&addInfo={Uri.EscapeDataString(orderInfo)}"
        };

        return Task.FromResult(result);
    }

    public bool VerifySignature(IDictionary<string, string> requestData, string secureHash)
    {
        _logger.LogInformation("[PaymentGatewayServiceStub] Kiểm tra chữ ký số webhook IPN");
        // TODO: Tính toán HMAC-SHA256 theo secret key cấu hình
        return true;
    }
}
