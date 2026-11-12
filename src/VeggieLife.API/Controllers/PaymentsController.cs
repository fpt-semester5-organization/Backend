using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VeggieLife.BLL.Common.Responses;
using VeggieLife.BLL.DTOs.Subscriptions.Requests;
using VeggieLife.BLL.Services.Subscriptions;

namespace VeggieLife.API.Controllers;

/// <summary>
/// Controller tiếp nhận kết quả thanh toán từ các cổng MoMo / VietQR (FN-SUB-03).
/// </summary>
[ApiController]
public class PaymentsController : BaseApiController
{
    private readonly ISubscriptionService _subscriptionService;

    public PaymentsController(ISubscriptionService subscriptionService)
    {
        _subscriptionService = subscriptionService;
    }

    /// <summary>
    /// FN-SUB-03: Nhận kết quả thanh toán (webhook IPN).
    /// Auth: Chữ ký HMAC-SHA256 của cổng (không dùng JWT).
    /// </summary>
    [HttpPost("/api/v1/payments/{gateway}/ipn")]
    [AllowAnonymous]
    public async Task<IActionResult> HandlePaymentIpn(
        [FromRoute] string gateway,
        [FromBody] HandlePaymentIpnRequest request,
        CancellationToken ct)
    {
        await _subscriptionService.HandlePaymentIpnAsync(gateway, request, ct);
        return Ok(new ApiResponse<object?>(200, "OK", null));
    }

    /// <summary>
    /// Webhook nhận thông báo giao dịch chuyển khoản VietQR tự động từ PayOS.
    /// Auth: Chữ ký số HMAC-SHA256 (ChecksumKey của PayOS).
    /// </summary>
    [HttpPost("/api/v1/payments/payos/webhook")]
    [AllowAnonymous]
    public async Task<IActionResult> HandlePayOsWebhook(
        [FromBody] PayOS.Models.Webhooks.Webhook webhook,
        CancellationToken ct)
    {
        await _subscriptionService.HandlePayOsWebhookAsync(webhook, ct);
        return Ok(new ApiResponse<object?>(200, "Webhook processed successfully", null));
    }

    /// <summary>
    /// Giao diện giả lập MoMo Sandbox Gateway phục vụ thử nghiệm và kiểm thử tích hợp (Integration Test).
    /// Người dùng xác nhận thanh toán để gọi Webhook IPN và tự động điều hướng về Frontend.
    /// </summary>
    [HttpGet("/api/v1/payments/momo/sandbox-checkout")]
    [AllowAnonymous]
    public IActionResult MoMoSandboxCheckout(
        [FromQuery] string orderCode,
        [FromQuery] long amount,
        [FromQuery] string? orderInfo,
        [FromQuery] string? returnUrl,
        [FromQuery] string? cancelUrl)
    {
        var safeOrderCode = System.Net.WebUtility.HtmlEncode(orderCode ?? string.Empty);
        var safeOrderInfo = System.Net.WebUtility.HtmlEncode(orderInfo ?? "Thanh toán gói hội viên VeggieLife");
        var safeReturnUrl = !string.IsNullOrWhiteSpace(returnUrl) ? returnUrl : "http://localhost:3000/payment/success";
        var safeCancelUrl = !string.IsNullOrWhiteSpace(cancelUrl) ? cancelUrl : "http://localhost:3000/payment/cancel";

        var html = $@"<!DOCTYPE html>
<html lang=""vi"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>MoMo Sandbox Payment Gateway - VeggieLife</title>
    <style>
        * {{ margin: 0; padding: 0; box-sizing: border-box; font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Helvetica, Arial, sans-serif; }}
        body {{ background: #f4f5f7; display: flex; align-items: center; justify-content: center; min-height: 100vh; padding: 20px; }}
        .card {{ background: #fff; width: 100%; max-width: 440px; border-radius: 16px; box-shadow: 0 10px 25px rgba(0,0,0,0.08); overflow: hidden; }}
        .header {{ background: linear-gradient(135deg, #a50064, #d82d8b); color: #fff; padding: 24px; text-align: center; }}
        .logo {{ width: 56px; height: 56px; background: #fff; border-radius: 12px; display: inline-flex; align-items: center; justify-content: center; margin-bottom: 12px; box-shadow: 0 4px 10px rgba(0,0,0,0.15); }}
        .logo svg {{ width: 36px; height: 36px; }}
        .header h1 {{ font-size: 20px; font-weight: 700; margin-bottom: 4px; }}
        .badge {{ display: inline-block; background: rgba(255,255,255,0.25); color: #fff; font-size: 11px; font-weight: 600; padding: 3px 10px; border-radius: 20px; text-transform: uppercase; letter-spacing: 0.5px; }}
        .content {{ padding: 24px; }}
        .row {{ display: flex; justify-content: space-between; margin-bottom: 14px; font-size: 14px; }}
        .label {{ color: #6b7280; }}
        .value {{ color: #111827; font-weight: 600; text-align: right; word-break: break-all; }}
        .price {{ color: #a50064; font-size: 24px; font-weight: 700; }}
        .divider {{ height: 1px; background: #e5e7eb; margin: 18px 0; }}
        .notice {{ background: #fff1f2; border: 1px solid #fecdd3; border-radius: 8px; padding: 12px; font-size: 13px; color: #9f1239; margin-bottom: 20px; line-height: 1.4; }}
        .btn {{ display: block; width: 100%; padding: 14px; border-radius: 10px; font-size: 15px; font-weight: 600; text-align: center; cursor: pointer; border: none; transition: all 0.2s; }}
        .btn-pay {{ background: #a50064; color: #fff; margin-bottom: 10px; }}
        .btn-pay:hover {{ background: #8c0054; }}
        .btn-cancel {{ background: #f3f4f6; color: #4b5563; text-decoration: none; }}
        .btn-cancel:hover {{ background: #e5e7eb; }}
        .loading {{ display: none; margin-top: 10px; color: #a50064; font-size: 13px; text-align: center; font-weight: 500; }}
    </style>
</head>
<body>
    <div class=""card"">
        <div class=""header"">
            <div class=""logo"">
                <svg viewBox=""0 0 40 40"" fill=""none"">
                    <rect width=""40"" height=""40"" rx=""8"" fill=""#A50064""/>
                    <path d=""M12 28V16C12 13.7909 13.7909 12 16 12C18.2091 12 20 13.7909 20 16V28M20 28V16C20 13.7909 21.7909 12 24 12C26.2091 12 28 13.7909 28 16V28"" stroke=""white"" stroke-width=""3"" stroke-linecap=""round"" stroke-linejoin=""round""/>
                </svg>
            </div>
            <h1>Cổng thanh toán MoMo</h1>
            <span class=""badge"">Môi trường thử nghiệm Sandbox</span>
        </div>
        <div class=""content"">
            <div class=""row"">
                <span class=""label"">Nhà cung cấp</span>
                <span class=""value"">VeggieLife Dinh Dưỡng Chay</span>
            </div>
            <div class=""row"">
                <span class=""label"">Mã đơn hàng</span>
                <span class=""value"">{safeOrderCode}</span>
            </div>
            <div class=""row"">
                <span class=""label"">Nội dung</span>
                <span class=""value"">{safeOrderInfo}</span>
            </div>
            <div class=""divider""></div>
            <div class=""row"" style=""align-items: center;"">
                <span class=""label"">Số tiền thanh toán</span>
                <span class=""value price"">{amount:N0} đ</span>
            </div>
            <button class=""btn btn-pay"" id=""btnPay"" onclick=""processPayment()"">Xác nhận thanh toán (MoMo Sandbox)</button>
            <a class=""btn btn-cancel"" href=""{safeCancelUrl}"">Hủy thanh toán</a>
            <div class=""loading"" id=""loading"">Đang xử lý Webhook và kích hoạt VIP...</div>
        </div>
    </div>

    <script>
        async function processPayment() {{
            const btn = document.getElementById('btnPay');
            const loading = document.getElementById('loading');
            btn.disabled = true;
            btn.style.opacity = '0.6';
            loading.style.display = 'block';

            try {{
                const payload = {{
                    orderCode: '{safeOrderCode}',
                    partnerTransactionId: 'MOMO_SANDBOX_' + Date.now(),
                    amount: {amount},
                    resultCode: 0,
                    signature: 'SANDBOX_MOCK_SIGNATURE'
                }};

                const res = await fetch('/api/v1/payments/momo/ipn', {{
                    method: 'POST',
                    headers: {{ 'Content-Type': 'application/json' }},
                    body: JSON.stringify(payload)
                }});

                if (res.ok) {{
                    window.location.href = '{safeReturnUrl}';
                }} else {{
                    const err = await res.json().catch(() => ({{}}));
                    alert('Lỗi kích hoạt đơn hàng từ Webhook: ' + (err.message || res.statusText));
                    btn.disabled = false;
                    btn.style.opacity = '1';
                    loading.style.display = 'none';
                }}
            }} catch (e) {{
                alert('Không thể kết nối đến máy chủ: ' + e.message);
                btn.disabled = false;
                btn.style.opacity = '1';
                loading.style.display = 'none';
            }}
        }}
    </script>
</body>
</html>";

        return Content(html, "text/html; charset=utf-8");
    }
}
