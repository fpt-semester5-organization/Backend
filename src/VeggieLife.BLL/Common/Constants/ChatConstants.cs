namespace VeggieLife.BLL.Common.Constants;

/// <summary>
/// Các hằng số nghiệp vụ dùng cho Trợ lý AI và Chatbot tư vấn dinh dưỡng (FN-CHAT-01..06, BR-USR-10, BR-GUEST-AI-01).
/// </summary>
public static class ChatConstants
{
    // === Context Types ===
    public const string ContextNutritionConsultation = "NUTRITION_CONSULTATION";
    public const string ContextMealPlanAssistant = "MEAL_PLAN_ASSISTANT";
    public const string ContextEmptyFridge = "EMPTY_FRIDGE";

    // === Sender Types ===
    public const string SenderUser = "USER";
    public const string SenderAssistant = "ASSISTANT";

    // === Lời miễn trừ trách nhiệm y khoa bắt buộc (BR-USR-10, D108) ===
    public const string MedicalDisclaimer = "Khuyến nghị mang tính chất tham khảo dinh dưỡng, vui lòng tham vấn ý kiến bác sĩ chuyên khoa đối với các bệnh lý nền.";

    // === Hạn mức sử dụng AI (BR-GUEST-02, BR-USR-10, BR-PAY-SUB-01) ===
    public const int GuestAiLimit = 3;
    public const int FreeMemberDailyLimit = 20;
    public const int VipDailyLimit = 50;

    // === Mã tính năng AI Quota Logs (BR-ADM-AI-003) ===
    public const string FeatureCodeAiChat = "AI_CHAT";
    public const string ActionAiRequest = "AI_REQUEST";
}
