using System;
using System.Collections.Generic;

namespace VeggieLife.DAL.Entities;

public partial class VeganVenue
{
    public int VenueId { get; set; }

    public string ExternalSource { get; set; } = null!;

    public string? ExternalPlaceId { get; set; }

    public string VenueName { get; set; } = null!;

    public string Address { get; set; } = null!;

    public decimal Latitude { get; set; }

    public decimal Longitude { get; set; }

    public decimal Rating { get; set; }

    public string? ThumbnailUrl { get; set; }

    public bool IsFullyVegan { get; set; }

    public bool IsActive { get; set; }

    public string? RawOriginJson { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual ICollection<VenueCategory> VenueCategories { get; set; } = new List<VenueCategory>();

    public virtual ICollection<VenueMenuItem> VenueMenuItems { get; set; } = new List<VenueMenuItem>();
}
