using System.Collections.Generic;

namespace VeggieLife.DAL.External.AiService;

/// <summary>
/// Yêu cầu gửi tới ai-service (/meal/generate) để xếp 21 bữa ăn trong tuần.
/// </summary>
public class AiMealPlanRequest
{
    public decimal TargetDailyCalories { get; set; }
    public string DietarySchoolCode { get; set; } = null!;
    public bool RequiresAlliumFree { get; set; }
    public List<string> ForbiddenFoodGroups { get; set; } = new();
    public List<int> ExcludedRecipeIds { get; set; } = new();
    public List<AiCandidateRecipeDto> CandidateRecipes { get; set; } = new();
    public string? CustomInstructions { get; set; }
}

/// <summary>
/// DTO đại diện cho công thức nấu ăn ứng viên gửi cho AI xếp lịch.
/// </summary>
public class AiCandidateRecipeDto
{
    public int RecipeId { get; set; }
    public string Title { get; set; } = null!;
    public decimal CaloriesKcal { get; set; }
    public string? CategoryName { get; set; }
    public bool UsesFridgeItem { get; set; }
    public string? MealTypePreference { get; set; }
}

/// <summary>
/// Phản hồi từ ai-service (/meal/generate).
/// </summary>
public class AiMealPlanResponse
{
    public bool Success { get; set; }
    public List<AiMealSlotResultDto> Slots { get; set; } = new();
    public int TokensConsumed { get; set; }
    public string ModelVersion { get; set; } = "gemini-2.5-flash";
    public string? ErrorMessage { get; set; }
    public int ExecutionTimeMs { get; set; }
}

/// <summary>
/// Vị trí bữa ăn do AI đề xuất.
/// </summary>
public class AiMealSlotResultDto
{
    public string DayOfWeek { get; set; } = null!; // MON, TUE, WED, THU, FRI, SAT, SUN
    public string MealType { get; set; } = null!;  // BREAKFAST, LUNCH, DINNER
    public int RecipeId { get; set; }
}
