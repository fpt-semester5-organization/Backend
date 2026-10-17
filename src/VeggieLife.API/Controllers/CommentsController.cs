using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using VeggieLife.BLL.Common.Constants;
using VeggieLife.BLL.Common.Exceptions;
using VeggieLife.BLL.Common.Responses;
using VeggieLife.BLL.DTOs.Community.Requests;
using VeggieLife.BLL.DTOs.Community.Responses;
using VeggieLife.BLL.Services.Community;

namespace VeggieLife.API.Controllers;

/// <summary>
/// Controller xử lý các thao tác sửa và xóa bình luận của chính người dùng (FN-COMM-03).
/// </summary>
[ApiController]
[Route("api/v1/comments")]
public class CommentsController : BaseApiController
{
    private readonly ICommentService _commentService;

    public CommentsController(ICommentService commentService)
    {
        _commentService = commentService;
    }

    /// <summary>
    /// FN-COMM-03: Sửa nội dung bình luận của mình.
    /// Tác giả sửa nội dung bình luận; nội dung được AI quét lại trước khi cập nhật.
    /// </summary>
    /// <param name="id">Mã định danh bình luận (> 0).</param>
    /// <param name="request">Payload nội dung mới (2–500 ký tự).</param>
    /// <param name="ct">CancellationToken.</param>
    /// <returns>Dữ liệu bình luận sau khi cập nhật.</returns>
    [HttpPut("{id:int}")]
    [Authorize(Policy = PolicyNames.RequireMember)]
    [Tags("INT")]
    [ProducesResponseType(typeof(ApiResponse<CommentDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateComment(
        [FromRoute] int id,
        [FromBody] UpdateCommentRequest request,
        CancellationToken ct)
    {
        EnsureAuthorizedMember();

        var userId = (int)CurrentUserId!.Value;
        var result = await _commentService.UpdateCommentAsync(id, request, userId, ct);

        return OkResponse(result, "Đã cập nhật.");
    }

    /// <summary>
    /// FN-COMM-03: Xóa mềm bình luận của mình.
    /// Tác giả xóa mềm bình luận của mình; kéo theo ẩn toàn bộ cây phản hồi con (Cascade Soft-Delete - BR-ADM-CMT-001).
    /// </summary>
    /// <param name="id">Mã định danh bình luận (> 0).</param>
    /// <param name="ct">CancellationToken.</param>
    /// <returns>Số lượng bình luận phản hồi con bị ẩn kèm theo.</returns>
    [HttpDelete("{id:int}")]
    [Authorize(Policy = PolicyNames.RequireMember)]
    [Tags("INT")]
    [ProducesResponseType(typeof(ApiResponse<DeleteCommentResponseData>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteComment(
        [FromRoute] int id,
        CancellationToken ct)
    {
        EnsureAuthorizedMember();

        var userId = (int)CurrentUserId!.Value;
        var result = await _commentService.DeleteCommentAsync(id, userId, ct);

        return OkResponse(result, "Đã xóa bình luận.");
    }

    /// <summary>
    /// Kiểm tra quyền thành viên chính thức thực hiện tác vụ ghi dữ liệu cộng đồng (FN-AUTH-03).
    /// </summary>
    private void EnsureAuthorizedMember()
    {
        if (User?.Identity == null || !User.Identity.IsAuthenticated)
        {
            throw AppException.Unauthorized(ErrorCodes.ErrAuthInvalidToken, "Thiếu hoặc sai chữ ký, hết hạn JWT.");
        }

        if (IsGuestUser())
        {
            throw AppException.Forbidden(ErrorCodes.ErrGuestCommunityActionDenied, "Khách vãng lai thực hiện thao tác ghi cộng đồng (FN-AUTH-03).");
        }

        if (!CurrentUserId.HasValue)
        {
            throw AppException.Unauthorized(ErrorCodes.ErrAuthInvalidToken, "Thiếu thông tin người dùng trong token.");
        }
    }

    private bool IsGuestUser()
    {
        if (User?.Identity == null || !User.Identity.IsAuthenticated)
        {
            return true;
        }

        var role = CurrentUserRole;
        return string.IsNullOrEmpty(role) || string.Equals(role, "GUEST", System.StringComparison.OrdinalIgnoreCase);
    }
}
