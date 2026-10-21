namespace VeggieLife.BLL.DTOs.Admin.Venues;

/// <summary>
/// DTO dữ liệu trả về khi cập nhật trạng thái quán chay thành công (FN-ADM-14).
/// </summary>
public class UpdateAdminVenueResponseData
{
    public int VenueId { get; set; }
    public bool IsActive { get; set; }
}
