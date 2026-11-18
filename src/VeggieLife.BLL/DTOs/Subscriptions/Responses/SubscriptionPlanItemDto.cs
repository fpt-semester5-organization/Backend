namespace VeggieLife.BLL.DTOs.Subscriptions.Responses;

/// <summary>
/// Thông tin chi tiết một gói hội viên VIP (FN-SUB-01).
/// </summary>
public class SubscriptionPlanItemDto
{
    public int PlanId { get; set; }
    public string PlanName { get; set; } = string.Empty;
    public int DurationDays { get; set; }
    public long PriceVnd { get; set; }
    public bool IsActive { get; set; }

    /// <summary>
    /// Phiên bản gói: phiên bản đang bán (danh sách gói) hoặc phiên bản lúc mua (gói đang dùng của người dùng).
    /// </summary>
    public int Version { get; set; } = 1;

    /// <summary>
    /// Bốn hạn mức người mua gói nhận được (đọc từ Features_JSON). Null ở gói đang dùng: xem `features` của người dùng.
    /// </summary>
    public PlanQuotaBenefitsDto? Benefits { get; set; }
}

/// <summary>
/// Bốn hạn mức VIP của một gói (D121, D122).
/// </summary>
public class PlanQuotaBenefitsDto
{
    public int ChatLimitDaily { get; set; }
    public int MealPlanLimitWeekly { get; set; }
    public int FridgeScanLimitDaily { get; set; }
    public int BarcodeScanLimitDaily { get; set; }
}
