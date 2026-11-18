using System;

namespace VeggieLife.BLL.DTOs.Admin.Knowledge;

/// <summary>
/// DTO thông tin tài liệu dinh dưỡng PDF lưu trong Qdrant Vector DB (FN-CHAT-07).
/// </summary>
public class AdminNutritionDocumentDto
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string FileType { get; set; } = "PDF";
    public int Chunks { get; set; }

    /// <summary>Số phần đã nạp lên Qdrant (tài liệu dài được chia: sourceId = Id, Id_P2, Id_P3...).</summary>
    public int Parts { get; set; } = 1;
    public bool IsActive { get; set; } = true;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public string? FileUrl { get; set; }
}
