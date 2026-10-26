using System;

namespace VeggieLife.BLL.DTOs.Admin.Subscriptions;

/// <summary>
/// DTO chi tiết giao dịch nạp VIP trong danh sách trả về cho Quản trị viên (FN-ADM-13).
/// </summary>
public class AdminPaymentTransactionItemDto
{
    public long TransactionId { get; set; }

    public string PartnerTransactionCode { get; set; } = string.Empty;

    public int UserId { get; set; }

    public string UserEmail { get; set; } = string.Empty;

    public string UserFullName { get; set; } = string.Empty;

    public int PlanId { get; set; }

    public string PlanName { get; set; } = string.Empty;

    public int PlanDurationDays { get; set; }

    public string GatewayProvider { get; set; } = string.Empty;

    public long AmountVnd { get; set; }

    public string TransactionStatus { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    public DateTime? CompletedAt { get; set; }

    public DateTime StartsAt { get; set; }

    public DateTime EndsAt { get; set; }

    public string? ReceiptUrl { get; set; }

    /// <summary>
    /// Cho biết giao dịch có thể duyệt thủ công được không (đơn chưa thành công và chưa bị hoàn tiền).
    /// </summary>
    public bool CanApprove { get; set; }

    /// <summary>
    /// Cho biết giao dịch có thể đối soát (bù hạn / hoàn tiền) được không (chưa từng bị đối soát và chưa bị hoàn tiền).
    /// </summary>
    public bool CanReconcile { get; set; }
}
