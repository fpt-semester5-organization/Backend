using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using VeggieLife.BLL.Common.Constants;
using VeggieLife.BLL.Common.Exceptions;
using VeggieLife.BLL.Common.Responses;
using VeggieLife.BLL.DTOs.Admin.Subscriptions;
using VeggieLife.BLL.Services.Admin;

namespace VeggieLife.API.Controllers.Admin;

/// <summary>
/// API Controller quản trị và đối soát giao dịch thanh toán dành cho Quản trị viên (FN-ADM-13, BR-ADM-PAY-002).
/// </summary>
[Route("api/v1/admin/payments")]
[Authorize(Policy = PolicyNames.RequireAdmin)]
public class AdminPaymentsController : BaseApiController
{
    private readonly IAdminSubscriptionService _subscriptionService;

    public AdminPaymentsController(IAdminSubscriptionService subscriptionService)
    {
        _subscriptionService = subscriptionService;
    }

    /// <summary>
    /// FN-ADM-13: Tra cứu, tìm kiếm, lọc và phân trang danh sách giao dịch nạp VIP trong hệ thống.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<AdminPaymentTransactionItemDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> ListPayments(
        [FromQuery] AdminPaymentListFilterRequest filter,
        CancellationToken ct)
    {
        EnsureAdmin();
        var (items, meta) = await _subscriptionService.GetPaymentTransactionsAsync(filter, ct);
        return PagedResponse(items, meta, "OK");
    }

    /// <summary>
    /// FN-ADM-13: Duyệt thanh toán thủ công giao dịch (MoMo / VietQR) khi tiền đã về tài khoản ngân hàng.
    /// Kích hoạt VIP cho người dùng, ghi AuditLog.
    /// </summary>
    [HttpPost("{id:long}/approve")]
    [ProducesResponseType(typeof(ApiResponse<ApproveAdminPaymentResponseData>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> ApprovePayment(
        [FromRoute] long id,
        CancellationToken ct)
    {
        var adminId = EnsureAdmin();
        var result = await _subscriptionService.ApprovePaymentAsync(id, adminId, ct);
        return OkResponse(result, "Đã duyệt thanh toán thành công.");
    }

    /// <summary>
    /// FN-ADM-13: Đối soát hoặc hoàn tiền giao dịch thanh toán khiếu nại (bù hạn VIP hoặc hoàn tiền).
    /// Mỗi giao dịch chỉ được đối soát đúng 1 lần (BR-ADM-PAY-002, BR-ADM-SYS-002).
    /// </summary>
    [HttpPost("{id:long}/reconcile")]
    [ProducesResponseType(typeof(ApiResponse<ReconcileAdminPaymentResponseData>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> ReconcilePayment(
        [FromRoute] long id,
        [FromBody] ReconcileAdminPaymentRequest request,
        CancellationToken ct)
    {
        var adminId = EnsureAdmin();
        var result = await _subscriptionService.ReconcilePaymentAsync(id, request, adminId, ct);
        return OkResponse(result, "Đã đối soát.");
    }

    private int EnsureAdmin()
    {
        if (User.Identity == null || !User.Identity.IsAuthenticated)
        {
            throw AppException.Unauthorized(ErrorCodes.ErrAuthInvalidToken, "Thiếu, sai chữ ký hoặc hết hạn JWT.");
        }

        if (!string.Equals(CurrentUserRole, UserRoles.Admin, StringComparison.OrdinalIgnoreCase))
        {
            throw AppException.Forbidden(ErrorCodes.ErrAdmForbidden, "Claim role khác ADMIN.");
        }

        if (CurrentUserId == null)
        {
            throw AppException.Unauthorized(ErrorCodes.ErrAuthInvalidToken, "Thiếu, sai chữ ký hoặc hết hạn JWT.");
        }

        return (int)CurrentUserId.Value;
    }
}
