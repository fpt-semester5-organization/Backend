using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace VeggieLife.DAL.External.Email;

/// <summary>
/// Triển khai client giao tiếp SMTP thực tế sử dụng System.Net.Mail.SmtpClient.
/// Hỗ trợ gửi thư đa nhà cung cấp (SendGrid, Gmail, Mailtrap) kèm cơ chế tự động chuyển đổi dự phòng (Automatic Failover Chain).
/// Hỗ trợ gửi email HTML đa phương tiện kèm văn bản thuần dự phòng (RFC 2046 MIME multipart/alternative).
/// Tự động bắt lỗi mạng, timeout và chuyển đổi thành SmtpEmailException để tầng BLL xử lý.
/// </summary>
public class SmtpEmailClient : ISmtpEmailClient
{
    private readonly SmtpSettings _settings;
    private readonly ILogger<SmtpEmailClient> _logger;

    public SmtpEmailClient(
        IOptions<SmtpSettings> options,
        ILogger<SmtpEmailClient> logger)
    {
        _settings = options.Value ?? new SmtpSettings();
        _logger = logger;
    }

    /// <summary>
    /// Cho biết có ít nhất một dịch vụ SMTP đã được cấu hình đầy đủ thông tin hay chưa.
    /// </summary>
    public bool IsConfigured => _settings.IsConfigured;

    /// <summary>
    /// Gửi thư điện tử qua giao thức mạng SMTP với chuỗi nhà cung cấp tự động chuyển đổi khi gặp sự cố.
    /// </summary>
    public async Task SendEmailAsync(
        string toEmail, 
        string subject, 
        string htmlBody, 
        string? plainTextBody = null, 
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(toEmail))
        {
            throw new ArgumentException("Địa chỉ email người nhận không được để trống.", nameof(toEmail));
        }

        var providers = _settings.GetConfiguredProviders();

        // Nếu môi trường Local/Dev chưa thiết lập bất kỳ tài khoản SMTP thật nào, ghi log cảnh báo và bỏ qua gửi vật lý
        if (providers.Count == 0)
        {
            _logger.LogWarning(
                "[SmtpEmailClient] Chưa có nhà cung cấp SMTP nào được cấu hình (SendGrid/Gmail/Mailtrap đều thiếu thông tin). " +
                "Email tới '{ToEmail}' với tiêu đề '{Subject}' không thể gửi vật lý. " +
                "Vui lòng thiết lập cấu hình section 'Smtp' trong appsettings.json để kích hoạt.",
                toEmail, subject);
            return;
        }

        var failedAttempts = new List<string>();
        Exception? lastException = null;

        foreach (var provider in providers)
        {
            try
            {
                _logger.LogInformation(
                    "[SmtpEmailClient] Đang gửi email tới '{ToEmail}' qua nhà cung cấp '{ProviderName}' ({Host}:{Port})...",
                    toEmail, provider.Name, provider.Host, provider.Port);

                await SendViaProviderAsync(provider, toEmail, subject, htmlBody, plainTextBody, ct);

                _logger.LogInformation(
                    "[SmtpEmailClient] Đã gửi thành công email tới '{ToEmail}' qua nhà cung cấp '{ProviderName}'!",
                    toEmail, provider.Name);

                return; // Thành công! Hoàn tất tác vụ, không cần chuyển sang dự phòng
            }
            catch (Exception ex)
            {
                lastException = ex;
                var reason = ex is SmtpException se ? $"{se.Message} (Status: {se.StatusCode})" : ex.Message;
                failedAttempts.Add($"{provider.Name}: {reason}");

                _logger.LogWarning(ex,
                    "[SmtpEmailClient] Gửi email qua nhà cung cấp '{ProviderName}' ({Host}:{Port}) thất bại: {Message}. " +
                    "Đang tự động chuyển sang nhà cung cấp dự phòng tiếp theo trong danh sách...",
                    provider.Name, provider.Host, provider.Port, ex.Message);
            }
        }

        // Toàn bộ nhà cung cấp trong chuỗi đều thất bại
        var allErrors = string.Join("; ", failedAttempts);
        _logger.LogError(lastException,
            "[SmtpEmailClient] Toàn bộ {Count} nhà cung cấp SMTP ({Providers}) đều gửi email thất bại tới '{ToEmail}': {AllErrors}",
            providers.Count, string.Join(", ", providers.Select(p => p.Name)), toEmail, allErrors);

        throw new SmtpEmailException(
            $"Toàn bộ nhà cung cấp email ({string.Join(", ", providers.Select(p => p.Name))}) đều gửi thất bại: {allErrors}",
            lastException!);
    }

    /// <summary>
    /// Thực hiện gửi thư qua một cấu hình SMTP cụ thể.
    /// </summary>
    private static async Task SendViaProviderAsync(
        EmailProviderConfig provider,
        string toEmail,
        string subject,
        string htmlBody,
        string? plainTextBody,
        CancellationToken ct)
    {
        using var client = new SmtpClient(provider.Host, provider.Port)
        {
            EnableSsl = provider.EnableSsl,
            UseDefaultCredentials = false,
            Credentials = new NetworkCredential(provider.User, provider.Password),
            DeliveryMethod = SmtpDeliveryMethod.Network,
            Timeout = 15000 // 15 giây timeout cho mỗi nhà cung cấp
        };

        var fromAddress = !string.IsNullOrWhiteSpace(provider.From)
            ? provider.From
            : (string.Equals(provider.Name, "Resend", StringComparison.OrdinalIgnoreCase)
                ? "onboarding@resend.dev"
                : (string.Equals(provider.Name, "SendGrid", StringComparison.OrdinalIgnoreCase)
                    ? "noreply@veggielife.vn"
                    : provider.User));
        var fromDisplayName = string.IsNullOrWhiteSpace(provider.DisplayName) ? "VeggieLife" : provider.DisplayName;

        using var message = new MailMessage
        {
            From = new MailAddress(fromAddress, fromDisplayName, Encoding.UTF8),
            Subject = subject,
            SubjectEncoding = Encoding.UTF8,
            BodyEncoding = Encoding.UTF8,
            IsBodyHtml = true
        };

        message.To.Add(new MailAddress(toEmail.Trim()));

        if (!string.IsNullOrWhiteSpace(plainTextBody))
        {
            var plainView = AlternateView.CreateAlternateViewFromString(plainTextBody, Encoding.UTF8, "text/plain");
            var htmlView = AlternateView.CreateAlternateViewFromString(htmlBody, Encoding.UTF8, "text/html");

            message.AlternateViews.Add(plainView);
            message.AlternateViews.Add(htmlView);
        }
        else
        {
            message.Body = htmlBody;
        }

        await client.SendMailAsync(message, ct);
    }
}
