using System.Collections.Generic;

namespace VeggieLife.BLL.DTOs.Profile.Responses;

/// <summary>
/// Dữ liệu hồ sơ người dùng đầy đủ kèm chỉ số sức khỏe và danh sách dị ứng (FN-PROF-01).
/// </summary>
public class CurrentUserProfileResponse
{
    public int UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public string? AvatarUrl { get; set; }
    public string Role { get; set; } = string.Empty;
    public bool IsVip { get; set; }
    public HealthProfileDto? Health { get; set; }
    public List<AllergenBriefDto> Allergens { get; set; } = new();
}
