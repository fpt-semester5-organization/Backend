namespace VeggieLife.BLL.DTOs.Venues.Requests;

/// <summary>
/// Tham số truy vấn tìm kiếm quán chay lân cận (FN-MAP-01).
/// </summary>
public class ListNearbyVenuesRequest
{
    /// <summary>
    /// Vĩ độ GPS (-90..90). Cần đi kèm lng hoặc dùng province.
    /// </summary>
    public double? Lat { get; set; }

    /// <summary>
    /// Kinh độ GPS (-180..180). Cần đi kèm lat hoặc dùng province.
    /// </summary>
    public double? Lng { get; set; }

    /// <summary>
    /// Tên tỉnh / thành phố khi người dùng từ chối cấp quyền GPS (1..50 ký tự).
    /// </summary>
    public string? Province { get; set; }

    /// <summary>
    /// Tên quận / huyện khi người dùng từ chối cấp quyền GPS (1..50 ký tự).
    /// </summary>
    public string? District { get; set; }

    /// <summary>
    /// Bán kính tìm kiếm theo km (Khách = 5, Thành viên 1..10, mặc định 5).
    /// </summary>
    public double RadiusKm { get; set; } = 5.0;

    /// <summary>
    /// Lọc theo danh mục món ăn (FoodCategory ID > 0).
    /// </summary>
    public int? CategoryId { get; set; }

    /// <summary>
    /// Trang kết quả (>= 1, mặc định 1).
    /// </summary>
    public int Page { get; set; } = 1;

    /// <summary>
    /// Kích thước trang (1..20, mặc định 10).
    /// </summary>
    public int Size { get; set; } = 10;
}
