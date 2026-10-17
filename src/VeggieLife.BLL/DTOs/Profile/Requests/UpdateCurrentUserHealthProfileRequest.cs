using System;
using System.ComponentModel.DataAnnotations;

namespace VeggieLife.BLL.DTOs.Profile.Requests;

/// <summary>
/// Yêu cầu thiết lập hoặc cập nhật hồ sơ sức khỏe và dinh dưỡng (FN-PROF-04).
/// </summary>
public class UpdateCurrentUserHealthProfileRequest
{
    /// <summary>
    /// Chiều cao người dùng tính bằng cm (50–250 cm).
    /// </summary>
    [Range(50, 250, ErrorMessage = "Chiều cao phải nằm trong khoảng từ 50 đến 250 cm.")]
    public decimal HeightCm { get; set; }

    /// <summary>
    /// Cân nặng hiện tại tính bằng kg (20–300 kg).
    /// </summary>
    [Range(20, 300, ErrorMessage = "Cân nặng phải nằm trong khoảng từ 20 đến 300 kg.")]
    public decimal WeightKg { get; set; }

    /// <summary>
    /// Ngày sinh định dạng YYYY-MM-DD (tuổi ≥ 10).
    /// </summary>
    public DateOnly DateOfBirth { get; set; }

    /// <summary>
    /// Giới tính sinh học: MALE hoặc FEMALE.
    /// </summary>
    [Required(ErrorMessage = "Giới tính là bắt buộc.")]
    public string Gender { get; set; } = string.Empty;

    /// <summary>
    /// Mã định danh 1 trong 07 trường phái ăn chay cốt lõi (1–7).
    /// </summary>
    [Range(1, int.MaxValue, ErrorMessage = "Trường phái ăn chay không hợp lệ.")]
    public int DietarySchoolId { get; set; }

    /// <summary>
    /// Mục tiêu sức khỏe: LOSE_WEIGHT | MAINTAIN | GAIN_MUSCLE.
    /// </summary>
    [Required(ErrorMessage = "Mục tiêu sức khỏe là bắt buộc.")]
    public string HealthGoal { get; set; } = string.Empty;

    /// <summary>
    /// Cân nặng mục tiêu tính bằng kg (20–300 kg, tùy chọn).
    /// </summary>
    [Range(20, 300, ErrorMessage = "Cân nặng mục tiêu phải nằm trong khoảng từ 20 đến 300 kg.")]
    public decimal? TargetWeightKg { get; set; }

    /// <summary>
    /// Kiêng ngũ vị tân (hành, tỏi, kiệu, hẹ, nén). Mặc định false.
    /// </summary>
    public bool? RequiresAlliumFree { get; set; } = false;

    /// <summary>
    /// Khẩu vị vùng miền ưu tiên: NORTH | CENTRAL | SOUTH.
    /// </summary>
    public string? RegionPreference { get; set; }
}
