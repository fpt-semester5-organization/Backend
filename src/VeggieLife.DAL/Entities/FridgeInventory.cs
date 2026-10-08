using System;
using System.Collections.Generic;

namespace VeggieLife.DAL.Entities;

public partial class FridgeInventory
{
    public int InventoryId { get; set; }

    public int UserId { get; set; }

    public int IngredientId { get; set; }

    public int? SessionId { get; set; }

    public string CustomItemName { get; set; } = null!;

    public string FreshnessStatus { get; set; } = null!;

    public virtual Ingredient Ingredient { get; set; } = null!;

    public virtual FridgeSession? Session { get; set; }

    public virtual User User { get; set; } = null!;
}
