using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace VeggieLife.BLL.DTOs.Profile.Requests;

/// <summary>
/// Phần tử khai báo dị ứng cá nhân.
/// </summary>
public class UserAllergyItemRequest
{
    [Range(1, int.MaxValue, ErrorMessage = "Mã chất dị ứng không hợp lệ.")]
    public int AllergenId { get; set; }

    [StringLength(255, ErrorMessage = "Ghi chú dị ứng tối đa 255 ký tự.")]
    public string? Notes { get; set; }
}

/// <summary>
/// Yêu cầu thay thế toàn bộ danh sách dị ứng của người dùng (FN-PROF-05).
/// </summary>
public class ReplaceCurrentUserAllergiesRequest
{
    /// <summary>
    /// Danh sách các chất dị ứng (tối đa 20 mục).
    /// </summary>
    [Required(ErrorMessage = "Danh sách dị ứng là bắt buộc.")]
    [MaxLength(20, ErrorMessage = "Danh sách dị ứng tối đa 20 mục.")]
    public List<UserAllergyItemRequest> Allergens { get; set; } = new();
}
