using System;
using System.Collections.Generic;

namespace VeggieLife.DAL.Entities;

public partial class Allergen
{
    public int AllergenId { get; set; }

    public string AllergenName { get; set; } = null!;

    public string Code { get; set; } = null!;

    public string? Description { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual ICollection<ProductAllergen> ProductAllergens { get; set; } = new List<ProductAllergen>();

    public virtual ICollection<UserAllergy> UserAllergies { get; set; } = new List<UserAllergy>();
}
