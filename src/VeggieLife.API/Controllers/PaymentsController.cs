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
    [HttpPost("/payments/{gateway}/ipn")]
    [AllowAnonymous]
    public async Task<IActionResult> HandlePaymentIpn(
        [FromRoute] string gateway,
        [FromBody] HandlePaymentIpnRequest request,
        CancellationToken ct)
    {
        await _subscriptionService.HandlePaymentIpnAsync(gateway, request, ct);
        return Ok(new ApiResponse<object?>(200, "OK", null));
    }
}
