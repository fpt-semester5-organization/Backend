using System.Threading;
using System.Threading.Tasks;
using VeggieLife.BLL.DTOs.Auth;

namespace VeggieLife.BLL.Services.Auth;

/// <summary>
/// Interface dịch vụ xác thực tài khoản và quản lý vòng đời OTP (Đăng ký, Xác thực, Gửi lại OTP).
/// </summary>
public interface IAuthService
{
    /// <summary>
    /// Đăng ký tài khoản mới bằng Email và Mật khẩu, lưu ở trạng thái PENDING_ACTIVATION và gửi OTP 6 số (FN-AUTH-04).
    /// </summary>
    Task<RegisterResponse> RegisterAsync(RegisterRequest request, CancellationToken ct = default);

    /// <summary>
    /// Xác thực mã OTP để kích hoạt tài khoản sang ACTIVE và cấp cặp Token đăng nhập (FN-AUTH-05).
    /// </summary>
    Task<VerifyOtpResponse> VerifyOtpAsync(VerifyOtpRequest request, CancellationToken ct = default);

    /// <summary>
    /// Gửi lại mã OTP mới có hiệu lực 5 phút (FN-AUTH-06).
    /// </summary>
    Task ResendOtpAsync(ResendOtpRequest request, CancellationToken ct = default);

    /// <summary>
    /// Đăng nhập bằng Email và Mật khẩu, khóa đăng nhập 15 phút sau 5 lần sai liên tiếp, cấp cặp token (FN-AUTH-08).
    /// </summary>
    Task<LoginResponse> LoginAsync(LoginRequest request, string? clientIp = null, string? userAgent = null, CancellationToken ct = default);

    /// <summary>
    /// Làm mới Access Token và xoay vòng Refresh Token; phát hiện dùng lại token bị thu hồi (FN-AUTH-09).
    /// </summary>
    Task<RefreshTokenResponse> RefreshTokenAsync(string? refreshToken, string? clientIp = null, string? userAgent = null, CancellationToken ct = default);

    /// <summary>
    /// Đăng xuất khỏi hệ thống: Thu hồi Refresh Token và đưa jti của Access Token vào danh sách đen Redis (FN-AUTH-10).
    /// </summary>
    Task LogoutAsync(long userId, string? userRole, string? jti, System.DateTime? tokenExpiryUtc, string? refreshToken, CancellationToken ct = default);

    /// <summary>
    /// Quên mật khẩu: gửi mã OTP 6 số qua email của tài khoản ACTIVE (FN-AUTH-11).
    /// </summary>
    Task ForgotPasswordAsync(ForgotPasswordRequest request, CancellationToken ct = default);

    /// <summary>
    /// Đặt lại mật khẩu bằng mã OTP; mở khóa tài khoản và thu hồi toàn bộ phiên đăng nhập cũ (FN-AUTH-12).
    /// </summary>
    Task ResetPasswordAsync(ResetPasswordRequest request, CancellationToken ct = default);

    /// <summary>
    /// Đổi mật khẩu cho người dùng đang đăng nhập; thu hồi các phiên đăng nhập khác của tài khoản (FN-AUTH-13).
    /// </summary>
    Task ChangePasswordAsync(long userId, ChangePasswordRequest request, string? currentRefreshToken = null, CancellationToken ct = default);
}
