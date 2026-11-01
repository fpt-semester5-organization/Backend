namespace VeggieLife.BLL.DTOs.Admin.Llmops;

/// <summary>
/// Cơ cấu phân bổ mức độ tiêu thụ Token AI và Chi phí USD theo hạng hội viên.
/// </summary>
public class UserTierDistributionDto
{
    public long VipTokens { get; set; }
    public decimal VipCostUsd { get; set; }

    public long MemberTokens { get; set; }
    public decimal MemberCostUsd { get; set; }

    public long SystemTokens { get; set; }
    public decimal SystemCostUsd { get; set; }
}
