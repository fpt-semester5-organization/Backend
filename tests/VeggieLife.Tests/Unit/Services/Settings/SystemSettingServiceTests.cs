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
                SettingKey = "LEGAL_DOCUMENTS",
                SettingValue = "{\"user_agreement\":\"https://res.cloudinary.com/swrsuenp/image/upload/veggielife/legal/116_2025_QH15_666020.jpg\"}",
                DefaultValue = "{\"user_agreement\":\"https://res.cloudinary.com/swrsuenp/image/upload/veggielife/legal/116_2025_QH15_666020.jpg\"}",
                DataType = "STRING",
                Category = "GENERAL",
                Description = "Tài liệu pháp lý VeggieLife",
                LegalDocumentsUrl = "{\"user_agreement\":\"https://res.cloudinary.com/swrsuenp/image/upload/veggielife/legal/116_2025_QH15_666020.jpg\"}",
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
    [Fact]
    public async Task GetSettingByKeyAsync_WhenLegalDocumentsSetting_ReturnsLegalDocumentsUrl()
    {
        // Act
        var result = await _service.GetSettingByKeyAsync("LEGAL_DOCUMENTS");

        // Assert
        result.Should().NotBeNull();
        result.SettingKey.Should().Be("LEGAL_DOCUMENTS");
        result.LegalDocumentsUrl.Should().Contain("116_2025_QH15_666020.jpg");
        result.SettingValue.Should().Contain("116_2025_QH15_666020.jpg");
    }

    [Fact]
    public async Task UpdateSettingAsync_WhenUpdatingLegalDocumentsUrl_UpdatesBothSettingValueAndLegalDocumentsUrl()
    {
        // Arrange
        const string newUrl = "https://res.cloudinary.com/swrsuenp/image/upload/veggielife/legal/13_2023_ND-CP_465185.jpg";
        var request = new UpdateSystemSettingRequest
        {
            SettingValue = newUrl,
            LegalDocumentsUrl = newUrl
        };

        // Act
        var result = await _service.UpdateSettingAsync("LEGAL_DOCUMENTS", request, AdminId);

        // Assert
        result.SettingValue.Should().Be(newUrl);
        result.LegalDocumentsUrl.Should().Be(newUrl);

        var dbSetting = await _db.SystemSettings.FindAsync("LEGAL_DOCUMENTS");
        dbSetting!.SettingValue.Should().Be(newUrl);
        dbSetting.LegalDocumentsUrl.Should().Be(newUrl);
    }

    [Fact]
    public async Task GetLegalDocumentsAsync_WhenInCache_ReturnsCachedResult()
    {
        // Arrange
        var cachedData = new VeggieLife.BLL.DTOs.Auth.LegalDocumentsResponseData
        {
            UserAgreementUrl = "https://custom.cloudinary.com/user_agreement.png",
            PrivacyPolicyUrl = "https://custom.cloudinary.com/privacy.png",
            TermsOfServiceUrl = "https://custom.cloudinary.com/terms.png",
            VeganDisclaimerUrl = "https://custom.cloudinary.com/vegan.png",
            MedicalDisclaimer = "Custom medical disclaimer",
            UpdatedAt = DateTime.UtcNow
        };

        _mockRedis.Setup(r => r.GetAsync<VeggieLife.BLL.DTOs.Auth.LegalDocumentsResponseData>("sys_setting:legal_documents:all", It.IsAny<CancellationToken>()))
            .ReturnsAsync(cachedData);

        // Act
        var result = await _service.GetLegalDocumentsAsync();

        // Assert
        result.Should().NotBeNull();
        result.UserAgreementUrl.Should().Be("https://custom.cloudinary.com/user_agreement.png");
        result.PrivacyPolicyUrl.Should().Be("https://custom.cloudinary.com/privacy.png");
        result.MedicalDisclaimer.Should().Be("Custom medical disclaimer");
    }

    [Fact]
    public async Task GetLegalDocumentsAsync_WhenCacheMiss_ReadsFromDbAndPopulatesCache()
    {
        // Arrange
        _mockRedis.Setup(r => r.GetAsync<VeggieLife.BLL.DTOs.Auth.LegalDocumentsResponseData>("sys_setting:legal_documents:all", It.IsAny<CancellationToken>()))
            .ReturnsAsync((VeggieLife.BLL.DTOs.Auth.LegalDocumentsResponseData?)null);

        // Act
        var result = await _service.GetLegalDocumentsAsync();

        // Assert
        result.Should().NotBeNull();
        result.UserAgreementUrl.Should().Contain("116_2025_QH15_666020.jpg");
        result.PrivacyPolicyUrl.Should().Contain("13_2023_ND-CP_465185.jpg");
        result.MedicalDisclaimer.Should().Contain("Khuyến nghị mang tính chất tham khảo dinh dưỡng");

        _mockRedis.Verify(r => r.SetAsync("sys_setting:legal_documents:all", It.IsAny<VeggieLife.BLL.DTOs.Auth.LegalDocumentsResponseData>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateSettingAsync_WhenUpdatingLegalSetting_InvalidatesLegalDocumentsCache()
    {
        // Arrange
        var request = new UpdateSystemSettingRequest
        {
            SettingValue = "https://example.com/new-user-agreement.png"
        };

        // Act
        await _service.UpdateSettingAsync("LEGAL_DOCUMENTS", request, AdminId);

        // Assert
        _mockRedis.Verify(r => r.RemoveAsync("sys_setting:legal_documents:all", It.IsAny<CancellationToken>()), Times.Once);
    }
}
