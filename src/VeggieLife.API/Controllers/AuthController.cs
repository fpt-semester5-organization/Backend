using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using VeggieLife.BLL.DTOs.Auth;
using VeggieLife.BLL.Services.Auth;
using VeggieLife.BLL.Services.Email;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using VeggieLife.BLL.Services.Guest;

namespace VeggieLife.API.Controllers;

/// <summary>
/// Controller xử lý xác thực tài khoản: Khởi tạo phiên khách, Đăng ký, Xác thực OTP và Gửi lại OTP (FN-AUTH-01, FN-AUTH-04, FN-AUTH-05, FN-AUTH-06).
/// </summary>
public class AuthController : BaseApiController
{
    private readonly IAuthService _authService;
    private readonly IGuestService _guestService;
    private readonly IWebHostEnvironment _environment;

    public AuthController(
        IAuthService authService,
        IGuestService guestService,
        IWebHostEnvironment environment)
    {
        _authService = authService;
        _guestService = guestService;
        _environment = environment;
    }

    /// <summary>
    /// FN-AUTH-01: Khởi tạo phiên khách vãng lai (Guest UUID).
    /// Nhận diện thiết bị bằng Cookie guest_uuid, cấp Anonymous JWT hiệu lực 24 giờ.
    /// </summary>
    [HttpPost("anonymous")]
    [AllowAnonymous]
    public async Task<IActionResult> CreateAnonymousSession(CancellationToken ct)
    {
        Request.Cookies.TryGetValue("guest_uuid", out var cookieGuestUuid);
        if (string.IsNullOrEmpty(cookieGuestUuid) && Request.Headers.TryGetValue("X-Guest-UUID", out var headerGuestUuid))
        {
            cookieGuestUuid = headerGuestUuid.ToString().Trim();
        }

        var clientIp = HttpContext.Connection.RemoteIpAddress?.ToString();
        var clientCountry = Request.Headers.TryGetValue("X-Country-Code", out var cVal) ? cVal.ToString() : null;

        var result = await _guestService.CreateAnonymousSessionAsync(cookieGuestUuid, clientIp, clientCountry, ct);

        // Thiết lập Cookie guest_uuid cho Client
        Response.Cookies.Append("guest_uuid", result.GuestId, new CookieOptions
        {
            HttpOnly = false,
            Secure = Request.IsHttps,
            SameSite = SameSiteMode.Lax,
            Expires = DateTimeOffset.UtcNow.AddDays(365)
        });

        return OkResponse(result, "Khởi tạo phiên khách thành công.");
    }

    /// <summary>
    /// Đăng ký tài khoản người dùng mới bằng Email và Mật khẩu (FN-AUTH-04).
    /// </summary>
    /// <param name="request">Thông tin đăng ký gồm Họ tên, Email, Mật khẩu.</param>
    /// <param name="ct">Cancellation Token.</param>
    /// <returns>HTTP 201 Created cùng thông tin hiệu lực mã OTP.</returns>
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request, CancellationToken ct)
    {
        var result = await _authService.RegisterAsync(request, ct);
        return CreatedResponse(result, "Đăng ký thành công. Vui lòng nhập mã OTP đã gửi tới email.");
    }

    /// <summary>
    /// Xác thực mã OTP 6 số để kích hoạt tài khoản và cấp cặp Token đăng nhập (FN-AUTH-05).
    /// </summary>
    /// <param name="request">Email và mã OTP 6 số.</param>
    /// <param name="ct">Cancellation Token.</param>
    /// <returns>HTTP 200 OK kèm Access Token và thông tin tài khoản.</returns>
    [HttpPost("verify-otp")]
    public async Task<IActionResult> VerifyOtp([FromBody] VerifyOtpRequest request, CancellationToken ct)
    {
        // Tự động nhận diện Guest UUID từ Cookie hoặc Header nếu client không truyền trực tiếp trong body
        if (string.IsNullOrWhiteSpace(request.GuestUuid))
        {
            Request.Cookies.TryGetValue("guest_uuid", out var cookieGuestUuid);
            if (string.IsNullOrEmpty(cookieGuestUuid) && Request.Headers.TryGetValue("X-Guest-UUID", out var headerGuestUuid))
            {
                cookieGuestUuid = headerGuestUuid.ToString().Trim();
            }

            if (!string.IsNullOrEmpty(cookieGuestUuid))
            {
                request.GuestUuid = cookieGuestUuid;
            }
        }

        var result = await _authService.VerifyOtpAsync(request, ct);

        // Thiết lập Refresh Token an toàn qua HttpOnly Cookie (BR-AUTH-004)
        if (!string.IsNullOrEmpty(result.RefreshToken))
        {
            Response.Cookies.Append("refreshToken", result.RefreshToken, new CookieOptions
            {
                HttpOnly = true,
                Secure = Request.IsHttps,
                SameSite = SameSiteMode.Strict,
                Expires = DateTimeOffset.UtcNow.AddDays(7)
            });
        }

        return OkResponse(result, "Kích hoạt tài khoản thành công.");
    }

    /// <summary>
    /// Yêu cầu gửi lại mã xác thực OTP (FN-AUTH-06).
    /// Luôn trả về thông báo chung để chống rò rỉ dữ liệu người dùng (User Enumeration).
    /// </summary>
    /// <param name="request">Email và mục đích phát hành mã.</param>
    /// <param name="ct">Cancellation Token.</param>
    /// <returns>HTTP 200 OK.</returns>
    [HttpPost("resend-otp")]
    public async Task<IActionResult> ResendOtp([FromBody] ResendOtpRequest request, CancellationToken ct)
    {
        await _authService.ResendOtpAsync(request, ct);
        return OkResponse<object?>(null, "Nếu email hợp lệ, mã OTP mới đã được gửi.");
    }

    /// <summary>
    /// Endpoint hỗ trợ kiểm thử tự động trong môi trường Development: Lấy mã OTP vừa sinh.
    /// Không kích hoạt trong môi trường Production.
    /// </summary>
    [HttpGet("dev-latest-otp")]
    public IActionResult GetDevLatestOtp([FromQuery] string email)
    {
        if (!_environment.IsDevelopment())
        {
            return NotFound();
        }

        var normalizedEmail = email?.Trim().ToLowerInvariant() ?? string.Empty;
        if (EmailServiceStub.LastSentOtps.TryGetValue(normalizedEmail, out var otp))
        {
            return OkResponse(new { email = normalizedEmail, otp = otp }, "Lấy mã OTP môi trường dev thành công.");
        }

        return NotFound(new { message = "Chưa có mã OTP nào được gửi tới email này trong phiên chạy hiện tại." });
    }
}
