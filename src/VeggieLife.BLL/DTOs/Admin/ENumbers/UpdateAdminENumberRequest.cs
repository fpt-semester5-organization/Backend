using System.ComponentModel.DataAnnotations;

namespace VeggieLife.BLL.DTOs.Admin.ENumbers;

/// <summary>
/// Yêu cầu cập nhật thông tin mã E-number (FN-ADM-11, BR-ADM-MAS-002).
/// </summary>
public class UpdateAdminENumberRequest
{
    /// <summary>
    /// Tên chất phụ gia (tùy chọn, tối đa 150 ký tự).
    /// </summary>
    [StringLength(150, ErrorMessage = "Tên phụ gia tối đa 150 ký tự.")]
    public string? Name { get; set; }

    /// <summary>
    /// Trạng thái chay (tùy chọn, thuộc {VEGAN, DOUBTFUL, NON_VEGAN}).
    /// </summary>
    public string? VeganStatus { get; set; }

    /// <summary>
    /// Nguồn gốc xuất xứ (tùy chọn, tối đa 100 ký tự).
    /// </summary>
    [StringLength(100, ErrorMessage = "Nguồn gốc xuất xứ tối đa 100 ký tự.")]
    public string? OriginSource { get; set; }

    /// <summary>
    /// Loại nhóm phụ gia (tùy chọn, tối đa 100 ký tự).
    /// </summary>
    [StringLength(100, ErrorMessage = "Loại nhóm phụ gia tối đa 100 ký tự.")]
    public string? CategoryType { get; set; }

    /// <summary>
    /// Ghi chú cảnh báo sức khỏe hoặc độc tính (tùy chọn).
    /// </summary>
    public string? HealthRiskNotes { get; set; }
}
