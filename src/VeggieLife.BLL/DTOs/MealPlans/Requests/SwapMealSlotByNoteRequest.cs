using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace VeggieLife.BLL.DTOs.MealPlans.Requests;

/// <summary>
/// Yêu cầu đổi món ăn theo ghi chú hoặc khẩu vị tùy chỉnh bằng AI (FN-MEAL-03).
/// </summary>
public class SwapMealSlotByNoteRequest
{
    /// <summary>
    /// Ghi chú, mong muốn hoặc yêu cầu cụ thể của người dùng (ví dụ: "Muốn ăn món thanh đạm, ít dầu mỡ, nhiều nấm").
    /// </summary>
    [Required(ErrorMessage = "Ghi chú yêu cầu đổi món là bắt buộc.")]
    [MaxLength(500, ErrorMessage = "Ghi chú đổi món không được vượt quá 500 ký tự.")]
    public string Note { get; set; } = null!;

    /// <summary>
    /// Danh sách mã món ăn cần loại trừ thêm (không muốn chọn).
    /// </summary>
    public List<int>? ExcludedRecipeIds { get; set; }
}

