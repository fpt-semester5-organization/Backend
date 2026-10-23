using System;
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
/// Unit Tests cho PaymentExpiryService (FN-SUB-02, BR-PAY-TXN-01).
/// Kiểm thử với đồng hồ giả (FakeTimeProvider): đơn quá 5 phút bị hết hạn; chạy lại 2 lần không đổi kết quả (Idempotent).
/// </summary>
public class PaymentExpiryServiceTests
{
    private readonly AppDbContext _db;
    private readonly Mock<IRedisService> _mockRedis;
    private readonly FakeTimeProvider _fakeClock;
    private readonly PaymentExpiryService _service;

    public PaymentExpiryServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: "PaymentExpiryTestsDb_" + Guid.NewGuid())
            .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        _db = new AppDbContext(options);
        _mockRedis = new Mock<IRedisService>();
        _fakeClock = new FakeTimeProvider(new DateTimeOffset(2026, 10, 24, 10, 0, 0, TimeSpan.Zero));
        _service = new PaymentExpiryService(_db, _mockRedis.Object, NullLogger<PaymentExpiryService>.Instance, _fakeClock);
    }

    [Fact]
    public async Task Expire_WhenNoPendingPayments_Returns0()
    {
        // ARRANGE: Không có giao dịch nào
        // ACT
        var count = await _service.ExpirePendingPaymentsAsync();

        // ASSERT
        count.Should().Be(0);
    }

    [Fact]
    public async Task Expire_WhenPendingUnder5Minutes_DoesNotExpire()
    {
        // ARRANGE: Đơn tạo lúc 09:57:00 (hiện tại là 10:00:00 -> mới 3 phút)
        var tx = new PaymentTransaction
        {
            TransactionId = 101,
            UserId = 1,
            PlanId = 1,
            GatewayProvider = "MOMO",
            PartnerTransactionCode = "VL_20261024_001",
            AmountVnd = 49000,
            TransactionStatus = "PENDING",
            SignatureHash = "mock_hash_101",
            CreatedAt = new DateTime(2026, 10, 24, 9, 57, 0, DateTimeKind.Utc),
            StartsAt = new DateTime(2026, 10, 24, 9, 57, 0, DateTimeKind.Utc),
            EndsAt = new DateTime(2026, 11, 23, 23, 59, 59, DateTimeKind.Utc)
        };
        _db.PaymentTransactions.Add(tx);
        await _db.SaveChangesAsync();

        // ACT
        var count = await _service.ExpirePendingPaymentsAsync();

        // ASSERT
        count.Should().Be(0);

        var txInDb = await _db.PaymentTransactions.FirstAsync(p => p.TransactionId == 101);
        txInDb.TransactionStatus.Should().Be("PENDING");
        txInDb.CompletedAt.Should().BeNull();
    }

    [Fact]
    public async Task Expire_WhenPendingOver5Minutes_ExpiresAndInsertsAuditLogAndClearsRedis()
    {
        // ARRANGE: Đơn tạo lúc 09:54:00 (hiện tại là 10:00:00 -> đã 6 phút, quá 5 phút)
        var tx = new PaymentTransaction
        {
            TransactionId = 102,
            UserId = 1,
            PlanId = 1,
            GatewayProvider = "VIETQR",
            PartnerTransactionCode = "VL_20261024_002",
            AmountVnd = 99000,
            TransactionStatus = "PENDING",
            SignatureHash = "mock_hash_102",
            CreatedAt = new DateTime(2026, 10, 24, 9, 54, 0, DateTimeKind.Utc),
            StartsAt = new DateTime(2026, 10, 24, 9, 54, 0, DateTimeKind.Utc),
            EndsAt = new DateTime(2026, 11, 23, 23, 59, 59, DateTimeKind.Utc)
        };
        _db.PaymentTransactions.Add(tx);
        await _db.SaveChangesAsync();

        // ACT
        var count = await _service.ExpirePendingPaymentsAsync();

        // ASSERT
        count.Should().Be(1);

        var txInDb = await _db.PaymentTransactions.FirstAsync(p => p.TransactionId == 102);
        txInDb.TransactionStatus.Should().Be("EXPIRED");
        txInDb.CompletedAt.Should().Be(_fakeClock.GetUtcNow().UtcDateTime);

        // Kiểm tra xóa khóa Redis
        _mockRedis.Verify(r => r.RemoveAsync("payment_expire:102", It.IsAny<CancellationToken>()), Times.Once);

        // Kiểm tra ghi nhận AuditLog hệ thống
        var audit = await _db.AuditLogs.FirstOrDefaultAsync(a => a.TargetTable == "PaymentTransactions" && a.TargetId == "102");
        audit.Should().NotBeNull();
        audit!.Action.Should().Be("PAYMENT_EXPIRED");
        audit.ActorRole.Should().Be("SYSTEM");
        audit.ActorUserId.Should().BeNull();
    }

    [Fact]
    public async Task Expire_IgnoresCompletedOrNonPendingTransactions()
    {
        // ARRANGE: Các đơn đã SUCCESS hoặc FAILED tạo cách đây 10 phút
        _db.PaymentTransactions.AddRange(
            new PaymentTransaction
            {
                TransactionId = 201,
                UserId = 1,
                PlanId = 1,
                GatewayProvider = "MOMO",
                PartnerTransactionCode = "VL_20261024_201",
                AmountVnd = 49000,
                TransactionStatus = "SUCCESS",
                SignatureHash = "mock_hash_201",
                CreatedAt = new DateTime(2026, 10, 24, 9, 50, 0, DateTimeKind.Utc)
            },
            new PaymentTransaction
            {
                TransactionId = 202,
                UserId = 2,
                PlanId = 1,
                GatewayProvider = "VIETQR",
                PartnerTransactionCode = "VL_20261024_202",
                AmountVnd = 49000,
                TransactionStatus = "FAILED",
                SignatureHash = "mock_hash_202",
                CreatedAt = new DateTime(2026, 10, 24, 9, 50, 0, DateTimeKind.Utc)
            }
        );
        await _db.SaveChangesAsync();

        // ACT
        var count = await _service.ExpirePendingPaymentsAsync();

        // ASSERT
        count.Should().Be(0);

        var successTx = await _db.PaymentTransactions.FirstAsync(p => p.TransactionId == 201);
        successTx.TransactionStatus.Should().Be("SUCCESS");

        var failedTx = await _db.PaymentTransactions.FirstAsync(p => p.TransactionId == 202);
        failedTx.TransactionStatus.Should().Be("FAILED");
    }

    [Fact]
    public async Task Expire_IsIdempotent_RunningTwiceProducesSameResultAndNoDuplicateAuditLogs()
    {
        // ARRANGE: Đơn tạo cách đây 10 phút
        var tx = new PaymentTransaction
        {
            TransactionId = 301,
            UserId = 1,
            PlanId = 1,
            GatewayProvider = "MOMO",
            PartnerTransactionCode = "VL_20261024_301",
            AmountVnd = 49000,
            TransactionStatus = "PENDING",
            SignatureHash = "mock_hash_301",
            CreatedAt = new DateTime(2026, 10, 24, 9, 50, 0, DateTimeKind.Utc)
        };
        _db.PaymentTransactions.Add(tx);
        await _db.SaveChangesAsync();

        // ACT 1: Chạy lần thứ nhất
        var firstCount = await _service.ExpirePendingPaymentsAsync();
        firstCount.Should().Be(1);

        // ACT 2: Chạy lại lần thứ hai ngay sau đó
        var secondCount = await _service.ExpirePendingPaymentsAsync();
        secondCount.Should().Be(0);

        // ASSERT: Chỉ có 1 bản ghi AuditLog duy nhất, không tạo thừa
        var auditLogs = await _db.AuditLogs
            .Where(a => a.TargetTable == "PaymentTransactions" && a.TargetId == "301" && a.Action == "PAYMENT_EXPIRED")
            .ToListAsync();

        auditLogs.Should().HaveCount(1);
    }

    [Fact]
    public async Task Expire_WithFakeClock_AdvancesTimeAndTriggersExpiration()
    {
        // ARRANGE: Tạo đơn tại thời điểm hiện tại của đồng hồ giả (10:00:00)
        var orderTime = _fakeClock.GetUtcNow().UtcDateTime;
        var tx = new PaymentTransaction
        {
            TransactionId = 401,
            UserId = 5,
            PlanId = 1,
            GatewayProvider = "MOMO",
            PartnerTransactionCode = "VL_20261024_401",
            AmountVnd = 49000,
            TransactionStatus = "PENDING",
            SignatureHash = "mock_hash_401",
            CreatedAt = orderTime
        };
        _db.PaymentTransactions.Add(tx);
        await _db.SaveChangesAsync();

        // 1. Tua đồng hồ tới 10:04:00 (mới trôi qua 4 phút)
        _fakeClock.Advance(TimeSpan.FromMinutes(4));
        var countAt4m = await _service.ExpirePendingPaymentsAsync();
        countAt4m.Should().Be(0, "đơn chưa quá 5 phút nên không bị hết hạn");

        // 2. Tua tiếp 1 phút 1 giây (tổng cộng 5 phút 1 giây từ lúc tạo) -> Đã quá 5 phút!
        _fakeClock.Advance(TimeSpan.FromSeconds(61));
        var countAt5m1s = await _service.ExpirePendingPaymentsAsync();
        countAt5m1s.Should().Be(1, "đơn đã quá 5 phút phải bị chuyển sang EXPIRED");

        var expiredTx = await _db.PaymentTransactions.FirstAsync(p => p.TransactionId == 401);
        expiredTx.TransactionStatus.Should().Be("EXPIRED");

        // 3. Tua tiếp 10 phút và chạy lại -> Vẫn là 0 (đã hết hạn từ trước)
        _fakeClock.Advance(TimeSpan.FromMinutes(10));
        var countAt15m = await _service.ExpirePendingPaymentsAsync();
        countAt15m.Should().Be(0, "không lặp lại thao tác với đơn đã hết hạn");
    }
}
