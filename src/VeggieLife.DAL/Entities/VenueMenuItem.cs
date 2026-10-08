using System;
using System.Collections.Generic;

namespace VeggieLife.DAL.Entities;

public partial class VenueMenuItem
{
    public int ItemId { get; set; }

    public int VenueId { get; set; }

    public string DishName { get; set; } = null!;

    public decimal? PriceVnd { get; set; }

    public string? Description { get; set; }

    public string? ImageUrl { get; set; }

    public bool IsSignature { get; set; }

    public virtual VeganVenue Venue { get; set; } = null!;
}
