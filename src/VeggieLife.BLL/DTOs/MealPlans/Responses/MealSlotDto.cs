namespace VeggieLife.BLL.DTOs.MealPlans.Responses;

/// <summary>
/// Chi tiết một bữa ăn trong kế hoạch thực đơn 7 ngày (21 bữa).
/// </summary>
public class MealSlotDto
{
    public int SlotId { get; set; }

    /// <summary>
    /// Thứ trong tuần: MON, TUE, WED, THU, FRI, SAT, SUN.
    /// </summary>
    public string DayOfWeek { get; set; } = null!;

    /// <summary>
    /// Loại bữa ăn: BREAKFAST, LUNCH, DINNER, SNACK.
    /// </summary>
    public string MealType { get; set; } = null!;

    /// <summary>
    /// Đã hoàn thành bữa ăn hay chưa.
    /// </summary>
    public bool IsCompleted { get; set; }

    /// <summary>
    /// Thông tin công thức món ăn.
    /// </summary>
    public MealSlotRecipeDto? Recipe { get; set; }
}
