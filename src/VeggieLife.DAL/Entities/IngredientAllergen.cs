using System;
using System.Collections.Generic;

namespace VeggieLife.DAL.Entities;

public partial class IngredientAllergen
{
    public int IngredientId { get; set; }

    public int AllergenId { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual Allergen Allergen { get; set; } = null!;
}
