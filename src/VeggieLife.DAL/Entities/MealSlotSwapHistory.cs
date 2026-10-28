using System;

namespace VeggieLife.DAL.Entities;

/// <summary>
/// Thực thể lưu vết lịch sử đổi món của từng bữa ăn trong kế hoạch thực đơn.
/// </summary>
public partial class MealSlotSwapHistory
{
    public int SwapHistoryId { get; set; }

    public int SlotId { get; set; }

    public int OldRecipeId { get; set; }

    public int NewRecipeId { get; set; }

    public string? SwapReason { get; set; }

    public decimal CaloriesDelta { get; set; }

    public DateTime SwappedAt { get; set; }

    public virtual MealSlot Slot { get; set; } = null!;

    public virtual Recipe OldRecipe { get; set; } = null!;

    public virtual Recipe NewRecipe { get; set; } = null!;
}
