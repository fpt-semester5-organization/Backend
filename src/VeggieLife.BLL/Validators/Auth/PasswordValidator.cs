using System.Linq;

namespace VeggieLife.BLL.Validators.Auth;

/// <summary>
/// Bộ kiểm tra quy tắc an toàn mật khẩu theo chuẩn BR-AUTH-001 và SEC-1:
/// Độ dài tối thiểu 8 ký tự, bao gồm ít nhất 01 chữ cái viết hoa, 01 chữ cái viết thường, 01 chữ số và 01 ký tự đặc biệt.
/// </summary>
public static class PasswordValidator
{
    public static (bool IsValid, string? ErrorMessage) Validate(string? password)
    {
        if (string.IsNullOrWhiteSpace(password))
        {
            return (false, "Mật khẩu không được để trống.");
        }

        if (password.Length < 8)
        {
            return (false, "Mật khẩu phải có độ dài tối thiểu 8 ký tự.");
        }

        if (!password.Any(char.IsUpper))
        {
            return (false, "Mật khẩu phải chứa ít nhất 01 chữ cái viết hoa (A-Z).");
        }

        if (!password.Any(char.IsLower))
        {
            return (false, "Mật khẩu phải chứa ít nhất 01 chữ cái viết thường (a-z).");
        }

        if (!password.Any(char.IsDigit))
        {
            return (false, "Mật khẩu phải chứa ít nhất 01 chữ số (0-9).");
        }

        if (!password.Any(ch => !char.IsLetterOrDigit(ch)))
        {
            return (false, "Mật khẩu phải chứa ít nhất 01 ký tự đặc biệt (ví dụ: !@#$%^&*...).");
        }

        return (true, null);
    }
}
