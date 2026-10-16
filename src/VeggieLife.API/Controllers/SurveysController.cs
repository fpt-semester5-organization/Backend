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
using VeggieLife.BLL.DTOs.Surveys.Requests;
using VeggieLife.BLL.DTOs.Surveys.Responses;
using VeggieLife.BLL.Services.Chat;

namespace VeggieLife.API.Controllers;

/// <summary>
/// Controller xử lý khảo sát và đánh giá câu trả lời của AI (FN-CHAT-06).
/// </summary>
[ApiController]
[Route("api/v1/surveys")]
public class SurveysController : BaseApiController
{
    private readonly IChatService _chatService;

    public SurveysController(IChatService chatService)
    {
        _chatService = chatService;
    }

    /// <summary>
    /// FN-CHAT-06: Đánh giá câu trả lời của AI hoặc tính năng AI.
    /// Auth: Bearer (MEMBER | VIP)
    /// </summary>
    [HttpPost]
    [Authorize(Policy = PolicyNames.RequireMember)]
    [ProducesResponseType(typeof(ApiResponse<CreateSurveyResponseDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CreateSurvey(
        [FromBody] CreateSurveyRequest request,
        CancellationToken ct)
    {
        var isGuest = User.IsInRole(UserRoles.Guest) ||
                      User.Claims.Any(c =>
                          (c.Type == ClaimTypes.Role || c.Type == "role" || c.Type.EndsWith("/role")) &&
                          string.Equals(c.Value, UserRoles.Guest, StringComparison.OrdinalIgnoreCase));

        if (isGuest || !CurrentUserId.HasValue)
        {
            throw AppException.Forbidden(
                ErrorCodes.ErrGuestCommunityActionDenied,
                "Khách vãng lai thực hiện thao tác chỉ dành cho Thành viên (FN-AUTH-03).");
        }

        var surveyId = await _chatService.CreateSurveyAsync(CurrentUserId.Value, request, ct);

        return CreatedResponse(
            new CreateSurveyResponseDto { SurveyId = surveyId },
            "Cảm ơn bạn đã đánh giá.");
    }
}
