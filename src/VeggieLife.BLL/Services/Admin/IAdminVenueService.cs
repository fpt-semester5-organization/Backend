using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using VeggieLife.BLL.Common.Responses;
using VeggieLife.BLL.DTOs.Admin.Venues;

namespace VeggieLife.BLL.Services.Admin;

/// <summary>
/// Interface dịch vụ quản trị quán chay và đồng bộ nguồn dữ liệu bên ngoài (FN-ADM-14).
/// </summary>
public interface IAdminVenueService
{
    /// <summary>
    /// Lấy danh sách quán chay theo bộ lọc (trạng thái ẩn/hiện, nguồn, từ khóa) và phân trang.
    /// </summary>
    Task<(IReadOnlyList<AdminVenueListItemDto> Items, PagedMeta Meta)> GetVenuesAsync(
        AdminVenueFilterRequest request,
        CancellationToken ct = default);

    /// <summary>
    /// Cập nhật trạng thái ẩn (IsActive = false) hoặc hiện lại (IsActive = true) cho quán chay (BR-ADM-MAP-001).
    /// </summary>
    Task<UpdateAdminVenueResponseData> UpdateVenueStatusAsync(
        int id,
        UpdateAdminVenueRequest request,
        int adminId,
        CancellationToken ct = default);

    /// <summary>
    /// Kích hoạt xếp job đồng bộ dữ liệu quán chay từ HappyCow / Google Places (không đồng bộ thực đơn, D100).
    /// </summary>
    Task<SyncAdminVenuesResponseData> SyncVenuesAsync(
        SyncAdminVenuesRequest request,
        int adminId,
        CancellationToken ct = default);
}
