namespace VeggieLife.BLL.DTOs.Settings;

/// <summary>
/// DTO chứa các tham số cấu hình công khai của hệ thống để Frontend hiển thị và đồng bộ theo thời gian thực.
/// Không chứa các thông tin bảo mật hay ngưỡng tài chính nội bộ.
/// </summary>
public class PublicSystemSettingsDto
{
    // Hạn mức Thực đơn & Đổi món (MEAL_PLAN)
    public int MealPlanWeeklyLimitFree { get; set; } = 1;
    public int MealPlanWeeklyLimitVip { get; set; } = 3;
    public int MealSwapLimitDraft { get; set; } = 21;
    public int MealSwapLimitActive { get; set; } = 21;
    public int MealSwapCalorieTolerancePercent { get; set; } = 15;

    // Định mức AI Chatbot & Quét tủ lạnh (AI_QUOTA)
    public int ChatLimitDailyGuest { get; set; } = 3;
    public int ChatLimitDailyFree { get; set; } = 20;
    public int ChatLimitDailyVip { get; set; } = 200;
    public int FridgeScanLimitDailyFree { get; set; } = 10;
    public int FridgeScanLimitDailyVip { get; set; } = 30;

    // Tài liệu Pháp lý & Chính sách công khai (GENERAL / LEGAL)
    public string? LegalUserAgreementUrl { get; set; }
    public string? LegalPrivacyPolicyUrl { get; set; }
    public string? LegalTermsOfServiceUrl { get; set; }
    public string? LegalVeganDisclaimerUrl { get; set; }
}
