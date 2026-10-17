using System;
using System.Collections.Generic;

namespace VeggieLife.BLL.DTOs.Venues.Responses;

/// <summary>
/// DTO thông tin chi tiết một quán ăn thuần chay (FN-MAP-02).
/// Không chứa thực đơn món ăn theo Quyết định D100 và yêu cầu MVP.
/// </summary>
public class VenueDetailDto
{
    public int VenueId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public double Lat => Latitude;
    public double Lng => Longitude;
    public double? DistanceKm { get; set; }
    public decimal Rating { get; set; }
    public bool IsFullyVegan { get; set; }
    public string? ThumbnailUrl { get; set; }
    public List<string> Categories { get; set; } = new();
    public string DirectionsUrl { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public DateTime LastSyncedAt { get; set; }
}
