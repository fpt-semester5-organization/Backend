using System.Collections.Generic;

namespace VeggieLife.BLL.DTOs.Profile.Responses;

/// <summary>
/// Thông tin rút gọn về chất dị ứng của người dùng.
/// </summary>
public class AllergenBriefDto
{
    public int AllergenId { get; set; }
    public string Name { get; set; } = string.Empty;
}

/// <summary>
/// Dữ liệu phản hồi khai báo dị ứng (FN-PROF-05).
/// </summary>
public class ReplaceCurrentUserAllergiesResponse
{
    public List<AllergenBriefDto> Allergens { get; set; } = new();
}
