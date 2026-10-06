using System;
using System.Collections.Generic;

namespace VeggieLife.DAL.Entities;

public partial class FridgeInventoryList
{
    public int InventoryId { get; set; }

    public int UserId { get; set; }

    public int IngredientId { get; set; }

    public int? SessionId { get; set; }

    public string CustomItemName { get; set; } = null!;

    public string FreshnessStatus { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public virtual Ingredient Ingredient { get; set; } = null!;
}
