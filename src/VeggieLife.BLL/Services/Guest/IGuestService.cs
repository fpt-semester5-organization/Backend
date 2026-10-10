using System.Threading;
using System.Threading.Tasks;
using VeggieLife.BLL.Common.Constants;
using VeggieLife.BLL.DTOs.Guest.Responses;

namespace VeggieLife.BLL.Services.Guest;

/// <summary>
/// Interface dịch vụ quản lý phiên Khách vãng lai, hạn ngạch dùng thử và chính sách bảo mật (FN-AUTH-01, FN-AUTH-02).
/// </summary>
public interface IGuestService
{
    /// <summary>
    /// FN-AUTH-01: Khởi tạo phiên khách vãng lai, sinh/xác định Guest_UUID, cấp Anonymous JWT (vai trò GUEST, hiệu lực 24 giờ).
    /// </summary>
    Task<CreateAnonymousSessionResponse> CreateAnonymousSessionAsync(string? cookieGuestUuid, string? clientIp, string? clientCountry, CancellationToken ct = default);

    /// <summary>
    /// FN-AUTH-02: Tra cứu hạn mức dùng thử còn lại của phiên khách (AI 3 lượt, Barcode 3 lần/ngày, Bản đồ 3 lần).
    /// </summary>
    Task<GetGuestQuotaResponse> GetGuestQuotaAsync(string guestUuid, CancellationToken ct = default);

    /// <summary>
    /// Tiêu hao 01 lượt dùng thử nguyên tử (Atomic Quota Consumption) chống đua lệnh (Race Condition).
    /// </summary>
    Task<GuestTrialResultDto> ConsumeTrialQuotaAsync(string guestUuid, GuestTrialType trialType, CancellationToken ct = default);
}
