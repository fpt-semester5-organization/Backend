using System.Threading;
using System.Threading.Tasks;
using VeggieLife.BLL.Common.Responses;
using VeggieLife.BLL.DTOs.Chat.Requests;
using VeggieLife.BLL.DTOs.Chat.Responses;
using VeggieLife.BLL.DTOs.Surveys.Requests;

namespace VeggieLife.BLL.Services.Chat;

/// <summary>
/// Interface dịch vụ Chatbot tư vấn dinh dưỡng thuần chay (FN-CHAT-01..06).
/// Quản lý phiên, kiểm tra hạn mức, giao tiếp AI Microservice và lưu vết có dẫn nguồn.
/// </summary>
public interface IChatService
{
    /// <summary>
    /// FN-CHAT-01: Tạo phiên hội thoại mới cho Khách hoặc Thành viên.
    /// </summary>
    Task<ChatSessionDto> CreateSessionAsync(
        long? userId,
        string? guestUuid,
        CreateChatSessionRequest request,
        CancellationToken ct = default);

    /// <summary>
    /// FN-CHAT-03: Lấy danh sách phiên hội thoại của Thành viên.
    /// </summary>
    Task<PagedResult<ChatSessionSummaryDto>> ListSessionsAsync(
        long userId,
        int page,
        int size,
        CancellationToken ct = default);

    /// <summary>
    /// FN-CHAT-02: Gửi câu hỏi, kiểm tra hạn mức, gọi AI và nhận tư vấn kèm nguồn dẫn chứng.
    /// </summary>
    Task<CreateChatMessageResultDto> SendMessageAsync(
        long sessionId,
        long? userId,
        string? guestUuid,
        string userRole,
        CreateChatSessionMessageRequest request,
        CancellationToken ct = default);

    /// <summary>
    /// FN-CHAT-04: Đọc lịch sử tin nhắn của phiên (kèm nguồn và miễn trừ y tế).
    /// </summary>
    Task<ListChatSessionMessagesResponseDto> ListMessagesAsync(
        long sessionId,
        long? userId,
        string? guestUuid,
        long? before,
        int size,
        CancellationToken ct = default);

    /// <summary>
    /// FN-CHAT-05: Xóa phiên hội thoại và toàn bộ tin nhắn.
    /// </summary>
    Task DeleteSessionAsync(
        long sessionId,
        long userId,
        CancellationToken ct = default);

    /// <summary>
    /// FN-CHAT-06: Đánh giá câu trả lời của AI hoặc tính năng AI.
    /// </summary>
    Task<long> CreateSurveyAsync(
        long userId,
        CreateSurveyRequest request,
        CancellationToken ct = default);
}
