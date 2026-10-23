namespace VeggieLife.BLL.DTOs.Admin.ENumbers;

/// <summary>
/// Tham số tìm kiếm và phân trang từ điển E-number dành cho Quản trị viên (FN-ADM-11).
/// </summary>
public class AdminENumberFilterRequest
{
    /// <summary>
    /// Lọc theo mức độ phù hợp ăn chay (VEGAN | DOUBTFUL | NON_VEGAN).
    /// </summary>
    public string? VeganStatus { get; set; }

    /// <summary>
    /// Lọc theo loại danh mục phụ gia (Category_Type).
    /// </summary>
    public string? CategoryType { get; set; }

    /// <summary>
    /// Từ khóa tìm kiếm theo mã, tên hoặc nguồn gốc (tối đa 150 ký tự).
    /// </summary>
    public string? Keyword { get; set; }

    /// <summary>
    /// Trang số (mặc định 1).
    /// </summary>
    public int Page { get; set; } = 1;

    /// <summary>
    /// Kích thước trang (1–100, mặc định 20).
    /// </summary>
    public int Size { get; set; } = 20;
}
