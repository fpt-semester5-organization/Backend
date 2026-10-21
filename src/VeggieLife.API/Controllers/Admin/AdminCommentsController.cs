using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using VeggieLife.BLL.Common.Constants;
using VeggieLife.BLL.Common.Exceptions;
using VeggieLife.BLL.Common.Responses;
using VeggieLife.BLL.DTOs.Admin.Moderation;
using VeggieLife.BLL.Services.Admin;

namespace VeggieLife.API.Controllers.Admin;

/// <summary>
/// API Controller quản trị xóa mềm bình luận vi phạm và toàn bộ phản hồi con (FN-ADM-06, BR-ADM-CMT-001).
/// </summary>
[Route("api/v1/admin/comments")]
[Authorize(Policy = PolicyNames.RequireAdmin)]
public class AdminCommentsController : BaseApiController
{
    private readonly IAdminModerationService _moderationService;

    public AdminCommentsController(IAdminModerationService moderationService)
    {
        _moderationService = moderationService;
    }

    /// <summary>
    /// FN-ADM-06: Xóa mềm một bình luận và toàn bộ cây phản hồi con bên dưới (cascade).
    /// </summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(typeof(ApiResponse<DeleteAdminCommentResponseData>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteComment(
        [FromRoute] int id,
        [FromQuery] string reason,
        CancellationToken ct)
    {
        var adminId = EnsureAdmin();
        var result = await _moderationService.DeleteCommentAsync(id, reason, adminId, ct);
        return OkResponse(result, "Đã xóa bình luận.");
    }

    private int EnsureAdmin()
    {
        if (User.Identity == null || !User.Identity.IsAuthenticated)
        {
            throw AppException.Unauthorized(ErrorCodes.ErrAuthInvalidToken, "Thiếu, sai chữ ký hoặc hết hạn JWT.");
        }

        if (!string.Equals(CurrentUserRole, UserRoles.Admin, StringComparison.OrdinalIgnoreCase))
        {
            throw AppException.Forbidden(ErrorCodes.ErrAdmForbidden, "Claim role khác ADMIN.");
        }

        if (CurrentUserId == null)
        {
            throw AppException.Unauthorized(ErrorCodes.ErrAuthInvalidToken, "Thiếu, sai chữ ký hoặc hết hạn JWT.");
        }

        return (int)CurrentUserId.Value;
    }
}
