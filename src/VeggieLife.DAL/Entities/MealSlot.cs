using System;
using System.Collections.Generic;

namespace VeggieLife.DAL.Entities;

public partial class MealSlot
{
    public int SlotId { get; set; }

    public int PlanId { get; set; }

    public int RecipeId { get; set; }

    public string DayOfWeek { get; set; } = null!;

    public string MealType { get; set; } = null!;

    public bool IsCompleted { get; set; }

    public string? Notes { get; set; }

    public virtual MealPlan Plan { get; set; } = null!;

    public virtual Recipe Recipe { get; set; } = null!;
}
