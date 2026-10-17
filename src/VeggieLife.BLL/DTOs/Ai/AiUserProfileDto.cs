using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace VeggieLife.BLL.DTOs.Ai;

/// <summary>
/// Hồ sơ thể trạng và sở thích ăn uống gửi sang AI Service (dietarySchoolCode, allergenGroups, forbiddenFoodGroups).
/// </summary>
public class AiUserProfileDto
{
    [JsonPropertyName("dietarySchoolCode")]
    public string? DietarySchoolCode { get; set; }

    [JsonPropertyName("allergenGroups")]
    public List<string> AllergenGroups { get; set; } = new();

    [JsonPropertyName("forbiddenFoodGroups")]
    public List<string> ForbiddenFoodGroups { get; set; } = new();

    [JsonIgnore]
    public string? DietarySchool
    {
        get => DietarySchoolCode;
        set => DietarySchoolCode = value;
    }

    [JsonIgnore]
    public bool RequiresAlliumFree
    {
        get => ForbiddenFoodGroups.Contains("ALLIUM");
        set
        {
            if (value && !ForbiddenFoodGroups.Contains("ALLIUM"))
            {
                ForbiddenFoodGroups.Add("ALLIUM");
            }
            else if (!value)
            {
                ForbiddenFoodGroups.Remove("ALLIUM");
            }
        }
    }

    [JsonIgnore]
    public List<string> Allergens
    {
        get => AllergenGroups;
        set => AllergenGroups = value;
    }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonPropertyName("heightCm")]
    public decimal? HeightCm { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonPropertyName("currentWeightKg")]
    public decimal? CurrentWeightKg { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonPropertyName("targetWeightKg")]
    public decimal? TargetWeightKg { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonPropertyName("targetDailyCalories")]
    public decimal? TargetDailyCalories { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonPropertyName("healthGoal")]
    public string? HealthGoal { get; set; }
}
