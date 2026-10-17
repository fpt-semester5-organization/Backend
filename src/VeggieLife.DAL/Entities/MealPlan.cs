using System;
using System.Collections.Generic;

namespace VeggieLife.DAL.Entities;

public partial class MealPlan
{
    public int PlanId { get; set; }

    public int UserId { get; set; }

    public int DietarySchoolId { get; set; }

    public string PlanName { get; set; } = null!;

    public DateOnly StartDate { get; set; }

    public DateOnly EndDate { get; set; }

    public decimal TargetDailyKcal { get; set; }

    public string BudgetTier { get; set; } = null!;

    public string? CustomInstructions { get; set; }

    public string PlanStatus { get; set; } = null!;

    public bool GeneratedByAi { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual DietarySchool DietarySchool { get; set; } = null!;

    public virtual ICollection<MealSlot> MealSlots { get; set; } = new List<MealSlot>();

    public virtual ICollection<ShoppingList> ShoppingLists { get; set; } = new List<ShoppingList>();

    public virtual User User { get; set; } = null!;
}
