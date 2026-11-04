namespace VeggieLife.BLL.DTOs.Admin.Knowledge;

/// <summary>
/// DTO phản hồi tiếp nhận job đồng bộ lại kho tri thức RAG (FN-CHAT-07).
/// </summary>
public class CreateAdminKnowledgeReindexResponseData
{
    public string JobId { get; set; } = string.Empty;
    public string Scope { get; set; } = "ALL";
    public string Status { get; set; } = "ACCEPTED";
}
