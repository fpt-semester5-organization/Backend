using System.Threading;
using System.Threading.Tasks;
using VeggieLife.BLL.DTOs.Profile.Requests;
using VeggieLife.BLL.DTOs.Profile.Responses;

namespace VeggieLife.BLL.Services.Profile;

/// <summary>
/// Interface dịch vụ quản lý hồ sơ người dùng, chỉ số sức khỏe & danh mục dị ứng (FN-PROF-01, FN-PROF-02, FN-PROF-04, FN-PROF-05).
/// </summary>
public interface IProfileService
{
    /// <summary>
    /// Lấy thông tin cá nhân, chỉ số sức khỏe và danh sách dị ứng của người dùng hiện tại (FN-PROF-01).
    /// </summary>
    Task<CurrentUserProfileResponse> GetCurrentUserAsync(int userId, CancellationToken ct = default);

    /// <summary>
    /// Cập nhật thông tin cá nhân (Họ tên, Số điện thoại) của người dùng hiện tại (FN-PROF-02).
    /// </summary>
    Task<UpdateCurrentUserResponse> UpdateCurrentUserAsync(int userId, UpdateCurrentUserRequest request, CancellationToken ct = default);

    /// <summary>
    /// Thiết lập hoặc cập nhật hồ sơ sức khỏe, tự động tính BMI/BMR/TDEE/Calo mục tiêu và ghi lịch sử cân nặng (FN-PROF-04).
    /// </summary>
    Task<HealthProfileDto> UpdateHealthProfileAsync(int userId, UpdateCurrentUserHealthProfileRequest request, CancellationToken ct = default);

    /// <summary>
    /// Thay thế toàn bộ danh sách chất dị ứng của người dùng (FN-PROF-05).
    /// </summary>
    Task<ReplaceCurrentUserAllergiesResponse> ReplaceAllergiesAsync(int userId, ReplaceCurrentUserAllergiesRequest request, CancellationToken ct = default);
}
