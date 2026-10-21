using System;

namespace VeggieLife.BLL.DTOs.Admin.Users;

/// <summary>
/// DTO tham số tìm kiếm và lọc danh sách người dùng cho Quản trị viên (FN-ADM-01).
/// </summary>
public class AdminUserListFilterRequest
{
    public string? Keyword { get; set; }

    public string? AccountStatus { get; set; }

    public string? Role { get; set; }

    public int? DietarySchoolId { get; set; }

    public DateTime? CreatedFrom { get; set; }

    public DateTime? CreatedTo { get; set; }

    public int Page { get; set; } = 1;

    public int Size { get; set; } = 20;
}
