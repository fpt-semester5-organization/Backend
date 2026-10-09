using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace VeggieLife.BLL.Services.Email;

/// <summary>
/// Triển khai bản rỗng (Stub/Mock) của IEmailService để thành viên khác hoàn thiện gửi mail (SMTP / SendGrid / Resend).
/// </summary>
public class EmailServiceStub : IEmailService
{
    private readonly ILogger<EmailServiceStub> _logger;

    public EmailServiceStub(ILogger<EmailServiceStub> logger)
    {
        _logger = logger;
    }

    public Task SendOtpEmailAsync(string toEmail, string otpCode, string purpose, CancellationToken ct = default)
    {
        _logger.LogInformation("[EmailServiceStub] Gửi mã OTP '{OtpCode}' mục đích '{Purpose}' tới {ToEmail}", otpCode, purpose, toEmail);
        // TODO: Cấu hình FluentEmail / MailKit / SendGrid để gửi email thực tế
        return Task.CompletedTask;
    }

    public Task SendNotificationEmailAsync(string toEmail, string subject, string htmlBody, CancellationToken ct = default)
    {
        _logger.LogInformation("[EmailServiceStub] Gửi email thông báo '{Subject}' tới {ToEmail}", subject, toEmail);
        // TODO: Gửi email thực tế
        return Task.CompletedTask;
    }
}
