namespace VeggieLife.BLL.DTOs.Admin.Llmops;

/// <summary>
/// Chi phí và số lượng token tiêu thụ theo từng mốc giờ trong ngày (FN-ADM-16 / Dashboard).
/// </summary>
public class HourlyCostDto
{
    /// <summary>
    /// Giờ trong ngày (0 - 23).
    /// </summary>
    public int Hour { get; set; }

    /// <summary>
    /// Chi phí phát sinh trong giờ đó (USD).
    /// </summary>
    public decimal CostUsd { get; set; }

    /// <summary>
    /// Số lượng token tiêu thụ trong giờ đó.
    /// </summary>
    public long Tokens { get; set; }
}
