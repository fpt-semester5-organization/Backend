using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using VeggieLife.BLL.Common.Responses;
using VeggieLife.BLL.DTOs.Admin.AuditLogs;

namespace VeggieLife.BLL.Services.Admin;

/// <summary>
/// Giao diện dịch vụ tra cứu nhật ký kiểm toán (AuditLogs) dành cho Quản trị viên (FN-ADM-17).
/// Tuân thủ BR-ADM-SYS-002: Bảng chỉ ghi (Append-only), nghiêm cấm sửa hoặc xóa.
/// </summary>
public interface IAdminAuditLogService
{
    /// <summary>
    /// Tra cứu, tìm kiếm, lọc và phân trang danh sách nhật ký kiểm toán.
    /// Dữ liệu nhạy cảm được tự động che giấu.
    /// </summary>
    Task<(IReadOnlyList<AdminAuditLogListItemDto> Logs, PagedMeta Meta)> GetAuditLogsAsync(
        AdminAuditLogFilterRequest filter,
        CancellationToken ct = default);
}
