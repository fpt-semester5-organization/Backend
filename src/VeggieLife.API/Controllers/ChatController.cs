using System;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using VeggieLife.BLL.Common.Constants;
using VeggieLife.BLL.Common.Exceptions;
using VeggieLife.BLL.Common.Responses;
using VeggieLife.BLL.DTOs.Chat.Requests;
using VeggieLife.BLL.DTOs.Chat.Responses;
using VeggieLife.BLL.Services.Chat;

namespace VeggieLife.API.Controllers;

/// <summary>
/// Controller xử lý các chức năng Chatbot tư vấn dinh dưỡng (FN-CHAT-01..05).
/// </summary>
[ApiController]
[Route("api/v1/chat")]
public class ChatController : BaseApiController
{
    private readonly IChatService _chatService;

    public ChatController(IChatService chatService)
    {
        _chatService = chatService;
    }

    /// <summary>
    /// FN-CHAT-01: Tạo phiên hội thoại mới với trợ lý AI.
    /// Auth: Bearer (GUEST | MEMBER | VIP)
    /// </summary>
    [HttpPost("sessions")]
    [Authorize(Policy = PolicyNames.RequireGuestOrAbove)]
    [ProducesResponseType(typeof(ApiResponse<ChatSessionDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CreateSession(
        [FromBody] CreateChatSessionRequest? request,
        CancellationToken ct)
    {
        var req = request ?? new CreateChatSessionRequest();
        var guestId = ResolveGuestId();

        var result = await _chatService.CreateSessionAsync(CurrentUserId, guestId, req, ct);
        return CreatedResponse(result, "Đã tạo phiên hội thoại.");
    }

    /// <summary>
    /// FN-CHAT-03: Lấy danh sách phiên hội thoại của Thành viên.
    /// Auth: Bearer (MEMBER | VIP)
    /// </summary>
    [HttpGet("sessions")]
    [Authorize(Policy = PolicyNames.RequireMember)]
    [ProducesResponseType(typeof(ApiResponse<ChatSessionSummaryDto[]>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> ListSessions(
        [FromQuery] int page = 1,
        [FromQuery] int size = 20,
        CancellationToken ct = default)
    {
        EnforceMemberOnly("FN-CHAT-03");

        var pagedResult = await _chatService.ListSessionsAsync(CurrentUserId!.Value, page, size, ct);
        return PagedResponse(pagedResult.Items, new PagedMeta(pagedResult.PageNumber, pagedResult.PageSize, pagedResult.TotalItems), "OK");
    }

    /// <summary>
    /// FN-CHAT-02: Gửi câu hỏi & nhận tư vấn dinh dưỡng kèm nguồn tham khảo và lời miễn trừ y khoa.
    /// Auth: Bearer (GUEST | MEMBER | VIP)
    /// </summary>
    [HttpPost("sessions/{id:long}/messages")]
    [Authorize(Policy = PolicyNames.RequireGuestOrAbove)]
    [ProducesResponseType(typeof(ApiResponse<CreateChatMessageResultDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> SendMessage(
        [FromRoute] long id,
        [FromBody] CreateChatSessionMessageRequest request,
        CancellationToken ct)
    {
        if (id <= 0)
        {
            throw AppException.BadRequest(ErrorCodes.ErrValidation, "Mã phiên hội thoại không hợp lệ (> 0).");
        }

        var guestId = ResolveGuestId();
        var role = CurrentUserRole ?? (guestId != null ? UserRoles.Guest : UserRoles.Member);

        var result = await _chatService.SendMessageAsync(
            id,
            CurrentUserId,
            guestId,
            role,
            request,
            ct);

        return OkResponse(result, "OK");
    }

    /// <summary>
    /// FN-CHAT-04: Đọc lịch sử tin nhắn của phiên (Khách chỉ đọc phiên trong vòng 24 giờ).
    /// Auth: Bearer (GUEST | MEMBER | VIP)
    /// </summary>
    [HttpGet("sessions/{id:long}/messages")]
    [Authorize(Policy = PolicyNames.RequireGuestOrAbove)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ListMessages(
        [FromRoute] long id,
        [FromQuery] long? before = null,
        [FromQuery] int size = 20,
        CancellationToken ct = default)
    {
        if (id <= 0)
        {
            throw AppException.BadRequest(ErrorCodes.ErrValidation, "Mã phiên hội thoại không hợp lệ (> 0).");
        }

        var guestId = ResolveGuestId();
        var result = await _chatService.ListMessagesAsync(
            id,
            CurrentUserId,
            guestId,
            before,
            size,
            ct);

        return Ok(new
        {
            statusCode = StatusCodes.Status200OK,
            message = "OK",
            data = result.Messages,
            meta = result.Meta
        });
    }

    /// <summary>
    /// FN-CHAT-05: Xóa phiên hội thoại cùng toàn bộ tin nhắn.
    /// Auth: Bearer (MEMBER | VIP)
    /// </summary>
    [HttpDelete("sessions/{id:long}")]
    [Authorize(Policy = PolicyNames.RequireMember)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteSession(
        [FromRoute] long id,
        CancellationToken ct)
    {
        if (id <= 0)
        {
            throw AppException.BadRequest(ErrorCodes.ErrValidation, "Mã phiên hội thoại không hợp lệ (> 0).");
        }

        EnforceMemberOnly("FN-CHAT-05");

        await _chatService.DeleteSessionAsync(id, CurrentUserId!.Value, ct);
        return Ok(new
        {
            statusCode = StatusCodes.Status200OK,
            message = "Đã xóa phiên hội thoại.",
            data = (object?)null
        });
    }

    private void EnforceMemberOnly(string actionName)
    {
        var isGuest = User.IsInRole(UserRoles.Guest) ||
                      User.Claims.Any(c =>
                          (c.Type == ClaimTypes.Role || c.Type == "role" || c.Type.EndsWith("/role")) &&
                          string.Equals(c.Value, UserRoles.Guest, StringComparison.OrdinalIgnoreCase));

        if (isGuest || !CurrentUserId.HasValue)
        {
            throw AppException.Forbidden(
                ErrorCodes.ErrGuestCommunityActionDenied,
                $"Khách vãng lai thực hiện thao tác chỉ dành cho Thành viên ({actionName}).");
        }
    }

    private string? ResolveGuestId()
    {
        if (CurrentUserId.HasValue)
        {
            return null;
        }

        if (!string.IsNullOrEmpty(CurrentGuestUuid))
        {
            return CurrentGuestUuid;
        }

        if (Request?.Headers != null && Request.Headers.TryGetValue("X-Guest-UUID", out var headerValue) && !string.IsNullOrWhiteSpace(headerValue))
        {
            return headerValue.ToString().Trim();
        }

        if (Request?.Cookies != null && Request.Cookies.TryGetValue("guest_uuid", out var cookieValue) && !string.IsNullOrWhiteSpace(cookieValue))
        {
            return cookieValue.Trim();
        }

        return null;
    }
}
