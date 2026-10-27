using System.Collections.Generic;

namespace VeggieLife.BLL.DTOs.Fridge.Responses;

/// <summary>
/// DTO phản hồi gợi ý công thức món ăn từ nguyên liệu tủ lạnh ảo (FN-VIS-03).
/// </summary>
public class FridgeRecipeSuggestionDto
{
    /// <summary>
    /// Mã định danh công thức món ăn.
    /// </summary>
    public int RecipeId { get; set; }

    /// <summary>
    /// Tiêu đề công thức món ăn.
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Đường dẫn ảnh đại diện của món ăn.
    /// </summary>
    public string? ImageUrl { get; set; }

    /// <summary>
    /// Điểm tương đồng nguyên liệu (0.7 - 1.0).
    /// </summary>
    public double Similarity { get; set; }

    /// <summary>
    /// Danh sách nguyên liệu cần có trong công thức mà người dùng còn thiếu trong tủ lạnh.
    /// </summary>
    public IReadOnlyList<MissingIngredientDto> MissingIngredients { get; set; } = new List<MissingIngredientDto>();

    /// <summary>
    /// Đánh dấu công thức có tận dụng nguyên liệu đang NON-FRESH (cần dùng sớm) trong tủ lạnh hay không.
    /// </summary>
    public bool UsesNonFreshItems { get; set; }

    /// <summary>
    /// Đánh dấu công thức đã đáp ứng đủ 100% định lượng của tất cả nguyên liệu có trong tủ.
    /// </summary>
    public bool IsQuantityFullyMatched { get; set; } = true;

    /// <summary>
    /// Có nguyên liệu nào trong tủ chưa đủ định lượng so với yêu cầu của món ăn hay không.
    /// </summary>
    public bool HasInsufficientIngredients { get; set; }

    /// <summary>
    /// Danh sách các nguyên liệu đã có trong tủ nhưng chưa đủ định lượng công thức yêu cầu.
    /// </summary>
    public IReadOnlyList<InsufficientIngredientDto> InsufficientIngredients { get; set; } = new List<InsufficientIngredientDto>();

    /// <summary>
    /// Các thông báo chi tiết về tình trạng định lượng nguyên liệu cho món ăn này.
    /// </summary>
    public IReadOnlyList<string> QuantityMessages { get; set; } = new List<string>();
}

/// <summary>
/// Chi tiết nguyên liệu còn thiếu trong công thức (FN-VIS-03).
/// </summary>
public class MissingIngredientDto
{
    /// <summary>
    /// Mã nguyên liệu trong hệ thống.
    /// </summary>
    public int IngredientId { get; set; }

    /// <summary>
    /// Tên nguyên liệu.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Định lượng yêu cầu trong công thức nếu có.
    /// </summary>
    public string? RequiredQuantity { get; set; }
}

/// <summary>
/// Chi tiết nguyên liệu đã có trong tủ nhưng chưa đủ định lượng cho công thức.
/// </summary>
public class InsufficientIngredientDto
{
    /// <summary>
    /// Mã nguyên liệu trong hệ thống.
    /// </summary>
    public int IngredientId { get; set; }

    /// <summary>
    /// Tên nguyên liệu.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Định lượng hiện có trong tủ (ví dụ: "0.5 trái", "300g").
    /// </summary>
    public string AvailableQuantity { get; set; } = string.Empty;

    /// <summary>
    /// Định lượng công thức yêu cầu (ví dụ: "1 trái", "500g").
    /// </summary>
    public string RequiredQuantity { get; set; } = string.Empty;

    /// <summary>
    /// Số lượng còn thiếu cần bổ sung thêm.
    /// </summary>
    public string MissingQuantity { get; set; } = string.Empty;

    /// <summary>
    /// Thông báo hướng dẫn người dùng.
    /// </summary>
    public string Message { get; set; } = string.Empty;
}
