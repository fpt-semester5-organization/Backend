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
}
