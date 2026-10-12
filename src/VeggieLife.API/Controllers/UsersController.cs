using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VeggieLife.BLL.Common.Constants;
using VeggieLife.BLL.Common.Exceptions;
using VeggieLife.BLL.Common.Responses;
using VeggieLife.BLL.DTOs.Products.Responses;
using VeggieLife.BLL.DTOs.Profile.Requests;
using VeggieLife.BLL.DTOs.Profile.Responses;
using VeggieLife.BLL.Services.Products;
using VeggieLife.BLL.Services.Profile;

namespace VeggieLife.API.Controllers;

/// <summary>
/// API Controller quản lý thông tin người dùng, hồ sơ sức khỏe và khai báo dị ứng (FN-PROF-01, FN-PROF-02, FN-PROF-04, FN-PROF-05).
/// </summary>
[Route("api/v1/users")]
public class UsersController : BaseApiController
{
    private readonly IProfileService _profileService;
    private readonly IProductService _productService;

    public UsersController(IProfileService profileService, IProductService? productService = null)
    {
        _profileService = profileService;
        _productService = productService!;
    }

    /// <summary>
    /// FN-PROF-01: Xem thông tin cá nhân, chỉ số sức khỏe & danh sách dị ứng của tài khoản hiện tại.
    /// </summary>
    [HttpGet("me")]
    [Authorize(Policy = PolicyNames.RequireMember)]
    public async Task<IActionResult> GetCurrentUser(CancellationToken ct)
    {
        EnsureAuthorizedMember();
        var result = await _profileService.GetCurrentUserAsync((int)CurrentUserId!.Value, ct);
        return OkResponse(result, "Lấy hồ sơ thành công.");
    }

    /// <summary>
    /// FN-PROF-02: Cập nhật thông tin cá nhân (Họ tên, Số điện thoại).
    /// </summary>
    [HttpPatch("me")]
    [Authorize(Policy = PolicyNames.RequireMember)]
    public async Task<IActionResult> UpdateCurrentUser([FromBody] UpdateCurrentUserRequest request, CancellationToken ct)
    {
        EnsureAuthorizedMember();
        var result = await _profileService.UpdateCurrentUserAsync((int)CurrentUserId!.Value, request, ct);
        return OkResponse(result, "Cập nhật thông tin thành công.");
    }

    /// <summary>
    /// FN-PROF-04: Thiết lập / cập nhật hồ sơ sức khỏe & dinh dưỡng.
    /// Tự tính toán chỉ số BMI/BMR/TDEE/Calo mục tiêu và ghi nhận lịch sử cân nặng.
    /// </summary>
    [HttpPut("me/health-profile")]
    [Authorize(Policy = PolicyNames.RequireMember)]
    public async Task<IActionResult> UpdateCurrentUserHealthProfile([FromBody] UpdateCurrentUserHealthProfileRequest request, CancellationToken ct)
    {
        EnsureAuthorizedMember();
        var result = await _profileService.UpdateHealthProfileAsync((int)CurrentUserId!.Value, request, ct);
        return OkResponse(result, "Cập nhật hồ sơ sức khỏe thành công.");
    }

    /// <summary>
    /// FN-PROF-05: Khai báo thay thế toàn bộ danh sách chất dị ứng của người dùng.
    /// </summary>
    [HttpPut("me/allergies")]
    [Authorize(Policy = PolicyNames.RequireMember)]
    public async Task<IActionResult> ReplaceCurrentUserAllergies([FromBody] ReplaceCurrentUserAllergiesRequest request, CancellationToken ct)
    {
        EnsureAuthorizedMember();
        var result = await _profileService.ReplaceAllergiesAsync((int)CurrentUserId!.Value, request, ct);
        return OkResponse(result, "Cập nhật dị ứng thành công.");
    }

    /// <summary>
    /// FN-PROF-03: Cập nhật ảnh đại diện người dùng lên Cloudinary (chuẩn hóa 500x500 px).
    /// </summary>
    [HttpPost("me/avatar")]
    [Authorize(Policy = PolicyNames.RequireMember)]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> UploadCurrentUserAvatar(Microsoft.AspNetCore.Http.IFormFile? file, CancellationToken ct)
    {
        EnsureAuthorizedMember();

        if (file == null || file.Length == 0)
        {
            throw AppException.BadRequest(ErrorCodes.ErrMediaInvalidFile, "Tệp hình ảnh không được để trống.");
        }

        if (file.Length > 5 * 1024 * 1024)
        {
            throw new AppException(413, ErrorCodes.ErrMediaFileTooLarge, "Tệp vượt quá dung lượng tối đa 5 MB.");
        }

        await using var stream = file.OpenReadStream();
        var result = await _profileService.UploadAvatarAsync((int)CurrentUserId!.Value, stream, file.FileName, ct);
        return OkResponse(result, "Cập nhật ảnh đại diện thành công.");
    }

    /// <summary>
    /// FN-PROF-06: Ghi nhận mốc cân nặng mới và tự động tính lại chỉ số sức khỏe.
    /// </summary>
    [HttpPost("me/weight-history")]
    [Authorize(Policy = PolicyNames.RequireMember)]
    public async Task<IActionResult> CreateCurrentUserWeightEntry([FromBody] CreateCurrentUserWeightEntryRequest request, CancellationToken ct)
    {
        EnsureAuthorizedMember();
        var result = await _profileService.CreateWeightEntryAsync((int)CurrentUserId!.Value, request, ct);
        return CreatedResponse(result, "Đã ghi cân nặng.");
    }

    /// <summary>
    /// FN-PROF-06: Xem danh sách lịch sử cân nặng theo khoảng ngày có phân trang.
    /// </summary>
    [HttpGet("me/weight-history")]
    [Authorize(Policy = PolicyNames.RequireMember)]
    public async Task<IActionResult> ListCurrentUserWeightHistory(
        [FromQuery] System.DateOnly? from,
        [FromQuery] System.DateOnly? to,
        [FromQuery] int page = 1,
        [FromQuery] int size = 20,
        CancellationToken ct = default)
    {
        EnsureAuthorizedMember();
        var (items, meta) = await _profileService.ListWeightHistoryAsync((int)CurrentUserId!.Value, from, to, page, size, ct);
        return PagedResponse(items, meta, "OK");
    }

    /// <summary>
    /// BR-USR-09: Lịch sử quét mã vạch
    /// </summary>
    [HttpGet("me/scans")]
    [Authorize(Policy = PolicyNames.RequireMember)]
    public async Task<IActionResult> GetMyScans(
        [FromQuery] string? verdict, 
        [FromQuery] bool? allergenOnly, 
        [FromQuery] int page = 1, 
        [FromQuery] int size = 20)
    {
        EnsureAuthorizedMember();
        var result = await _productService.GetUserScanHistoryAsync((int)CurrentUserId!.Value, verdict, allergenOnly, page, size);
        return OkResponse(result, "Lấy lịch sử quét mã vạch thành công.");
    }

    /// <summary>
    /// Kiểm tra tính hợp lệ của tài khoản đăng nhập và từ chối khách vãng lai gọi tính năng cá nhân hóa (FN-AUTH-03).
    /// </summary>
    private void EnsureAuthorizedMember()
    {
        if (string.Equals(CurrentUserRole, UserRoles.Guest, System.StringComparison.OrdinalIgnoreCase))
        {
            throw AppException.Forbidden(
                ErrorCodes.ErrGuestPersonalizationRestricted,
                "Khách vãng lai gọi tính năng cá nhân hóa (FN-AUTH-03).");
        }

        if (!CurrentUserId.HasValue)
        {
            throw AppException.Unauthorized(
                ErrorCodes.ErrAuthInvalidToken,
                "Phiên đăng nhập không hợp lệ hoặc thiếu thông tin định danh.");
        }
    }
}
