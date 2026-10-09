using System;

namespace VeggieLife.BLL.DTOs.Profile.Responses;

/// <summary>
/// DTO mục lịch sử cân nặng trong danh sách tra cứu theo khoảng thời gian (FN-PROF-06).
/// </summary>
public class WeightHistoryEntryDto
{
    /// <summary>
    /// Mã bản ghi lịch sử cân nặng.
    /// </summary>
    public long LogId { get; set; }

    /// <summary>
    /// Mức cân nặng đã ghi nhận (kg).
    /// </summary>
    public decimal WeightKg { get; set; }

    /// <summary>
    /// Ngày ghi nhận cân nặng (YYYY-MM-DD).
    /// </summary>
    public DateOnly LoggedDate { get; set; }
}
