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

    /// <summary>
    /// Tiếp nhận và xử lý Webhook tự động từ PayOS VietQR Napas247 (xác thực chữ ký ChecksumKey HMAC-SHA256, tự động kích hoạt VIP).
    /// </summary>
    Task HandlePayOsWebhookAsync(
        PayOS.Models.Webhooks.Webhook webhook,
        CancellationToken ct = default);

    /// <summary>
    /// Hủy đơn thanh toán đang chờ của người dùng để giải phóng và cho phép tạo đơn mới ngay lập tức.
    /// </summary>
    Task<CancelPendingSubscriptionResponseDto> CancelPendingSubscriptionAsync(
        int userId,
        CancellationToken ct = default);

    /// <summary>
    /// Lấy chi tiết thông tin biên lai thanh toán giao dịch nâng cấp VIP thành công (FN-SUB-04).
    /// </summary>
    Task<PaymentReceiptDto> GetPaymentReceiptAsync(
        long transactionId,
        int currentUserId,
        string currentUserRole,
        CancellationToken ct = default);
}
