using System.Linq;
using VeggieLife.BLL.Common.Constants;
using VeggieLife.BLL.Common.Exceptions;
using VeggieLife.BLL.DTOs.Profile.Requests;
using VeggieLife.DAL.Entities;

namespace VeggieLife.BLL.Validators.Profile;

/// <summary>
/// Bộ kiểm tra hợp lệ dữ liệu yêu cầu vô hiệu hóa tài khoản (FN-PROF-07).
/// </summary>
public static class DeleteCurrentUserValidator
{
    public static void Validate(DeleteCurrentUserRequest request, User user)
    {
        if (request == null)
        {
            throw AppException.BadRequest(ErrorCodes.ErrValidation, "Dữ liệu yêu cầu không được để trống.");
        }

        if (!string.IsNullOrWhiteSpace(request.Reason) && request.Reason.Trim().Length > 255)
        {
            throw AppException.BadRequest(ErrorCodes.ErrValidation, "Lý do vô hiệu hóa không được vượt quá 255 ký tự.");
        }

        // Ưu tiên 1: Nếu người dùng gửi mã OTP xác nhận
        if (!string.IsNullOrWhiteSpace(request.Otp))
        {
            var trimmedOtp = request.Otp.Trim();
            if (trimmedOtp.Length != 6 || !trimmedOtp.All(char.IsDigit))
            {
                throw AppException.BadRequest(
                    ErrorCodes.ErrValidation,
                    "Mã xác thực OTP phải gồm đúng 6 chữ số.");
            }
            return;
        }

        // Ưu tiên 2 (Tương thích ngược): Kiểm tra mật khẩu hoặc Google ID Token
        if (!string.IsNullOrEmpty(user.PasswordHash))
        {
            if (string.IsNullOrWhiteSpace(request.ConfirmPassword))
            {
                throw AppException.BadRequest(
                    ErrorCodes.ErrValidation,
                    "Mã xác thực OTP hoặc Mật khẩu xác nhận không được để trống đối với tài khoản sử dụng mật khẩu.");
            }
        }
        else if (!string.IsNullOrEmpty(user.GoogleId))
        {
            if (string.IsNullOrWhiteSpace(request.GoogleIdToken))
            {
                throw AppException.BadRequest(
                    ErrorCodes.ErrValidation,
                    "Mã xác thực OTP hoặc Google ID Token không được để trống đối với tài khoản chỉ dùng Google.");
            }
        }
        else
        {
            throw AppException.BadRequest(
                ErrorCodes.ErrValidation,
                "Vui lòng cung cấp mã xác thực OTP để hoàn tất vô hiệu hóa tài khoản.");
        }
    }
}
