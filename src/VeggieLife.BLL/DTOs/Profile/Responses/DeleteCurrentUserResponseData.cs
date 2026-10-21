using System;

namespace VeggieLife.BLL.DTOs.Profile.Responses;

/// <summary>
/// DTO dữ liệu trả về khi người dùng vô hiệu hóa tài khoản thành công (FN-PROF-07).
/// </summary>
public class DeleteCurrentUserResponseData
{
    /// <summary>
    /// Hạn chót khôi phục tài khoản (sau 30 ngày kể từ thời điểm gửi yêu cầu).
    /// </summary>
    public DateTime RestoreDeadline { get; set; }
}
