using System.ComponentModel.DataAnnotations;

namespace VeggieLife.BLL.DTOs.Admin.ENumbers;

/// <summary>
/// Yêu cầu thêm mới mã phụ gia E-number vào từ điển (FN-ADM-11, BR-ADM-MAS-002).
/// </summary>
public class CreateAdminENumberRequest
{
    /// <summary>
    /// Mã ký hiệu phụ gia E-number (bắt buộc, duy nhất, tối đa 15 ký tự, ví dụ: E441).
    /// </summary>
    [Required(ErrorMessage = "Mã E-number không được để trống.")]
    [StringLength(15, ErrorMessage = "Mã E-number tối đa 15 ký tự.")]
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// Tên chất phụ gia (bắt buộc, tối đa 150 ký tự).
    /// </summary>
    [Required(ErrorMessage = "Tên phụ gia không được để trống.")]
    [StringLength(150, ErrorMessage = "Tên phụ gia tối đa 150 ký tự.")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Phân loại ăn chay (bắt buộc, thuộc {VEGAN, DOUBTFUL, NON_VEGAN}).
    /// </summary>
    [Required(ErrorMessage = "Trạng thái chay không được để trống.")]
    public string VeganStatus { get; set; } = string.Empty;

    /// <summary>
    /// Nguồn gốc xuất xứ hóa học / tự nhiên (bắt buộc, tối đa 100 ký tự).
    /// </summary>
    [Required(ErrorMessage = "Nguồn gốc xuất xứ không được để trống.")]
    [StringLength(100, ErrorMessage = "Nguồn gốc xuất xứ tối đa 100 ký tự.")]
    public string OriginSource { get; set; } = string.Empty;

    /// <summary>
    /// Loại nhóm phụ gia (tùy chọn, tối đa 100 ký tự, ví dụ: Chất tạo màu, Chất bảo quản).
    /// </summary>
    [StringLength(100, ErrorMessage = "Loại nhóm phụ gia tối đa 100 ký tự.")]
    public string? CategoryType { get; set; }

    /// <summary>
    /// Ghi chú cảnh báo sức khỏe hoặc độc tính (tùy chọn).
    /// </summary>
    public string? HealthRiskNotes { get; set; }
}
