namespace VeggieLife.BLL.DTOs.Ai;

/// <summary>
/// Dữ liệu kiểm toán AI (Raw Prompt & Raw Response) phục vụ lưu vết Decision D118.
/// </summary>
public class ModerationAudit
{
    public string Prompt { get; set; } = string.Empty;
    public string RawResponse { get; set; } = string.Empty;
}
