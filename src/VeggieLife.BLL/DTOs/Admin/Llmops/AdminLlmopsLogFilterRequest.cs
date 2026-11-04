namespace VeggieLife.BLL.DTOs.Admin.Llmops;

/// <summary>
/// Tham số truy vấn bộ lọc và phân trang danh sách nhật ký AIQuotaLogs.
/// </summary>
public class AdminLlmopsLogFilterRequest
{
    public string? From { get; set; }
    public string? To { get; set; }
    public string? Feature { get; set; }
    public string? Status { get; set; }
    public string? Search { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}
