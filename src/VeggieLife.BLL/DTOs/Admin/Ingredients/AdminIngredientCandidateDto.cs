using System;

namespace VeggieLife.BLL.DTOs.Admin.Ingredients;

/// <summary>
/// Ảnh ứng viên cắt từ ảnh tủ lạnh người dùng chờ Admin duyệt (FN-ADM-12-CAND, BR-ADM-ING-02).
/// </summary>
public class AdminIngredientCandidateDto
{
    public int CandidateId { get; set; }
    public string CroppedImageUrl { get; set; } = string.Empty;
    /// <summary>Độ tin cậy AI 0..1.</summary>
    public decimal ConfidenceScore { get; set; }
    public string Status { get; set; } = string.Empty;
    public int SessionId { get; set; }
    public DateTime CreatedAt { get; set; }
}
