using System.Threading;
using System.Threading.Tasks;
using VeggieLife.BLL.DTOs.Admin.Knowledge;

namespace VeggieLife.BLL.Services.Knowledge;

/// <summary>
/// Interface dịch vụ điều phối nạp lại (reindex) kho tri thức RAG theo phạm vi yêu cầu từ Quản trị viên (FN-CHAT-07).
/// </summary>
public interface IKnowledgeReindexService
{
    /// <summary>
    /// Kích hoạt job quét CSDL và nạp lại vector embedding theo phân vùng scope (ALL, RECIPE, POST, VIDEO, NUTRITION_DOC, DICTIONARY).
    /// </summary>
    Task<CreateAdminKnowledgeReindexResponseData> TriggerReindexAsync(string scope, CancellationToken ct = default);
}
