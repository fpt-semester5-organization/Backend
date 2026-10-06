using System;
using System.Collections.Generic;

namespace VeggieLife.DAL.Entities;

public partial class ShoppingList
{
    public int ListId { get; set; }

    public int PlanId { get; set; }

    public string ListTitle { get; set; } = null!;

    public DateOnly TargetShoppingDate { get; set; }

    public string Status { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public virtual MealPlan Plan { get; set; } = null!;

    public virtual ICollection<ShoppingListItem> ShoppingListItems { get; set; } = new List<ShoppingListItem>();
}
