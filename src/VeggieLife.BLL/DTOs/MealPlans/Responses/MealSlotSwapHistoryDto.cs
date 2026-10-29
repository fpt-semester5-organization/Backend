using System;

namespace VeggieLife.BLL.DTOs.MealPlans.Responses;

/// <summary>
/// DTO thông tin lịch sử đổi món của một bữa ăn (FN-MEAL-03).
/// </summary>
public class MealSlotSwapHistoryDto
{
    public int HistoryId { get; set; }

    public int SlotId { get; set; }

    public int OldRecipeId { get; set; }

    public string OldRecipeTitle { get; set; } = string.Empty;

    public string? OldRecipeImageUrl { get; set; }

    public decimal OldRecipeCalories { get; set; }

    public MealSlotRecipeDto? OldRecipe { get; set; }

    public int NewRecipeId { get; set; }

    public string NewRecipeTitle { get; set; } = string.Empty;

    public string? NewRecipeImageUrl { get; set; }

    public decimal NewRecipeCalories { get; set; }

    public MealSlotRecipeDto? NewRecipe { get; set; }

    public string? SwapReason { get; set; }

    public decimal CaloriesDelta { get; set; }

    public DateTime SwappedAt { get; set; }
}
