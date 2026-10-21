using System;

namespace VeggieLife.BLL.DTOs.Admin.Users;

/// <summary>
/// DTO phản hồi sau khi cập nhật trạng thái kỷ luật tài khoản (FN-ADM-02).
/// Khớp chuẩn response data: { userId, accountStatus, lockedUntil, violationCount }
/// </summary>
public class UpdateAdminUserStatusResponseData
{
    public int UserId { get; set; }

    public string AccountStatus { get; set; } = string.Empty;

    public DateTime? LockedUntil { get; set; }

    public int ViolationCount { get; set; }
}
