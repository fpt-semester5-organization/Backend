using System.Threading;
using System.Threading.Tasks;
using VeggieLife.BLL.DTOs.Subscriptions.Requests;
using VeggieLife.BLL.DTOs.Subscriptions.Responses;

namespace VeggieLife.BLL.Services.Subscriptions;

/// <summary>
/// Interface dịch vụ quản lý các gói hội viên VIP và quy trình thanh toán (FN-SUB-01..04, BR-PAY-SUB-01, BR-PAY-TXN-01, BR-PAY-TXN-02).
/// </summary>
public interface ISubscriptionService
{
    /// <summary>
    /// FN-SUB-01: Lấy danh sách gói VIP đang mở bán và bảng so sánh đặc quyền Free / VIP.
    /// </summary>
    Task<ListSubscriptionPlansResponseDto> GetSubscriptionPlansAsync(CancellationToken ct = default);

    /// <summary>
    /// FN-SUB-02: Khởi tạo đơn thanh toán gói VIP qua MoMo hoặc VietQR.
    /// </summary>
    Task<CheckoutSubscriptionResponseDto> CheckoutSubscriptionAsync(
        int userId,
        string userRole,
        CheckoutSubscriptionRequest request,
        CancellationToken ct = default);

    /// <summary>
    /// FN-SUB-03: Tiếp nhận và xử lý kết quả thanh toán từ cổng qua Webhook IPN (xác thực HMAC-SHA256, bất biến Idempotent).
    /// </summary>
    Task HandlePaymentIpnAsync(
        string gateway,
        HandlePaymentIpnRequest request,
        CancellationToken ct = default);

    /// <summary>
    /// FN-SUB-04: Lấy trạng thái gói VIP hiện tại và toàn bộ lịch sử giao dịch của người dùng.
    /// </summary>
    Task<CurrentUserSubscriptionResponseDto> GetCurrentUserSubscriptionAsync(
        int userId,
        CancellationToken ct = default);
}
