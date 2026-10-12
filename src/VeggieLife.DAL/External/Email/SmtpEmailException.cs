using System;

namespace VeggieLife.DAL.External.Email;

/// <summary>
/// Ngoại lệ phát sinh trong quá trình kết nối hoặc truyền tải thư điện tử qua máy chủ SMTP tại tầng DAL.
/// Được tầng BLL bắt và chuyển đổi thành AppException với mã lỗi 502 (ERR_EXT_EMAIL_FAILED).
/// </summary>
public class SmtpEmailException : Exception
{
    public SmtpEmailException(string message) : base(message)
    {
    }

    public SmtpEmailException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
