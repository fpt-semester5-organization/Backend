using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using VeggieLife.BLL.Common.Constants;
using VeggieLife.BLL.Common.Exceptions;
using VeggieLife.BLL.DTOs.Admin.Settings;
using VeggieLife.BLL.Services.Cache;
using VeggieLife.BLL.Services.Settings;
using VeggieLife.DAL.Context;
using VeggieLife.DAL.Entities;
using Xunit;

namespace VeggieLife.Tests.Unit.Services.Settings;

public class SystemSettingServiceTests : IDisposable
{
    private readonly AppDbContext _db;
    private readonly Mock<IRedisService> _mockRedis = new();
    private readonly SystemSettingService _service;
    private const int AdminId = 1;

    public SystemSettingServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase("SystemSettingServiceTestsDb_" + Guid.NewGuid())
            .Options;

        _db = new AppDbContext(options);
        _service = new SystemSettingService(_db, _mockRedis.Object, NullLogger<SystemSettingService>.Instance);

        SeedTestData();
    }

    public void Dispose()
    {
        _db.Database.EnsureDeleted();
        _db.Dispose();
    }

    private void SeedTestData()
    {
        _db.SystemSettings.AddRange(
            new SystemSetting
            {
                SettingKey = "MEAL_SWAP_LIMIT_DRAFT",
                SettingValue = "21",
                DefaultValue = "21",
                DataType = "INT",
                Category = "MEAL_PLAN",
                Description = "Hạn mức đổi món giai đoạn DRAFT",
                UpdatedAt = DateTime.UtcNow
            },
            new SystemSetting
            {
                SettingKey = "MEAL_SWAP_LIMIT_ACTIVE",
                SettingValue = "21",
                DefaultValue = "21",
                DataType = "INT",
                Category = "MEAL_PLAN",
                Description = "Hạn mức đổi món giai đoạn ACTIVE",
                UpdatedAt = DateTime.UtcNow
            },
            new SystemSetting
            {
                SettingKey = "LLMOPS_DAILY_BUDGET_USD",
                SettingValue = "12.5",
                DefaultValue = "12.5",
                DataType = "DECIMAL",
                Category = "LLMOPS",
                Description = "Ngân sách AI tối đa ngày",
                UpdatedAt = DateTime.UtcNow
            },
            new SystemSetting
            {
                SettingKey = "FEATURE_COMMUNITY_ENABLED",
                SettingValue = "true",
                DefaultValue = "true",
                DataType = "BOOLEAN",
                Category = "COMMUNITY",
                Description = "Bật tắt tính năng cộng đồng",
                UpdatedAt = DateTime.UtcNow
            },
            new SystemSetting
            {
                SettingKey = "MEAL_SWAP_CALORIE_TOLERANCE_PERCENT",
                SettingValue = "15",
                DefaultValue = "15",
                DataType = "INT",
                Category = "MEAL_PLAN",
                Description = "Dung sai calo tối đa khi đổi món (%)",
                UpdatedAt = DateTime.UtcNow
            }
        );
        _db.SaveChanges();
    }

    [Fact]
    public async Task GetIntSettingAsync_WhenInCache_ReturnsCachedValueWithoutQueryingDb()
    {
        // Arrange
        _mockRedis.Setup(r => r.GetAsync<string>("sys_setting:MEAL_SWAP_LIMIT_DRAFT", It.IsAny<CancellationToken>()))
            .ReturnsAsync("30");

        // Act
        var result = await _service.GetIntSettingAsync("MEAL_SWAP_LIMIT_DRAFT", 21);

        // Assert
        result.Should().Be(30);
    }

    [Fact]
    public async Task GetIntSettingAsync_WhenCacheMiss_ReadsFromDbAndPopulatesCache()
    {
        // Arrange
        _mockRedis.Setup(r => r.GetAsync<string>("sys_setting:MEAL_SWAP_LIMIT_DRAFT", It.IsAny<CancellationToken>()))
            .ReturnsAsync((string?)null);

        // Act
        var result = await _service.GetIntSettingAsync("MEAL_SWAP_LIMIT_DRAFT", 99);

        // Assert
        result.Should().Be(21);
        _mockRedis.Verify(r => r.SetAsync("sys_setting:MEAL_SWAP_LIMIT_DRAFT", "21", It.IsAny<TimeSpan?>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetIntSettingAsync_WhenNotInDbNorCache_ReturnsDefaultFallback()
    {
        // Arrange
        _mockRedis.Setup(r => r.GetAsync<string>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string?)null);

        // Act
        var result = await _service.GetIntSettingAsync("UNKNOWN_SETTING_KEY", 42);

        // Assert
        result.Should().Be(42);
    }

    [Fact]
    public async Task GetDecimalSettingAsync_ParsesCorrectly()
    {
        // Arrange
        _mockRedis.Setup(r => r.GetAsync<string>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string?)null);

        // Act
        var result = await _service.GetDecimalSettingAsync("LLMOPS_DAILY_BUDGET_USD", 10.0m);

        // Assert
        result.Should().Be(12.5m);
    }

    [Fact]
    public async Task GetBoolSettingAsync_ParsesCorrectly()
    {
        // Arrange
        _mockRedis.Setup(r => r.GetAsync<string>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string?)null);

        // Act
        var result = await _service.GetBoolSettingAsync("FEATURE_COMMUNITY_ENABLED", false);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task GetAllSettingsAsync_WithCategoryFilter_ReturnsFilteredSettings()
    {
        // Act
        var mealPlanSettings = await _service.GetAllSettingsAsync("MEAL_PLAN");

        // Assert
        mealPlanSettings.Should().HaveCount(3);
        mealPlanSettings.All(s => s.Category == "MEAL_PLAN").Should().BeTrue();
    }

    [Fact]
    public async Task GetSettingByKeyAsync_WhenKeyNotFound_ThrowsNotFoundException()
    {
        // Act
        var act = async () => await _service.GetSettingByKeyAsync("NON_EXISTING_KEY");

        // Assert
        await act.Should().ThrowAsync<AppException>()
            .Where(e => e.ErrorCode == ErrorCodes.ErrAdmNotFound);
    }

    [Fact]
    public async Task UpdateSettingAsync_WhenValidInt_UpdatesDbLogsAuditAndUpdatesCache()
    {
        // Arrange
        var request = new UpdateSystemSettingRequest { SettingValue = "15" };

        // Act
        var result = await _service.UpdateSettingAsync("MEAL_SWAP_LIMIT_DRAFT", request, AdminId);

        // Assert
        result.SettingValue.Should().Be("15");

        var dbSetting = await _db.SystemSettings.FindAsync("MEAL_SWAP_LIMIT_DRAFT");
        dbSetting!.SettingValue.Should().Be("15");
        dbSetting.UpdatedBy.Should().Be(AdminId);

        // Kiểm tra AuditLog
        var auditLog = await _db.AuditLogs.FirstOrDefaultAsync(a => a.TargetId == "MEAL_SWAP_LIMIT_DRAFT");
        auditLog.Should().NotBeNull();
        auditLog!.Action.Should().Be("UPDATE_SYSTEM_SETTING");
        auditLog.OldValue.Should().Contain("21");
        auditLog.NewValue.Should().Contain("15");
        auditLog.ActorUserId.Should().Be(AdminId);

        // Kiểm tra Redis cache cập nhật
        _mockRedis.Verify(r => r.SetAsync("sys_setting:MEAL_SWAP_LIMIT_DRAFT", "15", It.IsAny<TimeSpan?>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateSettingAsync_WhenInvalidInt_ThrowsBadRequestException()
    {
        // Arrange
        var request = new UpdateSystemSettingRequest { SettingValue = "abc_invalid_number" };

        // Act
        var act = async () => await _service.UpdateSettingAsync("MEAL_SWAP_LIMIT_DRAFT", request, AdminId);

        // Assert
        await act.Should().ThrowAsync<AppException>()
            .Where(e => e.ErrorCode == ErrorCodes.ErrValidation);
    }

    [Fact]
    public async Task UpdateSettingAsync_WhenPercentOver100_ThrowsBadRequestException()
    {
        // Arrange
        var request = new UpdateSystemSettingRequest { SettingValue = "150" };

        // Act
        var act = async () => await _service.UpdateSettingAsync("MEAL_SWAP_CALORIE_TOLERANCE_PERCENT", request, AdminId);

        // Assert
        await act.Should().ThrowAsync<AppException>()
            .Where(e => e.ErrorCode == ErrorCodes.ErrValidation && e.Message.Contains("100%"));
    }

    [Fact]
    public async Task ResetToDefaultAsync_ResetsToDefaultValueAndLogsAudit()
    {
        // Arrange: Cập nhật thành 10 trước
        await _service.UpdateSettingAsync("MEAL_SWAP_LIMIT_DRAFT", new UpdateSystemSettingRequest { SettingValue = "10" }, AdminId);

        // Act
        var result = await _service.ResetToDefaultAsync("MEAL_SWAP_LIMIT_DRAFT", AdminId);

        // Assert
        result.SettingValue.Should().Be("21");

        var dbSetting = await _db.SystemSettings.FindAsync("MEAL_SWAP_LIMIT_DRAFT");
        dbSetting!.SettingValue.Should().Be("21");

        var resetAudit = await _db.AuditLogs.FirstOrDefaultAsync(a => a.Action == "RESET_SYSTEM_SETTING" && a.TargetId == "MEAL_SWAP_LIMIT_DRAFT");
        resetAudit.Should().NotBeNull();
        resetAudit!.NewValue.Should().Contain("21");
    }
}
