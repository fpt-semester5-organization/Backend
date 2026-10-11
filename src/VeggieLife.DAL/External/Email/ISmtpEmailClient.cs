using System.Threading;
using System.Threading.Tasks;

namespace VeggieLife.DAL.External.Email;

/// <summary>
/// Hợp đồng giao tiếp hạ tầng gửi thư điện tử qua máy chủ SMTP.
/// </summary>
public interface ISmtpEmailClient
{
    /// <summary>
    /// Cho biết dịch vụ SMTP đã được cấu hình đầy đủ thông tin xác thực hay chưa.
    /// </summary>
    bool IsConfigured { get; }

    /// <summary>
    /// Gửi thư điện tử qua giao thức SMTP.
    /// </summary>
    /// <param name="toEmail">Địa chỉ email người nhận.</param>
    /// <param name="subject">Tiêu đề email.</param>
    /// <param name="htmlBody">Nội dung email định dạng HTML.</param>
    /// <param name="plainTextBody">Nội dung văn bản thuần dự phòng (plain text) cho các ứng dụng đọc mail không hỗ trợ HTML.</param>
    /// <param name="ct">Mã hủy tác vụ bất đồng bộ.</param>
    Task SendEmailAsync(
        string toEmail, 
        string subject, 
        string htmlBody, 
        string? plainTextBody = null, 
        CancellationToken ct = default);
}
