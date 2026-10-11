using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using VeggieLife.BLL.Common.Constants;
using VeggieLife.BLL.DTOs.Auth;
using VeggieLife.BLL.Services.Auth;
using VeggieLife.BLL.Services.Email;
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

        var clientIp = ResolveClientIp();
        var clientCountry = Request.Headers.TryGetValue("X-Country-Code", out var cVal) 
            ? cVal.ToString() 
            : (Request.Headers.TryGetValue("CF-IPCountry", out var cfVal) ? cfVal.ToString() : null);

        var result = await _guestService.CreateAnonymousSessionAsync(cookieGuestUuid, clientIp, clientCountry, ct);

        // Thiết lập Cookie guest_uuid cho Client (HttpOnly, Secure, SameSite=Strict - FN-AUTH-01)
        Response.Cookies.Append("guest_uuid", result.GuestId, new CookieOptions
        {
            HttpOnly = true,
            Secure = Request.IsHttps || !_environment.IsDevelopment(),
            SameSite = SameSiteMode.Strict,
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
        SetRefreshTokenCookie(result.RefreshToken);

        return OkResponse(result, "Kích hoạt tài khoản thành công.");
    }

    /// <summary>
    /// FN-AUTH-08: Đăng nhập bằng Email và Mật khẩu.
    /// Khóa đăng nhập 15 phút sau 5 lần sai liên tiếp, cấp cặp Token đăng nhập (Access Token 15 phút, Refresh Token 7 ngày).
    /// </summary>
    /// <param name="request">Thông tin đăng nhập gồm email, password, restoreAccount, guestUuid.</param>
    /// <param name="ct">Cancellation Token.</param>
    /// <returns>HTTP 200 OK kèm Access Token và thông tin tài khoản.</returns>
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken ct)
    {
        var clientIp = HttpContext.Connection.RemoteIpAddress?.ToString();
        var userAgent = Request.Headers.UserAgent.ToString();

        // Nhận diện GuestUuid từ request hoặc cookie nếu có để hợp nhất ngữ cảnh (BR-GUEST-AUTH-01)
        if (string.IsNullOrEmpty(request.GuestUuid))
        {
            if (Request.Cookies.TryGetValue("guest_uuid", out var cookieGuestUuid))
            {
                request.GuestUuid = cookieGuestUuid;
            }
            else if (Request.Headers.TryGetValue("X-Guest-UUID", out var headerGuestUuid))
            {
                request.GuestUuid = headerGuestUuid.ToString().Trim();
            }
        }

        var result = await _authService.LoginAsync(request, clientIp, userAgent, ct);

        // Thiết lập Cookie HttpOnly cho Refresh Token (BR-AUTH-004)
        SetRefreshTokenCookie(result.RefreshToken);

        return OkResponse(result, "Đăng nhập thành công.");
    }

    /// <summary>
    /// FN-AUTH-09: Làm mới Access Token và xoay vòng Refresh Token (Token Rotation).
    /// Phát hiện Refresh Token đã thu hồi bị dùng lại thì hủy toàn bộ phiên của tài khoản.
    /// </summary>
    /// <param name="ct">Cancellation Token.</param>
    /// <returns>HTTP 200 OK kèm Access Token mới và hiệu lực 900 giây.</returns>
    [HttpPost("refresh-token")]
    [AllowAnonymous]
    public async Task<IActionResult> RefreshToken(CancellationToken ct)
    {
        Request.Cookies.TryGetValue("refresh_token", out var refreshToken);
        if (string.IsNullOrEmpty(refreshToken) && Request.Headers.TryGetValue("X-Refresh-Token", out var headerToken))
        {
            refreshToken = headerToken.ToString().Trim();
        }

        var clientIp = HttpContext.Connection.RemoteIpAddress?.ToString();
        var userAgent = Request.Headers.UserAgent.ToString();

        var result = await _authService.RefreshTokenAsync(refreshToken, clientIp, userAgent, ct);

        // Cập nhật lại Cookie HttpOnly với Refresh Token mới xoay vòng
        SetRefreshTokenCookie(result.RefreshToken);

        return OkResponse(result, "Làm mới phiên thành công.");
    }

    /// <summary>
    /// FN-AUTH-10: Đăng xuất khỏi hệ thống.
    /// Thu hồi Refresh Token hiện tại và đưa jti của Access Token vào danh sách đen Redis đến khi hết hạn.
    /// </summary>
    /// <param name="ct">Cancellation Token.</param>
    /// <returns>HTTP 200 OK.</returns>
    [HttpPost("logout")]
    [Authorize(Policy = PolicyNames.RequireMember)]
    public async Task<IActionResult> Logout(CancellationToken ct)
    {
        Request.Cookies.TryGetValue("refresh_token", out var refreshToken);
        if (string.IsNullOrEmpty(refreshToken) && Request.Headers.TryGetValue("X-Refresh-Token", out var headerToken))
        {
            refreshToken = headerToken.ToString().Trim();
        }

        var jti = User.FindFirstValue(JwtRegisteredClaimNames.Jti)
                  ?? User.FindFirstValue("jti")
                  ?? User.FindFirstValue("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/jwtid");

        DateTime? expiryUtc = null;
        var expClaim = User.FindFirstValue("exp");
        if (long.TryParse(expClaim, out var expSeconds))
        {
            expiryUtc = DateTimeOffset.FromUnixTimeSeconds(expSeconds).UtcDateTime;
        }

        await _authService.LogoutAsync(CurrentUserId ?? 0, CurrentUserRole, jti, expiryUtc, refreshToken, ct);

        // Xóa Cookie refresh_token trên client
        DeleteRefreshTokenCookie();

        return OkResponse<object?>(null, "Đăng xuất thành công.");
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

    private void SetRefreshTokenCookie(string refreshToken)
    {
        if (string.IsNullOrEmpty(refreshToken)) return;

        Response.Cookies.Append("refresh_token", refreshToken, new CookieOptions
        {
            HttpOnly = true,
            Secure = Request.IsHttps,
            SameSite = SameSiteMode.Strict,
            Path = "/",
            Expires = DateTimeOffset.UtcNow.AddDays(7)
        });
    }

    private void DeleteRefreshTokenCookie()
    {
        var cookieOptions = new CookieOptions
        {
            HttpOnly = true,
            Secure = Request.IsHttps,
            SameSite = SameSiteMode.Strict,
            Path = "/"
        };

        Response.Cookies.Delete("refresh_token", cookieOptions);
        Response.Cookies.Delete("refreshToken", cookieOptions);
    }
}
