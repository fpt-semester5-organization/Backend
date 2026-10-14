using System.Threading;
using System.Threading.Tasks;
using VeggieLife.BLL.DTOs.Venues.Requests;
using VeggieLife.BLL.DTOs.Venues.Responses;

namespace VeggieLife.BLL.Services.Venues;

/// <summary>
/// Dịch vụ bản đồ quán ăn chay và tìm kiếm lân cận (FN-MAP-01).
/// </summary>
public interface IVenueService
{
    /// <summary>
    /// Tìm danh sách quán chay xung quanh vị trí GPS (hoặc tỉnh/thành) theo bán kính và lọc danh mục.
    /// Khách vãng lai: bán kính 5 km, tối đa 3 lượt dùng thử (BR-GUEST-03).
    /// Thành viên: bán kính 1..10 km, không giới hạn, lọc bỏ quán &lt; 2.5 sao (BR-USR-11).
    /// </summary>
    Task<ListNearbyVenuesResult> ListNearbyVenuesAsync(
        ListNearbyVenuesRequest request,
        int? userId,
        string userRole,
        string? guestUuid,
        CancellationToken ct = default);
}
