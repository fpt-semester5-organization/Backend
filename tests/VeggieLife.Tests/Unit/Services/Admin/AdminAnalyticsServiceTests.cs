using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using VeggieLife.BLL.DTOs.Admin.Analytics;
using VeggieLife.BLL.Services.Admin;
using VeggieLife.BLL.Services.Cache;
using VeggieLife.DAL.Context;
using VeggieLife.DAL.Entities;
using Xunit;

namespace VeggieLife.Tests.Unit.Services.Admin;

public class AdminAnalyticsServiceTests
{
    private readonly AppDbContext _db;
    private readonly Mock<IRedisService> _mockRedis = new();
    private readonly AdminAnalyticsService _service;

    public AdminAnalyticsServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase("AdminAnalyticsServiceTestsDb_" + Guid.NewGuid())
            .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        _db = new AppDbContext(options);
        _service = new AdminAnalyticsService(_db, _mockRedis.Object, NullLogger<AdminAnalyticsService>.Instance);
    }

    [Fact]
    public async Task GetOverviewAsync_CalculatesAllMetricsCorrectly()
    {
        var now = DateTime.UtcNow;

        // 1. Users
        _db.Users.AddRange(
            new User { UserId = 1, Email = "u1@test.com", FullName = "U1", Role = "MEMBER", AccountStatus = "ACTIVE", CreatedAt = now.AddDays(-10) },
            new User { UserId = 2, Email = "u2@test.com", FullName = "U2", Role = "VIP", AccountStatus = "ACTIVE", CreatedAt = now.AddDays(-5) },
            new User { UserId = 3, Email = "u3@test.com", FullName = "U3", Role = "MEMBER", AccountStatus = "SUSPENDED", CreatedAt = now.AddDays(-2) }
        );

        // 2. Content
        _db.Recipes.AddRange(
            new Recipe { RecipeId = 1, Title = "R1", Instructions = "I1", DifficultyLevel = "EASY", Source = "USER", Status = "APPROVED_ACTIVE", CreatedAt = now },
            new Recipe { RecipeId = 2, Title = "R2", Instructions = "I2", DifficultyLevel = "EASY", Source = "USER", Status = "APPROVED_ACTIVE", CreatedAt = now }
        );
        _db.Posts.AddRange(
            new Post { PostId = 1, UserId = 1, Title = "P1", Content = "C1", Visibility = "PUBLIC", ModerationStatus = "APPROVED_ACTIVE", CreatedAt = now }
        );
        _db.Videos.AddRange(
            new Video { VideoId = 1, PostId = 1, Title = "V1", VideoUrl = "http://video.mp4", Status = "APPROVED_ACTIVE", CreatedAt = now }
        );

        // 3. Subscription & Payments
        _db.SubscriptionPlans.Add(new SubscriptionPlan
        {
            PlanId = 1,
            AdminId = 1,
            PlanName = "Gói tháng",
            Price = 49000,
            DurationDays = 30,
            IsActive = true,
            CreatedAt = now
        });

        _db.PaymentTransactions.AddRange(
            // Thành công & còn hạn -> VIP Active + tính doanh thu
            new PaymentTransaction
            {
                TransactionId = 1,
                UserId = 2,
                PlanId = 1,
                GatewayProvider = "MOMO",
                PartnerTransactionCode = "TXN01",
                AmountVnd = 49000,
                TransactionStatus = "SUCCESS",
                SignatureHash = "hash1",
                StartsAt = now.AddDays(-5),
                EndsAt = now.AddDays(25),
                CreatedAt = now.AddDays(-5)
            },
            // Đã hết hạn -> không tính VIP active nhưng có tính doanh thu
            new PaymentTransaction
            {
                TransactionId = 2,
                UserId = 1,
                PlanId = 1,
                GatewayProvider = "VIETQR",
                PartnerTransactionCode = "TXN02",
                AmountVnd = 129000,
                TransactionStatus = "SUCCESS",
                SignatureHash = "hash2",
                StartsAt = now.AddDays(-60),
                EndsAt = now.AddDays(-30),
                CreatedAt = now.AddDays(-60)
            },
            // Thất bại -> không tính
            new PaymentTransaction
            {
                TransactionId = 3,
                UserId = 3,
                PlanId = 1,
                GatewayProvider = "MOMO",
                PartnerTransactionCode = "TXN03",
                AmountVnd = 49000,
                TransactionStatus = "FAILED",
                SignatureHash = "hash3",
                StartsAt = now,
                EndsAt = now.AddDays(30),
                CreatedAt = now
            }
        );

        // 4. Hàng đợi kiểm duyệt
        _db.ViolationReports.AddRange(
            new ViolationReport { ReportId = 1, ViolationType = "SPAM", EvidenceSummary = "Spam", ReportSource = "USER", Status = "PENDING", CreatedAt = now },
            new ViolationReport { ReportId = 2, ViolationType = "OFFENSIVE", EvidenceSummary = "Offensive", ReportSource = "USER", Status = "RESOLVED", CreatedAt = now }
        );

        // 5. Chi phí AI
        _db.AiquotaLogs.AddRange(
            new AiquotaLog { LogId = 1, UserId = 1, FeatureCode = "AI_CHAT", TokensConsumed = 1000, CostEstimateUsd = 0.05m, RequestStatus = "SUCCESS", CreatedAt = now },
            new AiquotaLog { LogId = 2, UserId = 2, FeatureCode = "MEAL_PLAN", TokensConsumed = 2000, CostEstimateUsd = 0.10m, RequestStatus = "SUCCESS", CreatedAt = now }
        );

        await _db.SaveChangesAsync();

        var filter = new AdminAnalyticsOverviewFilterRequest();
        var result = await _service.GetOverviewAsync(filter);

        // Kiểm tra kết quả
        result.Users.Total.Should().Be(3);
        result.Users.ByStatus["ACTIVE"].Should().Be(2);
        result.Users.ByStatus["SUSPENDED"].Should().Be(1);

        result.Content.Recipes.Should().Be(2);
        result.Content.Posts.Should().Be(1);
        result.Content.Videos.Should().Be(1);

        result.Vip.Active.Should().Be(1);
        result.Vip.RevenueVnd.Should().Be(49000 + 129000);

        result.ModerationQueue.Should().Be(1);
        result.AiCostUsd.Should().Be(0.15);

        // Kiểm tra lưu cache 5 phút
        _mockRedis.Verify(r => r.SetAsync(
            "admin_overview",
            It.IsAny<AdminAnalyticsOverviewResponseData>(),
            TimeSpan.FromMinutes(5),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetOverviewAsync_CacheHit_ReturnsFromCacheWithoutQueryingDb()
    {
        var cachedData = new AdminAnalyticsOverviewResponseData
        {
            Users = new UsersOverviewDto { Total = 9999 },
            ModerationQueue = 42
        };

        _mockRedis.Setup(r => r.GetAsync<AdminAnalyticsOverviewResponseData>("admin_overview", It.IsAny<CancellationToken>()))
            .ReturnsAsync(cachedData);

        var filter = new AdminAnalyticsOverviewFilterRequest();
        var result = await _service.GetOverviewAsync(filter);

        result.Should().BeSameAs(cachedData);
        result.Users.Total.Should().Be(9999);
        result.ModerationQueue.Should().Be(42);

        // DB không bị query
        _mockRedis.Verify(r => r.SetAsync(It.IsAny<string>(), It.IsAny<AdminAnalyticsOverviewResponseData>(), It.IsAny<TimeSpan?>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
