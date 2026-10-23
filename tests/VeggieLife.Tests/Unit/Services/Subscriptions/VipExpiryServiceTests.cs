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
using VeggieLife.BLL.Services.Cache;
using VeggieLife.BLL.Services.Subscriptions;
using VeggieLife.DAL.Context;
using VeggieLife.DAL.Entities;
using VeggieLife.Tests.Helpers;
using Xunit;

namespace VeggieLife.Tests.Unit.Services.Subscriptions;

/// <summary>
/// Unit Tests cho VipExpiryService (FN-SUB-04, BR-PAY-SUB-02).
/// Kiểm thử với đồng hồ giả (FakeTimeProvider): VIP hết hạn bị hạ; giữ nguyên dữ liệu dinh dưỡng; chạy lại 2 lần không đổi kết quả.
/// </summary>
public class VipExpiryServiceTests
{
    private readonly AppDbContext _db;
    private readonly Mock<IRedisService> _mockRedis;
    private readonly FakeTimeProvider _fakeClock;
    private readonly VipExpiryService _service;

    public VipExpiryServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: "VipExpiryTestsDb_" + Guid.NewGuid())
            .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        _db = new AppDbContext(options);
        _mockRedis = new Mock<IRedisService>();
        _fakeClock = new FakeTimeProvider(new DateTimeOffset(2026, 10, 24, 23, 50, 0, TimeSpan.Zero));
        _service = new VipExpiryService(_db, _mockRedis.Object, NullLogger<VipExpiryService>.Instance, _fakeClock);
    }

    [Fact]
    public async Task Downgrade_WhenNoVipUsers_Returns0()
    {
        // ARRANGE: Chỉ có người dùng MEMBER
        _db.Users.Add(new User
        {
            UserId = 1,
            Email = "member@veggielife.vn",
            FullName = "Nguyen Member",
            Role = UserRoles.Member,
            AccountStatus = AccountStatuses.Active,
            CreatedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();

        // ACT
        var count = await _service.DowngradeExpiredVipUsersAsync();

        // ASSERT
        count.Should().Be(0);
    }

    [Fact]
    public async Task Downgrade_WhenVipStillActive_DoesNotDowngrade()
    {
        // ARRANGE: Người dùng VIP còn hạn đến 2026-11-24 (hiện tại là 2026-10-24 23:50:00)
        var user = new User
        {
            UserId = 2,
            Email = "vip_active@veggielife.vn",
            FullName = "Tran VIP Active",
            Role = UserRoles.Vip,
            AccountStatus = AccountStatuses.Active,
            CreatedAt = new DateTime(2026, 10, 1, 0, 0, 0, DateTimeKind.Utc)
        };
        _db.Users.Add(user);

        _db.PaymentTransactions.Add(new PaymentTransaction
        {
            TransactionId = 201,
            UserId = 2,
            PlanId = 1,
            GatewayProvider = "MOMO",
            PartnerTransactionCode = "VL_20261001_001",
            AmountVnd = 49000,
            TransactionStatus = "SUCCESS",
            SignatureHash = "sig_hash_201",
            StartsAt = new DateTime(2026, 10, 1, 0, 0, 0, DateTimeKind.Utc),
            EndsAt = new DateTime(2026, 11, 24, 23, 59, 59, DateTimeKind.Utc),
            CreatedAt = new DateTime(2026, 10, 1, 0, 0, 0, DateTimeKind.Utc)
        });
        await _db.SaveChangesAsync();

        // ACT
        var count = await _service.DowngradeExpiredVipUsersAsync();

        // ASSERT
        count.Should().Be(0);

        var userInDb = await _db.Users.FirstAsync(u => u.UserId == 2);
        userInDb.Role.Should().Be(UserRoles.Vip);
    }

    [Fact]
    public async Task Downgrade_WhenVipExpired_DowngradesToMemberAndPreservesNutritionData()
    {
        // ARRANGE: Người dùng VIP hết hạn lúc 2026-10-24 23:59:59
        // Đồng hồ giả tua tới 2026-10-25 00:00:05 (đã qua nửa đêm)
        _fakeClock.SetUtcNow(new DateTimeOffset(2026, 10, 25, 0, 0, 5, TimeSpan.Zero));

        var user = new User
        {
            UserId = 3,
            Email = "vip_expired@veggielife.vn",
            FullName = "Le VIP Expired",
            Role = UserRoles.Vip,
            AccountStatus = AccountStatuses.Active,
            CreatedAt = new DateTime(2026, 9, 24, 0, 0, 0, DateTimeKind.Utc),
            HealthProfile = new HealthProfile
            {
                Gender = "FEMALE",
                DateOfBirth = new DateOnly(1998, 5, 15),
                HeightCm = 165,
                CurrentWeightKg = 55,
                TargetWeightKg = 52,
                BmiValue = 20.2m,
                BmrKcal = 1350,
                TdeeKcal = 1800,
                TargetDailyCalories = 1700,
                HealthGoal = "MAINTAIN_WEIGHT",
                DietarySchoolId = 1,
                RegionPreference = "SOUTH"
            }
        };
        _db.Users.Add(user);

        // Lịch sử cân nặng
        _db.UserWeightHistories.Add(new UserWeightHistory
        {
            UserId = 3,
            WeightKg = 55,
            LoggedDate = new DateOnly(2026, 10, 20),
            CreatedAt = new DateTime(2026, 10, 20, 8, 0, 0, DateTimeKind.Utc)
        });

        // Dị ứng
        _db.UserAllergies.Add(new UserAllergy
        {
            UserId = 3,
            AllergenId = 1
        });

        // Thực đơn tuần
        _db.MealPlans.Add(new MealPlan
        {
            PlanId = 301,
            UserId = 3,
            DietarySchoolId = 1,
            PlanName = "Thực đơn mẫu tuần",
            StartDate = new DateOnly(2026, 10, 20),
            EndDate = new DateOnly(2026, 10, 27),
            TargetDailyKcal = 1700,
            BudgetTier = "MEDIUM",
            PlanStatus = "ACTIVE",
            GeneratedByAi = true,
            CreatedAt = new DateTime(2026, 10, 20, 0, 0, 0, DateTimeKind.Utc)
        });

        // Giao dịch VIP đã hết hạn lúc 2026-10-24 23:59:59
        _db.PaymentTransactions.Add(new PaymentTransaction
        {
            TransactionId = 301,
            UserId = 3,
            PlanId = 1,
            GatewayProvider = "VIETQR",
            PartnerTransactionCode = "VL_20260924_001",
            AmountVnd = 49000,
            TransactionStatus = "SUCCESS",
            SignatureHash = "sig_hash_301",
            StartsAt = new DateTime(2026, 9, 25, 0, 0, 0, DateTimeKind.Utc),
            EndsAt = new DateTime(2026, 10, 24, 23, 59, 59, DateTimeKind.Utc),
            CreatedAt = new DateTime(2026, 9, 24, 20, 0, 0, DateTimeKind.Utc)
        });
        await _db.SaveChangesAsync();

        // ACT
        var count = await _service.DowngradeExpiredVipUsersAsync();

        // ASSERT
        count.Should().Be(1);

        var userInDb = await _db.Users
            .Include(u => u.HealthProfile)
            .Include(u => u.UserWeightHistories)
            .Include(u => u.UserAllergies)
            .Include(u => u.MealPlans)
            .FirstAsync(u => u.UserId == 3);

        // 1. Vai trò chuyển về MEMBER
        userInDb.Role.Should().Be(UserRoles.Member);
        userInDb.UpdatedAt.Should().NotBeNull();

        // 2. DỮ LIỆU DINH DƯỠNG ĐƯỢC GIỮ NGUYÊN 100% (FN-SUB-04, BR-PAY-SUB-02)
        userInDb.HealthProfile.Should().NotBeNull();
        userInDb.HealthProfile!.HeightCm.Should().Be(165);
        userInDb.HealthProfile.CurrentWeightKg.Should().Be(55);
        userInDb.HealthProfile.TargetWeightKg.Should().Be(52);
        userInDb.HealthProfile.BmiValue.Should().Be(20.2m);
        userInDb.HealthProfile.TargetDailyCalories.Should().Be(1700);

        userInDb.UserWeightHistories.Should().HaveCount(1);
        userInDb.UserWeightHistories.First().WeightKg.Should().Be(55);

        userInDb.UserAllergies.Should().HaveCount(1);
        userInDb.MealPlans.Should().HaveCount(1);

        // 3. Xóa cache Redis
        _mockRedis.Verify(r => r.RemoveAsync("vip_status:3", It.IsAny<CancellationToken>()), Times.Once);

        // 4. Ghi AuditLog hệ thống
        var audit = await _db.AuditLogs.FirstOrDefaultAsync(a => a.TargetTable == "Users" && a.TargetId == "3" && a.Action == "VIP_EXPIRED");
        audit.Should().NotBeNull();
        audit!.ActorRole.Should().Be("SYSTEM");
        audit.ActorUserId.Should().BeNull();
    }

    [Fact]
    public async Task Downgrade_WhenUserHasMultipleTransactions_OneExpiredOneActive_DoesNotDowngrade()
    {
        // ARRANGE: Người dùng có 2 giao dịch: 1 cũ đã hết hạn, 1 mới mua gia hạn còn hạn
        _fakeClock.SetUtcNow(new DateTimeOffset(2026, 10, 25, 0, 0, 5, TimeSpan.Zero));

        var user = new User
        {
            UserId = 4,
            Email = "multi_tx@veggielife.vn",
            FullName = "Pham Multi Tx",
            Role = UserRoles.Vip,
            AccountStatus = AccountStatuses.Active,
            CreatedAt = new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc)
        };
        _db.Users.Add(user);

        _db.PaymentTransactions.AddRange(
            // Giao dịch 1: Hết hạn ngày 2026-10-24 23:59:59
            new PaymentTransaction
            {
                TransactionId = 401,
                UserId = 4,
                PlanId = 1,
                GatewayProvider = "MOMO",
                PartnerTransactionCode = "VL_20260924_401",
                AmountVnd = 49000,
                TransactionStatus = "SUCCESS",
                SignatureHash = "sig_hash_401",
                StartsAt = new DateTime(2026, 9, 25, 0, 0, 0, DateTimeKind.Utc),
                EndsAt = new DateTime(2026, 10, 24, 23, 59, 59, DateTimeKind.Utc),
                CreatedAt = new DateTime(2026, 9, 24, 10, 0, 0, DateTimeKind.Utc)
            },
            // Giao dịch 2 (Gia hạn nối tiếp): Còn hạn tới 2026-11-23 23:59:59
            new PaymentTransaction
            {
                TransactionId = 402,
                UserId = 4,
                PlanId = 1,
                GatewayProvider = "VIETQR",
                PartnerTransactionCode = "VL_20261024_402",
                AmountVnd = 49000,
                TransactionStatus = "SUCCESS",
                SignatureHash = "sig_hash_402",
                StartsAt = new DateTime(2026, 10, 24, 23, 59, 59, DateTimeKind.Utc),
                EndsAt = new DateTime(2026, 11, 23, 23, 59, 59, DateTimeKind.Utc),
                CreatedAt = new DateTime(2026, 10, 24, 15, 0, 0, DateTimeKind.Utc)
            }
        );
        await _db.SaveChangesAsync();

        // ACT
        var count = await _service.DowngradeExpiredVipUsersAsync();

        // ASSERT: Còn giao dịch 402 hiệu lực nên không bị hạ cấp
        count.Should().Be(0);

        var userInDb = await _db.Users.FirstAsync(u => u.UserId == 4);
        userInDb.Role.Should().Be(UserRoles.Vip);
    }

    [Fact]
    public async Task Downgrade_IsIdempotent_RunningTwiceProducesSameResultAndNoDuplicateAuditLogs()
    {
        // ARRANGE: Người dùng VIP hết hạn
        _fakeClock.SetUtcNow(new DateTimeOffset(2026, 10, 25, 0, 0, 5, TimeSpan.Zero));

        var user = new User
        {
            UserId = 5,
            Email = "idempotent@veggielife.vn",
            FullName = "Hoang Idempotent",
            Role = UserRoles.Vip,
            AccountStatus = AccountStatuses.Active,
            CreatedAt = new DateTime(2026, 9, 20, 0, 0, 0, DateTimeKind.Utc)
        };
        _db.Users.Add(user);

        _db.PaymentTransactions.Add(new PaymentTransaction
        {
            TransactionId = 501,
            UserId = 5,
            PlanId = 1,
            GatewayProvider = "MOMO",
            PartnerTransactionCode = "VL_20260920_501",
            AmountVnd = 49000,
            TransactionStatus = "SUCCESS",
            SignatureHash = "sig_hash_501",
            StartsAt = new DateTime(2026, 9, 21, 0, 0, 0, DateTimeKind.Utc),
            EndsAt = new DateTime(2026, 10, 20, 23, 59, 59, DateTimeKind.Utc),
            CreatedAt = new DateTime(2026, 9, 20, 10, 0, 0, DateTimeKind.Utc)
        });
        await _db.SaveChangesAsync();

        // ACT 1: Chạy lần thứ nhất
        var firstCount = await _service.DowngradeExpiredVipUsersAsync();
        firstCount.Should().Be(1);

        // ACT 2: Chạy lần thứ hai ngay sau đó
        var secondCount = await _service.DowngradeExpiredVipUsersAsync();
        secondCount.Should().Be(0);

        // ASSERT: Chỉ có đúng 1 bản ghi AuditLog duy nhất
        var auditLogs = await _db.AuditLogs
            .Where(a => a.TargetTable == "Users" && a.TargetId == "5" && a.Action == "VIP_EXPIRED")
            .ToListAsync();

        auditLogs.Should().HaveCount(1);
    }

    [Fact]
    public async Task Downgrade_WithFakeClock_AdvancesTimePastMidnightAndDowngrades()
    {
        // ARRANGE: Thiết lập thời điểm ban đầu 23:50:00 của ngày cuối cùng
        _fakeClock.SetUtcNow(new DateTimeOffset(2026, 10, 24, 23, 50, 0, TimeSpan.Zero));

        var user = new User
        {
            UserId = 6,
            Email = "clock_advance@veggielife.vn",
            FullName = "Vu Clock Advance",
            Role = UserRoles.Vip,
            AccountStatus = AccountStatuses.Active,
            CreatedAt = new DateTime(2026, 9, 25, 0, 0, 0, DateTimeKind.Utc)
        };
        _db.Users.Add(user);

        _db.PaymentTransactions.Add(new PaymentTransaction
        {
            TransactionId = 601,
            UserId = 6,
            PlanId = 1,
            GatewayProvider = "MOMO",
            PartnerTransactionCode = "VL_20260925_601",
            AmountVnd = 49000,
            TransactionStatus = "SUCCESS",
            SignatureHash = "sig_hash_601",
            StartsAt = new DateTime(2026, 9, 25, 0, 0, 0, DateTimeKind.Utc),
            EndsAt = new DateTime(2026, 10, 24, 23, 59, 59, DateTimeKind.Utc),
            CreatedAt = new DateTime(2026, 9, 25, 10, 0, 0, DateTimeKind.Utc)
        });
        await _db.SaveChangesAsync();

        // 1. Chạy lúc 23:50:00 (chưa hết hạn) -> Không bị hạ cấp
        var countBeforeMidnight = await _service.DowngradeExpiredVipUsersAsync();
        countBeforeMidnight.Should().Be(0);
        (await _db.Users.FirstAsync(u => u.UserId == 6)).Role.Should().Be(UserRoles.Vip);

        // 2. Giả lập đồng hồ trôi qua nửa đêm: Tua 10 phút 5 giây -> Hiện tại là 2026-10-25 00:00:05
        _fakeClock.Advance(TimeSpan.FromMinutes(10) + TimeSpan.FromSeconds(5));

        // 3. Chạy sau nửa đêm -> Bị hạ cấp ngay lập tức
        var countAfterMidnight = await _service.DowngradeExpiredVipUsersAsync();
        countAfterMidnight.Should().Be(1);

        var downgradedUser = await _db.Users.FirstAsync(u => u.UserId == 6);
        downgradedUser.Role.Should().Be(UserRoles.Member);

        // 4. Chạy lại lần nữa -> 0
        var countAgain = await _service.DowngradeExpiredVipUsersAsync();
        countAgain.Should().Be(0);
    }
}
