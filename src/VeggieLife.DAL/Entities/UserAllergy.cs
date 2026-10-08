using System;
using System.Collections.Generic;

namespace VeggieLife.DAL.Entities;

public partial class UserAllergy
{
    public int UserId { get; set; }

    public int AllergenId { get; set; }

    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual Allergen Allergen { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
