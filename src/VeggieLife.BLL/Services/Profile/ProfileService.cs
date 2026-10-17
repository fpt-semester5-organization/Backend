using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using VeggieLife.BLL.Common.Constants;
using VeggieLife.BLL.Common.Exceptions;
using VeggieLife.BLL.DTOs.Profile.Requests;
using VeggieLife.BLL.DTOs.Profile.Responses;
using VeggieLife.BLL.Services.Cache;
using VeggieLife.DAL.Context;
using VeggieLife.BLL.Common.Responses;
using VeggieLife.BLL.Services.Media;
using VeggieLife.DAL.Entities;

namespace VeggieLife.BLL.Services.Profile;

/// <summary>
/// Dịch vụ quản lý hồ sơ sức khỏe và dinh dưỡng người dùng.
/// Thực thi các quy tắc nghiệp vụ: BR-USR-01, BR-USR-02, BR-USR-NUT-03, BR-SCI-03, BR-SCI-04, BR-ADM-MAS-001, BR-SYS-MAS-01, BR-PAY-SUB-02, BR-USR-MEDIA-01.
/// </summary>
public class ProfileService : IProfileService
{
    private readonly AppDbContext _db;
    private readonly IRedisService _redis;
    private readonly IMediaService _mediaService;
    private readonly ILogger<ProfileService> _logger;

    private static readonly Regex PhoneRegex = new(@"^0(3|5|7|8|9)[0-9]{8}$", RegexOptions.Compiled);

    public ProfileService(
        AppDbContext db,
        IRedisService redis,
        IMediaService mediaService,
        ILogger<ProfileService> logger)
    {
        _db = db;
        _redis = redis;
        _mediaService = mediaService;
        _logger = logger;
    }

    /// <summary>
    /// FN-PROF-01: Xem hồ sơ cá nhân, chỉ số sức khỏe & danh mục dị ứng.
    /// </summary>
    public async Task<CurrentUserProfileResponse> GetCurrentUserAsync(int userId, CancellationToken ct = default)
    {
        // 1. Kiểm tra bộ nhớ đệm Redis (cache 10 phút - D31)
        var cacheKey = $"cache:profile:{userId}";
        var cached = await _redis.GetAsync<CurrentUserProfileResponse>(cacheKey, ct);
        if (cached != null)
        {
            return cached;
        }

        // 2. Truy vấn dữ liệu từ CSDL
        var user = await _db.Users
            .Include(u => u.HealthProfile)
                .ThenInclude(hp => hp!.DietarySchool)
            .Include(u => u.UserAllergies)
                .ThenInclude(ua => ua.Allergen)
            .Include(u => u.PaymentTransactions)
            .FirstOrDefaultAsync(u => u.UserId == userId, ct);

        if (user == null)
        {
            throw AppException.NotFound(ErrorCodes.ErrContentNotFound, "Không tìm thấy thông tin tài khoản người dùng.");
        }

        // 3. Đánh dấu chỉ số cũ nếu quá 30 ngày chưa cập nhật (BR-USR-02, D31)
        if (user.HealthProfile != null)
        {
            var daysSinceUpdate = (DateTime.UtcNow - user.HealthProfile.UpdatedAt).TotalDays;
            if (daysSinceUpdate > 30 && !user.HealthProfile.IsStaleMetrics)
            {
                user.HealthProfile.IsStaleMetrics = true;
                await _db.SaveChangesAsync(ct);
            }
        }

        // 4. Kiểm tra trạng thái VIP còn hạn (BR-PAY-SUB-02)
        var now = DateTime.UtcNow;
        var isVip = string.Equals(user.Role, UserRoles.Vip, StringComparison.OrdinalIgnoreCase) ||
                    user.PaymentTransactions.Any(pt =>
                        string.Equals(pt.TransactionStatus, "SUCCESS", StringComparison.OrdinalIgnoreCase) &&
                        pt.StartsAt <= now && now < pt.EndsAt);

        // 5. Ánh xạ hồ sơ sức khỏe nếu đã hoàn tất thiết lập
        HealthProfileDto? healthDto = null;
        if (user.HealthProfile != null)
        {
            healthDto = new HealthProfileDto
            {
                Gender = user.HealthProfile.Gender,
                DateOfBirth = user.HealthProfile.DateOfBirth.ToString("yyyy-MM-dd"),
                HeightCm = user.HealthProfile.HeightCm,
                CurrentWeightKg = user.HealthProfile.CurrentWeightKg,
                TargetWeightKg = user.HealthProfile.TargetWeightKg,
                Bmi = user.HealthProfile.BmiValue,
                BmiCategory = HealthMetricCalculator.GetBmiCategory(user.HealthProfile.BmiValue),
                BmrKcal = user.HealthProfile.BmrKcal,
                TdeeKcal = user.HealthProfile.TdeeKcal,
                TargetDailyCalories = user.HealthProfile.TargetDailyCalories,
                HealthGoal = user.HealthProfile.HealthGoal,
                DietarySchool = new DietarySchoolBriefDto
                {
                    Id = user.HealthProfile.DietarySchool.DietarySchoolId,
                    Code = user.HealthProfile.DietarySchool.SchoolCode,
                    Name = user.HealthProfile.DietarySchool.SchoolName
                },
                RequiresAlliumFree = user.HealthProfile.RequiresAlliumFree,
                RegionPreference = user.HealthProfile.RegionPreference,
                IsStaleMetrics = user.HealthProfile.IsStaleMetrics,
                UpdatedAt = user.HealthProfile.UpdatedAt
            };
        }

        // 6. Ánh xạ danh sách dị ứng
        var allergensList = user.UserAllergies
            .Select(ua => new AllergenBriefDto
            {
                AllergenId = ua.AllergenId,
                Name = ua.Allergen.AllergenName
            })
            .ToList();

        var response = new CurrentUserProfileResponse
        {
            UserId = user.UserId,
            FullName = user.FullName,
            Email = user.Email,
            PhoneNumber = user.PhoneNumber,
            AvatarUrl = user.AvatarUrl,
            Role = user.Role,
            IsVip = isVip,
            Health = healthDto,
            Allergens = allergensList
        };

        // 7. Lưu vào bộ nhớ đệm Redis 10 phút
        await _redis.SetAsync(cacheKey, response, TimeSpan.FromMinutes(10), ct);

        return response;
    }

    /// <summary>
    /// FN-PROF-02: Cập nhật thông tin cá nhân (Họ tên, Số điện thoại).
    /// </summary>
    public async Task<UpdateCurrentUserResponse> UpdateCurrentUserAsync(int userId, UpdateCurrentUserRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.FullName) && string.IsNullOrWhiteSpace(request.PhoneNumber))
        {
            throw AppException.BadRequest(ErrorCodes.ErrValidation, "Vui lòng cung cấp ít nhất họ tên hoặc số điện thoại để cập nhật.");
        }

        var user = await _db.Users.FirstOrDefaultAsync(u => u.UserId == userId, ct);
        if (user == null)
        {
            throw AppException.NotFound(ErrorCodes.ErrContentNotFound, "Không tìm thấy tài khoản người dùng.");
        }

        if (!string.IsNullOrWhiteSpace(request.FullName))
        {
            var trimmedName = request.FullName.Trim();
            if (trimmedName.Length < 2 || trimmedName.Length > 100)
            {
                throw AppException.BadRequest(ErrorCodes.ErrValidation, "Họ tên phải từ 2 đến 100 ký tự.");
            }
            user.FullName = trimmedName;
        }

        if (!string.IsNullOrWhiteSpace(request.PhoneNumber))
        {
            var trimmedPhone = request.PhoneNumber.Trim();
            if (!PhoneRegex.IsMatch(trimmedPhone))
            {
                throw AppException.BadRequest(ErrorCodes.ErrValidation, "Số điện thoại không đúng định dạng số di động Việt Nam.");
            }
            user.PhoneNumber = trimmedPhone;
        }

        user.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        // Xóa cache hồ sơ cá nhân
        await _redis.RemoveAsync($"cache:profile:{userId}", ct);

        return new UpdateCurrentUserResponse
        {
            UserId = user.UserId,
            FullName = user.FullName,
            PhoneNumber = user.PhoneNumber
        };
    }

    /// <summary>
    /// FN-PROF-04: Thiết lập / cập nhật hồ sơ sức khỏe & dinh dưỡng.
    /// Tự tính BMI/BMR/TDEE/Calo mục tiêu và ghi một mốc cân nặng vào lịch sử.
    /// </summary>
    public async Task<HealthProfileDto> UpdateHealthProfileAsync(int userId, UpdateCurrentUserHealthProfileRequest request, CancellationToken ct = default)
    {
        // 1. Kiểm tra khoảng giá trị hợp lệ (BR-USR-01)
        if (request.HeightCm < 50 || request.HeightCm > 250)
        {
            throw AppException.BadRequest(ErrorCodes.ErrValidation, "Chiều cao phải nằm trong khoảng từ 50 đến 250 cm.");
        }

        if (request.WeightKg < 20 || request.WeightKg > 300)
        {
            throw AppException.BadRequest(ErrorCodes.ErrValidation, "Cân nặng phải nằm trong khoảng từ 20 đến 300 kg.");
        }

        if (request.TargetWeightKg.HasValue && (request.TargetWeightKg.Value < 20 || request.TargetWeightKg.Value > 300))
        {
            throw AppException.BadRequest(ErrorCodes.ErrValidation, "Cân nặng mục tiêu phải nằm trong khoảng từ 20 đến 300 kg.");
        }

        // Kiểm tra tuổi ≥ 10 và ngày sinh không ở tương lai
        if (request.DateOfBirth > DateOnly.FromDateTime(DateTime.UtcNow))
        {
            throw AppException.BadRequest(ErrorCodes.ErrValidation, "Ngày sinh không được ở tương lai.");
        }

        var age = HealthMetricCalculator.CalculateAge(request.DateOfBirth);
        if (age < 10)
        {
            throw AppException.BadRequest(ErrorCodes.ErrValidation, "Người dùng phải từ 10 tuổi trở lên để thiết lập hồ sơ sức khỏe.");
        }

        // Kiểm tra giới tính
        var rawGender = (request.Gender ?? string.Empty).Trim().ToUpperInvariant();
        var normalizedGender = rawGender switch
        {
            "NAM" or "MALE" => "MALE",
            "NU" or "NỮ" or "FEMALE" => "FEMALE",
            _ => rawGender
        };
        if (normalizedGender != "MALE" && normalizedGender != "FEMALE")
        {
            throw AppException.BadRequest(ErrorCodes.ErrValidation, "Giới tính phải là MALE hoặc FEMALE.");
        }

        // Kiểm tra mục tiêu sức khỏe
        var rawGoal = (request.HealthGoal ?? string.Empty).Trim().ToUpperInvariant();
        var normalizedGoal = rawGoal switch
        {
            "GIAM_CAN" or "LOSE_WEIGHT" => "LOSE_WEIGHT",
            "GIU_CAN" or "MAINTAIN" => "MAINTAIN",
            "TANG_CO" or "GAIN_MUSCLE" => "GAIN_MUSCLE",
            _ => rawGoal
        };
        if (normalizedGoal != "LOSE_WEIGHT" && normalizedGoal != "MAINTAIN" && normalizedGoal != "GAIN_MUSCLE")
        {
            throw AppException.BadRequest(ErrorCodes.ErrValidation, "Mục tiêu sức khỏe phải là LOSE_WEIGHT, MAINTAIN hoặc GAIN_MUSCLE.");
        }

        // Kiểm tra khẩu vị vùng miền (tùy chọn)
        var regionPref = "SOUTH";
        if (!string.IsNullOrWhiteSpace(request.RegionPreference))
        {
            var rawRegion = request.RegionPreference.Trim().ToUpperInvariant();
            regionPref = rawRegion switch
            {
                "MIEN_BAC" or "BAC" or "NORTH" => "NORTH",
                "MIEN_TRUNG" or "TRUNG" or "CENTRAL" => "CENTRAL",
                "MIEN_NAM" or "NAM" or "SOUTH" => "SOUTH",
                _ => rawRegion
            };
            if (regionPref != "NORTH" && regionPref != "CENTRAL" && regionPref != "SOUTH")
            {
                throw AppException.BadRequest(ErrorCodes.ErrValidation, "Khẩu vị vùng miền phải là NORTH, CENTRAL hoặc SOUTH.");
            }
        }

        // 2. Kiểm tra trường phái ăn chay tồn tại (BR-ADM-MAS-001)
        var dietarySchool = await _db.DietarySchools.FirstOrDefaultAsync(ds => ds.DietarySchoolId == request.DietarySchoolId, ct);
        if (dietarySchool == null)
        {
            throw AppException.NotFound(ErrorCodes.ErrDietSchoolNotFound, "Trường phái ăn chay không tồn tại trong hệ thống.");
        }

        var user = await _db.Users.Include(u => u.HealthProfile).FirstOrDefaultAsync(u => u.UserId == userId, ct);
        if (user == null)
        {
            throw AppException.NotFound(ErrorCodes.ErrContentNotFound, "Không tìm thấy thông tin tài khoản người dùng.");
        }

        // 3. Tự tính các chỉ số dẫn xuất dinh dưỡng theo công thức chuẩn
        // BR-USR-02: BMI = Cân nặng (kg) / (Chiều cao (m))^2
        var bmi = HealthMetricCalculator.CalculateBmi(request.HeightCm, request.WeightKg);
        // BR-SCI-03: Phân loại WHO/WPRO Châu Á
        var bmiCategory = HealthMetricCalculator.GetBmiCategory(bmi);
        // BR-USR-NUT-03: BMR theo Mifflin-St Jeor
        var bmr = HealthMetricCalculator.CalculateBmr(request.HeightCm, request.WeightKg, age, normalizedGender);
        // BR-USR-NUT-03: TDEE = BMR × 1.2
        var tdee = HealthMetricCalculator.CalculateTdee(bmr);
        // BR-USR-NUT-03: Calo mục tiêu / ngày
        var targetCalories = HealthMetricCalculator.CalculateTargetDailyCalories(tdee, normalizedGoal, normalizedGender);

        var now = DateTime.UtcNow;

        // 4. Cập nhật HealthProfiles và thêm bản ghi UserWeightHistory (SaveChangesAsync tự động chạy trong transaction nguyên tử)
        if (user.HealthProfile == null)
        {
            var newProfile = new HealthProfile
            {
                UserId = userId,
                DietarySchoolId = request.DietarySchoolId,
                Gender = normalizedGender,
                DateOfBirth = request.DateOfBirth,
                HeightCm = request.HeightCm,
                CurrentWeightKg = request.WeightKg,
                TargetWeightKg = request.TargetWeightKg,
                BmiValue = bmi,
                BmrKcal = bmr,
                TdeeKcal = tdee,
                TargetDailyCalories = targetCalories,
                RequiresAlliumFree = request.RequiresAlliumFree ?? false,
                RegionPreference = regionPref,
                HealthGoal = normalizedGoal,
                IsStaleMetrics = false,
                CreatedAt = now,
                UpdatedAt = now
            };
            _db.HealthProfiles.Add(newProfile);
            user.HealthProfile = newProfile;
        }
        else
        {
            user.HealthProfile.DietarySchoolId = request.DietarySchoolId;
            user.HealthProfile.Gender = normalizedGender;
            user.HealthProfile.DateOfBirth = request.DateOfBirth;
            user.HealthProfile.HeightCm = request.HeightCm;
            user.HealthProfile.CurrentWeightKg = request.WeightKg;
            user.HealthProfile.TargetWeightKg = request.TargetWeightKg;
            user.HealthProfile.BmiValue = bmi;
            user.HealthProfile.BmrKcal = bmr;
            user.HealthProfile.TdeeKcal = tdee;
            user.HealthProfile.TargetDailyCalories = targetCalories;
            user.HealthProfile.RequiresAlliumFree = request.RequiresAlliumFree ?? false;
            user.HealthProfile.RegionPreference = regionPref;
            user.HealthProfile.HealthGoal = normalizedGoal;
            user.HealthProfile.IsStaleMetrics = false;
            user.HealthProfile.UpdatedAt = now;
        }

        // Ghi một mốc cân nặng vào lịch sử cân nặng
        var weightLog = new UserWeightHistory
        {
            UserId = userId,
            WeightKg = request.WeightKg,
            LoggedDate = DateOnly.FromDateTime(now),
            Note = "Cập nhật từ hồ sơ sức khỏe",
            CreatedAt = now
        };
        _db.UserWeightHistories.Add(weightLog);

        await _db.SaveChangesAsync(ct);

        // 5. Xóa cache hồ sơ cá nhân và cache gợi ý thực đơn (BR-SCI-04)
        await _redis.RemoveAsync($"cache:profile:{userId}", ct);
        await _redis.RemoveAsync($"meal_plan_cache:{userId}", ct);

        return new HealthProfileDto
        {
            Gender = normalizedGender,
            DateOfBirth = request.DateOfBirth.ToString("yyyy-MM-dd"),
            HeightCm = request.HeightCm,
            CurrentWeightKg = request.WeightKg,
            TargetWeightKg = request.TargetWeightKg,
            Bmi = bmi,
            BmiCategory = bmiCategory,
            BmrKcal = bmr,
            TdeeKcal = tdee,
            TargetDailyCalories = targetCalories,
            HealthGoal = normalizedGoal,
            DietarySchool = new DietarySchoolBriefDto
            {
                Id = dietarySchool.DietarySchoolId,
                Code = dietarySchool.SchoolCode,
                Name = dietarySchool.SchoolName
            },
            RequiresAlliumFree = request.RequiresAlliumFree ?? false,
            RegionPreference = regionPref,
            IsStaleMetrics = false,
            UpdatedAt = now
        };
    }

    /// <summary>
    /// FN-PROF-05: Khai báo và thay thế toàn bộ danh sách dị ứng của người dùng.
    /// </summary>
    public async Task<ReplaceCurrentUserAllergiesResponse> ReplaceAllergiesAsync(int userId, ReplaceCurrentUserAllergiesRequest request, CancellationToken ct = default)
    {
        if (request.Allergens == null)
        {
            throw AppException.BadRequest(ErrorCodes.ErrValidation, "Danh sách dị ứng không được để trống.");
        }

        if (request.Allergens.Count > 20)
        {
            throw AppException.BadRequest(ErrorCodes.ErrValidation, "Danh sách dị ứng tối đa 20 mục.");
        }

        var distinctAllergenIds = request.Allergens.Select(a => a.AllergenId).Distinct().ToList();

        // Kiểm tra tất cả allergenId tồn tại trong bảng Allergens
        if (distinctAllergenIds.Count > 0)
        {
            var existingAllergens = await _db.Allergens
                .Where(a => distinctAllergenIds.Contains(a.AllergenId))
                .ToListAsync(ct);

            if (existingAllergens.Count != distinctAllergenIds.Count)
            {
                throw AppException.NotFound(ErrorCodes.ErrAllergenNotFound, "Chất dị ứng không tồn tại trong hệ thống.");
            }
        }

        var user = await _db.Users.FirstOrDefaultAsync(u => u.UserId == userId, ct);
        if (user == null)
        {
            throw AppException.NotFound(ErrorCodes.ErrContentNotFound, "Không tìm thấy người dùng.");
        }

        var now = DateTime.UtcNow;

        // Cập nhật UserAllergies: đồng bộ danh sách chất dị ứng (cập nhật nếu đã có, xóa nếu bỏ chọn, thêm nếu mới)
        var oldAllergies = await _db.UserAllergies.Where(ua => ua.UserId == userId).ToListAsync(ct);
        var oldMap = oldAllergies.ToDictionary(ua => ua.AllergenId);

        var requestedIds = new HashSet<int>();
        foreach (var item in request.Allergens)
        {
            if (!requestedIds.Add(item.AllergenId))
            {
                continue;
            }

            if (oldMap.TryGetValue(item.AllergenId, out var existing))
            {
                existing.Notes = item.Notes?.Trim();
                existing.UpdatedAt = now;
            }
            else
            {
                _db.UserAllergies.Add(new UserAllergy
                {
                    UserId = userId,
                    AllergenId = item.AllergenId,
                    Notes = item.Notes?.Trim(),
                    CreatedAt = now,
                    UpdatedAt = now
                });
            }
        }

        var toRemove = oldAllergies.Where(ua => !requestedIds.Contains(ua.AllergenId)).ToList();
        if (toRemove.Count > 0)
        {
            _db.UserAllergies.RemoveRange(toRemove);
        }

        await _db.SaveChangesAsync(ct);

        // Xóa cache hồ sơ và gợi ý thực đơn (BR-SCI-04)
        await _redis.RemoveAsync($"cache:profile:{userId}", ct);
        await _redis.RemoveAsync($"meal_plan_cache:{userId}", ct);

        // Đọc lại danh sách dị ứng kèm tên chất dị ứng để trả về
        var resultList = await _db.UserAllergies
            .Where(ua => ua.UserId == userId)
            .Include(ua => ua.Allergen)
            .Select(ua => new AllergenBriefDto
            {
                AllergenId = ua.AllergenId,
                Name = ua.Allergen.AllergenName
            })
            .ToListAsync(ct);

        return new ReplaceCurrentUserAllergiesResponse
        {
            Allergens = resultList
        };
    }

    /// <summary>
    /// FN-PROF-03: Cập nhật ảnh đại diện người dùng lên Cloudinary và tối ưu hóa 500x500 px.
    /// </summary>
    public async Task<UploadCurrentUserAvatarResponse> UploadAvatarAsync(
        int userId,
        Stream fileStream,
        string fileName,
        CancellationToken ct = default)
    {
        // 1. Kiểm tra người dùng tồn tại
        var user = await _db.Users.FirstOrDefaultAsync(u => u.UserId == userId, ct);
        if (user == null)
        {
            throw AppException.Unauthorized(ErrorCodes.ErrAuthInvalidToken, "Không tìm thấy thông tin tài khoản người dùng.");
        }

        var oldAvatarUrl = user.AvatarUrl;

        // 2. Tải ảnh lên Cloudinary thư mục veggielife/avatars (BR-USR-MEDIA-01: kiểm tra magic bytes, <= 5 MB)
        var uploadResult = await _mediaService.UploadImageAsync(fileStream, fileName, "veggielife/avatars", ct);

        // 3. Tối ưu hóa kích thước thu về 500x500 px, định dạng WebP
        var avatarUrl = _mediaService.GetImageUrl(uploadResult.PublicId, width: 500, height: 500, crop: "fill", format: "webp");
        if (string.IsNullOrWhiteSpace(avatarUrl))
        {
            avatarUrl = uploadResult.SecureUrl ?? uploadResult.Url;
        }

        // 4. Cập nhật Users.Avatar_Url trong CSDL
        user.AvatarUrl = avatarUrl;
        user.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        // 5. Xóa bộ nhớ đệm hồ sơ người dùng
        await _redis.RemoveAsync($"cache:profile:{userId}", ct);

        // 6. Xóa ảnh cũ trên Cloudinary nếu có (thực hiện ngầm an toàn)
        if (!string.IsNullOrWhiteSpace(oldAvatarUrl) && oldAvatarUrl.Contains("veggielife/avatars"))
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    var uri = new Uri(oldAvatarUrl);
                    var path = uri.AbsolutePath;
                    var uploadIdx = path.IndexOf("veggielife/avatars");
                    if (uploadIdx >= 0)
                    {
                        var publicIdWithExt = path[uploadIdx..];
                        var dotIdx = publicIdWithExt.LastIndexOf('.');
                        var oldPublicId = dotIdx > 0 ? publicIdWithExt[..dotIdx] : publicIdWithExt;
                        await _mediaService.DeleteResourceAsync(oldPublicId, "image", CancellationToken.None);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Không thể xóa ảnh đại diện cũ trên Cloudinary: {Url}", oldAvatarUrl);
                }
            }, CancellationToken.None);
        }

        return new UploadCurrentUserAvatarResponse
        {
            AvatarUrl = avatarUrl
        };
    }

    /// <summary>
    /// FN-PROF-06: Ghi nhận mốc cân nặng mới và tự động cập nhật lại các chỉ số sức khỏe dẫn xuất.
    /// </summary>
    public async Task<CreateCurrentUserWeightEntryResponse> CreateWeightEntryAsync(
        int userId,
        CreateCurrentUserWeightEntryRequest request,
        CancellationToken ct = default)
    {
        // 1. Kiểm tra cân nặng hợp lệ: 20–300 kg (BR-USR-01)
        if (request.WeightKg < 20m || request.WeightKg > 300m)
        {
            throw AppException.BadRequest(ErrorCodes.ErrValidation, "Cân nặng phải trong khoảng từ 20 đến 300 kg.");
        }

        // 2. Ngày ghi nhận cân nặng không được ở tương lai
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var entryDate = request.LoggedDate ?? today;
        if (entryDate > today)
        {
            throw AppException.BadRequest(ErrorCodes.ErrValidation, "Ngày ghi nhận cân nặng không được ở tương lai.");
        }

        if (request.Note != null && request.Note.Length > 255)
        {
            throw AppException.BadRequest(ErrorCodes.ErrValidation, "Ghi chú mốc cân nặng tối đa 255 ký tự.");
        }

        // 3. Kiểm tra điều kiện trước: Đã có HealthProfile
        var profile = await _db.HealthProfiles.FirstOrDefaultAsync(hp => hp.UserId == userId, ct);
        if (profile == null)
        {
            throw new AppException(409, ErrorCodes.ErrProfileNotCompleted, "Người dùng chưa hoàn tất hồ sơ sức khỏe.");
        }

        // 4. Thêm bản ghi mới vào UserWeightHistory
        var weightLog = new UserWeightHistory
        {
            UserId = userId,
            WeightKg = request.WeightKg,
            LoggedDate = entryDate,
            Note = request.Note?.Trim(),
            CreatedAt = DateTime.UtcNow
        };
        _db.UserWeightHistories.Add(weightLog);

        // 5. Cập nhật Current_Weight_Kg và tính toán lại các chỉ số dẫn xuất (BR-USR-02, BR-USR-NUT-03)
        profile.CurrentWeightKg = request.WeightKg;
        var age = HealthMetricCalculator.CalculateAge(profile.DateOfBirth, entryDate);

        profile.BmiValue = HealthMetricCalculator.CalculateBmi(profile.HeightCm, request.WeightKg);
        profile.BmrKcal = HealthMetricCalculator.CalculateBmr(profile.HeightCm, request.WeightKg, age, profile.Gender);
        profile.TdeeKcal = HealthMetricCalculator.CalculateTdee(profile.BmrKcal);
        profile.TargetDailyCalories = HealthMetricCalculator.CalculateTargetDailyCalories(profile.TdeeKcal, profile.HealthGoal, profile.Gender);
        profile.IsStaleMetrics = false;
        profile.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);

        // 6. Xóa cache hồ sơ và gợi ý thực đơn
        await _redis.RemoveAsync($"cache:profile:{userId}", ct);
        await _redis.RemoveAsync($"meal_plan_cache:{userId}", ct);

        return new CreateCurrentUserWeightEntryResponse
        {
            LogId = weightLog.LogId,
            WeightKg = weightLog.WeightKg,
            Bmi = profile.BmiValue,
            TargetDailyCalories = profile.TargetDailyCalories
        };
    }

    /// <summary>
    /// FN-PROF-06: Xem danh sách lịch sử cân nặng theo khoảng thời gian và phân trang.
    /// </summary>
    public async Task<(IReadOnlyList<WeightHistoryEntryDto> Items, PagedMeta Meta)> ListWeightHistoryAsync(
        int userId,
        DateOnly? from,
        DateOnly? to,
        int page = 1,
        int size = 20,
        CancellationToken ct = default)
    {
        // 1. Kiểm tra tham số phân trang & khoảng ngày
        if (page < 1)
        {
            throw AppException.BadRequest(ErrorCodes.ErrValidation, "Số trang (page) phải lớn hơn hoặc bằng 1.");
        }

        if (size < 1 || size > 100)
        {
            throw AppException.BadRequest(ErrorCodes.ErrValidation, "Kích thước trang (size) phải từ 1 đến 100.");
        }

        if (from.HasValue && to.HasValue && from.Value > to.Value)
        {
            throw AppException.BadRequest(ErrorCodes.ErrValidation, "Ngày bắt đầu (from) không được lớn hơn ngày kết thúc (to).");
        }

        // 2. Truy vấn lịch sử cân nặng của người dùng
        var query = _db.UserWeightHistories
            .AsNoTracking()
            .Where(w => w.UserId == userId);

        if (from.HasValue)
        {
            query = query.Where(w => w.LoggedDate >= from.Value);
        }

        if (to.HasValue)
        {
            query = query.Where(w => w.LoggedDate <= to.Value);
        }

        var total = await query.CountAsync(ct);

        // Sắp xếp mới nhất trước (BR-USR-02)
        var items = await query
            .OrderByDescending(w => w.LoggedDate)
            .ThenByDescending(w => w.LogId)
            .Skip((page - 1) * size)
            .Take(size)
            .Select(w => new WeightHistoryEntryDto
            {
                LogId = w.LogId,
                WeightKg = w.WeightKg,
                LoggedDate = w.LoggedDate
            })
            .ToListAsync(ct);

        var meta = new PagedMeta(page, size, total);
        return (items, meta);
    }
}
