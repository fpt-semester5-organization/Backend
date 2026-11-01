namespace VeggieLife.BLL.DTOs.Admin.Llmops;

/// <summary>
/// Một điểm dữ liệu tiêu thụ AI trong chuỗi thời gian (FN-ADM-16).
/// </summary>
public class CostPointDto
{
    /// <summary>
    /// Ngày ghi nhận (YYYY-MM-DD).
    /// </summary>
    public string? Date { get; set; }

    /// <summary>
    /// Mã tính năng AI (AI_CHAT, MEAL_PLAN_GEN, AI_FRIDGE, AI_BARCODE, SUMMARIZE_VIDEO, CONTENT_MODERATION, AI_EMBEDDING, OTHER).
    /// </summary>
    public string? Feature { get; set; }

    /// <summary>
    /// Số lượng token tiêu thụ.
    /// </summary>
    public int Tokens { get; set; }

    /// <summary>
    /// Chi phí ước tính bằng USD.
    /// </summary>
    public decimal CostUsd { get; set; }
}
