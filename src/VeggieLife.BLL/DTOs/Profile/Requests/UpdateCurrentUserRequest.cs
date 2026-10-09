using System.ComponentModel.DataAnnotations;

namespace VeggieLife.BLL.DTOs.Profile.Requests;

/// <summary>
/// Yêu cầu cập nhật thông tin cá nhân tài khoản (FN-PROF-02).
/// </summary>
public class UpdateCurrentUserRequest
{
    /// <summary>
    /// Họ và tên (2–100 ký tự).
    /// </summary>
    [StringLength(100, MinimumLength = 2, ErrorMessage = "Họ tên phải từ 2 đến 100 ký tự.")]
    public string? FullName { get; set; }

    /// <summary>
    /// Số điện thoại di động Việt Nam 10 chữ số: ^0(3|5|7|8|9)[0-9]{8}$
    /// </summary>
    [RegularExpression(@"^0(3|5|7|8|9)[0-9]{8}$", ErrorMessage = "Số điện thoại không đúng định dạng số di động Việt Nam.")]
    public string? PhoneNumber { get; set; }
}
