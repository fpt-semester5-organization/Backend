namespace VeggieLife.BLL.DTOs.Admin.Users;

/// <summary>
/// DTO tham số lọc danh sách nội dung và vi phạm của người dùng (FN-ADM-03).
/// </summary>
public class AdminUserContentsFilterRequest
{
    /// <summary>
    /// Loại nội dung tùy chọn: POST | RECIPE | VIDEO | COMMENT
    /// </summary>
    public string? Type { get; set; }

    public int Page { get; set; } = 1;

    public int Size { get; set; } = 20;
}
