using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using VeggieLife.BLL.Common.Responses;
using VeggieLife.BLL.DTOs.Admin.Subscriptions;

namespace VeggieLife.BLL.Services.Admin;

/// <summary>
/// Giao diện dịch vụ Quản trị gói VIP và đối soát giao dịch (FN-ADM-13).
/// </summary>
public interface IAdminSubscriptionService
{
    /// <summary>
    /// Lấy toàn bộ danh sách gói VIP (bao gồm cả gói đang kích hoạt và gói ngừng bán).
    /// </summary>
    Task<IReadOnlyList<AdminSubscriptionPlanItemDto>> GetAllSubscriptionPlansAsync(CancellationToken ct = default);

    /// <summary>
    /// Tạo mới một gói VIP (PlanName duy nhất, DurationDays > 0, PriceVnd > 0).
    /// </summary>
    Task<CreateAdminSubscriptionPlanResponseData> CreateSubscriptionPlanAsync(
        CreateAdminSubscriptionPlanRequest request,
        int adminId,
        CancellationToken ct = default);

    /// <summary>
    /// Cập nhật giá và trạng thái bật/tắt của gói VIP (BR-ADM-PAY-001).
    /// </summary>
    Task<UpdateAdminSubscriptionPlanResponseData> UpdateSubscriptionPlanAsync(
        int id,
        UpdateAdminSubscriptionPlanRequest request,
        int adminId,
        CancellationToken ct = default);

    /// <summary>
    /// Đối soát hoặc hoàn tiền giao dịch thanh toán (BR-ADM-PAY-002, BR-ADM-SYS-002).
    /// Mỗi giao dịch chỉ được đối soát một lần duy nhất.
    /// </summary>
    Task<ReconcileAdminPaymentResponseData> ReconcilePaymentAsync(
        long transactionId,
        ReconcileAdminPaymentRequest request,
        int adminId,
        CancellationToken ct = default);

    /// <summary>
    /// Tra cứu, tìm kiếm, lọc và phân trang danh sách giao dịch nạp VIP (FN-ADM-13).
    /// </summary>
    Task<(IReadOnlyList<AdminPaymentTransactionItemDto> Items, PagedMeta Meta)> GetPaymentTransactionsAsync(
        AdminPaymentListFilterRequest filter,
        CancellationToken ct = default);

    /// <summary>
    /// Duyệt thanh toán thủ công giao dịch MoMo / VietQR (FN-ADM-13).
    /// </summary>
    Task<ApproveAdminPaymentResponseData> ApprovePaymentAsync(
        long transactionId,
        int adminId,
        CancellationToken ct = default);
}
