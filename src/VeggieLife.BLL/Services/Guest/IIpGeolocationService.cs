using System.Threading;
using System.Threading.Tasks;

namespace VeggieLife.BLL.Services.Guest;

/// <summary>
/// Interface dịch vụ xác định vị trí địa lý (quốc gia) theo địa chỉ IP (FN-AUTH-01, BR-GUEST-06).
/// Kết quả được lưu bộ nhớ đệm Redis với thời hạn 24 giờ.
/// </summary>
public interface IIpGeolocationService
{
    /// <summary>
    /// Tra cứu mã quốc gia (ISO 3166-1 alpha-2, ví dụ: "VN", "US") từ địa chỉ IP và Header tùy chọn.
    /// Kết quả được lưu bộ nhớ đệm Redis với thời hạn 24 giờ (BR-GUEST-06).
    /// </summary>
    /// <param name="clientIp">Địa chỉ IP của người gửi yêu cầu.</param>
    /// <param name="headerCountry">Mã quốc gia truyền qua header nếu có (X-Country-Code, CF-IPCountry).</param>
    /// <param name="ct">CancellationToken.</param>
    /// <returns>Mã quốc gia 2 ký tự (viết hoa), mặc định "VN".</returns>
    Task<string> ResolveCountryAsync(string? clientIp, string? headerCountry = null, CancellationToken ct = default);
}
