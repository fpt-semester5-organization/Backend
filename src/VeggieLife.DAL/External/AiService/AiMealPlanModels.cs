using System.Collections.Generic;
using System.Text.Json.Serialization;

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
/// DTO thông tin sử dụng token và mô hình từ AI-Service.
/// </summary>
public class AiUsageDto
{
    [JsonPropertyName("model")]
    public string Model { get; set; } = "gemini-2.5-flash";

    [JsonPropertyName("promptTokens")]
    public int PromptTokens { get; set; }

    [JsonPropertyName("completionTokens")]
    public int CompletionTokens { get; set; }

    [JsonPropertyName("latencyMs")]
    public int LatencyMs { get; set; }

    [JsonPropertyName("temperature")]
    public decimal Temperature { get; set; }
}

/// <summary>
/// Phản hồi từ ai-service (/meal/generate).
/// </summary>
public class AiMealPlanResponse
{
    [JsonPropertyName("success")]
    public bool Success { get; set; } = true;

    [JsonPropertyName("slots")]
    public List<AiMealSlotResultDto> Slots { get; set; } = new();

    [JsonPropertyName("usage")]
    public AiUsageDto? Usage { get; set; }

    public int TokensConsumed => (Usage?.PromptTokens ?? 0) + (Usage?.CompletionTokens ?? 0);

    public string ModelVersion => Usage?.Model ?? "gemini-2.5-flash";

    public string? ErrorMessage { get; set; }

    public int ExecutionTimeMs { get; set; }
}

/// <summary>
/// Vị trí bữa ăn do AI đề xuất.
/// </summary>
public class AiMealSlotResultDto
{
    [JsonPropertyName("dayOfWeek")]
    public string DayOfWeek { get; set; } = null!; // MON, TUE, WED, THU, FRI, SAT, SUN

    [JsonPropertyName("mealType")]
    public string MealType { get; set; } = null!;  // BREAKFAST, LUNCH, DINNER

    [JsonPropertyName("recipeId")]
    public int RecipeId { get; set; }
}
