using System.Collections.Generic;

namespace VeggieLife.BLL.DTOs.Admin.Llmops;

/// <summary>
/// Dữ liệu phản hồi phân trang danh sách nhật ký AIQuotaLogs cho Quản trị viên.
/// </summary>
public class GetAdminLlmopsLogsResponseData
{
    public List<AdminLlmopsLogItemDto> Items { get; set; } = new();
    public int Total { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public int TotalPages { get; set; }
}
