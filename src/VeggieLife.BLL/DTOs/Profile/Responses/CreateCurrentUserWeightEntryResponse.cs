namespace VeggieLife.BLL.DTOs.Profile.Responses;

/// <summary>
/// DTO phản hồi sau khi ghi nhận mốc cân nặng và tính lại chỉ số sức khỏe thành công (FN-PROF-06).
/// </summary>
public class CreateCurrentUserWeightEntryResponse
{
    /// <summary>
    /// Mã bản ghi lịch sử cân nặng mới tạo.
    /// </summary>
    public long LogId { get; set; }

    /// <summary>
    /// Cân nặng vừa ghi nhận (kg).
    /// </summary>
    public decimal WeightKg { get; set; }

    /// <summary>
    /// Chỉ số BMI được tính toán lại theo cân nặng mới.
    /// </summary>
    public decimal Bmi { get; set; }

    /// <summary>
    /// Lượng calo mục tiêu mỗi ngày được tính toán lại.
    /// </summary>
    public decimal TargetDailyCalories { get; set; }
}
