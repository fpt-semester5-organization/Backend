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

        // Bắt buộc nhập mật khẩu xác nhận đối với tài khoản sử dụng mật khẩu
        if (!string.IsNullOrEmpty(user.PasswordHash))
        {
            if (string.IsNullOrWhiteSpace(request.ConfirmPassword))
            {
                throw AppException.BadRequest(
                    ErrorCodes.ErrValidation,
                    "Mật khẩu xác nhận không được để trống đối với tài khoản sử dụng mật khẩu.");
            }
        }
        else if (!string.IsNullOrEmpty(user.GoogleId))
        {
            // Bắt buộc nhập Google ID Token đối với tài khoản chỉ liên kết Google
            if (string.IsNullOrWhiteSpace(request.GoogleIdToken))
            {
                throw AppException.BadRequest(
                    ErrorCodes.ErrValidation,
                    "Google ID Token không được để trống đối với tài khoản chỉ dùng Google.");
            }
        }
    }
}
