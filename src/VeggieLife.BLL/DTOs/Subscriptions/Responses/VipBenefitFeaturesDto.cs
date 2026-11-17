namespace VeggieLife.BLL.DTOs.Subscriptions.Responses;

/// <summary>
/// Quyền lợi và hạn mức tài nguyên được phân giải cụ thể cho người dùng (FN-SUB-01..04, BR-PAY-SUB-01).
/// Ưu tiên lấy từ Snapshot_Features_JSON của giao dịch đang hoạt động (Grandfathering), fallback SystemSettings nếu thiếu.
/// </summary>
public class VipBenefitFeaturesDto
{
    public bool IsVip { get; set; }
    public int PlanVersion { get; set; } = 1;
    public string? PlanName { get; set; }

    // Hạn mức tài nguyên
    public int ChatLimitDaily { get; set; } = 20;
    public int MealPlanLimitWeekly { get; set; } = 1;
    public int FridgeScanLimitDaily { get; set; } = 10;

    // Đặc quyền tính năng
    public bool ExclusiveBadge { get; set; }
    public bool AdFree { get; set; }
    public bool PriorityAiResponse { get; set; }
    public bool VipDietitianAi { get; set; }
    public bool VideoToRecipeStt { get; set; }
    public string? Description { get; set; }
}
