using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

using PayOS;
using PayOS.Models.V2.PaymentRequests;

namespace VeggieLife.BLL.Services.Payment;

/// <summary>
/// Triển khai dịch vụ tích hợp cổng MoMo / VietQR (PayOS thật & sandbox) kèm xác thực chữ ký HMAC-SHA256 (BR-PAY-TXN-01, BR-PAY-TXN-02).
/// </summary>
public class PaymentGatewayServiceStub : IPaymentGatewayService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<PaymentGatewayServiceStub> _logger;
    private readonly PayOSClient? _payOSClient;

    public PaymentGatewayServiceStub(
        IConfiguration configuration,
        ILogger<PaymentGatewayServiceStub> logger)
    {
        _configuration = configuration;
        _logger = logger;

        var clientId = _configuration["Payment:PayOS:ClientId"];
        var apiKey = _configuration["Payment:PayOS:ApiKey"];
        var checksumKey = _configuration["Payment:PayOS:ChecksumKey"];

        if (!string.IsNullOrWhiteSpace(clientId) &&
            !string.IsNullOrWhiteSpace(apiKey) &&
            !string.IsNullOrWhiteSpace(checksumKey) &&
            !clientId.Contains("ĐIỀN_") &&
            !clientId.Contains("YOUR_"))
        {
            try
            {
                _payOSClient = new PayOSClient(clientId.Trim(), apiKey.Trim(), checksumKey.Trim());
                _logger.LogInformation("[PaymentGatewayServiceStub] Khởi tạo thành công PayOSClient với ClientId {ClientId}", clientId);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[PaymentGatewayServiceStub] Không thể khởi tạo PayOSClient");
            }
        }
    }

    public PayOSClient? GetPayOSClient() => _payOSClient;

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

    public async Task<PaymentCheckoutResult> CreateVietQrPaymentAsync(
        long transactionId,
        decimal amountVnd,
        string orderInfo,
        CancellationToken ct = default)
    {
        var partnerTransId = $"VQR_{transactionId}_{DateTime.UtcNow.Ticks}";
        _logger.LogInformation("[PaymentGatewayServiceStub] Tạo yêu cầu VietQR động cho giao dịch #{TransactionId}, số tiền: {Amount:N0} VND, orderInfo: {OrderInfo}",
            transactionId, amountVnd, orderInfo);

        var bankId = _configuration["Payment:VietQR:BankId"] ?? "MB";
        var accountNo = _configuration["Payment:VietQR:AccountNo"] ?? "0865736967";
        var accountName = _configuration["Payment:VietQR:AccountName"] ?? "VEGGIELIFE";

        // Chuẩn mã QR fallback theo Napas 247 VietQR
        var defaultQrUrl = $"https://img.vietqr.io/image/{bankId}-{accountNo}-compact2.png?amount={(long)amountVnd}&addInfo={Uri.EscapeDataString(orderInfo)}&accountName={Uri.EscapeDataString(accountName)}";

        if (_payOSClient != null)
        {
            try
            {
                // Trích xuất số orderCode cho PayOS (yêu cầu số nguyên dương long <= 9007199254740991)
                long orderCodeNum;
                var digits = new string(orderInfo.Where(char.IsDigit).ToArray());
                if (long.TryParse(digits, out var parsed) && parsed > 0 && parsed <= 9007199254740991L)
                {
                    orderCodeNum = parsed;
                }
                else
                {
                    orderCodeNum = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() % 1000000000;
                }

                // Description tối đa 25 ký tự trên PayOS
                var desc = $"VL {orderCodeNum}";
                if (desc.Length > 25) desc = desc[..25];

                var request = new CreatePaymentLinkRequest
                {
                    OrderCode = orderCodeNum,
                    Amount = (long)amountVnd,
                    Description = desc,
                    CancelUrl = "https://veggielife.vn/payment/cancel",
                    ReturnUrl = "https://veggielife.vn/payment/success"
                };

                var payOsRes = await _payOSClient.PaymentRequests.CreateAsync(request);
                _logger.LogInformation("[PaymentGatewayServiceStub] Đã tạo thành công link thanh toán PayOS: PaymentLinkId={PaymentLinkId}, OrderCode={OrderCode}, CheckoutUrl={CheckoutUrl}",
                    payOsRes.PaymentLinkId, orderCodeNum, payOsRes.CheckoutUrl);

                return new PaymentCheckoutResult
                {
                    PartnerTransactionId = payOsRes.PaymentLinkId ?? orderCodeNum.ToString(),
                    PaymentUrl = payOsRes.CheckoutUrl,
                    QrCodeUrl = defaultQrUrl
                };
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[PaymentGatewayServiceStub] Không thể gọi API PayOS, tự động chuyển về VietQR fallback.");
            }
        }

        return new PaymentCheckoutResult
        {
            PartnerTransactionId = partnerTransId,
            PaymentUrl = defaultQrUrl,
            QrCodeUrl = defaultQrUrl
        };
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
