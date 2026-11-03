using System;

namespace VeggieLife.BLL.DTOs.Subscriptions.Responses;

/// <summary>
/// DTO thông tin chi tiết biên lai thanh toán điện tử cho giao dịch nâng cấp gói VIP (FN-SUB-04).
/// </summary>
public class PaymentReceiptDto
{
    /// <summary>
    /// Số biên lai chuẩn hóa, ví dụ: "REC-20261103-000123".
    /// </summary>
    public string ReceiptNumber { get; set; } = string.Empty;

    /// <summary>
    /// Định danh giao dịch trong hệ thống VeggieLife.
    /// </summary>
    public long TransactionId { get; set; }

    /// <summary>
    /// Mã đơn hàng hoặc mã giao dịch gửi tới đối tác cổng thanh toán (MoMo / VietQR).
    /// </summary>
    public string OrderCode { get; set; } = string.Empty;

    /// <summary>
    /// Cổng thanh toán xử lý giao dịch (VIETQR / MOMO).
    /// </summary>
    public string Gateway { get; set; } = string.Empty;

    /// <summary>
    /// Trạng thái giao dịch (SUCCESS).
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Thời điểm giao dịch hoàn tất thành công.
    /// </summary>
    public DateTime PaymentDate { get; set; }

    /// <summary>
    /// ID người dùng thực hiện thanh toán.
    /// </summary>
    public int UserId { get; set; }

    /// <summary>
    /// Họ và tên (hoặc Username nếu chưa cập nhật tên) của người mua.
    /// </summary>
    public string FullName { get; set; } = string.Empty;

    /// <summary>
    /// Email liên hệ nhận biên lai.
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Mã gói VIP đăng ký.
    /// </summary>
    public int PlanId { get; set; }

    /// <summary>
    /// Tên gói VIP đăng ký.
    /// </summary>
    public string PlanName { get; set; } = string.Empty;

    /// <summary>
    /// Thời hạn sử dụng của gói (tính theo ngày).
    /// </summary>
    public int DurationDays { get; set; }

    /// <summary>
    /// Số tiền thanh toán thực tế (VNĐ).
    /// </summary>
    public decimal AmountVnd { get; set; }

    /// <summary>
    /// Thời điểm bắt đầu hiệu lực quyền lợi VIP.
    /// </summary>
    public DateTime VipStartsAt { get; set; }

    /// <summary>
    /// Thời điểm kết thúc hiệu lực quyền lợi VIP.
    /// </summary>
    public DateTime VipEndsAt { get; set; }

    /// <summary>
    /// Diễn giải / Ghi chú nội dung thanh toán.
    /// </summary>
    public string Note { get; set; } = string.Empty;
}

