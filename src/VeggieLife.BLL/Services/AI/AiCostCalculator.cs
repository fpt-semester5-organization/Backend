using System;

namespace VeggieLife.BLL.Services.AI;

/// <summary>
/// Tiện ích tính toán chi phí AI ước tính theo mức tiêu thụ Token thực tế (LLMOps).
/// Căn cứ: Bảng giá niêm yết Google Gemini 2.5 Flash:
/// - Input (Prompt): $0.075 / 1,000,000 tokens
/// - Output (Completion): $0.300 / 1,000,000 tokens
/// Tuân thủ định dạng CSDL: DECIMAL(8, 4) (làm tròn 4 chữ số thập phân, sàn tối thiểu $0.0001 khi có tiêu thụ token).
/// </summary>
public static class AiCostCalculator
{
    public const decimal PromptPricePerMillion = 0.075m;
    public const decimal CompletionPricePerMillion = 0.300m;

    /// <summary>
    /// Tính toán chi phí USD dựa trên số lượng Prompt Tokens và Completion Tokens thực tế.
    /// </summary>
    /// <param name="promptTokens">Số lượng token ngữ cảnh đầu vào.</param>
    /// <param name="completionTokens">Số lượng token mô hình sinh ra.</param>
    /// <returns>Chi phí USD dạng decimal(8,4).</returns>
    public static decimal Calculate(int promptTokens, int completionTokens)
    {
        if (promptTokens <= 0 && completionTokens <= 0)
        {
            return 0.0000m;
        }

        var rawCost = ((decimal)Math.Max(0, promptTokens) * PromptPricePerMillion +
                       (decimal)Math.Max(0, completionTokens) * CompletionPricePerMillion) / 1_000_000m;

        // Làm tròn đến 4 chữ số thập phân theo kiểu Round AwayFromZero
        var rounded = Math.Round(rawCost, 4, MidpointRounding.AwayFromZero);

        // Đảm bảo mức sàn 0.0001 USD nếu có tiêu thụ token để không bị mất vết chi phí
        return Math.Max(0.0001m, rounded);
    }

    /// <summary>
    /// Tính toán chi phí USD khi chỉ có tổng số tokens (ước tính tỷ lệ 70% prompt / 30% completion).
    /// </summary>
    /// <param name="totalTokens">Tổng số tokens tiêu thụ.</param>
    /// <returns>Chi phí USD dạng decimal(8,4).</returns>
    public static decimal CalculateFromTotal(int totalTokens)
    {
        if (totalTokens <= 0)
        {
            return 0.0000m;
        }

        var estimatedPrompt = (int)Math.Round(totalTokens * 0.70);
        var estimatedCompletion = totalTokens - estimatedPrompt;
        return Calculate(estimatedPrompt, estimatedCompletion);
    }
}
