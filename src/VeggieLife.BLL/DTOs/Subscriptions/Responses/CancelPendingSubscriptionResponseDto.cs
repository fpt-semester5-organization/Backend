namespace VeggieLife.BLL.DTOs.Subscriptions.Responses;

/// <summary>
/// Phản hồi hủy đơn thanh toán đang chờ (FN-SUB-02).
/// </summary>
public class CancelPendingSubscriptionResponseDto
{
    public bool Cancelled { get; set; }
    public string Message { get; set; } = string.Empty;
}
