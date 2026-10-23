using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using VeggieLife.BLL.Common.Constants;
using VeggieLife.BLL.Common.Exceptions;
using VeggieLife.BLL.DTOs.Admin.DietarySchools;
using VeggieLife.BLL.Services.Admin;
using VeggieLife.BLL.Services.Cache;
using VeggieLife.DAL.Context;
using VeggieLife.DAL.Entities;
using Xunit;

namespace VeggieLife.Tests.Unit.Services.Admin;

/// <summary>
/// Bộ kiểm thử đơn vị cho AdminDietarySchoolService (FN-ADM-10, BR-ADM-MAS-001, D109).
/// </summary>
public class AdminDietarySchoolServiceTests : IDisposable
{
    private readonly AppDbContext _db;
    private readonly Mock<IRedisService> _mockRedis = new();
    private readonly AdminDietarySchoolService _service;
    private const int AdminId = 999;

    public AdminDietarySchoolServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase("AdminDietarySchoolServiceTestsDb_" + Guid.NewGuid())
            .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        _db = new AppDbContext(options);
        _service = new AdminDietarySchoolService(
            _db,
            _mockRedis.Object,
            NullLogger<AdminDietarySchoolService>.Instance);

        SeedTestData();
    }

    private void SeedTestData()
    {
        _db.DietarySchools.AddRange(
            new DietarySchool
            {
                DietarySchoolId = 1,
                SchoolCode = "VEGAN",
                SchoolName = "Thuần chay (Vegan)",
                ForbiddenFoodRules = "{\"forbiddenFoodGroups\":[\"EGG\",\"DAIRY\",\"POULTRY\",\"SEAFOOD\"],\"forbidAllium\":false,\"description\":\"Kiêng hoàn toàn đạm động vật.\"}",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new DietarySchool
            {
                DietarySchoolId = 2,
                SchoolCode = "OVO",
                SchoolName = "Chay có trứng (Ovo)",
                ForbiddenFoodRules = "{\"forbiddenFoodGroups\":[\"DAIRY\",\"POULTRY\",\"SEAFOOD\"],\"forbidAllium\":false,\"description\":\"Cho phép trứng.\"}",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new DietarySchool
            {
                DietarySchoolId = 3,
                SchoolCode = "LACTO",
                SchoolName = "Chay có sữa (Lacto)",
                ForbiddenFoodRules = "{\"forbiddenFoodGroups\":[\"EGG\",\"POULTRY\",\"SEAFOOD\"],\"forbidAllium\":false,\"description\":\"Cho phép sữa.\"}",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new DietarySchool
            {
                DietarySchoolId = 4,
                SchoolCode = "LACTO_OVO",
                SchoolName = "Chay trứng sữa (Lacto-Ovo)",
                ForbiddenFoodRules = "{\"forbiddenFoodGroups\":[\"POULTRY\",\"SEAFOOD\"],\"forbidAllium\":false,\"description\":\"Cho phép trứng và sữa.\"}",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new DietarySchool
            {
                DietarySchoolId = 5,
                SchoolCode = "BUDDHIST",
                SchoolName = "Chay Phật giáo",
                ForbiddenFoodRules = "{\"forbiddenFoodGroups\":[\"EGG\",\"DAIRY\",\"POULTRY\",\"SEAFOOD\"],\"forbidAllium\":true,\"description\":\"Cấm ngũ vị tân.\"}",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new DietarySchool
            {
                DietarySchoolId = 6,
                SchoolCode = "POLLOTARIAN",
                SchoolName = "Ăn chay bán phần - Gia cầm",
                ForbiddenFoodRules = "{\"forbiddenFoodGroups\":[\"SEAFOOD\"],\"forbidAllium\":false,\"description\":\"Cho phép gia cầm.\"}",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new DietarySchool
            {
                DietarySchoolId = 7,
                SchoolCode = "PESCATARIAN",
                SchoolName = "Ăn chay bán phần - Hải sản",
                ForbiddenFoodRules = "{\"forbiddenFoodGroups\":[\"POULTRY\"],\"forbidAllium\":false,\"description\":\"Cho phép hải sản.\"}",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            }
        );
        _db.SaveChanges();
    }

    [Fact]
    public async Task GetDietarySchoolsAsync_ShouldReturnAll7SchoolsWithParsedRules()
    {
        // Act
        var result = await _service.GetDietarySchoolsAsync();

        // Assert
        result.Should().HaveCount(7);
        var vegan = result.First(s => s.SchoolCode == "VEGAN");
        vegan.SchoolName.Should().Be("Thuần chay (Vegan)");
        vegan.ForbiddenFoodRules.ForbiddenFoodGroups.Should().BeEquivalentTo(new[] { "EGG", "DAIRY", "POULTRY", "SEAFOOD" });
        vegan.ForbiddenFoodRules.ForbidAllium.Should().BeFalse();

        var buddhist = result.First(s => s.SchoolCode == "BUDDHIST");
        buddhist.ForbiddenFoodRules.ForbidAllium.Should().BeTrue();
    }

    [Fact]
    public async Task UpdateDietarySchoolAsync_ValidRequest_ShouldUpdateAndRecordAuditAndInvalidateCache()
    {
        // Arrange
        var request = new UpdateAdminDietarySchoolRequest
        {
            SchoolName = "Thuần chay Quốc tế (Vegan Standard)",
            ForbiddenFoodRules = new ForbiddenFoodRulesDto
            {
                ForbiddenFoodGroups = new List<string> { "EGG", "DAIRY", "POULTRY", "SEAFOOD" },
                ForbidAllium = false,
                Description = "Quy tắc thuần chay nghiêm ngặt cập nhật 2026."
            }
        };

        // Act
        var response = await _service.UpdateDietarySchoolAsync(1, request, AdminId);

        // Assert
        response.DietarySchoolId.Should().Be(1);

        var updated = await _db.DietarySchools.FindAsync(1);
        updated!.SchoolName.Should().Be("Thuần chay Quốc tế (Vegan Standard)");
        updated.SchoolCode.Should().Be("VEGAN"); // SchoolCode không được thay đổi

        using var doc = JsonDocument.Parse(updated.ForbiddenFoodRules);
        doc.RootElement.GetProperty("description").GetString().Should().Be("Quy tắc thuần chay nghiêm ngặt cập nhật 2026.");

        // AuditLog
        var audit = await _db.AuditLogs.FirstOrDefaultAsync(a => a.Action == "UPDATE_DIETARY_SCHOOL" && a.TargetId == "1");
        audit.Should().NotBeNull();
        audit!.ActorUserId.Should().Be(AdminId);
        audit.TargetTable.Should().Be("DietarySchools");

        // Cache invalidated
        _mockRedis.Verify(r => r.RemoveAsync("dietary_schools_cache", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(8)]
    [InlineData(-1)]
    public async Task UpdateDietarySchoolAsync_InvalidId_ShouldThrowBadRequest(int invalidId)
    {
        // Arrange
        var request = new UpdateAdminDietarySchoolRequest
        {
            SchoolName = "Tên mới"
        };

        // Act & Assert
        var act = () => _service.UpdateDietarySchoolAsync(invalidId, request, AdminId);
        var ex = await act.Should().ThrowAsync<AppException>();
        ex.Which.StatusCode.Should().Be(400);
        ex.Which.ErrorCode.Should().Be(ErrorCodes.ErrValidation);
    }

    [Fact]
    public async Task UpdateDietarySchoolAsync_NonExistentSchool_ShouldThrowNotFound()
    {
        // Arrange: xóa trường phái 7 khỏi db giả lập
        var school7 = await _db.DietarySchools.FindAsync(7);
        _db.DietarySchools.Remove(school7!);
        await _db.SaveChangesAsync();

        var request = new UpdateAdminDietarySchoolRequest
        {
            SchoolName = "Tên mới"
        };

        // Act & Assert
        var act = () => _service.UpdateDietarySchoolAsync(7, request, AdminId);
        var ex = await act.Should().ThrowAsync<AppException>();
        ex.Which.StatusCode.Should().Be(404);
        ex.Which.ErrorCode.Should().Be(ErrorCodes.ErrAdmNotFound);
    }

    [Fact]
    public async Task UpdateDietarySchoolAsync_InvalidForbiddenGroup_ShouldThrowBadRequest()
    {
        // Arrange: nhóm cấm chứa PORK (ngoài EGG, DAIRY, POULTRY, SEAFOOD)
        var request = new UpdateAdminDietarySchoolRequest
        {
            ForbiddenFoodRules = new ForbiddenFoodRulesDto
            {
                ForbiddenFoodGroups = new List<string> { "PORK", "EGG" },
                ForbidAllium = false
            }
        };

        // Act & Assert
        var act = () => _service.UpdateDietarySchoolAsync(1, request, AdminId);
        var ex = await act.Should().ThrowAsync<AppException>();
        ex.Which.StatusCode.Should().Be(400);
        ex.Which.ErrorCode.Should().Be(ErrorCodes.ErrValidation);
    }

    [Fact]
    public async Task UpdateDietarySchoolAsync_EmptyRequest_ShouldThrowBadRequest()
    {
        // Arrange: không cung cấp trường nào để sửa
        var request = new UpdateAdminDietarySchoolRequest();

        // Act & Assert
        var act = () => _service.UpdateDietarySchoolAsync(1, request, AdminId);
        var ex = await act.Should().ThrowAsync<AppException>();
        ex.Which.StatusCode.Should().Be(400);
        ex.Which.ErrorCode.Should().Be(ErrorCodes.ErrValidation);
    }

    public void Dispose()
    {
        _db.Database.EnsureDeleted();
        _db.Dispose();
    }
}
