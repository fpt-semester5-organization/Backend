using System;
using System.Collections.Generic;

namespace VeggieLife.DAL.Entities;

public partial class Ingredient
{
    public int IngredientId { get; set; }

    public string IngredientName { get; set; } = null!;

    public string StandardUnit { get; set; } = null!;

    public string DefaultStallType { get; set; } = null!;

    public decimal CaloriesPer100g { get; set; }

    public bool IsAllium { get; set; }

    public string FoodGroup { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public virtual ICollection<FridgeInventory> FridgeInventories { get; set; } = new List<FridgeInventory>();

    public virtual ICollection<RecipeIngredient> RecipeIngredients { get; set; } = new List<RecipeIngredient>();

    public virtual ICollection<ShoppingListItem> ShoppingListItems { get; set; } = new List<ShoppingListItem>();
}
