using System.Threading;
using System.Threading.Tasks;
using VeggieLife.BLL.DTOs.Subscriptions.Responses;

namespace VeggieLife.BLL.Services.Subscriptions;

/// <summary>
/// Dịch vụ phân giải quyền lợi và hạn mức người dùng (Dynamic Entitlement & Grandfathering Resolver).
/// </summary>
public interface IUserBenefitService
{
    /// <summary>
    /// Lấy toàn bộ quyền lợi và hạn mức hiện hành của người dùng.
    /// Nếu là VIP: đọc chính xác từ Snapshot_Features_JSON của gói đã mua (bảo lưu đến hết hạn).
    /// Nếu là Free: đọc hạn mức từ SystemSettings.
    /// </summary>
    Task<VipBenefitFeaturesDto> GetUserBenefitsAsync(int userId, string? userRole = null, CancellationToken ct = default);

    /// <summary>
    /// Xóa cache quyền lợi của người dùng (khi mua gói mới, hủy gói hoặc hết hạn).
    /// </summary>
    Task InvalidateUserBenefitsCacheAsync(int userId, CancellationToken ct = default);
}
