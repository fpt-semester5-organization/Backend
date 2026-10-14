using System.Collections.Generic;

namespace VeggieLife.BLL.DTOs.Venues.Responses;

/// <summary>
/// Dữ liệu từng quán chay lân cận trả về trong danh sách (FN-MAP-01).
/// </summary>
public class NearbyVenueItemDto
{
    public int VenueId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public double DistanceKm { get; set; }
    public decimal Rating { get; set; }
    public bool IsFullyVegan { get; set; }
    public string? ThumbnailUrl { get; set; }
    public List<string> Categories { get; set; } = new();
    public string DirectionsUrl { get; set; } = string.Empty;
}
