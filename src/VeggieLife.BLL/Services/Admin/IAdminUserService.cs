using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using VeggieLife.BLL.Common.Responses;
using VeggieLife.BLL.DTOs.Admin.Users;

namespace VeggieLife.BLL.Services.Admin;

/// <summary>
/// Giao diện dịch vụ Quản trị người dùng dành cho Quản trị viên (FN-ADM-01, FN-ADM-02, FN-ADM-03).
/// Tuân thủ: BR-ADM-USR-001 (Bảo toàn Admin cuối cùng), BR-ADM-USR-002 (Thang kỷ luật lũy tiến), BR-ADM-SYS-002 (Nhật ký AuditLogs), BR-AUTH-004 (Thu hồi phiên).
/// </summary>
public interface IAdminUserService
{
    /// <summary>
    /// FN-ADM-01: Tra cứu, tìm kiếm và phân trang danh sách người dùng trong hệ thống.
    /// </summary>
    Task<(IReadOnlyList<AdminUserListItemDto> Items, PagedMeta Meta)> GetUsersAsync(
        AdminUserListFilterRequest filter,
        CancellationToken ct = default);

    /// <summary>
    /// FN-ADM-02: Cập nhật trạng thái và áp dụng chế tài kỷ luật tài khoản (Cảnh cáo, Hạn chế bình luận, Tạm khóa, Cấm, Mở lại).
    /// </summary>
    Task<UpdateAdminUserStatusResponseData> UpdateUserStatusAsync(
        int userId,
        UpdateAdminUserStatusRequest request,
        int adminId,
        CancellationToken ct = default);

    /// <summary>
    /// FN-ADM-03: Xem tổng hợp các nội dung đã tạo (bài viết, công thức, video, bình luận) và lịch sử vi phạm của người dùng.
    /// </summary>
    Task<(AdminUserContentsResponseData Data, PagedMeta Meta)> GetUserContentsAsync(
        int userId,
        AdminUserContentsFilterRequest filter,
        CancellationToken ct = default);
}
