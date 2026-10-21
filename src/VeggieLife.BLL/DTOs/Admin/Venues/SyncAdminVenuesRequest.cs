namespace VeggieLife.BLL.DTOs.Admin.Venues;

/// <summary>
/// DTO yêu cầu đồng bộ quán chay từ nguồn bên ngoài theo tỉnh/thành (FN-ADM-14).
/// </summary>
public class SyncAdminVenuesRequest
{
    public string Province { get; set; } = string.Empty;
}
