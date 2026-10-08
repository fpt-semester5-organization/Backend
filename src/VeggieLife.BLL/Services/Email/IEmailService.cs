using System.Threading;
using System.Threading.Tasks;

namespace VeggieLife.BLL.Services.Email;

/// <summary>
/// Interface dịch vụ gửi Email (OTP, xác thực tài khoản, thông báo hệ thống).
/// </summary>
public interface IEmailService
{
    /// <summary>
    /// Gửi mã xác thực OTP 6 số để kích hoạt tài khoản hoặc đặt lại mật khẩu.
    /// </summary>
    Task SendOtpEmailAsync(string toEmail, string otpCode, string purpose, CancellationToken ct = default);

    /// <summary>
    /// Gửi email thông báo kích hoạt thành công hoặc biên lai giao dịch.
    /// </summary>
    Task SendNotificationEmailAsync(string toEmail, string subject, string htmlBody, CancellationToken ct = default);
}
