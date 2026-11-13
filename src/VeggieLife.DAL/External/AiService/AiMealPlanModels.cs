using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace VeggieLife.DAL.External.AiService;

/// <summary>
/// Yêu cầu gửi tới ai-service (/meal/generate) để xếp các bữa ăn theo số ngày mong muốn.
/// </summary>
public class AiMealPlanRequest
{
    public decimal TargetDailyCalories { get; set; }
    public string DietarySchoolCode { get; set; } = null!;
    public string? DietarySchoolName { get; set; }
    public bool RequiresAlliumFree { get; set; }
    public string? HealthGoal { get; set; }
    public string? RegionPreference { get; set; }
    public string? BudgetTier { get; set; }
    public decimal? TargetWeightKg { get; set; }
    public int DurationDays { get; set; } = 7;
    public string? StartDate { get; set; }
    public List<string> ForbiddenFoodGroups { get; set; } = new();
    public List<int> ExcludedRecipeIds { get; set; } = new();
    public List<AiCandidateRecipeDto> CandidateRecipes { get; set; } = new();
    public List<string> FridgeIngredients { get; set; } = new();
    public string? CustomInstructions { get; set; }
}

/// <summary>
/// DTO đại diện cho nguyên liệu thành phần trong công thức gửi sang AI.
/// </summary>
public class AiRecipeIngredientDto
{
    public int IngredientId { get; set; }
    public string IngredientName { get; set; } = null!;
    public decimal QuantityNeeded { get; set; }
    public string Unit { get; set; } = null!;
    public string? PreparationNote { get; set; }
    public bool IsOptional { get; set; }
}

/// <summary>
/// DTO đại diện cho công thức nấu ăn ứng viên gửi cho AI xếp lịch với đầy đủ chi tiết công thức.
/// </summary>
public class AiCandidateRecipeDto
{
    public int RecipeId { get; set; }
    public string Title { get; set; } = null!;
    public string? Description { get; set; }
    public string? Instructions { get; set; }
    public int PrepTimeMinutes { get; set; }
    public int CookTimeMinutes { get; set; }
    public int Servings { get; set; }
    public string? DifficultyLevel { get; set; }
    public decimal CaloriesKcal { get; set; }
    public string? CategoryName { get; set; }
    public int? FoodId { get; set; }
    public string? FoodName { get; set; }
    public bool UsesFridgeItem { get; set; }
    public string? MealTypePreference { get; set; }
    public int FavoriteCount { get; set; }
    public List<AiRecipeIngredientDto> Ingredients { get; set; } = new();
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

    [JsonPropertyName("totalTokens")]
    public int TotalTokens { get; set; }

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
/// Vị trí bữa ăn do AI đề xuất kèm bằng chứng (Kcal, thời gian nấu, số tim, lý do khớp).
/// </summary>
public class AiMealSlotResultDto
{
    [JsonPropertyName("dayOfWeek")]
    public string DayOfWeek { get; set; } = null!; // MON, TUE, WED, THU, FRI, SAT, SUN

    [JsonPropertyName("mealType")]
    public string MealType { get; set; } = null!;  // BREAKFAST, LUNCH, DINNER

    [JsonPropertyName("recipeId")]
    public int RecipeId { get; set; }

    [JsonPropertyName("caloriesKcal")]
    public decimal? CaloriesKcal { get; set; }

    [JsonPropertyName("cookTimeMinutes")]
    public int? CookTimeMinutes { get; set; }

    [JsonPropertyName("favoriteCount")]
    public int? FavoriteCount { get; set; }

    [JsonPropertyName("matchReason")]
    public string? MatchReason { get; set; }
}

/// <summary>
/// Yêu cầu gửi tới ai-service (/meal/swap-by-note) để đổi món theo ghi chú/khẩu vị người dùng.
/// </summary>
public class AiSwapMealByNoteRequest
{
    public int CurrentRecipeId { get; set; }
    public string SwapNote { get; set; } = null!;
    public string DietarySchoolCode { get; set; } = null!;
    public string? DietarySchoolName { get; set; }
    public string MealType { get; set; } = null!;
    public decimal TargetCalories { get; set; }
    public List<int> ExcludedRecipeIds { get; set; } = new();
    public List<AiCandidateRecipeDto> CandidateRecipes { get; set; } = new();
    public string? HealthGoal { get; set; }
    public string? RegionPreference { get; set; }
    public string? BudgetTier { get; set; }
    public bool? RequiresAlliumFree { get; set; }
}

/// <summary>
/// Phản hồi từ ai-service (/meal/swap-by-note).
/// </summary>
public class AiSwapMealByNoteResponse
{
    [JsonPropertyName("success")]
    public bool Success { get; set; } = true;

    [JsonPropertyName("selectedRecipeId")]
    public int SelectedRecipeId { get; set; }

    [JsonPropertyName("reason")]
    public string? Reason { get; set; }

    [JsonPropertyName("caloriesKcal")]
    public decimal? CaloriesKcal { get; set; }

    [JsonPropertyName("cookTimeMinutes")]
    public int? CookTimeMinutes { get; set; }

    [JsonPropertyName("favoriteCount")]
    public int? FavoriteCount { get; set; }

    [JsonPropertyName("usage")]
    public AiUsageDto? Usage { get; set; }

    public int TokensConsumed => (Usage?.PromptTokens ?? 0) + (Usage?.CompletionTokens ?? 0);

    public string ModelVersion => Usage?.Model ?? "gemini-2.5-flash";

    public int ExecutionTimeMs { get; set; }

    public string? ErrorMessage { get; set; }
}
