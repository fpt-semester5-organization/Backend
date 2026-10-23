using System;
using System.Threading;
using System.Threading.Tasks;

namespace VeggieLife.BLL.Services.Subscriptions;

/// <summary>
/// Giao diện dịch vụ quét và chuyển các đơn thanh toán chờ (PENDING) quá 5 phút sang hết hạn (EXPIRED) (FN-SUB-02, BR-PAY-TXN-01).
/// </summary>
public interface IPaymentExpiryService
{
    /// <summary>
    /// Chuyển các đơn thanh toán PENDING quá 5 phút sang EXPIRED và ghi nhận AuditLog hệ thống.
    /// Chạy lặp lại an toàn (Idempotent).
    /// </summary>
    /// <param name="customNow">Mốc thời gian tùy chọn phục vụ kiểm thử với đồng hồ giả.</param>
    /// <param name="ct">CancellationToken.</param>
    /// <returns>Số lượng đơn thanh toán đã được cập nhật sang EXPIRED.</returns>
    Task<int> ExpirePendingPaymentsAsync(DateTime? customNow = null, CancellationToken ct = default);
}
