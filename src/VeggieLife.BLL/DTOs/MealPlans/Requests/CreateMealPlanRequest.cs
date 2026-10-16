using System.ComponentModel.DataAnnotations;

namespace VeggieLife.BLL.DTOs.MealPlans.Requests;

/// <summary>
/// Yêu cầu sinh thực đơn 7 ngày bằng AI (FN-MEAL-01).
/// </summary>
public class CreateMealPlanRequest
{
    /// <summary>
    /// Ngày bắt đầu thực đơn (YYYY-MM-DD), bắt buộc phải là ngày Thứ Hai và không ở quá khứ.
    /// </summary>
    [Required(ErrorMessage = "Ngày bắt đầu là bắt buộc.")]
    public string StartDate { get; set; } = null!;

    /// <summary>
    /// Mức ngân sách: ECONOMICAL | STANDARD | PREMIUM (D72).
    /// </summary>
    [Required(ErrorMessage = "Mức ngân sách là bắt buộc.")]
    public string BudgetTier { get; set; } = "STANDARD";

    /// <summary>
    /// Ưu tiên món dùng nguyên liệu trong tủ lạnh ảo (mặc định: false).
    /// </summary>
    public bool UseFridge { get; set; } = false;

    /// <summary>
    /// Ghi chú tùy chỉnh tự do cho AI, tối đa 200 ký tự (BR-USR-MEAL-CI-01, D88).
    /// </summary>
    [MaxLength(200, ErrorMessage = "Ghi chú tùy chỉnh tối đa 200 ký tự.")]
    public string? CustomInstructions { get; set; }
}
