using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using VeggieLife.API.DTOs.Fridge;
using VeggieLife.BLL.Common.Constants;
using VeggieLife.BLL.Common.Exceptions;
using VeggieLife.BLL.Common.Responses;
using VeggieLife.BLL.DTOs.Fridge.Requests;
using VeggieLife.BLL.DTOs.Fridge.Responses;
using VeggieLife.BLL.Services.Fridge;

namespace VeggieLife.API.Controllers;

/// <summary>
/// Controller xử lý nhận diện nguyên liệu qua ảnh chụp tủ lạnh bằng Gemini Vision (FN-VIS-01)
/// và quản lý nguyên liệu trong tủ lạnh ảo (FN-VIS-02).
/// </summary>
[ApiController]
[Route("api/v1/fridge")]
public class FridgeController : BaseApiController
{
    private readonly IFridgeService _fridgeService;

    public FridgeController(IFridgeService fridgeService)
    {
        _fridgeService = fridgeService;
    }

    /// <summary>
    /// FN-VIS-01: Nhận diện nguyên liệu qua ảnh (Gemini Vision).
    /// Nhận diện nguyên liệu thực vật và ước lượng độ tươi (FRESH / NON-FRESH) từ ảnh tủ lạnh/rổ rau.
    /// Thành viên: lưu vào tủ lạnh ảo; thường 10 ảnh/ngày, VIP 30 ảnh/ngày.
    /// Khách: chỉ xem kết quả, không lưu, tính vào 3 lượt dùng thử AI.
    /// </summary>
    [HttpPost("detections")]
    [Authorize]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(ApiResponse<CreateFridgeDetectionResponseData>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status413PayloadTooLarge)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> CreateFridgeDetection(
        [FromForm] CreateFridgeDetectionForm form,
        CancellationToken ct)
    {
        if (User?.Identity?.IsAuthenticated != true)
        {
            throw AppException.Unauthorized(
                ErrorCodes.ErrAuthInvalidToken,
                "Thiếu hoặc sai token.");
        }

        if (form == null || form.Image == null || form.Image.Length == 0)
        {
            throw AppException.BadRequest(
                ErrorCodes.ErrValidation,
                "Dữ liệu đầu vào không hợp lệ. Vui lòng tải lên tệp ảnh tủ lạnh.");
        }

        var userRole = CurrentUserRole ?? UserRoles.Member;
        var userId = CurrentUserId.HasValue ? (int)CurrentUserId.Value : (int?)null;
        var guestUuid = CurrentGuestUuid;

        using var stream = form.Image.OpenReadStream();
        var inputDto = new FridgeDetectionInputDto
        {
            ImageStream = stream,
            FileName = form.Image.FileName,
            FileLength = form.Image.Length,
            ContentType = form.Image.ContentType
        };

        var result = await _fridgeService.DetectIngredientsAsync(
            inputDto,
            userId,
            userRole,
            guestUuid,
            ct);

        return OkResponse(result, "Đã nhận diện nguyên liệu.");
    }

    /// <summary>
    /// FN-VIS-02: Xem danh sách nguyên liệu trong tủ lạnh ảo.
    /// Xem nguyên liệu trong tủ lạnh ảo, ưu tiên các mục NON-FRESH xếp trước.
    /// </summary>
    [HttpGet("items")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<FridgeItemDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> ListFridgeItems(
        [FromQuery] string? freshness,
        CancellationToken ct)
    {
        var userId = EnsureMemberUser();
        var items = await _fridgeService.GetFridgeItemsAsync(userId, freshness, ct);
        return OkResponse(items, "OK");
    }

    /// <summary>
    /// FN-VIS-02: Thêm thủ công nguyên liệu vào tủ lạnh ảo.
    /// Chọn Ingredient_ID (bắt buộc) và có thể đặt tên hiển thị tùy chọn.
    /// </summary>
    [HttpPost("items")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<FridgeItemDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateFridgeItem(
        [FromBody] CreateFridgeItemRequest request,
        CancellationToken ct)
    {
        var userId = EnsureMemberUser();
        var item = await _fridgeService.AddFridgeItemAsync(userId, request, ct);
        return CreatedResponse(item, "Đã thêm.");
    }

    /// <summary>
    /// FN-VIS-02: Cập nhật độ tươi (FRESH / NON-FRESH) hoặc tên hiển thị của nguyên liệu trong tủ lạnh ảo.
    /// </summary>
    [HttpPut("items/{id:int}")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<FridgeItemDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateFridgeItem(
        [FromRoute] int id,
        [FromBody] UpdateFridgeItemRequest request,
        CancellationToken ct)
    {
        var userId = EnsureMemberUser();
        var item = await _fridgeService.UpdateFridgeItemAsync(userId, id, request, ct);
        return OkResponse(item, "Đã cập nhật.");
    }

    /// <summary>
    /// FN-VIS-02: Xóa nguyên liệu khỏi tủ lạnh ảo.
    /// </summary>
    [HttpDelete("items/{id:int}")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteFridgeItem(
        [FromRoute] int id,
        CancellationToken ct)
    {
        var userId = EnsureMemberUser();
        await _fridgeService.DeleteFridgeItemAsync(userId, id, ct);
        return OkResponse<object?>(null, "Đã xóa.");
    }

    /// <summary>
    /// FN-VIS-03: Gợi ý món từ nguyên liệu trong tủ.
    /// Gợi ý công thức có độ tương đồng nguyên liệu ≥ 70% với tủ lạnh ảo,
    /// ưu tiên món dùng nguyên liệu đang NON-FRESH (cần dùng sớm),
    /// loại món vi phạm trường phái hoặc dị ứng.
    /// </summary>
    [HttpGet("recipe-suggestions")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<FridgeRecipeSuggestionDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> ListFridgeRecipeSuggestions(
        [FromQuery] double minSimilarity = 0.7,
        [FromQuery] int limit = 10,
        CancellationToken ct = default)
    {
        if (minSimilarity < 0.699999 || minSimilarity > 1.000001)
        {
            throw AppException.BadRequest(
                ErrorCodes.ErrValidation,
                "minSimilarity phải nằm trong khoảng từ 0.7 đến 1.0 (mặc định 0.7).");
        }

        if (limit < 1 || limit > 20)
        {
            throw AppException.BadRequest(
                ErrorCodes.ErrValidation,
                "limit phải nằm trong khoảng từ 1 đến 20 (mặc định 10).");
        }

        var userId = EnsureMemberUser();
        var suggestions = await _fridgeService.GetRecipeSuggestionsAsync(userId, minSimilarity, limit, ct);
        return OkResponse(suggestions, "OK");
    }


    private int EnsureMemberUser()
    {
        if (User?.Identity?.IsAuthenticated != true || CurrentUserId == null)
        {
            throw AppException.Unauthorized(
                ErrorCodes.ErrAuthInvalidToken,
                "Thiếu, sai chữ ký hoặc hết hạn JWT.");
        }

        var userRole = CurrentUserRole ?? UserRoles.Member;
        if (string.Equals(userRole, UserRoles.Guest, StringComparison.OrdinalIgnoreCase))
        {
            throw AppException.Forbidden(
                ErrorCodes.ErrGuestPersonalizationRestricted,
                "Khách vãng lai gọi tính năng cá nhân hóa (FN-AUTH-03).");
        }

        return (int)CurrentUserId.Value;
    }
}
