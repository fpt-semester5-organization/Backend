using System;
using System.Collections.Generic;

namespace VeggieLife.DAL.Entities;

public partial class FoodCategory
{
    public int CategoryId { get; set; }

    public string CategoryName { get; set; } = null!;

    public string? Description { get; set; }

    public bool IsCore { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual ICollection<Recipe> Recipes { get; set; } = new List<Recipe>();

    public virtual ICollection<VenueCategory> VenueCategories { get; set; } = new List<VenueCategory>();
}
