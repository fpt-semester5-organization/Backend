using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using VeggieLife.BLL.Common.Constants;
using VeggieLife.BLL.Common.Exceptions;
using VeggieLife.BLL.DTOs.Subscriptions.Requests;
using VeggieLife.BLL.Services.Cache;
using VeggieLife.BLL.Services.Email;
using VeggieLife.BLL.Services.Payment;
using VeggieLife.BLL.Services.Subscriptions;
using VeggieLife.DAL.Context;
using VeggieLife.DAL.Entities;
using Xunit;

namespace VeggieLife.Tests.Unit.Services.Subscriptions;

/// <summary>
/// Bộ kiểm thử đơn vị cho Module Gói VIP & Thanh toán (FN-SUB-01, FN-SUB-02, FN-SUB-03, FN-SUB-04).
/// Kiểm thử các quy tắc nghiệp vụ: BR-PAY-SUB-01, BR-PAY-SUB-02, BR-PAY-TXN-01, BR-PAY-TXN-02, BR-ADM-PAY-001.
/// </summary>
public class SubscriptionServiceTests
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

        public Task<bool> ExistsAsync(string key, CancellationToken ct = default)
        {
            if (_storage.TryGetValue(key, out var entry))
            {
                if (DateTime.UtcNow <= entry.ExpireAt)
                {
                    return Task.FromResult(true);
                }
                _storage.TryRemove(key, out _);
            }
            return Task.FromResult(false);
        }

        public Task<long> IncrementAsync(string key, long value = 1, TimeSpan? expiry = null, CancellationToken ct = default)
        {
            return Task.FromResult(1L);
        }

        public Task<(bool Allowed, long Current, long Remaining)> TryConsumeQuotaAsync(string key, int maxLimit, TimeSpan? expiry = null, CancellationToken ct = default)
        {
            return Task.FromResult((true, 1L, (long)(maxLimit - 1)));
        }

        public Task<(bool Allowed, bool IsBanned, long CurrentCount, TimeSpan? RetryAfter)> CheckIpRateLimitAsync(string clientIp, int maxRequestsPerMinute = 10, int banDurationMinutes = 10, CancellationToken ct = default)
        {
            return Task.FromResult((true, false, 1L, (TimeSpan?)null));
        }

        public Task EnqueueAsync<T>(string queueName, T item, CancellationToken ct = default)
        {
            return Task.CompletedTask;
        }
    }

    private class TestEmailService : IEmailService
    {
        public readonly List<(string Email, string Otp, string Purpose)> SentEmails = new();

        public Task SendOtpEmailAsync(string toEmail, string otpCode, string purpose, CancellationToken ct = default)
        {
            SentEmails.Add((toEmail, otpCode, purpose));
            return Task.CompletedTask;
        }

        public Task SendNotificationEmailAsync(string toEmail, string subject, string htmlBody, CancellationToken ct = default)
        {
            return Task.CompletedTask;
        }
    }

    private static (AppDbContext Db, SubscriptionService Service, PaymentGatewayServiceStub Gateway, InMemoryRedisService Redis, TestEmailService Email) CreateTestHarness()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        var db = new AppDbContext(options);
        var redis = new InMemoryRedisService();
        var email = new TestEmailService();

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Payment:MoMo:SecretKey"] = "test_momo_secret_key",
                ["Payment:VietQR:SecretKey"] = "test_vietqr_secret_key"
            })
            .Build();

        var gateway = new PaymentGatewayServiceStub(config, NullLogger<PaymentGatewayServiceStub>.Instance);
        var service = new SubscriptionService(db, redis, gateway, email, NullLogger<SubscriptionService>.Instance);

        return (db, service, gateway, redis, email);
    }

    private static string ComputeSignature(string secretKey, string orderCode, string partnerTransactionId, long amount, int resultCode)
    {
        var dict = new SortedDictionary<string, string>(StringComparer.Ordinal)
        {
            ["amount"] = amount.ToString(),
            ["orderCode"] = orderCode,
            ["partnerTransactionId"] = partnerTransactionId,
            ["resultCode"] = resultCode.ToString()
        };
        var rawData = string.Join("&", dict.Select(kv => $"{kv.Key}={kv.Value}"));

        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secretKey));
        var hashBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(rawData));
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }

    // ==========================================
    // FN-SUB-01: Xem các gói VIP
    // ==========================================

    [Fact]
    public async Task GetSubscriptionPlansAsync_ReturnsOnlyActivePlans_OrderedByDuration()
    {
        var (db, service, _, redis, _) = CreateTestHarness();

        // Seed Admin User
        var admin = new User { UserId = 1, Email = "admin@veggielife.vn", FullName = "Admin", Role = UserRoles.Admin, AccountStatus = "ACTIVE" };
        db.Users.Add(admin);

        // Seed Plans
        db.SubscriptionPlans.AddRange(
            new SubscriptionPlan { PlanId = 1, AdminId = 1, PlanName = "VIP 1 Tháng", Price = 49000, DurationDays = 30, IsActive = true, CreatedAt = DateTime.UtcNow },
            new SubscriptionPlan { PlanId = 2, AdminId = 1, PlanName = "VIP 1 Năm", Price = 449000, DurationDays = 365, IsActive = true, CreatedAt = DateTime.UtcNow },
            new SubscriptionPlan { PlanId = 3, AdminId = 1, PlanName = "VIP Cũ Ngừng Bán", Price = 99000, DurationDays = 60, IsActive = false, CreatedAt = DateTime.UtcNow }
        );
        await db.SaveChangesAsync();

        var result = await service.GetSubscriptionPlansAsync();

        result.Should().NotBeNull();
        result.Plans.Should().HaveCount(2);
        result.Plans[0].PlanName.Should().Be("VIP 1 Tháng");
        result.Plans[0].PriceVnd.Should().Be(49000);
        result.Plans[1].PlanName.Should().Be("VIP 1 Năm");
        result.Plans[1].PriceVnd.Should().Be(449000);

        // Kiểm tra benefits
        result.Benefits.Free.MealPlansPerWeek.Should().Be(1);
        result.Benefits.Free.ChatPerDay.Should().Be(20);
        result.Benefits.Free.FridgePhotosPerDay.Should().Be(10);

        result.Benefits.Vip.MealPlansPerWeek.Should().Be(3);
        result.Benefits.Vip.ChatPerDay.Should().BeNull();
        result.Benefits.Vip.FridgePhotosPerDay.Should().Be(30);

        // Kiểm tra Redis cache đã lưu
        var isCached = await redis.ExistsAsync("plans_cache");
        isCached.Should().BeTrue();
    }

    // ==========================================
    // FN-SUB-02: Thanh toán gói VIP
    // ==========================================

    [Fact]
    public async Task CheckoutSubscriptionAsync_WhenGuest_ThrowsForbidden()
    {
        var (_, service, _, _, _) = CreateTestHarness();

        var act = () => service.CheckoutSubscriptionAsync(100, UserRoles.Guest, new CheckoutSubscriptionRequest
        {
            PlanId = 1,
            Gateway = "MOMO"
        });

        var ex = await act.Should().ThrowAsync<AppException>();
        ex.Which.StatusCode.Should().Be(403);
        ex.Which.ErrorCode.Should().Be(ErrorCodes.ErrGuestPersonalizationRestricted);
    }

    [Theory]
    [InlineData("ZALOPAY")]
    [InlineData("PAYPAL")]
    [InlineData("")]
    public async Task CheckoutSubscriptionAsync_WhenInvalidGateway_ThrowsBadRequest(string invalidGateway)
    {
        var (_, service, _, _, _) = CreateTestHarness();

        var act = () => service.CheckoutSubscriptionAsync(100, UserRoles.Member, new CheckoutSubscriptionRequest
        {
            PlanId = 1,
            Gateway = invalidGateway
        });

        var ex = await act.Should().ThrowAsync<AppException>();
        ex.Which.StatusCode.Should().Be(400);
        ex.Which.ErrorCode.Should().Be(ErrorCodes.ErrValidation);
    }

    [Fact]
    public async Task CheckoutSubscriptionAsync_WhenPlanNotFoundOrInactive_ThrowsNotFound()
    {
        var (db, service, _, _, _) = CreateTestHarness();

        db.SubscriptionPlans.Add(new SubscriptionPlan
        {
            PlanId = 10,
            AdminId = 1,
            PlanName = "Gói Tạm Dừng",
            Price = 50000,
            DurationDays = 30,
            IsActive = false,
            CreatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var actNotFound = () => service.CheckoutSubscriptionAsync(100, UserRoles.Member, new CheckoutSubscriptionRequest
        {
            PlanId = 999,
            Gateway = "MOMO"
        });

        var ex1 = await actNotFound.Should().ThrowAsync<AppException>();
        ex1.Which.StatusCode.Should().Be(404);
        ex1.Which.ErrorCode.Should().Be(ErrorCodes.ErrPlanNotFound);

        var actInactive = () => service.CheckoutSubscriptionAsync(100, UserRoles.Member, new CheckoutSubscriptionRequest
        {
            PlanId = 10,
            Gateway = "VIETQR"
        });

        var ex2 = await actInactive.Should().ThrowAsync<AppException>();
        ex2.Which.StatusCode.Should().Be(404);
        ex2.Which.ErrorCode.Should().Be(ErrorCodes.ErrPlanNotFound);
    }

    [Fact]
    public async Task CheckoutSubscriptionAsync_WhenPendingExistsWithin5Minutes_ThrowsConflict()
    {
        var (db, service, _, _, _) = CreateTestHarness();

        db.SubscriptionPlans.Add(new SubscriptionPlan
        {
            PlanId = 1,
            AdminId = 1,
            PlanName = "VIP 1 Tháng",
            Price = 49000,
            DurationDays = 30,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        });

        // Đã có đơn PENDING tạo cách đây 2 phút
        db.PaymentTransactions.Add(new PaymentTransaction
        {
            TransactionId = 1001,
            PlanId = 1,
            UserId = 100,
            GatewayProvider = "MOMO",
            PartnerTransactionCode = "VL_OLD_PENDING",
            AmountVnd = 49000,
            TransactionStatus = "PENDING",
            SignatureHash = "old_sig",
            CreatedAt = DateTime.UtcNow.AddMinutes(-2),
            StartsAt = DateTime.UtcNow,
            EndsAt = DateTime.UtcNow.AddDays(30)
        });
        await db.SaveChangesAsync();

        var act = () => service.CheckoutSubscriptionAsync(100, UserRoles.Member, new CheckoutSubscriptionRequest
        {
            PlanId = 1,
            Gateway = "MOMO"
        });

        var ex = await act.Should().ThrowAsync<AppException>();
        ex.Which.StatusCode.Should().Be(409);
        ex.Which.ErrorCode.Should().Be(ErrorCodes.ErrPaymentPendingExists);
    }

    [Fact]
    public async Task CheckoutSubscriptionAsync_WhenPendingExpiredOver5Minutes_AllowsNewCheckout()
    {
        var (db, service, _, _, _) = CreateTestHarness();

        db.SubscriptionPlans.Add(new SubscriptionPlan
        {
            PlanId = 1,
            AdminId = 1,
            PlanName = "VIP 1 Tháng",
            Price = 49000,
            DurationDays = 30,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        });

        // Đơn PENDING cũ tạo cách đây 6 phút (đã quá 5 phút)
        db.PaymentTransactions.Add(new PaymentTransaction
        {
            TransactionId = 1001,
            PlanId = 1,
            UserId = 100,
            GatewayProvider = "MOMO",
            PartnerTransactionCode = "VL_EXPIRED_PENDING",
            AmountVnd = 49000,
            TransactionStatus = "PENDING",
            SignatureHash = "old_sig",
            CreatedAt = DateTime.UtcNow.AddMinutes(-6),
            StartsAt = DateTime.UtcNow,
            EndsAt = DateTime.UtcNow.AddDays(30)
        });
        await db.SaveChangesAsync();

        var result = await service.CheckoutSubscriptionAsync(100, UserRoles.Member, new CheckoutSubscriptionRequest
        {
            PlanId = 1,
            Gateway = "MOMO"
        });

        result.Should().NotBeNull();
        result.AmountVnd.Should().Be(49000);
        result.PayUrl.Should().Contain("momo");
        result.OrderCode.Should().StartWith("VL_");

        // Đơn mới được lưu trong DB
        var savedTx = await db.PaymentTransactions.FirstOrDefaultAsync(t => t.PartnerTransactionCode == result.OrderCode);
        savedTx.Should().NotBeNull();
        savedTx!.TransactionStatus.Should().Be("PENDING");
        savedTx.AmountVnd.Should().Be(49000);
    }

    [Fact]
    public async Task CheckoutSubscriptionAsync_WhenUserHasActiveVip_ChainsStartsAtToPreviousEndsAt()
    {
        var (db, service, _, _, _) = CreateTestHarness();

        db.SubscriptionPlans.Add(new SubscriptionPlan
        {
            PlanId = 1,
            AdminId = 1,
            PlanName = "VIP 1 Tháng",
            Price = 49000,
            DurationDays = 30,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        });

        var futureEndsAt = DateTime.UtcNow.AddDays(15);
        db.PaymentTransactions.Add(new PaymentTransaction
        {
            TransactionId = 999,
            PlanId = 1,
            UserId = 100,
            GatewayProvider = "MOMO",
            PartnerTransactionCode = "VL_EXISTING_VIP",
            AmountVnd = 49000,
            TransactionStatus = "SUCCESS",
            SignatureHash = "success_sig",
            CreatedAt = DateTime.UtcNow.AddDays(-15),
            StartsAt = DateTime.UtcNow.AddDays(-15),
            EndsAt = futureEndsAt
        });
        await db.SaveChangesAsync();

        var result = await service.CheckoutSubscriptionAsync(100, UserRoles.Vip, new CheckoutSubscriptionRequest
        {
            PlanId = 1,
            Gateway = "VIETQR"
        });

        var newTx = await db.PaymentTransactions.FirstOrDefaultAsync(t => t.PartnerTransactionCode == result.OrderCode);
        newTx.Should().NotBeNull();
        // Kỳ mới bắt đầu từ hạn cũ
        newTx!.StartsAt.Should().BeCloseTo(futureEndsAt, TimeSpan.FromSeconds(1));
    }

    // ==========================================
    // FN-SUB-03: Nhận kết quả thanh toán (IPN Webhook)
    // ==========================================

    [Fact]
    public async Task HandlePaymentIpnAsync_WhenInvalidSignature_ThrowsUnauthorized()
    {
        var (_, service, _, _, _) = CreateTestHarness();

        var act = () => service.HandlePaymentIpnAsync("momo", new HandlePaymentIpnRequest
        {
            OrderCode = "VL_20261015_001",
            PartnerTransactionId = "MOMO_123",
            Amount = 49000,
            ResultCode = 0,
            Signature = "invalid_signature_hash"
        });

        var ex = await act.Should().ThrowAsync<AppException>();
        ex.Which.StatusCode.Should().Be(401);
        ex.Which.ErrorCode.Should().Be(ErrorCodes.ErrPayInvalidSignature);
    }

    [Fact]
    public async Task HandlePaymentIpnAsync_WhenOrderNotFound_ThrowsNotFound()
    {
        var (_, service, _, _, _) = CreateTestHarness();

        var orderCode = "VL_NON_EXISTENT";
        var sig = ComputeSignature("test_momo_secret_key", orderCode, "TRANS_01", 49000, 0);

        var act = () => service.HandlePaymentIpnAsync("momo", new HandlePaymentIpnRequest
        {
            OrderCode = orderCode,
            PartnerTransactionId = "TRANS_01",
            Amount = 49000,
            ResultCode = 0,
            Signature = sig
        });

        var ex = await act.Should().ThrowAsync<AppException>();
        ex.Which.StatusCode.Should().Be(404);
        ex.Which.ErrorCode.Should().Be(ErrorCodes.ErrPayOrderNotFound);
    }

    [Fact]
    public async Task HandlePaymentIpnAsync_WhenAmountMismatch_MarksFailed_ThrowsUnprocessableEntity()
    {
        var (db, service, _, _, _) = CreateTestHarness();

        var user = new User { UserId = 10, Email = "test@veggielife.vn", FullName = "Test User", Role = UserRoles.Member, AccountStatus = "ACTIVE" };
        var plan = new SubscriptionPlan { PlanId = 1, AdminId = 1, PlanName = "VIP 1 Tháng", Price = 49000, DurationDays = 30, IsActive = true, CreatedAt = DateTime.UtcNow };
        var orderCode = "VL_20261015_MISMATCH";

        db.Users.Add(user);
        db.SubscriptionPlans.Add(plan);
        db.PaymentTransactions.Add(new PaymentTransaction
        {
            TransactionId = 2001,
            PlanId = 1,
            UserId = 10,
            GatewayProvider = "MOMO",
            PartnerTransactionCode = orderCode,
            AmountVnd = 49000,
            TransactionStatus = "PENDING",
            SignatureHash = "init_sig",
            CreatedAt = DateTime.UtcNow,
            StartsAt = DateTime.UtcNow,
            EndsAt = DateTime.UtcNow.AddDays(30)
        });
        await db.SaveChangesAsync();

        var receivedAmount = 20000L; // Lệch tiền
        var sig = ComputeSignature("test_momo_secret_key", orderCode, "MOMO_001", receivedAmount, 0);

        var act = () => service.HandlePaymentIpnAsync("momo", new HandlePaymentIpnRequest
        {
            OrderCode = orderCode,
            PartnerTransactionId = "MOMO_001",
            Amount = receivedAmount,
            ResultCode = 0,
            Signature = sig
        });

        var ex = await act.Should().ThrowAsync<AppException>();
        ex.Which.StatusCode.Should().Be(422);
        ex.Which.ErrorCode.Should().Be(ErrorCodes.ErrPayAmountMismatch);

        // Kiểm tra đơn chuyển sang FAILED
        var tx = await db.PaymentTransactions.FirstAsync(t => t.PartnerTransactionCode == orderCode);
        tx.TransactionStatus.Should().Be("FAILED");
    }

    [Fact]
    public async Task HandlePaymentIpnAsync_WhenOrderPendingExpiredOver5Minutes_MarksExpired_ThrowsConflict()
    {
        var (db, service, _, _, _) = CreateTestHarness();

        var user = new User { UserId = 11, Email = "expired@veggielife.vn", FullName = "Expired User", Role = UserRoles.Member, AccountStatus = "ACTIVE" };
        var plan = new SubscriptionPlan { PlanId = 1, AdminId = 1, PlanName = "VIP 1 Tháng", Price = 49000, DurationDays = 30, IsActive = true, CreatedAt = DateTime.UtcNow };
        var orderCode = "VL_20261015_EXPIRED";

        db.Users.Add(user);
        db.SubscriptionPlans.Add(plan);
        db.PaymentTransactions.Add(new PaymentTransaction
        {
            TransactionId = 2002,
            PlanId = 1,
            UserId = 11,
            GatewayProvider = "VIETQR",
            PartnerTransactionCode = orderCode,
            AmountVnd = 49000,
            TransactionStatus = "PENDING",
            SignatureHash = "init_sig",
            CreatedAt = DateTime.UtcNow.AddMinutes(-10), // Tạo 10 phút trước
            StartsAt = DateTime.UtcNow,
            EndsAt = DateTime.UtcNow.AddDays(30)
        });
        await db.SaveChangesAsync();

        var sig = ComputeSignature("test_vietqr_secret_key", orderCode, "VQR_001", 49000, 0);

        var act = () => service.HandlePaymentIpnAsync("vietqr", new HandlePaymentIpnRequest
        {
            OrderCode = orderCode,
            PartnerTransactionId = "VQR_001",
            Amount = 49000,
            ResultCode = 0,
            Signature = sig
        });

        var ex = await act.Should().ThrowAsync<AppException>();
        ex.Which.StatusCode.Should().Be(409);
        ex.Which.ErrorCode.Should().Be(ErrorCodes.ErrPayOrderExpired);

        var tx = await db.PaymentTransactions.FirstAsync(t => t.PartnerTransactionCode == orderCode);
        tx.TransactionStatus.Should().Be("EXPIRED");
    }

    [Fact]
    public async Task HandlePaymentIpnAsync_WhenResultCodeNonZero_MarksFailed_DoesNotUpgradeRole()
    {
        var (db, service, _, _, _) = CreateTestHarness();

        var user = new User { UserId = 12, Email = "failed@veggielife.vn", FullName = "Failed User", Role = UserRoles.Member, AccountStatus = "ACTIVE" };
        var plan = new SubscriptionPlan { PlanId = 1, AdminId = 1, PlanName = "VIP 1 Tháng", Price = 49000, DurationDays = 30, IsActive = true, CreatedAt = DateTime.UtcNow };
        var orderCode = "VL_20261015_FAIL_CODE";

        db.Users.Add(user);
        db.SubscriptionPlans.Add(plan);
        db.PaymentTransactions.Add(new PaymentTransaction
        {
            TransactionId = 2003,
            PlanId = 1,
            UserId = 12,
            GatewayProvider = "MOMO",
            PartnerTransactionCode = orderCode,
            AmountVnd = 49000,
            TransactionStatus = "PENDING",
            SignatureHash = "init_sig",
            CreatedAt = DateTime.UtcNow,
            StartsAt = DateTime.UtcNow,
            EndsAt = DateTime.UtcNow.AddDays(30)
        });
        await db.SaveChangesAsync();

        var sig = ComputeSignature("test_momo_secret_key", orderCode, "MOMO_CANCEL", 49000, 49); // resultCode != 0

        await service.HandlePaymentIpnAsync("momo", new HandlePaymentIpnRequest
        {
            OrderCode = orderCode,
            PartnerTransactionId = "MOMO_CANCEL",
            Amount = 49000,
            ResultCode = 49,
            Signature = sig
        });

        var tx = await db.PaymentTransactions.FirstAsync(t => t.PartnerTransactionCode == orderCode);
        tx.TransactionStatus.Should().Be("FAILED");

        var updatedUser = await db.Users.FindAsync(12);
        updatedUser!.Role.Should().Be(UserRoles.Member); // Vẫn là MEMBER
    }

    [Fact]
    public async Task HandlePaymentIpnAsync_WhenSuccessful_ActivatesVipAndExtendsPeriod()
    {
        var (db, service, _, redis, _) = CreateTestHarness();

        var user = new User { UserId = 20, Email = "buyer@veggielife.vn", FullName = "Buyer", Role = UserRoles.Member, AccountStatus = "ACTIVE" };
        var plan = new SubscriptionPlan { PlanId = 1, AdminId = 1, PlanName = "VIP 1 Tháng", Price = 49000, DurationDays = 30, IsActive = true, CreatedAt = DateTime.UtcNow };
        var orderCode = "VL_20261015_SUCCESS";

        db.Users.Add(user);
        db.SubscriptionPlans.Add(plan);
        db.PaymentTransactions.Add(new PaymentTransaction
        {
            TransactionId = 3001,
            PlanId = 1,
            UserId = 20,
            GatewayProvider = "MOMO",
            PartnerTransactionCode = orderCode,
            AmountVnd = 49000,
            TransactionStatus = "PENDING",
            SignatureHash = "init_sig",
            CreatedAt = DateTime.UtcNow,
            StartsAt = DateTime.UtcNow,
            EndsAt = DateTime.UtcNow.AddDays(30)
        });
        await db.SaveChangesAsync();

        // Giả lập cache VIP cũ
        await redis.SetAsync("vip_status:20", "old_cached_status");

        var sig = ComputeSignature("test_momo_secret_key", orderCode, "MOMO_OK_123", 49000, 0);

        await service.HandlePaymentIpnAsync("momo", new HandlePaymentIpnRequest
        {
            OrderCode = orderCode,
            PartnerTransactionId = "MOMO_OK_123",
            Amount = 49000,
            ResultCode = 0,
            Signature = sig
        });

        // Kiểm tra giao dịch đổi trạng thái SUCCESS
        var tx = await db.PaymentTransactions.FirstAsync(t => t.PartnerTransactionCode == orderCode);
        tx.TransactionStatus.Should().Be("SUCCESS");
        tx.CompletedAt.Should().NotBeNull();
        tx.SignatureHash.Should().Be(sig);
        // Hết hạn lúc 23:59:59 của ngày kết thúc
        tx.EndsAt.Hour.Should().Be(23);
        tx.EndsAt.Minute.Should().Be(59);
        tx.EndsAt.Second.Should().Be(59);

        // Kiểm tra User đổi role thành VIP
        var updatedUser = await db.Users.FindAsync(20);
        updatedUser!.Role.Should().Be(UserRoles.Vip);

        // Kiểm tra AuditLog được ghi
        var audit = await db.AuditLogs.FirstOrDefaultAsync(a => a.Action == "PAYMENT_SUCCESS" && a.TargetId == "3001");
        audit.Should().NotBeNull();
        audit!.ActorRole.Should().Be("SYSTEM");

        // Kiểm tra cache VIP đã bị xóa để làm mới
        var cacheExists = await redis.ExistsAsync("vip_status:20");
        cacheExists.Should().BeFalse();
    }

    [Fact]
    public async Task HandlePaymentIpnAsync_WhenReceivedTwice_IsIdempotent_DoesNotExtendTwice()
    {
        var (db, service, _, _, _) = CreateTestHarness();

        var user = new User { UserId = 25, Email = "double@veggielife.vn", FullName = "Double Buyer", Role = UserRoles.Member, AccountStatus = "ACTIVE" };
        var plan = new SubscriptionPlan { PlanId = 1, AdminId = 1, PlanName = "VIP 1 Tháng", Price = 49000, DurationDays = 30, IsActive = true, CreatedAt = DateTime.UtcNow };
        var orderCode = "VL_20261015_IDEMPOTENT";

        db.Users.Add(user);
        db.SubscriptionPlans.Add(plan);
        db.PaymentTransactions.Add(new PaymentTransaction
        {
            TransactionId = 3002,
            PlanId = 1,
            UserId = 25,
            GatewayProvider = "VIETQR",
            PartnerTransactionCode = orderCode,
            AmountVnd = 49000,
            TransactionStatus = "PENDING",
            SignatureHash = "init_sig",
            CreatedAt = DateTime.UtcNow,
            StartsAt = DateTime.UtcNow,
            EndsAt = DateTime.UtcNow.AddDays(30)
        });
        await db.SaveChangesAsync();

        var sig = ComputeSignature("test_vietqr_secret_key", orderCode, "VQR_999", 49000, 0);

        var request = new HandlePaymentIpnRequest
        {
            OrderCode = orderCode,
            PartnerTransactionId = "VQR_999",
            Amount = 49000,
            ResultCode = 0,
            Signature = sig
        };

        // Lần 1: Nhận webhook -> Thành công, chuyển sang VIP
        await service.HandlePaymentIpnAsync("vietqr", request);

        var txAfterFirst = await db.PaymentTransactions.FirstAsync(t => t.PartnerTransactionCode == orderCode);
        txAfterFirst.TransactionStatus.Should().Be("SUCCESS");
        var endsAtFirst = txAfterFirst.EndsAt;

        // Lần 2: Cổng gửi lại Webhook trùng lặp
        await service.HandlePaymentIpnAsync("vietqr", request);

        // Kiểm tra hạn EndsAt KHÔNG BỊ CỘNG THÊM
        var txAfterSecond = await db.PaymentTransactions.FirstAsync(t => t.PartnerTransactionCode == orderCode);
        txAfterSecond.EndsAt.Should().Be(endsAtFirst);
        txAfterSecond.TransactionStatus.Should().Be("SUCCESS");

        // Không sinh thêm giao dịch mới
        var totalTx = await db.PaymentTransactions.CountAsync(t => t.UserId == 25);
        totalTx.Should().Be(1);
    }

    // ==========================================
    // FN-SUB-04: Xem trạng thái VIP của tôi
    // ==========================================

    [Fact]
    public async Task GetCurrentUserSubscriptionAsync_WhenUserHasActiveVip_ReturnsIsVipTrue_AndTransactions()
    {
        var (db, service, _, _, _) = CreateTestHarness();

        var now = DateTime.UtcNow;
        var plan = new SubscriptionPlan { PlanId = 1, AdminId = 1, PlanName = "VIP 1 Tháng", Price = 49000, DurationDays = 30, IsActive = true, CreatedAt = now };
        db.SubscriptionPlans.Add(plan);

        var endsAt = now.AddDays(20);
        db.PaymentTransactions.AddRange(
            new PaymentTransaction
            {
                TransactionId = 4001,
                PlanId = 1,
                UserId = 30,
                GatewayProvider = "MOMO",
                PartnerTransactionCode = "VL_OLD",
                AmountVnd = 49000,
                TransactionStatus = "EXPIRED",
                SignatureHash = "sig1",
                CreatedAt = now.AddMonths(-2),
                StartsAt = now.AddMonths(-2),
                EndsAt = now.AddMonths(-1)
            },
            new PaymentTransaction
            {
                TransactionId = 4002,
                PlanId = 1,
                UserId = 30,
                GatewayProvider = "MOMO",
                PartnerTransactionCode = "VL_ACTIVE",
                AmountVnd = 49000,
                TransactionStatus = "SUCCESS",
                SignatureHash = "sig2",
                CreatedAt = now.AddDays(-10),
                StartsAt = now.AddDays(-10),
                EndsAt = endsAt
            }
        );
        await db.SaveChangesAsync();

        var result = await service.GetCurrentUserSubscriptionAsync(30);

        result.Should().NotBeNull();
        result.IsVip.Should().BeTrue();
        result.Plan.Should().NotBeNull();
        result.Plan!.PlanName.Should().Be("VIP 1 Tháng");
        result.EndsAt.Should().Be(endsAt);
        result.Transactions.Should().HaveCount(2);
        // Sắp xếp mới nhất trước
        result.Transactions[0].TransactionId.Should().Be(4002);
        result.Transactions[1].TransactionId.Should().Be(4001);
    }

    [Fact]
    public async Task GetCurrentUserSubscriptionAsync_WhenUserHasNoVip_ReturnsIsVipFalse()
    {
        var (_, service, _, _, _) = CreateTestHarness();

        var result = await service.GetCurrentUserSubscriptionAsync(999);

        result.Should().NotBeNull();
        result.IsVip.Should().BeFalse();
        result.Plan.Should().BeNull();
        result.EndsAt.Should().BeNull();
        result.Transactions.Should().BeEmpty();
    }
}
