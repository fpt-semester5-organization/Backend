namespace VeggieLife.BLL.DTOs.Admin.ENumbers;

/// <summary>
/// Dữ liệu phản hồi sau khi cập nhật mã E-number thành công (FN-ADM-11).
/// </summary>
public class UpdateAdminENumberResponseData
{
    public string Code { get; set; } = string.Empty;
    public string VeganStatus { get; set; } = string.Empty;
}
