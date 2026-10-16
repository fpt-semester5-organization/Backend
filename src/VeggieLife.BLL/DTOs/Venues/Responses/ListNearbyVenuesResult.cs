using System.Collections.Generic;

namespace VeggieLife.BLL.DTOs.Venues.Responses;

/// <summary>
/// Kết quả tổng hợp trả về từ Service cho tìm kiếm quán chay lân cận (FN-MAP-01).
/// </summary>
public class ListNearbyVenuesResult
{
    public List<NearbyVenueItemDto> Venues { get; set; } = new();
    public NearbyVenuesMeta Meta { get; set; } = new();
}
