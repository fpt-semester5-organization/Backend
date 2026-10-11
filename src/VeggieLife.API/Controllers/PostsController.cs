using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using VeggieLife.BLL.Common.Constants;
using VeggieLife.BLL.Common.Exceptions;
using VeggieLife.BLL.Common.Responses;
using VeggieLife.BLL.DTOs.Posts.Requests;
using VeggieLife.BLL.DTOs.Posts.Responses;
using VeggieLife.BLL.Services.Posts;

namespace VeggieLife.API.Controllers;

/// <summary>
/// Controller cung cấp các endpoint tìm kiếm, lọc danh sách và đọc bài viết chia sẻ cộng đồng (FN-POST-05, FN-POST-06).
/// Hỗ trợ cả Khách vãng lai (Guest) và Thành viên đã đăng nhập (Member, VIP, Admin).
/// </summary>
[ApiController]
[Route("api/v1/posts")]
public class PostsController : BaseApiController
{
    private readonly IPostService _postService;

    public PostsController(IPostService postService)
    {
        _postService = postService;
    }

    /// <summary>
    /// FN-POST-05: Tìm kiếm & xem danh sách bài viết.
    /// Danh sách bài viết chia sẻ (blog) đã duyệt, lọc theo từ khóa và tác giả, sắp xếp mới nhất/phổ biến, phân trang.
    /// Thành viên xem thêm bài PRIVATE của chính mình.
    /// </summary>
    /// <param name="filter">Bộ lọc tìm kiếm và phân trang.</param>
    /// <param name="ct">CancellationToken.</param>
    /// <returns>Danh sách bài viết tóm tắt kèm metadata phân trang.</returns>
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<PostSummaryDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> ListPosts([FromQuery] PostFilterRequest filter, CancellationToken ct)
    {
        var isGuest = IsGuestUser();
        var guestId = ResolveGuestId();
        var currentUserId = CurrentUserId.HasValue ? (int?)CurrentUserId.Value : null;

        var (items, meta) = await _postService.ListPostsAsync(filter, isGuest, guestId, currentUserId, ct);
        return PagedResponse(items, meta, "OK");
    }

    /// <summary>
    /// FN-POST-06: Xem chi tiết bài viết.
    /// Trả nội dung đầy đủ bài viết, ảnh, video đính kèm và công thức đính kèm (nếu có); tăng View_Count.
    /// </summary>
    /// <param name="id">Mã định danh bài viết (> 0).</param>
    /// <param name="ct">CancellationToken.</param>
    /// <returns>Chi tiết bài viết gồm nội dung, ảnh, video, công thức đính kèm và quyền tương tác.</returns>
    [HttpGet("{id:int}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<PostDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetPost([FromRoute] int id, CancellationToken ct)
    {
        var isGuest = IsGuestUser();
        var currentUserId = CurrentUserId.HasValue ? (int?)CurrentUserId.Value : null;

        var post = await _postService.GetPostByIdAsync(id, isGuest, currentUserId, ct);
        return OkResponse(post, "OK");
    }

    /// <summary>
    /// FN-POST-07: Đăng bài viết mới (lưu nháp hoặc gửi kiểm duyệt AI).
    /// </summary>
    /// <param name="request">Payload thông tin bài viết.</param>
    /// <param name="ct">CancellationToken.</param>
    /// <returns>Mã bài viết vừa tạo và trạng thái kiểm duyệt ban đầu.</returns>
    [HttpPost]
    [Authorize(Policy = PolicyNames.RequireMember)]
    [ProducesResponseType(typeof(ApiResponse<CreatePostResponseDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> CreatePost([FromBody] CreatePostRequest request, CancellationToken ct)
    {
        EnsureAuthorizedMember();

        var userId = (int)CurrentUserId!.Value;
        var role = CurrentUserRole ?? "MEMBER";

        var result = await _postService.CreatePostAsync(request, userId, role, ct);
        return CreatedResponse(result, "Đã đăng bài.");
    }

    /// <summary>
    /// FN-POST-07: Sửa bài viết của mình, gỡ khỏi cộng đồng (chuyển PRIVATE) hoặc gửi duyệt lại.
    /// </summary>
    /// <param name="id">Mã định danh bài viết (> 0).</param>
    /// <param name="request">Payload thông tin cập nhật.</param>
    /// <param name="ct">CancellationToken.</param>
    /// <returns>Mã bài viết và trạng thái kiểm duyệt sau cập nhật.</returns>
    [HttpPut("{id:int}")]
    [Authorize(Policy = PolicyNames.RequireMember)]
    [ProducesResponseType(typeof(ApiResponse<UpdatePostResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdatePost([FromRoute] int id, [FromBody] UpdatePostRequest request, CancellationToken ct)
    {
        EnsureAuthorizedMember();

        var userId = (int)CurrentUserId!.Value;
        var role = CurrentUserRole ?? "MEMBER";

        var result = await _postService.UpdatePostAsync(id, request, userId, role, ct);
        return OkResponse(result, "Đã cập nhật.");
    }

    /// <summary>
    /// FN-POST-07: Xóa mềm bài viết của mình (chuyển sang trạng thái DELETED).
    /// </summary>
    /// <param name="id">Mã định danh bài viết (> 0).</param>
    /// <param name="ct">CancellationToken.</param>
    /// <returns>Phản hồi thành công không kèm payload.</returns>
    [HttpDelete("{id:int}")]
    [Authorize(Policy = PolicyNames.RequireMember)]
    [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeletePost([FromRoute] int id, CancellationToken ct)
    {
        EnsureAuthorizedMember();

        var userId = (int)CurrentUserId!.Value;
        var role = CurrentUserRole ?? "MEMBER";

        await _postService.DeletePostAsync(id, userId, role, ct);
        return OkResponse<object?>(null, "Đã xóa bài viết.");
    }

    /// <summary>
    /// Kiểm tra và xác thực quyền thành viên chính thức cho các tác vụ ghi dữ liệu cộng đồng (FN-AUTH-03).
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


    /// <summary>
    /// Xác định xem người dùng hiện tại là khách vãng lai hay thành viên đã đăng nhập.
    /// </summary>
    private bool IsGuestUser()
    {
        if (User?.Identity == null || !User.Identity.IsAuthenticated)
        {
            return true;
        }

        var role = CurrentUserRole;
        return string.IsNullOrEmpty(role) || string.Equals(role, "GUEST", System.StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Phân giải mã định danh của khách vãng lai từ Claim, Header hoặc Cookie.
    /// </summary>
    private string? ResolveGuestId()
    {
        // 1. Từ JWT claim Guest_UUID
        if (!string.IsNullOrEmpty(CurrentGuestUuid))
        {
            return CurrentGuestUuid;
        }

        // 2. Từ HTTP Header: X-Guest-UUID
        if (Request.Headers.TryGetValue("X-Guest-UUID", out var headerValue) && !string.IsNullOrEmpty(headerValue.FirstOrDefault()))
        {
            return headerValue.FirstOrDefault();
        }

        // 3. Từ HTTP Cookie: guest_uuid
        if (Request.Cookies.TryGetValue("guest_uuid", out var cookieValue) && !string.IsNullOrEmpty(cookieValue))
        {
            return cookieValue;
        }

        return null;
    }
}
