using System;

namespace VeggieLife.BLL.DTOs.Admin.Users;

/// <summary>
/// DTO thông tin người dùng trong danh sách tra cứu của Quản trị viên (FN-ADM-01).
/// </summary>
public class AdminUserListItemDto
{
    public int UserId { get; set; }

    public string Email { get; set; } = string.Empty;

    public string FullName { get; set; } = string.Empty;

    public string Role { get; set; } = string.Empty;

    public string AccountStatus { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    public int? DietarySchoolId { get; set; }

    public string? DietarySchoolName { get; set; }

    public DateTime? LockedUntil { get; set; }

    public int CountViolationReports { get; set; }
}
