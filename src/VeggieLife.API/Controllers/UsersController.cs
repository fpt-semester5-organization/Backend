using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VeggieLife.BLL.Common.Constants;
using VeggieLife.BLL.Common.Exceptions;
using VeggieLife.BLL.DTOs.Profile.Requests;
using VeggieLife.BLL.DTOs.Profile.Responses;
using VeggieLife.BLL.Services.Profile;

namespace VeggieLife.API.Controllers;

/// <summary>
/// API Controller quản lý thông tin người dùng, hồ sơ sức khỏe và khai báo dị ứng (FN-PROF-01, FN-PROF-02, FN-PROF-04, FN-PROF-05).
/// </summary>
[Route("api/v1/users")]
public class UsersController : BaseApiController
{
    private readonly IProfileService _profileService;

    public UsersController(IProfileService profileService)
    {
        _profileService = profileService;
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
