using System;

namespace VeggieLife.BLL.DTOs.Admin.Subscriptions;

/// <summary>
/// DTO phản hồi khi Quản trị viên duyệt thanh toán thủ công thành công (FN-ADM-13).
/// </summary>
public class ApproveAdminPaymentResponseData
{
    public long TransactionId { get; set; }

    public string PartnerTransactionCode { get; set; } = string.Empty;

    public int UserId { get; set; }

    public string Status { get; set; } = string.Empty;

    public DateTime VipStartsAt { get; set; }

    public DateTime VipEndsAt { get; set; }
}
