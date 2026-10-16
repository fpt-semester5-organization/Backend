using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace VeggieLife.DAL.External.Goong;

/// <summary>
/// Giao diện client kết nối tới Goong Places API để tìm kiếm và lấy chi tiết quán ăn (FN-MAP-01).
/// </summary>
public interface IGoongPlacesClient
{
    /// <summary>
    /// Cho biết dịch vụ Goong API Key đã được cấu hình trong appsettings hay chưa.
    /// </summary>
    bool IsConfigured { get; }

    /// <summary>
    /// Tìm kiếm địa điểm quán ăn quanh tọa độ (lat, lng) trong bán kính radiusMeters qua Goong Place Autocomplete.
    /// </summary>
    Task<IReadOnlyList<GoongPlacePredictionDto>> SearchNearbyPlacesAsync(
        string keyword,
        double latitude,
        double longitude,
        int radiusMeters,
        int limit = 10,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Tìm kiếm địa điểm quán ăn theo chuỗi địa danh/tỉnh thành (ví dụ: "quán chay Đà Nẵng").
    /// </summary>
    Task<IReadOnlyList<GoongPlacePredictionDto>> SearchPlacesByTextAsync(
        string input,
        int limit = 10,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Lấy thông tin chi tiết địa điểm (tọa độ chính xác, địa chỉ đầy đủ, đánh giá) từ Goong Place Detail qua place_id.
    /// </summary>
    Task<GoongPlaceDetailDto?> GetPlaceDetailAsync(
        string placeId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Chuyển đổi địa chỉ nhập (Tỉnh/TP, Quận/Huyện) thành tọa độ trung tâm (lat, lng) qua Goong Geocode API (Bước 7c trong luồng Swimlane).
    /// </summary>
    Task<GoongLocation?> GeocodeAddressAsync(
        string address,
        CancellationToken cancellationToken = default);
}

