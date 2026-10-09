namespace VeggieLife.BLL.DTOs.Recipes.Requests;

/// <summary>
/// DTO chứa các tiêu chí tìm kiếm, lọc và phân trang danh sách công thức, video nấu ăn công khai (FN-POST-01).
/// </summary>
public class RecipeFilterRequest
{
    /// <summary>
    /// Từ khóa tìm kiếm theo tên món hoặc mô tả nội dung (1–100 ký tự).
    /// </summary>
    public string? Keyword { get; set; }

    /// <summary>
    /// Mã danh mục món ăn (Category_ID > 0).
    /// </summary>
    public int? CategoryId { get; set; }

    /// <summary>
    /// Mã trường phái chay (1 trong 07 trường phái: 1–7).
    /// </summary>
    public int? DietarySchoolId { get; set; }

    /// <summary>
    /// Tên nguyên liệu cần tìm trong công thức (1–50 ký tự).
    /// </summary>
    public string? Ingredient { get; set; }

    /// <summary>
    /// Thời gian nấu tối đa (1–600 phút).
    /// </summary>
    public int? MaxCookTime { get; set; }

    /// <summary>
    /// Loại nội dung cần tra cứu: ALL | RECIPE | VIDEO (mặc định: ALL).
    /// </summary>
    public string ContentType { get; set; } = "ALL";

    /// <summary>
    /// Tiêu chí sắp xếp: POPULAR | NEWEST (mặc định: POPULAR).
    /// </summary>
    public string Sort { get; set; } = "POPULAR";

    /// <summary>
    /// Số trang hiện tại (≥ 1, mặc định: 1).
    /// </summary>
    public int Page { get; set; } = 1;

    /// <summary>
    /// Số lượng bản ghi trên một trang (1–20, mặc định: 10).
    /// </summary>
    public int Size { get; set; } = 10;
}
