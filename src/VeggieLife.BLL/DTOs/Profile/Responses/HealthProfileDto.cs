using System;

namespace VeggieLife.BLL.DTOs.Profile.Responses;

/// <summary>
/// Thông tin rút gọn về trường phái ăn chay gắn liền với hồ sơ sức khỏe.
/// </summary>
public class DietarySchoolBriefDto
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
}

/// <summary>
/// DTO chi tiết hồ sơ sức khỏe và các chỉ số dinh dưỡng dẫn xuất (FN-PROF-04, FN-PROF-01).
/// </summary>
public class HealthProfileDto
{
    public string Gender { get; set; } = string.Empty;
    public string DateOfBirth { get; set; } = string.Empty;
    public decimal HeightCm { get; set; }
    public decimal CurrentWeightKg { get; set; }
    public decimal? TargetWeightKg { get; set; }
    public decimal Bmi { get; set; }
    public string BmiCategory { get; set; } = string.Empty;
    public decimal BmrKcal { get; set; }
    public decimal TdeeKcal { get; set; }
    public decimal TargetDailyCalories { get; set; }
    public string HealthGoal { get; set; } = string.Empty;
    public DietarySchoolBriefDto DietarySchool { get; set; } = new();
    public bool RequiresAlliumFree { get; set; }
    public string RegionPreference { get; set; } = string.Empty;
    public bool IsStaleMetrics { get; set; }
    public DateTime UpdatedAt { get; set; }
}
