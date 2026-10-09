using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using VeggieLife.BLL.Common.Responses;
using VeggieLife.BLL.DTOs.Profile.Requests;
using VeggieLife.BLL.DTOs.Profile.Responses;

namespace VeggieLife.BLL.Services.Profile;

/// <summary>
/// Interface dịch vụ quản lý hồ sơ người dùng, chỉ số sức khỏe, ảnh đại diện & lịch sử cân nặng (FN-PROF-01, FN-PROF-02, FN-PROF-03, FN-PROF-04, FN-PROF-05, FN-PROF-06).
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
    /// Cập nhật ảnh đại diện người dùng lên Cloudinary và tối ưu hóa 500x500 px (FN-PROF-03).
    /// </summary>
    Task<UploadCurrentUserAvatarResponse> UploadAvatarAsync(int userId, Stream fileStream, string fileName, CancellationToken ct = default);

    /// <summary>
    /// Thiết lập hoặc cập nhật hồ sơ sức khỏe, tự động tính BMI/BMR/TDEE/Calo mục tiêu và ghi lịch sử cân nặng (FN-PROF-04).
    /// </summary>
    Task<HealthProfileDto> UpdateHealthProfileAsync(int userId, UpdateCurrentUserHealthProfileRequest request, CancellationToken ct = default);

    /// <summary>
    /// Thay thế toàn bộ danh sách chất dị ứng của người dùng (FN-PROF-05).
    /// </summary>
    Task<ReplaceCurrentUserAllergiesResponse> ReplaceAllergiesAsync(int userId, ReplaceCurrentUserAllergiesRequest request, CancellationToken ct = default);

    /// <summary>
    /// Ghi nhận mốc cân nặng mới và tự động cập nhật lại các chỉ số sức khỏe dẫn xuất (FN-PROF-06).
    /// </summary>
    Task<CreateCurrentUserWeightEntryResponse> CreateWeightEntryAsync(int userId, CreateCurrentUserWeightEntryRequest request, CancellationToken ct = default);

    /// <summary>
    /// Xem lịch sử cân nặng theo khoảng thời gian và phân trang (FN-PROF-06).
    /// </summary>
    Task<(IReadOnlyList<WeightHistoryEntryDto> Items, PagedMeta Meta)> ListWeightHistoryAsync(int userId, DateOnly? from, DateOnly? to, int page = 1, int size = 20, CancellationToken ct = default);
}
