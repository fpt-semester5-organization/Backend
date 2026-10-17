namespace VeggieLife.BLL.DTOs.MealPlans.Responses;

/// <summary>
/// Dữ liệu phản hồi đánh dấu bữa ăn đã ăn / chưa ăn (FN-MEAL-04).
/// </summary>
public class PatchMealPlanSlotResponseData
{
    public int SlotId { get; set; }
    public bool IsCompleted { get; set; }
    public int Completed { get; set; }
    public int Total { get; set; }
}
