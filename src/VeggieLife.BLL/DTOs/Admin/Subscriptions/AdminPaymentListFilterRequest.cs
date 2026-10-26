namespace VeggieLife.BLL.DTOs.Admin.Subscriptions;

/// <summary>
/// Tham số truy vấn, tìm kiếm, lọc và phân trang danh sách giao dịch thanh toán cho Quản trị viên (FN-ADM-13).
/// </summary>
public class AdminPaymentListFilterRequest
{
    public int Page { get; set; } = 1;

    public int Size { get; set; } = 10;

    /// <summary>
    /// Trạng thái giao dịch: PENDING, SUCCESS, EXPIRED, FAILED, REFUNDED.
    /// </summary>
    public string? Status { get; set; }

    /// <summary>
    /// Cổng thanh toán: MOMO, VIETQR.
    /// </summary>
    public string? Gateway { get; set; }

    /// <summary>
    /// Từ khóa tìm kiếm: Email người dùng, Tên người dùng, hoặc Mã giao dịch đối tác (PartnerTransactionCode).
    /// </summary>
    public string? Keyword { get; set; }

    /// <summary>
    /// Lọc từ ngày (định dạng YYYY-MM-DD).
    /// </summary>
    public string? From { get; set; }

    /// <summary>
    /// Lọc đến ngày (định dạng YYYY-MM-DD).
    /// </summary>
    public string? To { get; set; }
}
