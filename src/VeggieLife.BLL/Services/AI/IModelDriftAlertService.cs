using System;
using System.Threading;
using System.Threading.Tasks;

namespace VeggieLife.BLL.Services.AI;

/// <summary>
/// Giao diện dịch vụ đánh giá suy thoái độ chính xác mô hình AI và phát cảnh báo chất lượng cho Admin (FN-CHAT-06, BR-ADM-AI-004).
/// </summary>
public interface IModelDriftAlertService
{
    /// <summary>
    /// Kiểm tra tỷ lệ đánh giá tiêu cực (1-2 sao) trên tổng số Surveys trong 07 ngày liên tiếp.
    /// Nếu cả 07 ngày đều vượt quá 20%, ghi nhận AuditLog MODEL_DRIFT_ALERT và gửi cảnh báo cho Admin.
    /// Chạy lặp lại an toàn (Idempotent).
    /// </summary>
    /// <param name="customNow">Mốc thời gian tùy chọn phục vụ kiểm thử với đồng hồ giả.</param>
    /// <param name="ct">CancellationToken.</param>
    /// <returns>True nếu cảnh báo suy thoái được kích hoạt; False nếu chất lượng đạt yêu cầu.</returns>
    Task<bool> CheckAndAlertModelDriftAsync(DateTime? customNow = null, CancellationToken ct = default);
}
