using System.ComponentModel.DataAnnotations;

namespace VeggieLife.BLL.DTOs.MealPlans.Requests;

/// <summary>
/// Yêu cầu đánh dấu trạng thái bữa ăn trong thực đơn (FN-MEAL-04).
/// </summary>
public class PatchMealPlanSlotRequest
{
    /// <summary>
    /// Đã hoàn thành bữa ăn (true) hoặc chưa hoàn thành (false).
    /// </summary>
    [Required]
    public bool IsCompleted { get; set; }
}
