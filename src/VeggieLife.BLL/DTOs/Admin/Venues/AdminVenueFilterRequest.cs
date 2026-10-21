namespace VeggieLife.BLL.DTOs.Admin.Venues;

/// <summary>
/// DTO tham số lọc danh sách quán chay cho Quản trị viên (FN-ADM-14).
/// </summary>
public class AdminVenueFilterRequest
{
    public bool? IsActive { get; set; }
    public string? Source { get; set; }
    public string? Keyword { get; set; }
    public int Page { get; set; } = 1;
    public int Size { get; set; } = 20;
}
