using System;
using System.Threading;
using System.Threading.Tasks;

namespace VeggieLife.BLL.Services.Subscriptions;

/// <summary>
/// Giao diện dịch vụ quét và hạ cấp thành viên VIP đã hết hạn về thành viên thường (FN-SUB-04, BR-PAY-SUB-02).
/// </summary>
public interface IVipExpiryService
{
    /// <summary>
    /// Hạ cấp các người dùng VIP không còn gói active về MEMBER, giữ nguyên toàn bộ dữ liệu dinh dưỡng.
    /// Chạy lặp lại an toàn (Idempotent).
    /// </summary>
    /// <param name="customNow">Mốc thời gian tùy chọn phục vụ kiểm thử với đồng hồ giả.</param>
    /// <param name="ct">CancellationToken.</param>
    /// <returns>Số lượng người dùng VIP đã được hạ về MEMBER.</returns>
    Task<int> DowngradeExpiredVipUsersAsync(DateTime? customNow = null, CancellationToken ct = default);
}
