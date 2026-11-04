namespace VeggieLife.BLL.DTOs.Admin.Ingredients;

/// <summary>
/// DTO tham số lọc danh sách nguyên liệu cho Quản trị viên (FN-ADM-12).
/// Hỗ trợ lọc theo từ khóa, nhóm dị ứng, nhóm thực phẩm, quầy hàng, trạng thái khóa và ảnh.
/// </summary>
public class AdminIngredientFilterRequest
{
    public string? Keyword { get; set; }
    public int? AllergenId { get; set; }
    public string? FoodGroup { get; set; }
    public string? StallType { get; set; }
    public bool? IsLocked { get; set; }
    public bool? HasImage { get; set; }
    public int Page { get; set; } = 1;
    public int Size { get; set; } = 20;
}
