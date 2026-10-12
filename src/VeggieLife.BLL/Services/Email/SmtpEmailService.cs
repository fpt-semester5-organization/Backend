using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using VeggieLife.BLL.Common.Constants;
using VeggieLife.BLL.Common.Exceptions;
using VeggieLife.DAL.External.Email;

namespace VeggieLife.BLL.Services.Email;

/// <summary>
/// Triển khai dịch vụ gửi thư điện tử thật qua máy chủ SMTP (Simple Mail Transfer Protocol).
/// Tạo mẫu email HTML chuẩn hóa, responsive, bảo mật cao cho mã xác thực OTP và thông báo hệ thống.
/// Đồng thời duy trì bộ nhớ đệm LastSentOtps để hỗ trợ môi trường Development và kiểm thử tự động.
/// </summary>
public class SmtpEmailService : IEmailService
{
    private readonly ISmtpEmailClient _smtpClient;
    private readonly ILogger<SmtpEmailService> _logger;

    public SmtpEmailService(
        ISmtpEmailClient smtpClient,
        ILogger<SmtpEmailService> logger)
    {
        _smtpClient = smtpClient;
        _logger = logger;
    }

    /// <summary>
    /// Gửi mã xác thực OTP 6 số (kích hoạt tài khoản hoặc đặt lại mật khẩu) qua Email SMTP với giao diện HTML chuẩn.
    /// </summary>
    public async Task SendOtpEmailAsync(string toEmail, string otpCode, string purpose, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(toEmail))
        {
            throw AppException.BadRequest(ErrorCodes.ErrValidation, "Địa chỉ email người nhận không được để trống.");
        }

        var normalizedEmail = toEmail.Trim().ToLowerInvariant();

        // Ghi nhận OTP vào bộ nhớ đệm phục vụ kiểm thử song song
        EmailServiceStub.LastSentOtps[normalizedEmail] = otpCode;

        var isEmailVerify = string.Equals(purpose, OtpPurposes.EmailVerify, StringComparison.OrdinalIgnoreCase);
        var isPasswordReset = string.Equals(purpose, "PASSWORD_RESET", StringComparison.OrdinalIgnoreCase);

        string subject;
        string actionTitle;
        string actionDescription;

        if (isEmailVerify)
        {
            subject = "[VeggieLife] Mã OTP kích hoạt tài khoản của bạn";
            actionTitle = "Kích hoạt tài khoản VeggieLife";
            actionDescription = "Cảm ơn bạn đã tham gia cộng đồng ăn chay VeggieLife! Vui lòng nhập mã OTP bên dưới để hoàn tất việc xác thực địa chỉ email và kích hoạt tài khoản của bạn.";
        }
        else if (isPasswordReset)
        {
            subject = "[VeggieLife] Mã OTP đặt lại mật khẩu của bạn";
            actionTitle = "Yêu cầu đặt lại mật khẩu";
            actionDescription = "Chúng tôi nhận được yêu cầu đặt lại mật khẩu cho tài khoản VeggieLife của bạn. Vui lòng nhập mã OTP bên dưới để tạo mật khẩu đăng nhập mới.";
        }
        else
        {
            subject = "[VeggieLife] Mã xác thực OTP bảo mật";
            actionTitle = "Xác thực bảo mật tài khoản";
            actionDescription = "Vui lòng nhập mã OTP bên dưới để xác nhận thao tác bảo mật trên tài khoản VeggieLife của bạn.";
        }

        var htmlBody = BuildOtpEmailHtml(actionTitle, actionDescription, otpCode);
        var plainTextBody = BuildOtpEmailPlainText(actionTitle, otpCode);

        try
        {
            await _smtpClient.SendEmailAsync(normalizedEmail, subject, htmlBody, plainTextBody, ct);
            _logger.LogInformation(
                "[SmtpEmailService] Đã xử lý gửi mã OTP '{Purpose}' tới {ToEmail} thành công.",
                purpose, normalizedEmail);
        }
        catch (SmtpEmailException ex)
        {
            _logger.LogError(ex,
                "[SmtpEmailService] Gặp sự cố kết nối SMTP khi gửi OTP tới {ToEmail}: {Message}",
                normalizedEmail, ex.Message);

            // Nếu máy chủ SMTP đã được cấu hình thật mà gửi thất bại, báo lỗi 502 Bad Gateway
            if (_smtpClient.IsConfigured)
            {
                throw AppException.BadGateway(
                    ErrorCodes.ErrExtEmailFailed,
                    "Không thể gửi email xác thực OTP qua dịch vụ SMTP. Vui lòng kiểm tra lại kết nối mạng hoặc thử lại sau.");
            }
        }
    }

    /// <summary>
    /// Gửi email thông báo chung (kích hoạt thành công, biên lai thanh toán, thông báo kiểm duyệt).
    /// </summary>
    public async Task SendNotificationEmailAsync(string toEmail, string subject, string htmlBody, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(toEmail))
        {
            throw AppException.BadRequest(ErrorCodes.ErrValidation, "Địa chỉ email người nhận không được để trống.");
        }

        var normalizedEmail = toEmail.Trim().ToLowerInvariant();
        var wrappedHtml = WrapInBrandedTemplate(subject, htmlBody);

        try
        {
            await _smtpClient.SendEmailAsync(normalizedEmail, subject, wrappedHtml, null, ct);
            _logger.LogInformation(
                "[SmtpEmailService] Đã gửi email thông báo '{Subject}' tới {ToEmail}.",
                subject, normalizedEmail);
        }
        catch (SmtpEmailException ex)
        {
            _logger.LogError(ex,
                "[SmtpEmailService] Gặp sự cố kết nối SMTP khi gửi thông báo tới {ToEmail}: {Message}",
                normalizedEmail, ex.Message);

            if (_smtpClient.IsConfigured)
            {
                throw AppException.BadGateway(
                    ErrorCodes.ErrExtEmailFailed,
                    "Không thể gửi email thông báo qua máy chủ SMTP. Vui lòng thử lại sau.");
            }
        }
    }

    /// <summary>
    /// Xây dựng khuôn mẫu HTML responsive hiện đại cho email chứa mã OTP.
    /// </summary>
    private static string BuildOtpEmailHtml(string actionTitle, string actionDescription, string otpCode)
    {
        return $@"<!DOCTYPE html>
<html lang=""vi"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>{actionTitle}</title>
    <style>
        body {{
            font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, 'Helvetica Neue', Arial, sans-serif;
            background-color: #f4f6f8;
            margin: 0;
            padding: 24px;
            color: #333333;
        }}
        .container {{
            max-width: 560px;
            margin: 0 auto;
            background-color: #ffffff;
            border-radius: 12px;
            overflow: hidden;
            box-shadow: 0 4px 16px rgba(0, 0, 0, 0.08);
            border: 1px solid #e0e0e0;
        }}
        .header {{
            background: linear-gradient(135deg, #2E7D32 0%, #1B5E20 100%);
            color: #ffffff;
            padding: 32px 24px;
            text-align: center;
        }}
        .brand {{
            font-size: 26px;
            font-weight: 700;
            letter-spacing: 1px;
            margin: 0;
            display: inline-block;
        }}
        .brand-subtitle {{
            font-size: 13px;
            color: #c8e6c9;
            margin-top: 6px;
            letter-spacing: 0.5px;
        }}
        .content {{
            padding: 32px 28px;
        }}
        .title {{
            font-size: 20px;
            font-weight: 600;
            color: #1b5e20;
            margin-top: 0;
            margin-bottom: 12px;
        }}
        .description {{
            font-size: 15px;
            line-height: 1.6;
            color: #555555;
            margin-bottom: 24px;
        }}
        .otp-box {{
            background-color: #e8f5e9;
            border: 2px dashed #81c784;
            border-radius: 10px;
            padding: 20px;
            text-align: center;
            margin: 24px 0;
        }}
        .otp-label {{
            font-size: 12px;
            font-weight: 600;
            color: #2e7d32;
            text-transform: uppercase;
            letter-spacing: 1.5px;
            margin-bottom: 8px;
        }}
        .otp-code {{
            font-family: 'SFMono-Regular', Consolas, 'Liberation Mono', Menlo, Courier, monospace;
            font-size: 36px;
            font-weight: 800;
            letter-spacing: 8px;
            color: #1b5e20;
            margin: 0;
            padding-left: 8px;
        }}
        .notice {{
            background-color: #fff8e1;
            border-left: 4px solid #ffb300;
            padding: 12px 16px;
            border-radius: 4px;
            font-size: 13px;
            color: #6d4c41;
            line-height: 1.5;
            margin-bottom: 20px;
        }}
        .footer {{
            background-color: #fafafa;
            border-top: 1px solid #eeeeee;
            padding: 20px 24px;
            text-align: center;
            font-size: 12px;
            color: #888888;
            line-height: 1.5;
        }}
    </style>
</head>
<body>
    <div class=""container"">
        <div class=""header"">
            <h1 class=""brand"">VeggieLife</h1>
            <div class=""brand-subtitle"">Nền tảng Thực dưỡng &amp; Món chay Thông minh</div>
        </div>
        <div class=""content"">
            <h2 class=""title"">{actionTitle}</h2>
            <p class=""description"">{actionDescription}</p>
            <div class=""otp-box"">
                <div class=""otp-label"">Mã xác thực của bạn</div>
                <div class=""otp-code"">{otpCode}</div>
            </div>
            <div class=""notice"">
                <strong>Hiệu lực:</strong> Mã xác thực có hiệu lực trong vòng <strong>5 phút (300 giây)</strong> kể từ thời điểm gửi.<br>
                <strong>Bảo mật:</strong> Tuyệt đối không cung cấp mã OTP này cho bất kỳ ai. Nhân viên VeggieLife sẽ không bao giờ yêu cầu bạn cung cấp mã xác thực.
            </div>
            <p class=""description"" style=""font-size: 13px; color: #777777; margin-bottom: 0;"">
                Nếu bạn không gửi yêu cầu này, vui lòng bỏ qua email hoặc thông báo cho bộ phận hỗ trợ của chúng tôi để được trợ giúp.
            </p>
        </div>
        <div class=""footer"">
            Email này được gửi tự động từ hệ thống VeggieLife.<br>
            &copy; 2026 VeggieLife Team. Mọi quyền được bảo lưu.
        </div>
    </div>
</body>
</html>";
    }

    /// <summary>
    /// Xây dựng nội dung văn bản thuần (plain text) dự phòng cho các ứng dụng không tải HTML.
    /// </summary>
    private static string BuildOtpEmailPlainText(string actionTitle, string otpCode)
    {
        var sb = new StringBuilder();
        sb.AppendLine("=== [VeggieLife] ===");
        sb.AppendLine(actionTitle);
        sb.AppendLine();
        sb.AppendLine($"MÃ XÁC THỰC OTP CỦA BẠN: {otpCode}");
        sb.AppendLine();
        sb.AppendLine("- Thời hạn sử dụng: 5 phút (300 giây).");
        sb.AppendLine("- Tuyệt đối không chia sẻ mã này cho bất kỳ ai.");
        sb.AppendLine("- Nếu bạn không thực hiện yêu cầu này, vui lòng bỏ qua email.");
        sb.AppendLine();
        sb.AppendLine("© 2026 VeggieLife System.");
        return sb.ToString();
    }

    /// <summary>
    /// Bọc nội dung thông báo chung vào khung giao diện thương hiệu VeggieLife.
    /// </summary>
    private static string WrapInBrandedTemplate(string title, string bodyContent)
    {
        return $@"<!DOCTYPE html>
<html lang=""vi"">
<head>
    <meta charset=""UTF-8"">
    <title>{title}</title>
    <style>
        body {{
            font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif;
            background-color: #f4f6f8;
            margin: 0;
            padding: 24px;
            color: #333333;
        }}
        .container {{
            max-width: 560px;
            margin: 0 auto;
            background-color: #ffffff;
            border-radius: 12px;
            overflow: hidden;
            box-shadow: 0 4px 16px rgba(0, 0, 0, 0.08);
            border: 1px solid #e0e0e0;
        }}
        .header {{
            background: linear-gradient(135deg, #2E7D32 0%, #1B5E20 100%);
            color: #ffffff;
            padding: 24px;
            text-align: center;
        }}
        .content {{
            padding: 28px;
            line-height: 1.6;
        }}
        .footer {{
            background-color: #fafafa;
            border-top: 1px solid #eeeeee;
            padding: 16px;
            text-align: center;
            font-size: 12px;
            color: #888888;
        }}
    </style>
</head>
<body>
    <div class=""container"">
        <div class=""header"">
            <h2 style=""margin:0; font-size: 22px;"">🌿 VeggieLife</h2>
        </div>
        <div class=""content"">
            {bodyContent}
        </div>
        <div class=""footer"">
            &copy; 2026 VeggieLife Team. Mọi quyền được bảo lưu.
        </div>
    </div>
</body>
</html>";
    }
}
