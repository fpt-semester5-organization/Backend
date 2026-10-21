namespace VeggieLife.BLL.DTOs.Admin.Venues;

/// <summary>
/// DTO yêu cầu cập nhật trạng thái ẩn/hiện quán chay (FN-ADM-14).
/// </summary>
public class UpdateAdminVenueRequest
{
    public bool IsActive { get; set; }
}
