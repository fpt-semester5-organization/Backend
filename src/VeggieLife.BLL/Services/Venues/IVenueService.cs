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
    /// Thành viên: bán kính 1..10 km, không giới hạn lượt tìm kiếm.
    /// </summary>
    Task<ListNearbyVenuesResult> ListNearbyVenuesAsync(
        ListNearbyVenuesRequest request,
        int? userId,
        string userRole,
        string? guestUuid,
        CancellationToken ct = default);

    /// <summary>
    /// FN-MAP-02: Xem thông tin chi tiết một quán ăn chay (địa chỉ, giờ mở cửa, đánh giá, danh mục, nguồn dữ liệu).
    /// Không bao gồm thực đơn quán theo Quyết định D100.
    /// Quán bị ẩn (Is_Active = 0) trả về 404 ERR_VENUE_NOT_FOUND (BR-ADM-MAP-001).
    /// Cache chi tiết quán với TTL 24 giờ trên Redis (BR-SYS-CACHE-01).
    /// </summary>
    Task<VenueDetailDto> GetVenueDetailAsync(
        int id,
        double? lat = null,
        double? lng = null,
        CancellationToken ct = default);

    /// <summary>
    /// FN-MAP-03: Gợi ý các quán chay gần người dùng có danh mục món trùng với công thức đang xem.
    /// Khách vãng lai: bán kính 5 km, trừ 1 lượt tìm kiếm; Thành viên: bán kính 10 km.
    /// Trả về tối đa 5 quán gần nhất.
    /// </summary>
    Task<System.Collections.Generic.List<NearbyVenueItemDto>> ListRecipeVenuesAsync(
        int recipeId,
        double lat,
        double lng,
        int limit,
        int? userId,
        string userRole,
        string? guestUuid,
        CancellationToken ct = default);
}
