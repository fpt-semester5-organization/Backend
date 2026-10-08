using System;
using System.Collections.Generic;

namespace VeggieLife.DAL.Entities;

public partial class ProductAllergen
{
    public int ProductId { get; set; }

    public int AllergenId { get; set; }

    public string PresenceType { get; set; } = null!;

    public virtual Allergen Allergen { get; set; } = null!;

    public virtual Product Product { get; set; } = null!;
}
