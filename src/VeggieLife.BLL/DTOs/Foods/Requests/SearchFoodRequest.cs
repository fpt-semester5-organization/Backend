namespace VeggieLife.BLL.DTOs.Foods.Requests;

/// <summary>
/// Tham số tìm kiếm món ăn theo từ khóa, sử dụng cho autocomplete combobox.
/// </summary>
public class SearchFoodRequest
{
    /// <summary>Từ khóa tìm kiếm theo tên món ăn (tối thiểu 1 ký tự).</summary>
    public string Query { get; set; } = string.Empty;
}

