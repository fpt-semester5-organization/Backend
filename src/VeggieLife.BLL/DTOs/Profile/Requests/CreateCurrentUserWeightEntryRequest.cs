using System;

namespace VeggieLife.BLL.DTOs.Profile.Requests;

/// <summary>
/// DTO yêu cầu ghi nhận một mốc cân nặng mới của người dùng (FN-PROF-06).
/// </summary>
public class CreateCurrentUserWeightEntryRequest
{
    /// <summary>
    /// Cân nặng thực tế tính bằng kg (khoảng cho phép: 20–300 kg).
    /// </summary>
    public decimal WeightKg { get; set; }

    /// <summary>
    /// Ngày ghi nhận cân nặng (YYYY-MM-DD, mặc định: hôm nay, không được ở tương lai).
    /// </summary>
    public DateOnly? LoggedDate { get; set; }

    /// <summary>
    /// Ghi chú kèm theo mốc cân nặng (tối đa 255 ký tự).
    /// </summary>
    public string? Note { get; set; }
}
