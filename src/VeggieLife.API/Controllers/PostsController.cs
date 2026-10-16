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

using VeggieLife.BLL.DTOs.Community.Requests;
using VeggieLife.BLL.DTOs.Community.Responses;
using VeggieLife.BLL.Services.Community;

namespace VeggieLife.API.Controllers;

/// <summary>
/// Controller cung cấp các endpoint tìm kiếm, lọc danh sách và đọc bài viết chia sẻ cộng đồng.
/// Hỗ trợ cả Khách vãng lai (Guest) và Thành viên đã đăng nhập (Member, VIP, Admin).
/// </summary>
[ApiController]
[Route("api/v1/posts")]
public class PostsController : BaseApiController
{
    private readonly IPostService _postService;
    private readonly ICommunityInteractionService? _communityService;
    private readonly ICommentService? _commentService;

    public PostsController(
        IPostService postService,
        ICommunityInteractionService? communityService = null,
        ICommentService? commentService = null)
    {
        _postService = postService;
        _communityService = communityService;
        _commentService = commentService;
    }

    /// <summary>
    /// Tìm kiếm & xem danh sách bài viết.
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
    /// Xem chi tiết bài viết.
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
    /// Đăng bài viết mới (lưu nháp hoặc gửi kiểm duyệt AI).
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
    /// Sửa bài viết của mình, gỡ khỏi cộng đồng (chuyển PRIVATE) hoặc gửi duyệt lại.
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
    /// Xóa mềm bài viết của mình (chuyển sang trạng thái DELETED).
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
    /// Thả tim hoặc bỏ tim cho bài viết (mỗi thành viên tối đa 1 tim, thả lần 2 = bỏ tim).
    /// </summary>
    /// <param name="id">Mã định danh bài viết (> 0).</param>
    /// <param name="ct">CancellationToken.</param>
    /// <returns>Trạng thái voted và tổng số vote mới nhất của bài viết.</returns>
    [HttpPost("{id:int}/votes")]
    [Authorize(Policy = PolicyNames.RequireMember)]
    [ProducesResponseType(typeof(ApiResponse<VoteResponseData>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CreatePostVote([FromRoute] int id, CancellationToken ct)
    {
        EnsureAuthorizedMember();

        if (_communityService == null)
        {
            throw AppException.Internal(ErrorCodes.ErrInternal, "Dịch vụ tương tác cộng đồng chưa được cấu hình.");
        }

        var userId = (int)CurrentUserId!.Value;
        var result = await _communityService.VotePostAsync(id, userId, ct);

        return OkResponse(result, "OK");
    }

    /// <summary>
    /// FN-COMM-01: Xem bình luận của bài viết.
    /// Danh sách bình luận dạng cây (tối đa 2 cấp hiển thị) của bài viết; chỉ bình luận ACTIVE và chưa xóa mềm.
    /// Khách chỉ đọc.
    /// </summary>
    /// <param name="id">Mã bài viết (> 0).</param>
    /// <param name="page">Số trang (>= 1, mặc định 1).</param>
    /// <param name="size">Kích thước trang (1–50, mặc định 20).</param>
    /// <param name="ct">CancellationToken.</param>
    /// <returns>Danh sách bình luận dạng cây kèm metadata phân trang.</returns>
    [HttpGet("{id:int}/comments")]
    [AllowAnonymous]
    [Tags("INT")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<CommentDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> ListPostComments(
        [FromRoute] int id,
        [FromQuery] int page = 1,
        [FromQuery] int size = 20,
        CancellationToken ct = default)
    {
        if (_commentService == null)
        {
            throw AppException.Internal(ErrorCodes.ErrInternal, "Dịch vụ bình luận chưa được cấu hình.");
        }

        var (items, meta) = await _commentService.ListPostCommentsAsync(id, page, size, ct);
        return PagedResponse(items, meta, "OK");
    }

    /// <summary>
    /// FN-COMM-02: Bình luận hoặc trả lời bình luận trên bài viết.
    /// Thành viên bình luận hoặc trả lời một bình luận trên bài viết đã duyệt; bình luận được AI quét trước khi hiển thị.
    /// </summary>
    /// <param name="id">Mã bài viết (> 0).</param>
    /// <param name="request">Payload nội dung bình luận (2–500 ký tự) và mã bình luận cha (nếu có).</param>
    /// <param name="ct">CancellationToken.</param>
    /// <returns>Dữ liệu bình luận vừa tạo.</returns>
    [HttpPost("{id:int}/comments")]
    [Authorize(Policy = PolicyNames.RequireMember)]
    [Tags("INT")]
    [ProducesResponseType(typeof(ApiResponse<CommentDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> CreatePostComment(
        [FromRoute] int id,
        [FromBody] CreateCommentRequest request,
        CancellationToken ct)
    {
        EnsureAuthorizedMember();

        if (_commentService == null)
        {
            throw AppException.Internal(ErrorCodes.ErrInternal, "Dịch vụ bình luận chưa được cấu hình.");
        }

        var userId = (int)CurrentUserId!.Value;
        var result = await _commentService.CreatePostCommentAsync(id, request, userId, ct);

        return CreatedResponse(result, "Đã bình luận.");
    }

    /// <summary>
    /// Kiểm tra và xác thực quyền thành viên chính thức cho các tác vụ ghi dữ liệu cộng đồng.
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
