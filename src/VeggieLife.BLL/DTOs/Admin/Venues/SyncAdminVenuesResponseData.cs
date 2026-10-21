namespace VeggieLife.BLL.DTOs.Admin.Venues;

/// <summary>
/// DTO dữ liệu trả về khi xếp job đồng bộ quán chay thành công (FN-ADM-14).
/// </summary>
public class SyncAdminVenuesResponseData
{
    public string JobId { get; set; } = string.Empty;
}
