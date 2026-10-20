using System.ComponentModel.DataAnnotations;

namespace VeggieLife.BLL.DTOs.Admin.Recipes;

/// <summary>
/// Yêu cầu xác minh gắn huy hiệu công thức chuẩn chay (FN-ADM-09).
/// </summary>
public class VerifyAdminRecipeRequest
{
    /// <summary>
    /// Ghi chú của quản trị viên khi cấp huy hiệu (tối đa 500 ký tự).
    /// </summary>
    [StringLength(500, ErrorMessage = "Ghi chú tối đa 500 ký tự.")]
    public string? Note { get; set; }
}
