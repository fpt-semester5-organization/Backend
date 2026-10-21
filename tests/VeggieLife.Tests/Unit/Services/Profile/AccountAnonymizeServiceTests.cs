using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using VeggieLife.BLL.Common.Constants;
using VeggieLife.BLL.Services.AccountAnonymization;
using VeggieLife.BLL.Services.Cache;
using VeggieLife.DAL.Context;
using VeggieLife.DAL.Entities;
using Xunit;

namespace VeggieLife.Tests.Unit.Services.Profile;

public class AccountAnonymizeServiceTests
{
    private readonly AppDbContext _db;
    private readonly Mock<IRedisService> _mockRedis;
    private readonly AccountAnonymizeService _service;

    public AccountAnonymizeServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: "AccountAnonymizeTestsDb_" + Guid.NewGuid())
            .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        _db = new AppDbContext(options);
        _mockRedis = new Mock<IRedisService>();
        _service = new AccountAnonymizeService(_db, _mockRedis.Object, NullLogger<AccountAnonymizeService>.Instance);
    }

    [Fact]
    public async Task Anonymize_WhenNoSoftDeletedUsers_Returns0()
    {
        // ARRANGE: Người dùng ACTIVE bình thường
        _db.Users.Add(new User
        {
            UserId = 1,
            Email = "active@veggielife.vn",
            FullName = "Nguyen Active",
            Role = UserRoles.Member,
            AccountStatus = AccountStatuses.Active,
            CreatedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();

        // ACT
        var count = await _service.AnonymizeOverdueSoftDeletedAccountsAsync();

        // ASSERT
        count.Should().Be(0);
    }

    [Fact]
    public async Task Anonymize_WhenSoftDeletedLessThan30Days_DoesNotAnonymize()
    {
        // ARRANGE: Người dùng xóa mềm 10 ngày trước (chưa hết hạn 30 ngày)
        var user = new User
        {
            UserId = 2,
            Email = "softdeleted10days@veggielife.vn",
            FullName = "Nguyen SoftDeleted",
            Role = UserRoles.Member,
            AccountStatus = AccountStatuses.SoftDeleted,
            CreatedAt = DateTime.UtcNow.AddDays(-20),
            UpdatedAt = DateTime.UtcNow.AddDays(-10)
        };
        _db.Users.Add(user);

        _db.AuditLogs.Add(new AuditLog
        {
            ActorUserId = user.UserId,
            ActorRole = UserRoles.Member,
            Action = "ACCOUNT_SOFT_DELETED",
            TargetTable = "Users",
            TargetId = user.UserId.ToString(),
            CreatedAt = DateTime.UtcNow.AddDays(-10)
        });

        await _db.SaveChangesAsync();

        // ACT
        var count = await _service.AnonymizeOverdueSoftDeletedAccountsAsync();

        // ASSERT
        count.Should().Be(0);

        var userInDb = await _db.Users.IgnoreQueryFilters().FirstAsync(u => u.UserId == 2);
        userInDb.AccountStatus.Should().Be(AccountStatuses.SoftDeleted);
        userInDb.Email.Should().Be("softdeleted10days@veggielife.vn");
    }

    [Fact]
    public async Task Anonymize_WhenSoftDeletedOver30Days_AnonymizesPersonalDataAndInsertsAuditLog()
    {
        // ARRANGE: Người dùng xóa mềm 35 ngày trước (đã quá hạn 30 ngày)
        var user = new User
        {
            UserId = 3,
            Email = "personal.email@veggielife.vn",
            FullName = "Tran Thi Ca Nhan",
            PhoneNumber = "0912345678",
            AvatarUrl = "https://cdn.veggielife.vn/avatars/3.png",
            PasswordHash = "$2a$11$somethingsecret",
            GoogleId = "google-sub-123",
            Role = UserRoles.Member,
            AccountStatus = AccountStatuses.SoftDeleted,
            CreatedAt = DateTime.UtcNow.AddDays(-60),
            UpdatedAt = DateTime.UtcNow.AddDays(-35)
        };
        _db.Users.Add(user);

        // HealthProfile
        var profile = new HealthProfile
        {
            UserId = 3,
            DietarySchoolId = 1,
            Gender = "FEMALE",
            DateOfBirth = new DateOnly(1995, 1, 1),
            HeightCm = 165m,
            CurrentWeightKg = 55m,
            TargetWeightKg = 50m,
            BmiValue = 20.2m,
            BmrKcal = 1200m,
            TdeeKcal = 1500m,
            TargetDailyCalories = 1400m,
            HealthGoal = "MAINTAIN",
            RegionPreference = "SOUTH",
            CreatedAt = DateTime.UtcNow.AddDays(-60),
            UpdatedAt = DateTime.UtcNow.AddDays(-60)
        };
        _db.HealthProfiles.Add(profile);

        // WeightHistory & Allergies
        _db.UserWeightHistories.Add(new UserWeightHistory
        {
            UserId = 3,
            WeightKg = 55m,
            LoggedDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-40)),
            CreatedAt = DateTime.UtcNow.AddDays(-40)
        });

        _db.UserAllergies.Add(new UserAllergy
        {
            UserId = 3,
            AllergenId = 1,
            Notes = "SEVERE",
            CreatedAt = DateTime.UtcNow.AddDays(-60),
            UpdatedAt = DateTime.UtcNow.AddDays(-60)
        });

        // AuditLog mốc xóa mềm 35 ngày trước
        _db.AuditLogs.Add(new AuditLog
        {
            ActorUserId = user.UserId,
            ActorRole = UserRoles.Member,
            Action = "ACCOUNT_SOFT_DELETED",
            TargetTable = "Users",
            TargetId = user.UserId.ToString(),
            Note = "Tạm ngừng dùng dịch vụ",
            CreatedAt = DateTime.UtcNow.AddDays(-35)
        });

        await _db.SaveChangesAsync();

        // ACT
        var count = await _service.AnonymizeOverdueSoftDeletedAccountsAsync();

        // ASSERT
        count.Should().Be(1);

        var anonymizedUser = await _db.Users.IgnoreQueryFilters().FirstAsync(u => u.UserId == 3);
        anonymizedUser.AccountStatus.Should().Be(AccountStatuses.AnonymizedDeleted);
        anonymizedUser.Email.Should().Be("anonymized_3@deleted.veggielife.vn");
        anonymizedUser.FullName.Should().Be("Người dùng #3");
        anonymizedUser.PhoneNumber.Should().BeNull();
        anonymizedUser.AvatarUrl.Should().BeNull();
        anonymizedUser.PasswordHash.Should().BeNull();
        anonymizedUser.GoogleId.Should().BeNull();

        var anonymizedProfile = await _db.HealthProfiles.FirstAsync(h => h.UserId == 3);
        anonymizedProfile.HeightCm.Should().Be(0);
        anonymizedProfile.CurrentWeightKg.Should().Be(0);
        anonymizedProfile.TargetWeightKg.Should().BeNull();
        anonymizedProfile.BmiValue.Should().Be(0);
        anonymizedProfile.BmrKcal.Should().Be(0);
        anonymizedProfile.TdeeKcal.Should().Be(0);
        anonymizedProfile.TargetDailyCalories.Should().Be(0);

        // Lịch sử cân nặng và dị ứng bị dọn dẹp
        var weights = await _db.UserWeightHistories.Where(w => w.UserId == 3).ToListAsync();
        weights.Should().BeEmpty();

        var allergies = await _db.UserAllergies.Where(a => a.UserId == 3).ToListAsync();
        allergies.Should().BeEmpty();

        // Ghi AuditLog hệ thống
        var audit = await _db.AuditLogs.FirstOrDefaultAsync(a => a.Action == "ACCOUNT_ANONYMIZED" && a.TargetId == "3");
        audit.Should().NotBeNull();
        audit!.ActorRole.Should().Be("SYSTEM");
        audit.ActorUserId.Should().BeNull();
    }

    [Fact]
    public async Task Anonymize_IsIdempotent_RunningTwiceDoesNotReAnonymizeOrFail()
    {
        // ARRANGE: Người dùng xóa mềm 40 ngày trước
        var user = new User
        {
            UserId = 4,
            Email = "user4@veggielife.vn",
            FullName = "User Bon",
            Role = UserRoles.Member,
            AccountStatus = AccountStatuses.SoftDeleted,
            CreatedAt = DateTime.UtcNow.AddDays(-50),
            UpdatedAt = DateTime.UtcNow.AddDays(-40)
        };
        _db.Users.Add(user);
        _db.AuditLogs.Add(new AuditLog
        {
            ActorUserId = user.UserId,
            ActorRole = UserRoles.Member,
            Action = "ACCOUNT_SOFT_DELETED",
            TargetTable = "Users",
            TargetId = user.UserId.ToString(),
            CreatedAt = DateTime.UtcNow.AddDays(-40)
        });
        await _db.SaveChangesAsync();

        // ACT: Lần chạy thứ nhất
        var firstRunCount = await _service.AnonymizeOverdueSoftDeletedAccountsAsync();
        firstRunCount.Should().Be(1);

        // ACT: Lần chạy thứ hai ngay sau đó (kiểm tra Idempotent - "chạy lại không gây hại")
        var secondRunCount = await _service.AnonymizeOverdueSoftDeletedAccountsAsync();

        // ASSERT
        secondRunCount.Should().Be(0);
    }
}
