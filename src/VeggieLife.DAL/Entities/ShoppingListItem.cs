using System;
using System.Collections.Generic;

namespace VeggieLife.DAL.Entities;

public partial class ShoppingListItem
{
    public int ItemId { get; set; }

    public int ListId { get; set; }

    public int IngredientId { get; set; }

    public decimal AggregatedQuantity { get; set; }

    public string Unit { get; set; } = null!;

    public bool IsPurchased { get; set; }

    public virtual Ingredient Ingredient { get; set; } = null!;

    public virtual ShoppingList List { get; set; } = null!;
}
