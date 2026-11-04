namespace VeggieLife.BLL.DTOs.Admin.Llmops;

/// <summary>
/// Thông tin người dùng tiêu thụ nhiều Token AI nhất trong kỳ lọc (Top Spender).
/// </summary>
public class TopSpenderDto
{
    public int UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public string Role { get; set; } = string.Empty;
    public long TotalTokens { get; set; }
    public decimal TotalCostUsd { get; set; }
    public int RequestCount { get; set; }
}
