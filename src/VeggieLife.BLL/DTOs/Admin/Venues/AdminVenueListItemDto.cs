namespace VeggieLife.BLL.DTOs.Admin.Venues;

/// <summary>
/// DTO đại diện cho một quán chay trong danh sách quản trị (FN-ADM-14).
/// </summary>
public class AdminVenueListItemDto
{
    public int VenueId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}
