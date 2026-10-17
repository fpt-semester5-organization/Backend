namespace VeggieLife.BLL.DTOs.Guest.Responses;

/// <summary>
/// DTO kết quả tiêu hao lượt dùng thử.
/// </summary>
public class GuestTrialResultDto
{
    public string TrialType { get; set; } = string.Empty;
    public int Used { get; set; }
    public int Limit { get; set; }
    public int Remaining { get; set; }
    public string Message { get; set; } = string.Empty;
}
