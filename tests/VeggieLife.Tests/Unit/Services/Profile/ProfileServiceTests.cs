using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using VeggieLife.BLL.Common.Constants;
using VeggieLife.BLL.Common.Exceptions;
using VeggieLife.BLL.DTOs.Media.Responses;
using VeggieLife.BLL.DTOs.Profile.Requests;
using VeggieLife.BLL.DTOs.Profile.Responses;
using VeggieLife.BLL.Services.Cache;
using VeggieLife.BLL.Services.Media;
using VeggieLife.BLL.Services.Profile;
using VeggieLife.DAL.Context;
using VeggieLife.DAL.Entities;
using Xunit;

namespace VeggieLife.Tests.Unit.Services.Profile;

/// <summary>
/// Bộ kiểm thử toàn diện cho ProfileService: Hồ sơ sức khỏe, Giới hạn khoảng giá trị, Khai báo dị ứng, Thông tin cá nhân.
/// </summary>
public class ProfileServiceTests : IDisposable
{
    private readonly AppDbContext _db;
    private readonly Mock<IRedisService> _mockRedis;
    private readonly Mock<ILogger<ProfileService>> _mockLogger;
    private readonly Mock<IMediaService> _mockMediaService;
    private readonly ProfileService _service;

    public ProfileServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        _db = new AppDbContext(options);
        _mockRedis = new Mock<IRedisService>();
        _mockLogger = new Mock<ILogger<ProfileService>>();
        _mockMediaService = new Mock<IMediaService>();

        _service = new ProfileService(_db, _mockRedis.Object, _mockMediaService.Object, _mockLogger.Object);

        SeedTestData();
    }

    public void Dispose()
    {
        _db.Database.EnsureDeleted();
        _db.Dispose();
    }

    private void SeedTestData()
    {
        // 1. Seed DietarySchools
        _db.DietarySchools.Add(new DietarySchool
        {
            DietarySchoolId = 1,
            SchoolCode = "VEGAN",
            SchoolName = "Thuần chay",
            ForbiddenFoodRules = "{\"forbiddenFoodGroups\":[\"EGG\",\"DAIRY\",\"POULTRY\",\"SEAFOOD\"],\"forbidAllium\":false}"
        });
        _db.DietarySchools.Add(new DietarySchool
        {
            DietarySchoolId = 5,
            SchoolCode = "BUDDHIST",
            SchoolName = "Chay Phật giáo",
            ForbiddenFoodRules = "{\"forbiddenFoodGroups\":[\"EGG\",\"DAIRY\",\"POULTRY\",\"SEAFOOD\"],\"forbidAllium\":true}"
        });

        // 2. Seed Allergens
        _db.Allergens.Add(new Allergen { AllergenId = 1, Code = "SOY", AllergenName = "Đậu nành" });
        _db.Allergens.Add(new Allergen { AllergenId = 2, Code = "GLUTEN", AllergenName = "Gluten" });
        _db.Allergens.Add(new Allergen { AllergenId = 3, Code = "PEANUT", AllergenName = "Đậu phộng" });

        // 3. Seed Users
        _db.Users.Add(new User
        {
            UserId = 101,
            Email = "member@veggielife.vn",
            FullName = "Nguyễn Văn Member",
            PhoneNumber = "0912345678",
            Role = UserRoles.Member,
            AccountStatus = "ACTIVE",
            CreatedAt = DateTime.UtcNow
        });

        _db.SaveChanges();
    }

    [Fact]
    public async Task UpdateHealthProfileAsync_ValidData_ShouldSucceedAndCalculateCorrectly()
    {
        var request = new UpdateCurrentUserHealthProfileRequest
        {
            HeightCm = 162m,
            WeightKg = 54.5m,
            DateOfBirth = new DateOnly(2003, 4, 12),
            Gender = "FEMALE",
            DietarySchoolId = 1,
            HealthGoal = "LOSE_WEIGHT",
            TargetWeightKg = 52m,
            RequiresAlliumFree = true,
            RegionPreference = "SOUTH"
        };

        var result = await _service.UpdateHealthProfileAsync(101, request);

        result.Should().NotBeNull();
        result.Gender.Should().Be("FEMALE");
        result.HeightCm.Should().Be(162m);
        result.CurrentWeightKg.Should().Be(54.5m);
        result.TargetWeightKg.Should().Be(52m);
        result.Bmi.Should().Be(20.8m);
        result.BmiCategory.Should().Be("NORMAL");
        result.RequiresAlliumFree.Should().BeTrue();
        result.DietarySchool.Code.Should().Be("VEGAN");

        // Kiểm tra CSDL lưu đúng HealthProfiles
        var savedProfile = await _db.HealthProfiles.FirstOrDefaultAsync(hp => hp.UserId == 101);
        savedProfile.Should().NotBeNull();
        savedProfile!.BmiValue.Should().Be(20.8m);

        // Kiểm tra CSDL ghi lịch sử cân nặng UserWeightHistories
        var weightLog = await _db.UserWeightHistories.FirstOrDefaultAsync(w => w.UserId == 101);
        weightLog.Should().NotBeNull();
        weightLog!.WeightKg.Should().Be(54.5m);

        // Kiểm tra xóa cache
        _mockRedis.Verify(r => r.RemoveAsync("cache:profile:101", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData(49)]
    [InlineData(251)]
    public async Task UpdateHealthProfileAsync_HeightOutOfBounds_ShouldThrowBadRequest(decimal height)
    {
        var request = new UpdateCurrentUserHealthProfileRequest
        {
            HeightCm = height,
            WeightKg = 55m,
            DateOfBirth = new DateOnly(2000, 1, 1),
            Gender = "MALE",
            DietarySchoolId = 1,
            HealthGoal = "MAINTAIN"
        };

        var act = async () => await _service.UpdateHealthProfileAsync(101, request);

        var ex = await act.Should().ThrowAsync<AppException>();
        ex.Which.StatusCode.Should().Be(400);
        ex.Which.ErrorCode.Should().Be(ErrorCodes.ErrValidation);
    }

    [Theory]
    [InlineData(19.9)]
    [InlineData(300.1)]
    public async Task UpdateHealthProfileAsync_WeightOutOfBounds_ShouldThrowBadRequest(decimal weight)
    {
        var request = new UpdateCurrentUserHealthProfileRequest
        {
            HeightCm = 170m,
            WeightKg = weight,
            DateOfBirth = new DateOnly(2000, 1, 1),
            Gender = "MALE",
            DietarySchoolId = 1,
            HealthGoal = "MAINTAIN"
        };

        var act = async () => await _service.UpdateHealthProfileAsync(101, request);

        var ex = await act.Should().ThrowAsync<AppException>();
        ex.Which.StatusCode.Should().Be(400);
        ex.Which.ErrorCode.Should().Be(ErrorCodes.ErrValidation);
    }

    [Theory]
    [InlineData(19.9)]
    [InlineData(301)]
    public async Task UpdateHealthProfileAsync_TargetWeightOutOfBounds_ShouldThrowBadRequest(decimal targetWeight)
    {
        var request = new UpdateCurrentUserHealthProfileRequest
        {
            HeightCm = 170m,
            WeightKg = 60m,
            TargetWeightKg = targetWeight,
            DateOfBirth = new DateOnly(2000, 1, 1),
            Gender = "MALE",
            DietarySchoolId = 1,
            HealthGoal = "MAINTAIN"
        };

        var act = async () => await _service.UpdateHealthProfileAsync(101, request);

        var ex = await act.Should().ThrowAsync<AppException>();
        ex.Which.StatusCode.Should().Be(400);
        ex.Which.ErrorCode.Should().Be(ErrorCodes.ErrValidation);
    }

    [Fact]
    public async Task UpdateHealthProfileAsync_AgeUnder10_ShouldThrowBadRequest()
    {
        var request = new UpdateCurrentUserHealthProfileRequest
        {
            HeightCm = 140m,
            WeightKg = 35m,
            DateOfBirth = DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-9)),
            Gender = "FEMALE",
            DietarySchoolId = 1,
            HealthGoal = "MAINTAIN"
        };

        var act = async () => await _service.UpdateHealthProfileAsync(101, request);

        var ex = await act.Should().ThrowAsync<AppException>();
        ex.Which.StatusCode.Should().Be(400);
        ex.Which.ErrorCode.Should().Be(ErrorCodes.ErrValidation);
    }

    [Fact]
    public async Task UpdateHealthProfileAsync_FutureDob_ShouldThrowBadRequest()
    {
        var request = new UpdateCurrentUserHealthProfileRequest
        {
            HeightCm = 160m,
            WeightKg = 50m,
            DateOfBirth = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1)),
            Gender = "FEMALE",
            DietarySchoolId = 1,
            HealthGoal = "MAINTAIN"
        };

        var act = async () => await _service.UpdateHealthProfileAsync(101, request);

        var ex = await act.Should().ThrowAsync<AppException>();
        ex.Which.StatusCode.Should().Be(400);
        ex.Which.ErrorCode.Should().Be(ErrorCodes.ErrValidation);
    }

    [Fact]
    public async Task UpdateHealthProfileAsync_InvalidGender_ShouldThrowBadRequest()
    {
        var request = new UpdateCurrentUserHealthProfileRequest
        {
            HeightCm = 165m,
            WeightKg = 60m,
            DateOfBirth = new DateOnly(2000, 1, 1),
            Gender = "OTHER",
            DietarySchoolId = 1,
            HealthGoal = "MAINTAIN"
        };

        var act = async () => await _service.UpdateHealthProfileAsync(101, request);

        var ex = await act.Should().ThrowAsync<AppException>();
        ex.Which.StatusCode.Should().Be(400);
        ex.Which.ErrorCode.Should().Be(ErrorCodes.ErrValidation);
    }

    [Fact]
    public async Task UpdateHealthProfileAsync_InvalidHealthGoal_ShouldThrowBadRequest()
    {
        var request = new UpdateCurrentUserHealthProfileRequest
        {
            HeightCm = 165m,
            WeightKg = 60m,
            DateOfBirth = new DateOnly(2000, 1, 1),
            Gender = "MALE",
            DietarySchoolId = 1,
            HealthGoal = "BULK_UP"
        };

        var act = async () => await _service.UpdateHealthProfileAsync(101, request);

        var ex = await act.Should().ThrowAsync<AppException>();
        ex.Which.StatusCode.Should().Be(400);
        ex.Which.ErrorCode.Should().Be(ErrorCodes.ErrValidation);
    }

    [Fact]
    public async Task UpdateHealthProfileAsync_DietarySchoolNotFound_ShouldThrow404()
    {
        var request = new UpdateCurrentUserHealthProfileRequest
        {
            HeightCm = 165m,
            WeightKg = 60m,
            DateOfBirth = new DateOnly(2000, 1, 1),
            Gender = "MALE",
            DietarySchoolId = 999, // Không tồn tại
            HealthGoal = "MAINTAIN"
        };

        var act = async () => await _service.UpdateHealthProfileAsync(101, request);

        var ex = await act.Should().ThrowAsync<AppException>();
        ex.Which.StatusCode.Should().Be(404);
        ex.Which.ErrorCode.Should().Be(ErrorCodes.ErrDietSchoolNotFound);
    }

    [Fact]
    public async Task ReplaceAllergiesAsync_ValidData_ShouldSucceed()
    {
        var request = new ReplaceCurrentUserAllergiesRequest
        {
            Allergens = new List<UserAllergyItemRequest>
            {
                new() { AllergenId = 1, Notes = "Phản ứng nhẹ" },
                new() { AllergenId = 3, Notes = "Phản vệ nặng" }
            }
        };

        var result = await _service.ReplaceAllergiesAsync(101, request);

        result.Should().NotBeNull();
        result.Allergens.Should().HaveCount(2);
        result.Allergens.Select(a => a.AllergenId).Should().Contain(new[] { 1, 3 });

        var inDb = await _db.UserAllergies.Where(ua => ua.UserId == 101).ToListAsync();
        inDb.Should().HaveCount(2);

        _mockRedis.Verify(r => r.RemoveAsync("cache:profile:101", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ReplaceAllergiesAsync_Exceed20Items_ShouldThrowBadRequest()
    {
        var list = Enumerable.Range(1, 21).Select(i => new UserAllergyItemRequest { AllergenId = 1 }).ToList();
        var request = new ReplaceCurrentUserAllergiesRequest { Allergens = list };

        var act = async () => await _service.ReplaceAllergiesAsync(101, request);

        var ex = await act.Should().ThrowAsync<AppException>();
        ex.Which.StatusCode.Should().Be(400);
        ex.Which.ErrorCode.Should().Be(ErrorCodes.ErrValidation);
    }

    [Fact]
    public async Task ReplaceAllergiesAsync_NonExistentAllergen_ShouldThrow404()
    {
        var request = new ReplaceCurrentUserAllergiesRequest
        {
            Allergens = new List<UserAllergyItemRequest>
            {
                new() { AllergenId = 999 } // Không tồn tại
            }
        };

        var act = async () => await _service.ReplaceAllergiesAsync(101, request);

        var ex = await act.Should().ThrowAsync<AppException>();
        ex.Which.StatusCode.Should().Be(404);
        ex.Which.ErrorCode.Should().Be(ErrorCodes.ErrAllergenNotFound);
    }

    [Fact]
    public async Task GetCurrentUserAsync_ShouldReturnProfileDataAndDetectStaleMetrics()
    {
        // Thêm hồ sơ đã cập nhật cách đây 35 ngày
        _db.HealthProfiles.Add(new HealthProfile
        {
            UserId = 101,
            DietarySchoolId = 1,
            Gender = "MALE",
            DateOfBirth = new DateOnly(1995, 1, 1),
            HeightCm = 175m,
            CurrentWeightKg = 70m,
            BmiValue = 22.9m,
            BmrKcal = 1648.8m,
            TdeeKcal = 1978.6m,
            TargetDailyCalories = 1978.6m,
            HealthGoal = "MAINTAIN",
            RequiresAlliumFree = false,
            RegionPreference = "SOUTH",
            IsStaleMetrics = false,
            CreatedAt = DateTime.UtcNow.AddDays(-35),
            UpdatedAt = DateTime.UtcNow.AddDays(-35)
        });
        await _db.SaveChangesAsync();

        var result = await _service.GetCurrentUserAsync(101);

        result.Should().NotBeNull();
        result.UserId.Should().Be(101);
        result.Health.Should().NotBeNull();
        result.Health!.Bmi.Should().Be(22.9m);
        result.Health.BmiCategory.Should().Be("NORMAL");

        // Đã qua 30 ngày -> Phải đánh dấu IsStaleMetrics = true trong CSDL (BR-USR-02)
        var updatedProfile = await _db.HealthProfiles.FirstAsync(hp => hp.UserId == 101);
        updatedProfile.IsStaleMetrics.Should().BeTrue();
    }

    [Fact]
    public async Task UpdateCurrentUserAsync_ValidNameAndPhone_ShouldUpdate()
    {
        var request = new UpdateCurrentUserRequest
        {
            FullName = "Nguyễn Văn Cập Nhật",
            PhoneNumber = "0987654321"
        };

        var result = await _service.UpdateCurrentUserAsync(101, request);

        result.FullName.Should().Be("Nguyễn Văn Cập Nhật");
        result.PhoneNumber.Should().Be("0987654321");

        var userInDb = await _db.Users.FirstAsync(u => u.UserId == 101);
        userInDb.FullName.Should().Be("Nguyễn Văn Cập Nhật");
        userInDb.PhoneNumber.Should().Be("0987654321");
    }

    [Theory]
    [InlineData("A")] // < 2 ký tự
    [InlineData("01234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567891")] // > 100 ký tự
    public async Task UpdateCurrentUserAsync_InvalidNameLength_ShouldThrowBadRequest(string name)
    {
        var request = new UpdateCurrentUserRequest { FullName = name };

        var act = async () => await _service.UpdateCurrentUserAsync(101, request);

        var ex = await act.Should().ThrowAsync<AppException>();
        ex.Which.StatusCode.Should().Be(400);
        ex.Which.ErrorCode.Should().Be(ErrorCodes.ErrValidation);
    }

    [Theory]
    [InlineData("1234567890")]
    [InlineData("091234567")]
    [InlineData("091234567890")]
    [InlineData("0212345678")]
    public async Task UpdateCurrentUserAsync_InvalidPhoneNumber_ShouldThrowBadRequest(string phone)
    {
        var request = new UpdateCurrentUserRequest { PhoneNumber = phone };

        var act = async () => await _service.UpdateCurrentUserAsync(101, request);

        var ex = await act.Should().ThrowAsync<AppException>();
        ex.Which.StatusCode.Should().Be(400);
        ex.Which.ErrorCode.Should().Be(ErrorCodes.ErrValidation);
    }

    #region FN-PROF-03: Upload Avatar Tests

    [Fact]
    public async Task UploadAvatarAsync_WhenValid_ShouldUpdateAvatarUrlAndInvalidateCache()
    {
        var publicId = "veggielife/avatars/user101_avatar";
        var transformedUrl = "https://res.cloudinary.com/veggielife/avatars/user101_avatar_w500.webp";

        _mockMediaService
            .Setup(m => m.UploadImageAsync(It.IsAny<Stream>(), It.IsAny<string>(), "veggielife/avatars", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MediaUploadResponse
            {
                PublicId = publicId,
                Url = "http://res.cloudinary.com/veggielife/avatars/user101_avatar.jpg",
                SecureUrl = "https://res.cloudinary.com/veggielife/avatars/user101_avatar.jpg",
                Format = "jpg",
                Bytes = 1024
            });

        _mockMediaService
            .Setup(m => m.GetImageUrl(publicId, 500, 500, "fill", "webp"))
            .Returns(transformedUrl);

        using var stream = new MemoryStream(new byte[] { 1, 2, 3 });
        var result = await _service.UploadAvatarAsync(101, stream, "my_avatar.png");

        result.Should().NotBeNull();
        result.AvatarUrl.Should().Be(transformedUrl);

        var userInDb = await _db.Users.FirstAsync(u => u.UserId == 101);
        userInDb.AvatarUrl.Should().Be(transformedUrl);

        _mockRedis.Verify(r => r.RemoveAsync("cache:profile:101", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UploadAvatarAsync_WhenUserNotFound_ShouldThrowUnauthorized()
    {
        using var stream = new MemoryStream(new byte[] { 1, 2, 3 });
        var act = async () => await _service.UploadAvatarAsync(9999, stream, "my_avatar.png");

        var ex = await act.Should().ThrowAsync<AppException>();
        ex.Which.StatusCode.Should().Be(401);
        ex.Which.ErrorCode.Should().Be(ErrorCodes.ErrAuthInvalidToken);
    }

    #endregion

    #region FN-PROF-06: Weight History Tests

    [Fact]
    public async Task CreateWeightEntryAsync_WhenValid_ShouldInsertLogAndUpdateHealthProfile()
    {
        // Chuẩn bị HealthProfile cho User 101
        _db.HealthProfiles.Add(new HealthProfile
        {
            UserId = 101,
            DietarySchoolId = 1,
            Gender = "FEMALE",
            DateOfBirth = new DateOnly(2000, 1, 1),
            HeightCm = 165m,
            CurrentWeightKg = 58m,
            TargetWeightKg = 52m,
            BmiValue = 21.3m,
            BmrKcal = 1350m,
            TdeeKcal = 1620m,
            TargetDailyCalories = 1220m,
            HealthGoal = "LOSE_WEIGHT",
            RegionPreference = "SOUTH",
            IsStaleMetrics = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();

        var request = new CreateCurrentUserWeightEntryRequest
        {
            WeightKg = 54.0m,
            LoggedDate = new DateOnly(2026, 10, 5),
            Note = "Cân buổi sáng"
        };

        var result = await _service.CreateWeightEntryAsync(101, request);

        result.Should().NotBeNull();
        result.WeightKg.Should().Be(54.0m);
        result.Bmi.Should().BeGreaterThan(0);
        result.TargetDailyCalories.Should().BeGreaterThan(0);

        var profile = await _db.HealthProfiles.FirstAsync(hp => hp.UserId == 101);
        profile.CurrentWeightKg.Should().Be(54.0m);
        profile.IsStaleMetrics.Should().BeFalse();

        var historyLog = await _db.UserWeightHistories
            .FirstOrDefaultAsync(w => w.UserId == 101 && w.WeightKg == 54.0m);
        historyLog.Should().NotBeNull();
        historyLog!.LoggedDate.Should().Be(new DateOnly(2026, 10, 5));
        historyLog.Note.Should().Be("Cân buổi sáng");

        _mockRedis.Verify(r => r.RemoveAsync("cache:profile:101", It.IsAny<CancellationToken>()), Times.Once);
        _mockRedis.Verify(r => r.RemoveAsync("meal_plan_cache:101", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateWeightEntryAsync_WhenHealthProfileNotExists_ShouldThrowConflict409()
    {
        // Tạo user mới chưa có HealthProfile
        var userWithoutProfile = new User
        {
            UserId = 202,
            Email = "noprofile@veggielife.vn",
            PasswordHash = "hash",
            Role = UserRoles.Member,
            FullName = "Chưa có Profile",
            AccountStatus = "ACTIVE",
            CreatedAt = DateTime.UtcNow
        };
        _db.Users.Add(userWithoutProfile);
        await _db.SaveChangesAsync();

        var request = new CreateCurrentUserWeightEntryRequest
        {
            WeightKg = 60.0m
        };

        var act = async () => await _service.CreateWeightEntryAsync(202, request);

        var ex = await act.Should().ThrowAsync<AppException>();
        ex.Which.StatusCode.Should().Be(409);
        ex.Which.ErrorCode.Should().Be(ErrorCodes.ErrProfileNotCompleted);
    }

    [Theory]
    [InlineData(19.9)]  // < 20 kg
    [InlineData(300.1)] // > 300 kg
    public async Task CreateWeightEntryAsync_WhenWeightOutOfRange_ShouldThrowBadRequest(decimal weight)
    {
        var request = new CreateCurrentUserWeightEntryRequest
        {
            WeightKg = weight
        };

        var act = async () => await _service.CreateWeightEntryAsync(101, request);

        var ex = await act.Should().ThrowAsync<AppException>();
        ex.Which.StatusCode.Should().Be(400);
        ex.Which.ErrorCode.Should().Be(ErrorCodes.ErrValidation);
    }

    [Fact]
    public async Task CreateWeightEntryAsync_WhenLoggedDateInFuture_ShouldThrowBadRequest()
    {
        var tomorrow = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1));
        var request = new CreateCurrentUserWeightEntryRequest
        {
            WeightKg = 60.0m,
            LoggedDate = tomorrow
        };

        var act = async () => await _service.CreateWeightEntryAsync(101, request);

        var ex = await act.Should().ThrowAsync<AppException>();
        ex.Which.StatusCode.Should().Be(400);
        ex.Which.ErrorCode.Should().Be(ErrorCodes.ErrValidation);
    }

    [Fact]
    public async Task ListWeightHistoryAsync_WhenValid_ShouldReturnPagedOrderedList()
    {
        // Thêm các mốc cân nặng
        _db.UserWeightHistories.AddRange(
            new UserWeightHistory { UserId = 101, WeightKg = 50.0m, LoggedDate = new DateOnly(2026, 10, 1), CreatedAt = DateTime.UtcNow },
            new UserWeightHistory { UserId = 101, WeightKg = 52.0m, LoggedDate = new DateOnly(2026, 10, 3), CreatedAt = DateTime.UtcNow },
            new UserWeightHistory { UserId = 101, WeightKg = 54.0m, LoggedDate = new DateOnly(2026, 10, 5), CreatedAt = DateTime.UtcNow }
        );
        await _db.SaveChangesAsync();

        var (items, meta) = await _service.ListWeightHistoryAsync(101, from: null, to: null, page: 1, size: 2);

        meta.Total.Should().Be(3);
        meta.Page.Should().Be(1);
        meta.Size.Should().Be(2);
        items.Should().HaveCount(2);
        // Sắp xếp mới nhất trước
        items[0].LoggedDate.Should().Be(new DateOnly(2026, 10, 5));
        items[1].LoggedDate.Should().Be(new DateOnly(2026, 10, 3));
    }

    [Fact]
    public async Task ListWeightHistoryAsync_WithFromToFilter_ShouldFilterCorrectly()
    {
        _db.UserWeightHistories.AddRange(
            new UserWeightHistory { UserId = 101, WeightKg = 50.0m, LoggedDate = new DateOnly(2026, 9, 15), CreatedAt = DateTime.UtcNow },
            new UserWeightHistory { UserId = 101, WeightKg = 52.0m, LoggedDate = new DateOnly(2026, 10, 1), CreatedAt = DateTime.UtcNow },
            new UserWeightHistory { UserId = 101, WeightKg = 54.0m, LoggedDate = new DateOnly(2026, 10, 15), CreatedAt = DateTime.UtcNow }
        );
        await _db.SaveChangesAsync();

        var (items, meta) = await _service.ListWeightHistoryAsync(
            101,
            from: new DateOnly(2026, 10, 1),
            to: new DateOnly(2026, 10, 10),
            page: 1,
            size: 10);

        items.Should().HaveCount(1);
        items[0].LoggedDate.Should().Be(new DateOnly(2026, 10, 1));
        meta.Total.Should().Be(1);
    }

    [Fact]
    public async Task ListWeightHistoryAsync_WhenFromGreaterThanTo_ShouldThrowBadRequest()
    {
        var act = async () => await _service.ListWeightHistoryAsync(
            101,
            from: new DateOnly(2026, 10, 15),
            to: new DateOnly(2026, 10, 1),
            page: 1,
            size: 10);

        var ex = await act.Should().ThrowAsync<AppException>();
        ex.Which.StatusCode.Should().Be(400);
        ex.Which.ErrorCode.Should().Be(ErrorCodes.ErrValidation);
    }

    #endregion
}
