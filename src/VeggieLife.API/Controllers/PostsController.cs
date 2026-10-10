using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
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
