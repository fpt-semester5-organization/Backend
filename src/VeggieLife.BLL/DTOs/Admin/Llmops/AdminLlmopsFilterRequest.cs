namespace VeggieLife.BLL.DTOs.Admin.Llmops;

/// <summary>
/// Tham số bộ lọc truy vấn bảng điều khiển LLMOps / Chi phí Token AI (FN-ADM-16).
/// </summary>
public class AdminLlmopsFilterRequest
{
    /// <summary>
    /// Ngày bắt đầu (YYYY-MM-DD).
    /// </summary>
    public string? From { get; set; }

    /// <summary>
    /// Ngày kết thúc (YYYY-MM-DD).
    /// </summary>
    public string? To { get; set; }

    /// <summary>
    /// Tiêu chí gom nhóm (DAY | FEATURE). Bỏ trống để gom cả hai chiều.
    /// </summary>
    public string? GroupBy { get; set; }
}
