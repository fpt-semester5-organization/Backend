using System;
using System.Collections.Generic;

namespace VeggieLife.DAL.Entities;

public partial class RecipeIngredient
{
    public int RecipeId { get; set; }

    public int IngredientId { get; set; }

    public decimal QuantityNeeded { get; set; }

    public string Unit { get; set; } = null!;

    public string? PreparationNote { get; set; }

    public bool IsOptional { get; set; }

    public virtual Ingredient Ingredient { get; set; } = null!;

    public virtual Recipe Recipe { get; set; } = null!;
}
