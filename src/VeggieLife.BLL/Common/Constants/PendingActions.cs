using System;
using System.Collections.Generic;

namespace VeggieLife.BLL.Common.Constants;

/// <summary>
/// Các loại hành động dang dở của Khách vãng lai trước khi bị chặn để lưu ngữ cảnh quay lại (FN-AUTH-03, BR-GUEST-AUTH-01).
/// LIKE | COMMENT | POST | UPLOAD_VIDEO | REPORT | HEALTH_PROFILE | MEAL_PLAN | SHOPPING_LIST | FRIDGE
/// </summary>
public static class PendingActions
{
    public const string Like = "LIKE";
    public const string Comment = "COMMENT";
    public const string Post = "POST";
    public const string UploadVideo = "UPLOAD_VIDEO";
    public const string Report = "REPORT";
    public const string HealthProfile = "HEALTH_PROFILE";
    public const string MealPlan = "MEAL_PLAN";
    public const string ShoppingList = "SHOPPING_LIST";
    public const string Fridge = "FRIDGE";

    public static readonly HashSet<string> ValidActions = new(StringComparer.OrdinalIgnoreCase)
    {
        Like,
        Comment,
        Post,
        UploadVideo,
        Report,
        HealthProfile,
        MealPlan,
        ShoppingList,
        Fridge
    };

    public static bool IsValid(string? action) => !string.IsNullOrWhiteSpace(action) && ValidActions.Contains(action);
}
