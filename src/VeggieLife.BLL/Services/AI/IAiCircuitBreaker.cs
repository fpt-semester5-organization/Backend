namespace VeggieLife.BLL.Services.AI;

/// <summary>
/// Interface kiểm soát cơ chế ngắt mạch AI (Circuit Breaker - BR-ADM-AI-002).
/// Tự động chuyển sang trạng thái OPEN trong 5 phút khi tỷ lệ lỗi vượt quá 50% trong 60 giây.
/// </summary>
public interface IAiCircuitBreaker
{
    /// <summary>
    /// Kiểm tra trạng thái ngắt mạch hiện tại.
    /// </summary>
    bool IsOpen { get; }

    /// <summary>
    /// Ném ngoại lệ 503 ERR_AI_CIRCUIT_OPEN nếu mạch đang ở trạng thái OPEN.
    /// </summary>
    void ThrowIfOpen();

    /// <summary>
    /// Ghi nhận 01 yêu cầu AI thành công.
    /// </summary>
    void RecordSuccess();

    /// <summary>
    /// Ghi nhận 01 yêu cầu AI thất bại (lỗi mạng hoặc timeout > 10s).
    /// </summary>
    void RecordFailure();

    /// <summary>
    /// Đặt lại trạng thái mạch về CLOSED (phục vụ kiểm thử hoặc can thiệp quản trị).
    /// </summary>
    void Reset();
}
