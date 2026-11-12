using System;
using System.Collections.Generic;

namespace VeggieLife.BLL.DTOs.Admin.Knowledge;

/// <summary>
/// DTO phản hồi trạng thái kho vector Qdrant cho Admin (FN-CHAT-07).
/// Sử dụng Dictionary<string, int> để bảo toàn key chữ hoa (RECIPE, POST, NUTRITION_DOC...) khi serialize sang JSON.
/// </summary>
public class AdminKnowledgeStatusResponseData
{
    public string Collection { get; set; } = "veggielife_knowledge";
    public int TotalPoints { get; set; }
    public Dictionary<string, int> BySource { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Số điểm bị Admin ẩn (payload active=false) — không được tìm khi chat; TotalPoints/BySource chỉ tính điểm đang dùng.</summary>
    public int InactivePoints { get; set; }
    public string? LastIndexedAt { get; set; }
    public int Pending { get; set; }
    public int Failed { get; set; }
}
