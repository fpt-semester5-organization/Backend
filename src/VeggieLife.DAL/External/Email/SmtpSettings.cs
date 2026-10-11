using System;
using System.Collections.Generic;

namespace VeggieLife.DAL.External.Email;

/// <summary>
/// Cấu hình chi tiết cho một nhà cung cấp dịch vụ gửi thư điện tử SMTP (SendGrid, Gmail, Mailtrap, v.v.).
/// </summary>
public class EmailProviderConfig
{
    /// <summary>
    /// Tên đại diện nhà cung cấp (ví dụ: SendGrid, Gmail, Mailtrap).
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Địa chỉ máy chủ SMTP (ví dụ: smtp.sendgrid.net, smtp.gmail.com, sandbox.smtp.mailtrap.io).
    /// </summary>
    public string Host { get; set; } = string.Empty;

    /// <summary>
    /// Cổng kết nối SMTP (thường là 587 cho STARTTLS hoặc 465 cho SSL).
    /// </summary>
    public int Port { get; set; } = 587;

    /// <summary>
    /// Tên đăng nhập SMTP (với SendGrid luôn là "apikey", với Gmail là địa chỉ email của bạn).
    /// </summary>
    public string User { get; set; } = string.Empty;

    /// <summary>
    /// Mật khẩu hoặc API Key bí mật. Tuyệt đối không hardcode giá trị thật.
    /// </summary>
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// Địa chỉ email người gửi hiển thị (From address). Nếu để trống sẽ sử dụng User.
    /// </summary>
    public string From { get; set; } = string.Empty;

    /// <summary>
    /// Tên đại diện người gửi hiển thị (Display Name).
    /// </summary>
    public string DisplayName { get; set; } = "VeggieLife";

    /// <summary>
    /// Kích hoạt chế độ mã hóa SSL/TLS khi kết nối.
    /// </summary>
    public bool EnableSsl { get; set; } = true;

    /// <summary>
    /// Kiểm tra nhà cung cấp này đã được điền đủ thông tin kết nối hay chưa.
    /// </summary>
    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(Host) &&
        Port > 0 &&
        !string.IsNullOrWhiteSpace(User) &&
        !string.IsNullOrWhiteSpace(Password);
}

/// <summary>
/// Cấu hình trung tâm cho dịch vụ gửi thư điện tử hỗ trợ đa nhà cung cấp (SendGrid, Gmail, Mailtrap)
/// kèm cơ chế tự động chuyển đổi dự phòng (Automatic Failover Chain) tăng tính sẵn sàng cho hệ thống.
/// </summary>
public class SmtpSettings
{
    public const string SectionName = "Smtp";

    /// <summary>
    /// Chế độ hoạt động: 
    /// - "Auto": Tự động thử lần lượt các nhà cung cấp có sẵn (SendGrid ➔ Gmail ➔ Mailtrap).
    /// - "SendGrid": Chỉ sử dụng SendGrid.
    /// - "Gmail": Chỉ sử dụng Gmail.
    /// - "Mailtrap": Chỉ sử dụng Mailtrap.
    /// </summary>
    public string ActiveProvider { get; set; } = "Auto";

    /// <summary>
    /// Cấu hình SendGrid SMTP (100 email/ngày miễn phí).
    /// </summary>
    public EmailProviderConfig SendGrid { get; set; } = new()
    {
        Name = "SendGrid",
        Host = "smtp.sendgrid.net",
        Port = 587,
        User = "apikey",
        EnableSsl = true
    };

    /// <summary>
    /// Cấu hình Gmail SMTP (kết nối qua App Password 16 ký tự).
    /// </summary>
    public EmailProviderConfig Gmail { get; set; } = new()
    {
        Name = "Gmail",
        Host = "smtp.gmail.com",
        Port = 587,
        EnableSsl = true
    };

    /// <summary>
    /// Cấu hình Mailtrap SMTP (dành cho kiểm thử phát triển hộp thư ảo).
    /// </summary>
    public EmailProviderConfig Mailtrap { get; set; } = new()
    {
        Name = "Mailtrap",
        Host = "sandbox.smtp.mailtrap.io",
        Port = 587,
        EnableSsl = true
    };

    // --- Các thuộc tính tương thích ngược (Flat properties cho cấu hình đơn lẻ) ---
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; } = 587;
    public string User { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string From { get; set; } = string.Empty;
    public string DisplayName { get; set; } = "VeggieLife";
    public bool EnableSsl { get; set; } = true;

    /// <summary>
    /// Lấy danh sách các nhà cung cấp email đã cấu hình theo mức độ ưu tiên xác định.
    /// </summary>
    public IReadOnlyList<EmailProviderConfig> GetConfiguredProviders()
    {
        var list = new List<EmailProviderConfig>();

        if (string.IsNullOrWhiteSpace(SendGrid.Name)) SendGrid.Name = "SendGrid";
        if (string.IsNullOrWhiteSpace(Gmail.Name)) Gmail.Name = "Gmail";
        if (string.IsNullOrWhiteSpace(Mailtrap.Name)) Mailtrap.Name = "Mailtrap";

        if (string.Equals(ActiveProvider, "SendGrid", StringComparison.OrdinalIgnoreCase))
        {
            if (SendGrid.IsConfigured) list.Add(SendGrid);
            return list;
        }

        if (string.Equals(ActiveProvider, "Gmail", StringComparison.OrdinalIgnoreCase))
        {
            if (Gmail.IsConfigured) list.Add(Gmail);
            return list;
        }

        if (string.Equals(ActiveProvider, "Mailtrap", StringComparison.OrdinalIgnoreCase))
        {
            if (Mailtrap.IsConfigured) list.Add(Mailtrap);
            return list;
        }

        // Chế độ Auto / Failover: Ưu tiên SendGrid ➔ Gmail ➔ Mailtrap
        if (SendGrid.IsConfigured) list.Add(SendGrid);
        if (Gmail.IsConfigured) list.Add(Gmail);
        if (Mailtrap.IsConfigured) list.Add(Mailtrap);

        // Fallback tương thích cấu hình đơn lẻ
        if (list.Count == 0 && !string.IsNullOrWhiteSpace(Host) && Port > 0 && !string.IsNullOrWhiteSpace(User) && !string.IsNullOrWhiteSpace(Password))
        {
            list.Add(new EmailProviderConfig
            {
                Name = "DefaultSmtp",
                Host = Host,
                Port = Port,
                User = User,
                Password = Password,
                From = From,
                DisplayName = DisplayName,
                EnableSsl = EnableSsl
            });
        }

        return list;
    }

    /// <summary>
    /// Kiểm tra xem có ít nhất một nhà cung cấp SMTP sẵn sàng hoạt động hay không.
    /// </summary>
    public bool IsConfigured => GetConfiguredProviders().Count > 0;
}
