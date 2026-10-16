namespace VeggieLife.BLL.DTOs.Subscriptions.Responses;

/// <summary>
/// Chi tiết quyền lợi tài khoản (FN-SUB-01, BR-PAY-SUB-01).
/// </summary>
public class BenefitDetailDto
{
    public int MealPlansPerWeek { get; set; }
    public int? ChatPerDay { get; set; }
    public int FridgePhotosPerDay { get; set; }
}

/// <summary>
/// Bảng so sánh quyền lợi giữa tài khoản Free và VIP (FN-SUB-01).
/// </summary>
public class SubscriptionBenefitsDto
{
    public BenefitDetailDto Free { get; set; } = new();
    public BenefitDetailDto Vip { get; set; } = new();
}
