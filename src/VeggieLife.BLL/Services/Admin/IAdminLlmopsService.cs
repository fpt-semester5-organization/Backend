using System.Threading;
using System.Threading.Tasks;
using VeggieLife.BLL.DTOs.Admin.Llmops;

namespace VeggieLife.BLL.Services.Admin;

/// <summary>
/// Giao diện dịch vụ giám sát token và chi phí AI (LLMOps) dành cho Quản trị viên (FN-ADM-16).
/// </summary>
public interface IAdminLlmopsService
{
    /// <summary>
    /// Lấy dữ liệu bảng điều khiển LLMOps tổng hợp lượng token, chi phí và hạn ngạch theo ngày / tính năng.
    /// </summary>
    Task<AdminLlmopsDashboardResponseData> GetDashboardAsync(
        AdminLlmopsFilterRequest filter,
        CancellationToken ct = default);

    /// <summary>
    /// Tra cứu chi tiết danh sách nhật ký gọi AI phân trang kèm bộ lọc tính năng và trạng thái.
    /// </summary>
    Task<GetAdminLlmopsLogsResponseData> GetLogsAsync(
        AdminLlmopsLogFilterRequest filter,
        CancellationToken ct = default);
}
