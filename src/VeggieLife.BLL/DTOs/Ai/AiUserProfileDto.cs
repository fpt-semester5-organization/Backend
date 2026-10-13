using System.Collections.Generic;

namespace VeggieLife.BLL.DTOs.Ai;

/// <summary>
/// Hồ sơ thể trạng và sở thích ăn uống gửi sang AI Service (Decision D109, BR-SCI-04).
/// </summary>
public class AiUserProfileDto
{
    public string? DietarySchool { get; set; }
    public bool RequiresAlliumFree { get; set; }
    public List<string> Allergens { get; set; } = new();
    public decimal? HeightCm { get; set; }
    public decimal? CurrentWeightKg { get; set; }
    public decimal? TargetWeightKg { get; set; }
    public decimal? TargetDailyCalories { get; set; }
    public string? HealthGoal { get; set; }
}
