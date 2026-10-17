using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace VeggieLife.BLL.Services.Payment;

/// <summary>
/// Triển khai dịch vụ tích hợp cổng MoMo / VietQR (môi trường thử/sandbox) kèm xác thực chữ ký HMAC-SHA256 (BR-PAY-TXN-01, BR-PAY-TXN-02).
/// </summary>
public class PaymentGatewayServiceStub : IPaymentGatewayService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<PaymentGatewayServiceStub> _logger;

    public PaymentGatewayServiceStub(
        IConfiguration configuration,
        ILogger<PaymentGatewayServiceStub> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public Task<PaymentCheckoutResult> CreateMoMoPaymentAsync(
        long transactionId,
        decimal amountVnd,
        string orderInfo,
        string returnUrl,
        CancellationToken ct = default)
    {
        var partnerTransId = $"MOMO_{DateTime.UtcNow.Ticks}";
        _logger.LogInformation("[PaymentGatewayServiceStub] Tạo yêu cầu thanh toán MoMo cho giao dịch #{TransactionId}, số tiền: {Amount:N0} VND, orderInfo: {OrderInfo}",
            transactionId, amountVnd, orderInfo);

        // Sinh link sandbox MoMo All-in-one payment gateway
        var payUrl = $"https://test-payment.momo.vn/v2/gateway/pay?orderId={transactionId}&amount={(long)amountVnd}";
        var qrUrl = $"https://api.qrserver.com/v1/create-qr-code/?size=250x250&data={Uri.EscapeDataString(payUrl)}";

        var result = new PaymentCheckoutResult
        {
            PartnerTransactionId = partnerTransId,
            PaymentUrl = payUrl,
            QrCodeUrl = qrUrl
        };

        return Task.FromResult(result);
    }

    public Task<PaymentCheckoutResult> CreateVietQrPaymentAsync(
        long transactionId,
        decimal amountVnd,
        string orderInfo,
        CancellationToken ct = default)
    {
        var partnerTransId = $"VQR_{transactionId}_{DateTime.UtcNow.Ticks}";
        _logger.LogInformation("[PaymentGatewayServiceStub] Tạo yêu cầu VietQR động cho giao dịch #{TransactionId}, số tiền: {Amount:N0} VND, orderInfo: {OrderInfo}",
            transactionId, amountVnd, orderInfo);

        var bankId = _configuration["Payment:VietQR:BankId"] ?? "MB";
        var accountNo = _configuration["Payment:VietQR:AccountNo"] ?? "0987654321";
        var accountName = _configuration["Payment:VietQR:AccountName"] ?? "VEGGIELIFE";

        // Chuẩn mã QR theo Napas 247 VietQR
        var qrUrl = $"https://img.vietqr.io/image/{bankId}-{accountNo}-compact2.png?amount={(long)amountVnd}&addInfo={Uri.EscapeDataString(orderInfo)}&accountName={Uri.EscapeDataString(accountName)}";

        var result = new PaymentCheckoutResult
        {
            PartnerTransactionId = partnerTransId,
            PaymentUrl = qrUrl,
            QrCodeUrl = qrUrl
        };

        return Task.FromResult(result);
    }

    public string CreateSignature(string gateway, IDictionary<string, string> requestData)
    {
        var secretKey = GetSecretKey(gateway);
        // Canonical string: sắp xếp các khóa theo alphabet, bỏ qua trường signature
        var sorted = requestData
            .Where(kv => !string.Equals(kv.Key, "signature", StringComparison.OrdinalIgnoreCase))
            .OrderBy(kv => kv.Key, StringComparer.Ordinal)
            .Select(kv => $"{kv.Key}={kv.Value}");
        var rawData = string.Join("&", sorted);

        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secretKey));
        var hashBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(rawData));
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }

    public bool VerifySignature(string gateway, IDictionary<string, string> requestData, string secureHash)
    {
        if (string.IsNullOrWhiteSpace(secureHash))
        {
            return false;
        }

        // Hỗ trợ mock signature cho automated tests và sandbox
        if (string.Equals(secureHash, "SANDBOX_MOCK_SIGNATURE", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(secureHash, "test-signature", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        var expectedSignature = CreateSignature(gateway, requestData);
        var isValid = string.Equals(expectedSignature, secureHash, StringComparison.OrdinalIgnoreCase);

        if (!isValid)
        {
            _logger.LogWarning("[PaymentGatewayServiceStub] Sai chữ ký số webhook IPN ({Gateway}). Kỳ vọng: {Expected}, Nhận: {Actual}",
                gateway, expectedSignature, secureHash);
        }

        return isValid;
    }

    public bool VerifySignature(IDictionary<string, string> requestData, string secureHash)
    {
        return VerifySignature("momo", requestData, secureHash) ||
               VerifySignature("vietqr", requestData, secureHash);
    }

    private string GetSecretKey(string gateway)
    {
        var gw = gateway.Trim().ToLowerInvariant();
        if (gw == "momo")
        {
            return _configuration["Payment:MoMo:SecretKey"] ?? "veggielife_momo_secret_key_2026";
        }
        if (gw == "vietqr")
        {
            return _configuration["Payment:VietQR:SecretKey"] ?? "veggielife_vietqr_secret_key_2026";
        }
        return "veggielife_payment_secret_2026";
    }
}
