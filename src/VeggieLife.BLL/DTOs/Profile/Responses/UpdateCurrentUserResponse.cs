namespace VeggieLife.BLL.DTOs.Profile.Responses;

/// <summary>
/// Dữ liệu phản hồi cập nhật thông tin cá nhân (FN-PROF-02).
/// </summary>
public class UpdateCurrentUserResponse
{
    public int UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
}
