namespace VeggieLife.BLL.DTOs.Admin.Knowledge;

/// <summary>
/// Yêu cầu xếp job đồng bộ lại kho tri thức RAG (FN-CHAT-07).
/// </summary>
public class CreateAdminKnowledgeReindexRequest
{
    /// <summary>
    /// Phạm vi đồng bộ: ALL, RECIPE, POST, VIDEO, NUTRITION_DOC, DICTIONARY.
    /// </summary>
    public string Scope { get; set; } = "ALL";
}
