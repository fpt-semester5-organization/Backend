using System.Collections.Generic;

namespace VeggieLife.BLL.DTOs.MealPlans.Responses;

/// <summary>
/// Chi tiết một bữa ăn trong kế hoạch thực đơn.
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
    /// Ghi chú lưu ý riêng cho món ăn này.
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// Thông tin công thức món ăn.
    /// </summary>
    public MealSlotRecipeDto? Recipe { get; set; }

    /// <summary>
    /// Số lần đã đổi món cho bữa ăn này.
    /// </summary>
    public int SwapCount { get; set; }

    /// <summary>
    /// Danh sách toàn bộ lịch sử các món đã đổi trước đó.
    /// </summary>
    public List<MealSlotSwapHistoryDto> SwapHistories { get; set; } = new();
}
