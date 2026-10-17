using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using VeggieLife.BLL.Common.Constants;
using VeggieLife.BLL.Common.Exceptions;
using VeggieLife.BLL.Common.Responses;
using VeggieLife.BLL.DTOs.Videos.Requests;
using VeggieLife.BLL.DTOs.Videos.Responses;
using VeggieLife.BLL.Services.Videos;

namespace VeggieLife.API.Controllers;

/// <summary>
/// Controller quản lý video và tệp đa phương tiện nấu ăn (FN-POST-08, FN-POST-09, FN-POST-10).
/// </summary>
[ApiController]
[Route("api/v1/videos")]
public class VideosController : BaseApiController
{
    private readonly IVideoService _videoService;

    public VideosController(IVideoService videoService)
    {
        _videoService = videoService;
    }

    /// <summary>
    /// FN-POST-08: Tải video hoặc ảnh lên (Cloudinary).
    /// Gắn vào một bài viết hoặc công thức; video mới kích hoạt bóc tách công thức ngầm và lưu CSDL với trạng thái 'PROCESSING'.
    /// </summary>
    /// <param name="file">Tệp đa phương tiện (MP4/MOV ≤ 100 MB hoặc ảnh JPEG/PNG/WEBP ≤ 5 MB).</param>
    /// <param name="postId">Mã định danh bài viết đích (cung cấp đúng 1 trong postId hoặc recipeId).</param>
    /// <param name="recipeId">Mã định danh công thức đích (cung cấp đúng 1 trong postId hoặc recipeId, > 0).</param>
    /// <param name="title">Tiêu đề video (tùy chọn, tối đa 150 ký tự).</param>
    /// <param name="ct">CancellationToken.</param>
    /// <returns>HTTP 202 Accepted: { videoId, status: "PROCESSING" }.</returns>
    [HttpPost]
    [Authorize(Policy = PolicyNames.RequireMember)]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(ApiResponse<CreateVideoResponseData>), StatusCodes.Status202Accepted)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status413PayloadTooLarge)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status502BadGateway)]
    public async Task<IActionResult> CreateVideo(
        IFormFile? file,
        [FromForm] int? postId,
        [FromForm] int? recipeId,
        [FromForm] string? title,
        CancellationToken ct)
    {
        EnsureAuthorizedMember();

        if (file == null || file.Length == 0)
        {
            throw AppException.BadRequest(ErrorCodes.ErrValidation, "Tệp tải lên không được để trống.");
        }

        await using var stream = file.OpenReadStream();
        var dto = new CreateVideoDto
        {
            FileStream = stream,
            FileName = file.FileName,
            FileLength = file.Length,
            PostId = postId,
            RecipeId = recipeId,
            Title = title
        };

        var result = await _videoService.CreateVideoAsync((int)CurrentUserId!.Value, dto, ct);

        var message = result.Status == "PROCESSING"
            ? "Đã nhận video, đang xử lý."
            : "Đã tải ảnh lên thành công.";

        return AcceptedResponse(result, message);
    }

    /// <summary>
    /// FN-POST-09: Xem bản phiên âm & công thức của video.
    /// Trả bản phiên âm (Speech-to-Text) và công thức đã trích xuất sẵn của video công khai.
    /// Khách bị trừ 1 trong 03 lượt AI dùng thử; Thành viên không giới hạn; người xem không kích hoạt bóc tách lại.
    /// </summary>
    /// <param name="id">Mã video (> 0).</param>
    /// <param name="ct">CancellationToken.</param>
    /// <returns>Bản phiên âm thô, công thức bóc tách và thông tin hạn mức (cho Khách).</returns>
    [HttpGet("{id:int}/transcript")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<VideoTranscriptResponseData>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> GetVideoTranscript([FromRoute] int id, CancellationToken ct)
    {
        if (id <= 0)
        {
            throw AppException.BadRequest(ErrorCodes.ErrValidation, "Mã video không hợp lệ (id > 0).");
        }

        var isGuest = IsGuestUser();
        var guestId = isGuest ? ResolveGuestId() : null;
        var currentUserId = isGuest ? null : (CurrentUserId.HasValue ? (int?)CurrentUserId.Value : null);

        var result = await _videoService.GetVideoTranscriptAsync(id, isGuest, guestId, currentUserId, ct);
        return OkResponse(result, "OK");
    }

    /// <summary>
    /// FN-POST-10: Bóc tách công thức từ video (Whisper + Gemini).
    /// Tác vụ ngầm chạy một lần cho mỗi video do tác giả đăng: trích âm thanh → Whisper STT → bản phiên âm thô → Gemini định dạng công thức → tạo Recipes nháp.
    /// </summary>
    /// <param name="id">Mã video của chính tác giả (> 0).</param>
    /// <param name="ct">CancellationToken.</param>
    /// <returns>HTTP 202 Accepted: { videoId, transcriptStatus: "PENDING" }.</returns>
    [HttpPost("{id:int}/extract-recipe")]
    [Authorize(Policy = PolicyNames.RequireMember)]
    [ProducesResponseType(typeof(ApiResponse<ExtractVideoRecipeResponseData>), StatusCodes.Status202Accepted)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> ExtractRecipe([FromRoute] int id, CancellationToken ct)
    {
        if (id <= 0)
        {
            throw AppException.BadRequest(ErrorCodes.ErrValidation, "Mã video không hợp lệ (id > 0).");
        }

        EnsureAuthorizedMember();

        var result = await _videoService.ExtractRecipeAsync(id, (int)CurrentUserId!.Value, ct);
        return AcceptedResponse(result, "Đang bóc tách công thức.");
    }

    /// <summary>
    /// Kiểm tra tính hợp lệ của tài khoản đăng nhập và từ chối khách vãng lai gọi thao tác ghi cộng đồng (FN-AUTH-03).
    /// </summary>
    private void EnsureAuthorizedMember()
    {
        if (string.Equals(CurrentUserRole, UserRoles.Guest, System.StringComparison.OrdinalIgnoreCase))
        {
            throw AppException.Forbidden(
                ErrorCodes.ErrGuestCommunityActionDenied,
                "Khách vãng lai thực hiện thao tác ghi cộng đồng (FN-AUTH-03).");
        }

        if (!CurrentUserId.HasValue)
        {
            throw AppException.Unauthorized(
                ErrorCodes.ErrAuthInvalidToken,
                "Phiên đăng nhập không hợp lệ hoặc thiếu thông tin định danh.");
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
        return string.IsNullOrEmpty(role) || string.Equals(role, UserRoles.Guest, System.StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Phân giải mã định danh của khách vãng lai từ Claim, Header hoặc Cookie.
    /// </summary>
    private string? ResolveGuestId()
    {
        if (!string.IsNullOrEmpty(CurrentGuestUuid))
        {
            return CurrentGuestUuid;
        }

        if (Request.Headers.TryGetValue("X-Guest-UUID", out var uuidHeader) && !string.IsNullOrEmpty(uuidHeader.FirstOrDefault()))
        {
            return uuidHeader.FirstOrDefault();
        }

        if (Request.Headers.TryGetValue("X-Guest-Id", out var idHeader) && !string.IsNullOrEmpty(idHeader.FirstOrDefault()))
        {
            return idHeader.FirstOrDefault();
        }

        if (Request.Cookies.TryGetValue("guest_uuid", out var cookieValue) && !string.IsNullOrEmpty(cookieValue))
        {
            return cookieValue;
        }

        return null;
    }
}
