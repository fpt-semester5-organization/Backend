using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace VeggieLife.BLL.Services.Email;

/// <summary>
/// Triển khai dịch vụ gửi Email mẫu (phục vụ môi trường Development và kiểm thử tự động).
/// Ghi log chi tiết nội dung mã OTP và lưu lại trong bộ nhớ để phục vụ kiểm thử.
/// </summary>
public class EmailServiceStub : IEmailService
{
    private readonly ILogger<EmailServiceStub> _logger;

    /// <summary>
    /// Lưu trữ mã OTP gửi gần nhất theo địa chỉ Email để tiện kiểm thử môi trường Dev.
    /// </summary>
    public static readonly ConcurrentDictionary<string, string> LastSentOtps = new();

    public EmailServiceStub(ILogger<EmailServiceStub> logger)
    {
        _logger = logger;
    }

    public Task SendOtpEmailAsync(string toEmail, string otpCode, string purpose, CancellationToken ct = default)
    {
        LastSentOtps[toEmail.Trim().ToLowerInvariant()] = otpCode;

        _logger.LogInformation("==========================================================================================");
        _logger.LogInformation("[EMAIL SERVICE DEV] >>> ĐÃ GỬI TỚI: {ToEmail} | MÃ OTP: {OtpCode} | MỤC ĐÍCH: {Purpose} <<<", toEmail, otpCode, purpose);
        _logger.LogInformation("==========================================================================================");

        return Task.CompletedTask;
    }

    public Task SendNotificationEmailAsync(string toEmail, string subject, string htmlBody, CancellationToken ct = default)
    {
        _logger.LogInformation("[EmailServiceStub] Gửi email thông báo '{Subject}' tới {ToEmail}", subject, toEmail);
        return Task.CompletedTask;
    }
}
