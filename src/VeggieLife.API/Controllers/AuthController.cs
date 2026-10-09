using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using VeggieLife.BLL.DTOs.Auth;
using VeggieLife.BLL.Services.Auth;
using VeggieLife.BLL.Services.Email;

namespace VeggieLife.API.Controllers;

/// <summary>
/// Controller xử lý xác thực tài khoản: Đăng ký, Xác thực OTP và Gửi lại OTP (FN-AUTH-04, FN-AUTH-05, FN-AUTH-06).
/// </summary>
public class AuthController : BaseApiController
{
    private readonly IAuthService _authService;
    private readonly IWebHostEnvironment _environment;

    public AuthController(IAuthService _authService, IWebHostEnvironment environment)
    {
        this._authService = _authService;
        _environment = environment;
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
        var result = await _authService.VerifyOtpAsync(request, ct);
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
