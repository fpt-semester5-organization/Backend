using System.Text.Json.Serialization;
using VeggieLife.BLL.Common.Responses;

namespace VeggieLife.BLL.DTOs.Venues.Responses;

/// <summary>
/// Metadata mở rộng cho kết quả tìm kiếm quán chay lân cận (FN-MAP-01).
/// </summary>
public class NearbyVenuesMeta : PagedMeta
{
    public double RadiusKm { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public GuestMapQuotaDto? Quota { get; set; }

    public NearbyVenuesMeta()
    {
    }

    public NearbyVenuesMeta(int page, int size, int total, double radiusKm, GuestMapQuotaDto? quota = null)
        : base(page, size, total)
    {
        RadiusKm = radiusKm;
        Quota = quota;
    }
}

/// <summary>
/// Thông tin hạn ngạch tra cứu bản đồ của khách vãng lai (BR-GUEST-03).
/// </summary>
public class GuestMapQuotaDto
{
    public int Used { get; set; }
    public int Limit { get; set; } = 3;
    public int Remaining { get; set; }
}
