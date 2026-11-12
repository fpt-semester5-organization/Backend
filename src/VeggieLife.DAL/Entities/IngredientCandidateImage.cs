using System;

namespace VeggieLife.DAL.Entities;

/// <summary>
/// Ảnh ứng viên cắt từ ảnh tủ lạnh người dùng theo box2d, chờ Admin duyệt làm ảnh đại diện nguyên liệu
/// (bảng dbo.IngredientCandidateImages — database/schema/47_IngredientCandidateImages.sql; BR-ADM-ING-02/03).
/// </summary>
public partial class IngredientCandidateImage
{
    public int CandidateId { get; set; }

    public int IngredientId { get; set; }

    public int SessionId { get; set; }

    public string CroppedImageUrl { get; set; } = null!;

    public decimal ConfidenceScore { get; set; }

    /// <summary>PENDING | APPROVED | REJECTED.</summary>
    public string Status { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public virtual Ingredient Ingredient { get; set; } = null!;

    public virtual FridgeSession Session { get; set; } = null!;
}
