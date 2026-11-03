using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using VeggieLife.BLL.Common.Constants;
using VeggieLife.BLL.Common.Exceptions;
using VeggieLife.BLL.Services.Cache;
using VeggieLife.BLL.Services.Email;
using VeggieLife.BLL.Services.Payment;
using VeggieLife.BLL.Services.Subscriptions;
using VeggieLife.DAL.Context;
using VeggieLife.DAL.Entities;
using Xunit;

namespace VeggieLife.Tests.Unit.Services.Subscriptions;

/// <summary>
/// Unit tests cho tính năng Biên lai thanh toán nâng cấp gói VIP (FN-SUB-04).
/// </summary>
public class SubscriptionReceiptTests
{
    private class InMemoryRedisService : IRedisService
    {
        private readonly ConcurrentDictionary<string, (object Value, DateTime ExpireAt)> _storage = new();

        public Task<T?> GetAsync<T>(string key, CancellationToken ct = default)
        {
            if (_storage.TryGetValue(key, out var entry))
            {
                if (DateTime.UtcNow <= entry.ExpireAt)
                {
                    return Task.FromResult((T?)entry.Value);
                }
                _storage.TryRemove(key, out _);
            }
            return Task.FromResult<T?>(default);
        }

        public Task SetAsync<T>(string key, T value, TimeSpan? expiry = null, CancellationToken ct = default)
        {
            var expireAt = DateTime.UtcNow.Add(expiry ?? TimeSpan.FromDays(1));
            _storage[key] = (value!, expireAt);
            return Task.CompletedTask;
        }

        public Task RemoveAsync(string key, CancellationToken ct = default)
        {
            _storage.TryRemove(key, out _);
            return Task.CompletedTask;
        }

        public Task<bool> ExistsAsync(string key, CancellationToken ct = default) => Task.FromResult(_storage.ContainsKey(key));
        public Task<bool> SetNxAsync(string key, string value, TimeSpan expiry, CancellationToken ct = default) => Task.FromResult(_storage.TryAdd(key, (value, DateTime.UtcNow.Add(expiry))));
        public Task<long> IncrementAsync(string key, long value = 1, TimeSpan? expiry = null, CancellationToken ct = default) => Task.FromResult(1L);
        public Task<TimeSpan?> GetTtlAsync(string key, CancellationToken ct = default) => Task.FromResult<TimeSpan?>(TimeSpan.FromMinutes(5));
        public Task<(bool Allowed, long Current, long Remaining)> TryConsumeQuotaAsync(string key, int maxLimit, TimeSpan? expiry = null, CancellationToken ct = default) => Task.FromResult((true, 1L, (long)(maxLimit - 1)));
        public Task<(bool Allowed, bool IsBanned, long CurrentCount, TimeSpan? RetryAfter)> CheckIpRateLimitAsync(string clientIp, int maxRequestsPerMinute = 10, int banDurationMinutes = 10, CancellationToken ct = default) => Task.FromResult((true, false, 1L, (TimeSpan?)null));
        public Task EnqueueAsync<T>(string queueName, T item, CancellationToken ct = default) => Task.CompletedTask;
    }

    private class TestEmailService : IEmailService
    {
        public Task SendOtpEmailAsync(string toEmail, string otpCode, string purpose, CancellationToken ct = default) => Task.CompletedTask;
        public Task SendNotificationEmailAsync(string toEmail, string subject, string htmlBody, CancellationToken ct = default) => Task.CompletedTask;
    }

    private static (AppDbContext Db, SubscriptionService Service) CreateHarness()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var db = new AppDbContext(options);
        var redis = new InMemoryRedisService();
        var email = new TestEmailService();

        var config = new ConfigurationBuilder().Build();
        var gateway = new PaymentGatewayServiceStub(config, NullLogger<PaymentGatewayServiceStub>.Instance);
        var service = new SubscriptionService(db, redis, gateway, email, NullLogger<SubscriptionService>.Instance);

        return (db, service);
    }

    [Fact]
    public async Task GetPaymentReceiptAsync_WhenTransactionSuccess_ReturnsReceiptDto()
    {
        // Arrange
        var (db, service) = CreateHarness();

        var user = new User
        {
            UserId = 1,
            Email = "member@veggielife.vn",
            FullName = "Nguyễn Văn Chay",
            Role = UserRoles.Vip,
            AccountStatus = "ACTIVE",
            CreatedAt = DateTime.UtcNow
        };
        var plan = new SubscriptionPlan
        {
            PlanId = 1,
            PlanName = "VIP 1 Tháng",
            DurationDays = 30,
            Price = 49000,
            IsActive = true
        };
        var tx = new PaymentTransaction
        {
            TransactionId = 1001,
            PlanId = plan.PlanId,
            UserId = user.UserId,
            GatewayProvider = "VIETQR",
            PartnerTransactionCode = "ORD-123456",
            AmountVnd = 49000,
            TransactionStatus = "SUCCESS",
            SignatureHash = "sig123",
            CreatedAt = new DateTime(2026, 11, 3, 10, 0, 0, DateTimeKind.Utc),
            CompletedAt = new DateTime(2026, 11, 3, 10, 2, 0, DateTimeKind.Utc),
            StartsAt = new DateTime(2026, 11, 3, 10, 0, 0, DateTimeKind.Utc),
            EndsAt = new DateTime(2026, 12, 3, 23, 59, 59, DateTimeKind.Utc),
            ReceiptUrl = "/api/v1/subscriptions/transactions/1001/receipt"
        };

        db.Users.Add(user);
        db.SubscriptionPlans.Add(plan);
        db.PaymentTransactions.Add(tx);
        await db.SaveChangesAsync();

        // Act
        var receipt = await service.GetPaymentReceiptAsync(1001, user.UserId, UserRoles.Member);

        // Assert
        receipt.Should().NotBeNull();
        receipt.TransactionId.Should().Be(1001);
        receipt.ReceiptNumber.Should().Be("REC-20261103-001001");
        receipt.OrderCode.Should().Be("ORD-123456");
        receipt.Gateway.Should().Be("VIETQR");
        receipt.Status.Should().Be("SUCCESS");
        receipt.FullName.Should().Be("Nguyễn Văn Chay");
        receipt.Email.Should().Be("member@veggielife.vn");
        receipt.PlanName.Should().Be("VIP 1 Tháng");
        receipt.AmountVnd.Should().Be(49000);
        receipt.DurationDays.Should().Be(30);
        receipt.PaymentDate.Should().Be(tx.CompletedAt!.Value);
    }

    [Fact]
    public async Task GetPaymentReceiptAsync_WhenAdminRequestsAnotherUsersReceipt_ReturnsReceiptDto()
    {
        // Arrange
        var (db, service) = CreateHarness();

        var user = new User
        {
            UserId = 1,
            Email = "member@veggielife.vn",
            FullName = "Nguyễn Văn Chay",
            Role = UserRoles.Vip,
            AccountStatus = "ACTIVE",
            CreatedAt = DateTime.UtcNow
        };
        var plan = new SubscriptionPlan
        {
            PlanId = 1,
            PlanName = "VIP 1 Tháng",
            DurationDays = 30,
            Price = 49000,
            IsActive = true
        };
        var tx = new PaymentTransaction
        {
            TransactionId = 2002,
            PlanId = plan.PlanId,
            UserId = user.UserId,
            GatewayProvider = "MOMO",
            PartnerTransactionCode = "MOMO-789",
            AmountVnd = 49000,
            TransactionStatus = "SUCCESS",
            SignatureHash = "sig",
            CreatedAt = DateTime.UtcNow,
            CompletedAt = DateTime.UtcNow,
            StartsAt = DateTime.UtcNow,
            EndsAt = DateTime.UtcNow.AddDays(30)
        };

        db.Users.Add(user);
        db.SubscriptionPlans.Add(plan);
        db.PaymentTransactions.Add(tx);
        await db.SaveChangesAsync();

        // Act (Admin with userId 999)
        var receipt = await service.GetPaymentReceiptAsync(2002, currentUserId: 999, currentUserRole: UserRoles.Admin);

        // Assert
        receipt.Should().NotBeNull();
        receipt.TransactionId.Should().Be(2002);
        receipt.UserId.Should().Be(user.UserId);
    }

    [Fact]
    public async Task GetPaymentReceiptAsync_WhenUserNotOwnerAndNotAdmin_ThrowsForbidden()
    {
        // Arrange
        var (db, service) = CreateHarness();

        var user = new User
        {
            UserId = 10,
            Email = "owner@veggielife.vn",
            FullName = "Chủ Đơn",
            Role = UserRoles.Vip,
            AccountStatus = "ACTIVE",
            CreatedAt = DateTime.UtcNow
        };
        var plan = new SubscriptionPlan
        {
            PlanId = 1,
            PlanName = "VIP 1 Tháng",
            DurationDays = 30,
            Price = 49000,
            IsActive = true
        };
        var tx = new PaymentTransaction
        {
            TransactionId = 3003,
            PlanId = plan.PlanId,
            UserId = user.UserId,
            GatewayProvider = "VIETQR",
            PartnerTransactionCode = "ORD-3003",
            AmountVnd = 49000,
            TransactionStatus = "SUCCESS",
            SignatureHash = "sig",
            CreatedAt = DateTime.UtcNow,
            CompletedAt = DateTime.UtcNow,
            StartsAt = DateTime.UtcNow,
            EndsAt = DateTime.UtcNow.AddDays(30)
        };
        db.Users.Add(user);
        db.SubscriptionPlans.Add(plan);
        db.PaymentTransactions.Add(tx);
        await db.SaveChangesAsync();

        // Act & Assert (Request by user 99)
        var act = () => service.GetPaymentReceiptAsync(3003, currentUserId: 99, currentUserRole: UserRoles.Member);
        await act.Should().ThrowAsync<AppException>()
            .Where(e => e.ErrorCode == ErrorCodes.ErrForbidden);
    }

    [Fact]
    public async Task GetPaymentReceiptAsync_WhenTransactionNotFound_ThrowsNotFound()
    {
        // Arrange
        var (_, service) = CreateHarness();

        // Act & Assert
        var act = () => service.GetPaymentReceiptAsync(999999, currentUserId: 1, currentUserRole: UserRoles.Member);
        await act.Should().ThrowAsync<AppException>()
            .Where(e => e.ErrorCode == ErrorCodes.ErrPayOrderNotFound);
    }

    [Fact]
    public async Task GetPaymentReceiptAsync_WhenTransactionNotSuccess_ThrowsBadRequest()
    {
        // Arrange
        var (db, service) = CreateHarness();

        var user = new User
        {
            UserId = 1,
            Email = "member@veggielife.vn",
            FullName = "Nguyễn Văn Chay",
            Role = UserRoles.Member,
            AccountStatus = "ACTIVE",
            CreatedAt = DateTime.UtcNow
        };
        var plan = new SubscriptionPlan
        {
            PlanId = 1,
            PlanName = "VIP 1 Tháng",
            DurationDays = 30,
            Price = 49000,
            IsActive = true
        };
        var tx = new PaymentTransaction
        {
            TransactionId = 4004,
            PlanId = plan.PlanId,
            UserId = user.UserId,
            GatewayProvider = "VIETQR",
            PartnerTransactionCode = "ORD-4004",
            AmountVnd = 49000,
            TransactionStatus = "PENDING",
            SignatureHash = "sig",
            CreatedAt = DateTime.UtcNow,
            StartsAt = DateTime.UtcNow,
            EndsAt = DateTime.UtcNow.AddDays(30)
        };
        db.Users.Add(user);
        db.SubscriptionPlans.Add(plan);
        db.PaymentTransactions.Add(tx);
        await db.SaveChangesAsync();

        // Act & Assert
        var act = () => service.GetPaymentReceiptAsync(4004, currentUserId: user.UserId, currentUserRole: UserRoles.Member);
        await act.Should().ThrowAsync<AppException>()
            .Where(e => e.ErrorCode == ErrorCodes.ErrValidation);
    }

    [Fact]
    public async Task GetCurrentUserSubscriptionAsync_MapsExtendedReceiptFields()
    {
        // Arrange
        var (db, service) = CreateHarness();

        var user = new User
        {
            UserId = 50,
            Email = "test@veggielife.vn",
            FullName = "Tester",
            Role = UserRoles.Vip,
            AccountStatus = "ACTIVE",
            CreatedAt = DateTime.UtcNow
        };
        var plan = new SubscriptionPlan
        {
            PlanId = 2,
            PlanName = "VIP 1 Năm",
            DurationDays = 365,
            Price = 399000,
            IsActive = true
        };
        var tx = new PaymentTransaction
        {
            TransactionId = 5005,
            PlanId = plan.PlanId,
            UserId = user.UserId,
            GatewayProvider = "VIETQR",
            PartnerTransactionCode = "ORD-5005",
            AmountVnd = 399000,
            TransactionStatus = "SUCCESS",
            SignatureHash = "sig",
            CreatedAt = DateTime.UtcNow.AddDays(-1),
            CompletedAt = DateTime.UtcNow.AddDays(-1),
            StartsAt = DateTime.UtcNow.AddDays(-1),
            EndsAt = DateTime.UtcNow.AddDays(364)
        };

        db.Users.Add(user);
        db.SubscriptionPlans.Add(plan);
        db.PaymentTransactions.Add(tx);
        await db.SaveChangesAsync();

        // Act
        var result = await service.GetCurrentUserSubscriptionAsync(user.UserId);

        // Assert
        result.Should().NotBeNull();
        result.Transactions.Should().HaveCount(1);
        var txDto = result.Transactions[0];
        txDto.PlanName.Should().Be("VIP 1 Năm");
        txDto.OrderCode.Should().Be("ORD-5005");
        txDto.CompletedAt.Should().Be(tx.CompletedAt);
        txDto.ReceiptUrl.Should().Be("/api/v1/subscriptions/transactions/5005/receipt");
        txDto.CanViewReceipt.Should().BeTrue();
    }
}
