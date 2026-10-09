using System.Collections.Generic;

namespace VeggieLife.BLL.DTOs.Recipes.Responses;

/// <summary>
/// DTO thông tin chi tiết đầy đủ của công thức nấu ăn công khai (FN-POST-02).
/// Hiển thị đầy đủ nguyên liệu, định lượng, các bước nấu, cờ video transcript và quyền thao tác.
/// </summary>
public class RecipeDetailDto
{
    /// <summary>
    /// Mã định danh duy nhất của công thức món chay.
    /// </summary>
    public int RecipeId { get; set; }

    /// <summary>
    /// Loại nội dung: RECIPE hoặc VIDEO.
    /// </summary>
    public string Type { get; set; } = "RECIPE";

    /// <summary>
    /// Tiêu đề công thức nấu ăn.
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Ảnh đại diện chất lượng cao của món ăn.
    /// </summary>
    public string? ImageUrl { get; set; }

    /// <summary>
    /// Mã trường phái chay (ví dụ: VEGAN, BUDDHIST, LACTO, ...).
    /// </summary>
    public string DietarySchool { get; set; } = string.Empty;

    /// <summary>
    /// Thời gian nấu chín món ăn (phút).
    /// </summary>
    public int CookTimeMinutes { get; set; }

    /// <summary>
    /// Năng lượng món ăn (kcal).
    /// </summary>
    public decimal CaloriesKcal { get; set; }

    /// <summary>
    /// Tổng lượt bình chọn / thả tim của bài viết đại diện.
    /// </summary>
    public int VoteCount { get; set; }

    /// <summary>
    /// Cờ xác nhận công thức thuần chay đã được Quản trị viên duyệt (Verified).
    /// </summary>
    public bool IsVerified { get; set; }

    /// <summary>
    /// Mô tả tóm tắt hương vị và giá trị dinh dưỡng của món ăn.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Khẩu phần phục vụ (số người ăn).
    /// </summary>
    public int Servings { get; set; }

    /// <summary>
    /// Thời gian sơ chế nguyên liệu (phút).
    /// </summary>
    public int PrepTimeMinutes { get; set; }

    /// <summary>
    /// Toàn văn các bước nấu ăn chi tiết theo thứ tự.
    /// </summary>
    public List<string> Instructions { get; set; } = new();

    /// <summary>
    /// Danh sách nguyên liệu và định lượng chuẩn bị.
    /// </summary>
    public List<RecipeIngredientDetailDto> Ingredients { get; set; } = new();

    /// <summary>
    /// Cờ biểu thị có sẵn bản phiên âm lời thoại / công thức bóc tách từ video hay không.
    /// </summary>
    public bool HasTranscript { get; set; }

    /// <summary>
    /// Tổng số lượng bình luận của bài viết.
    /// </summary>
    public int CommentCount { get; set; }

    /// <summary>
    /// Trạng thái quyền tương tác cộng đồng của người dùng đối với bài viết.
    /// </summary>
    public MemberActionsDto MemberActions { get; set; } = new();
}
