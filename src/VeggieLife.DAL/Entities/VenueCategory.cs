using System;
using System.Collections.Generic;

namespace VeggieLife.DAL.Entities;

public partial class VenueCategory
{
    public int CategoryId { get; set; }

    public int VenueId { get; set; }

    public bool IsPrimary { get; set; }

    public DateTime AssignedAt { get; set; }

    public virtual FoodCategory Category { get; set; } = null!;

    public virtual VeganVenue Venue { get; set; } = null!;
}
