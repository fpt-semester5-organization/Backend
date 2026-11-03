namespace VeggieLife.BLL.DTOs.Admin.Llmops;

/// <summary>
/// Ô dữ liệu tổng hợp chi phí và token cho ma trận (FN-ADM-16).
/// </summary>
public class CostCellDto
{
    /// <summary>
    /// Tổng chi phí ước tính bằng USD.
    /// </summary>
    public decimal CostUsd { get; set; }

    /// <summary>
    /// Tổng số lượng token tiêu thụ.
    /// </summary>
    public int Tokens { get; set; }

    /// <summary>
    /// Số lượng Input / Prompt Tokens (ngữ cảnh RAG, ảnh tải lên, prompt).
    /// </summary>
    public long PromptTokens { get; set; }

    /// <summary>
    /// Số lượng Output / Completion Tokens (câu trả lời do mô hình sinh).
    /// </summary>
    public long CompletionTokens { get; set; }
}
