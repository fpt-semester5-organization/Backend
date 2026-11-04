using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using VeggieLife.BLL.DTOs.Admin.Llmops;
using VeggieLife.BLL.Services.Admin;
using VeggieLife.BLL.Services.Cache;
using VeggieLife.DAL.Context;
using VeggieLife.DAL.Entities;
using Xunit;

namespace VeggieLife.Tests.Unit.Services.Admin;

public class AdminLlmopsServiceTests
{
    private readonly AppDbContext _db;
    private readonly Mock<IRedisService> _mockRedis = new();
    private readonly IConfiguration _config;
    private readonly AdminLlmopsService _service;

    public AdminLlmopsServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase("AdminLlmopsServiceTestsDb_" + Guid.NewGuid())
            .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        _db = new AppDbContext(options);

        var configValues = new Dictionary<string, string?>
        {
            { "AiBudget:ThrottleUsd", "12.5" },
            { "AiBudget:WarnUsd", "10" }
        };
        _config = new ConfigurationBuilder().AddInMemoryCollection(configValues).Build();

        _service = new AdminLlmopsService(_db, _mockRedis.Object, _config, NullLogger<AdminLlmopsService>.Instance);
    }

    [Fact]
    public async Task GetDashboardAsync_AggregatesTokensAndCostsCorrectly()
    {
        var date1 = new DateTime(2026, 10, 5, 10, 0, 0, DateTimeKind.Utc);
        var date2 = new DateTime(2026, 10, 6, 14, 0, 0, DateTimeKind.Utc);

        _db.AiquotaLogs.AddRange(
            new AiquotaLog { LogId = 1, UserId = 1, FeatureCode = "AI_CHAT", TokensConsumed = 120000, CostEstimateUsd = 4.2m, RequestStatus = "SUCCESS", CreatedAt = date1 },
            new AiquotaLog { LogId = 2, UserId = 2, FeatureCode = "MEAL_PLAN_GEN", TokensConsumed = 30000, CostEstimateUsd = 1.2m, RequestStatus = "SUCCESS", CreatedAt = date1 },
            new AiquotaLog { LogId = 3, UserId = 1, FeatureCode = "AI_CHAT", TokensConsumed = 80000, CostEstimateUsd = 2.75m, RequestStatus = "SUCCESS", CreatedAt = date2 }
        );
        await _db.SaveChangesAsync();

        var filter = new AdminLlmopsFilterRequest
        {
            From = "2026-10-05",
            To = "2026-10-06"
        };

        var result = await _service.GetDashboardAsync(filter);

        result.Should().NotBeNull();
        result.Series.Should().HaveCount(3);

        // Kiểm tra series khớp với mock frontend mong đợi
        var p1 = result.Series.Find(s => s.Date == "2026-10-05" && s.Feature == "AI_CHAT");
        p1.Should().NotBeNull();
        p1!.Tokens.Should().Be(120000);
        p1.CostUsd.Should().Be(4.2m);

        var p2 = result.Series.Find(s => s.Date == "2026-10-05" && s.Feature == "MEAL_PLAN_GEN");
        p2.Should().NotBeNull();
        p2!.Tokens.Should().Be(30000);
        p2.CostUsd.Should().Be(1.2m);

        var p3 = result.Series.Find(s => s.Date == "2026-10-06" && s.Feature == "AI_CHAT");
        p3.Should().NotBeNull();
        p3!.Tokens.Should().Be(80000);
        p3.CostUsd.Should().Be(2.75m);

        // Kiểm tra tổng cả kỳ
        result.Total.Tokens.Should().Be(230000);
        result.Total.CostUsd.Should().Be(8.15m);

        // Kiểm tra DayTotals
        result.DayTotals["2026-10-05"].CostUsd.Should().Be(5.4m);
        result.DayTotals["2026-10-05"].Tokens.Should().Be(150000);
        result.DayTotals["2026-10-06"].CostUsd.Should().Be(2.75m);
        result.DayTotals["2026-10-06"].Tokens.Should().Be(80000);

        // Kiểm tra FeatureTotals
        result.FeatureTotals["AI_CHAT"].CostUsd.Should().Be(6.95m);
        result.FeatureTotals["AI_CHAT"].Tokens.Should().Be(200000);
        result.FeatureTotals["MEAL_PLAN_GEN"].CostUsd.Should().Be(1.2m);
        result.FeatureTotals["MEAL_PLAN_GEN"].Tokens.Should().Be(30000);

        // Kiểm tra Dates và Features
        result.Dates.Should().ContainInOrder("2026-10-05", "2026-10-06");
        result.Features.Should().Contain("AI_CHAT", "MEAL_PLAN_GEN");

        // Kiểm tra MaxDayCost
        result.MaxDayCost.Should().Be(5.4m);
    }

    [Fact]
    public async Task GetDashboardAsync_NormalizesAliasFeatureCodes()
    {
        var testDate = new DateTime(2026, 10, 10, 8, 0, 0, DateTimeKind.Utc);

        _db.AiquotaLogs.AddRange(
            new AiquotaLog { LogId = 10, UserId = 1, FeatureCode = "CHATBOT", TokensConsumed = 1000, CostEstimateUsd = 0.05m, RequestStatus = "SUCCESS", CreatedAt = testDate },
            new AiquotaLog { LogId = 11, UserId = 2, FeatureCode = "FRIDGE_VISION", TokensConsumed = 2000, CostEstimateUsd = 0.08m, RequestStatus = "SUCCESS", CreatedAt = testDate },
            new AiquotaLog { LogId = 12, UserId = 3, FeatureCode = "BARCODE", TokensConsumed = 500, CostEstimateUsd = 0.02m, RequestStatus = "SUCCESS", CreatedAt = testDate },
            new AiquotaLog { LogId = 13, UserId = 4, FeatureCode = "STT", TokensConsumed = 3000, CostEstimateUsd = 0.12m, RequestStatus = "SUCCESS", CreatedAt = testDate },
            new AiquotaLog { LogId = 14, UserId = 5, FeatureCode = "MODERATION", TokensConsumed = 400, CostEstimateUsd = 0.01m, RequestStatus = "SUCCESS", CreatedAt = testDate },
            new AiquotaLog { LogId = 15, UserId = 6, FeatureCode = "MEAL_PLAN", TokensConsumed = 1500, CostEstimateUsd = 0.06m, RequestStatus = "SUCCESS", CreatedAt = testDate }
        );
        await _db.SaveChangesAsync();

        var filter = new AdminLlmopsFilterRequest
        {
            From = "2026-10-10",
            To = "2026-10-10"
        };

        var result = await _service.GetDashboardAsync(filter);

        result.Features.Should().Contain(new[]
        {
            "AI_CHAT",
            "AI_FRIDGE",
            "AI_BARCODE",
            "SUMMARIZE_VIDEO",
            "CONTENT_MODERATION",
            "MEAL_PLAN_GEN"
        });
    }

    [Fact]
    public async Task GetDashboardAsync_CalculatesTodayBudgetAndAlertLevel()
    {
        var today = DateTime.UtcNow.Date.AddHours(5);

        // 10.6 USD trên ngân sách 12.5 USD = 84.8% -> AlertLevel: WARNING
        _db.AiquotaLogs.Add(new AiquotaLog
        {
            LogId = 100,
            UserId = 1,
            FeatureCode = "AI_CHAT",
            TokensConsumed = 200000,
            CostEstimateUsd = 10.6m,
            RequestStatus = "SUCCESS",
            CreatedAt = today
        });
        await _db.SaveChangesAsync();

        var filter = new AdminLlmopsFilterRequest();
        var result = await _service.GetDashboardAsync(filter);

        result.TodayCostUsd.Should().Be(10.6m);
        result.DailyBudgetUsd.Should().Be(12.5m);
        result.UsagePercent.Should().Be(84.8);
        result.AlertLevel.Should().Be("WARNING");
    }

    [Fact]
    public async Task GetDashboardAsync_WhenUsageExceedsBudget_ReturnsThrottledAlert()
    {
        var today = DateTime.UtcNow.Date.AddHours(2);

        // 13.0 USD trên ngân sách 12.5 USD = 104.0% -> AlertLevel: THROTTLED
        _db.AiquotaLogs.Add(new AiquotaLog
        {
            LogId = 200,
            UserId = 1,
            FeatureCode = "AI_CHAT",
            TokensConsumed = 300000,
            CostEstimateUsd = 13.0m,
            RequestStatus = "SUCCESS",
            CreatedAt = today
        });
        await _db.SaveChangesAsync();

        var filter = new AdminLlmopsFilterRequest();
        var result = await _service.GetDashboardAsync(filter);

        result.UsagePercent.Should().Be(104.0);
        result.AlertLevel.Should().Be("THROTTLED");
    }

    [Fact]
    public async Task GetDashboardAsync_UsesRedisCacheWhenAvailable()
    {
        var cachedData = new AdminLlmopsDashboardResponseData
        {
            TodayCostUsd = 5.0m,
            DailyBudgetUsd = 12.5m,
            UsagePercent = 40.0,
            AlertLevel = "NONE",
            Total = new CostCellDto { CostUsd = 5.0m, Tokens = 1000 }
        };

        _mockRedis.Setup(r => r.GetAsync<AdminLlmopsDashboardResponseData>(
            It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(cachedData);

        var filter = new AdminLlmopsFilterRequest { From = "2026-10-01", To = "2026-10-02" };
        var result = await _service.GetDashboardAsync(filter);

        result.Should().BeSameAs(cachedData);
        result.TodayCostUsd.Should().Be(5.0m);
    }

    [Fact]
    public async Task GetDashboardAsync_ComputesPromptCompletionAndTopSpenders()
    {
        var date = new DateTime(2026, 10, 10, 8, 0, 0, DateTimeKind.Utc);
        _db.AiquotaLogs.AddRange(
            new AiquotaLog { LogId = 10, UserId = 1, FeatureCode = "AI_CHAT", TokensConsumed = 10000, CostEstimateUsd = 0.5m, RequestStatus = "SUCCESS", CreatedAt = date },
            new AiquotaLog { LogId = 11, UserId = 2, FeatureCode = "MEAL_PLAN_GEN", TokensConsumed = 5000, CostEstimateUsd = 0.25m, RequestStatus = "SUCCESS", CreatedAt = date }
        );
        await _db.SaveChangesAsync();

        var filter = new AdminLlmopsFilterRequest { From = "2026-10-10", To = "2026-10-10" };
        var result = await _service.GetDashboardAsync(filter);

        result.TotalPromptTokens.Should().BeGreaterThan(0);
        result.TotalCompletionTokens.Should().BeGreaterThan(0);
        (result.TotalPromptTokens + result.TotalCompletionTokens).Should().Be(15000);
        result.TopSpenders.Should().HaveCount(2);
        result.TopSpenders[0].UserId.Should().Be(1);
        result.TopSpenders[0].TotalTokens.Should().Be(10000);
    }

    [Fact]
    public async Task GetLogsAsync_FiltersAndPaginatesCorrectly()
    {
        var date = new DateTime(2026, 10, 12, 10, 0, 0, DateTimeKind.Utc);
        _db.AiquotaLogs.AddRange(
            new AiquotaLog { LogId = 20, UserId = 1, FeatureCode = "AI_CHAT", TokensConsumed = 500, CostEstimateUsd = 0.0001m, RequestStatus = "SUCCESS", ExecutionTimeMs = 300, CreatedAt = date },
            new AiquotaLog { LogId = 21, UserId = 1, FeatureCode = "AI_FRIDGE", TokensConsumed = 800, CostEstimateUsd = 0.0002m, RequestStatus = "ERROR", ErrorMessage = "Vision API timeout", ExecutionTimeMs = 5000, CreatedAt = date.AddMinutes(1) }
        );
        await _db.SaveChangesAsync();

        var filter = new AdminLlmopsLogFilterRequest
        {
            From = "2026-10-12",
            To = "2026-10-12",
            Status = "ERROR",
            Page = 1,
            PageSize = 10
        };

        var result = await _service.GetLogsAsync(filter);

        result.Total.Should().Be(1);
        result.Items.Should().HaveCount(1);
        result.Items[0].RequestStatus.Should().Be("ERROR");
        result.Items[0].ErrorMessage.Should().Contain("timeout");
        result.Items[0].ExecutionTimeMs.Should().Be(5000);
    }
}
