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
using VeggieLife.BLL.DTOs.Admin.Venues;
using VeggieLife.BLL.Services.Admin;
using VeggieLife.BLL.Services.Cache;
using VeggieLife.DAL.Context;
using VeggieLife.DAL.Entities;
using Xunit;

namespace VeggieLife.Tests.Unit.Services.Admin;

/// <summary>
/// Bộ kiểm thử đơn vị cho AdminVenueService (FN-ADM-14).
/// Kiểm tra:
/// 1. Lọc danh sách quán chay theo IsActive, Source, Keyword, phân trang.
/// 2. Cập nhật trạng thái ẩn/hiện quán chay (BR-ADM-MAP-001): kiểm tra 404, ghi audit log, xóa cache.
/// 3. Xếp job đồng bộ quán chay từ nguồn bên ngoài: tạo jobId, ghi audit log, xóa cache.
/// </summary>
public class AdminVenueServiceTests : IDisposable
{
    private readonly AppDbContext _db;
    private readonly Mock<IRedisService> _mockRedis = new();
    private readonly AdminVenueService _service;
    private const int AdminId = 999;

    public AdminVenueServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase("AdminVenueServiceTestsDb_" + Guid.NewGuid())
            .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        _db = new AppDbContext(options);
        _service = new AdminVenueService(
            _db,
            _mockRedis.Object,
            NullLogger<AdminVenueService>.Instance);

        SeedTestData();
    }

    public void Dispose()
    {
        _db.Database.EnsureDeleted();
        _db.Dispose();
    }

    private void SeedTestData()
    {
        _db.VeganVenues.AddRange(
            new VeganVenue
            {
                VenueId = 1,
                ExternalSource = "HAPPYCOW",
                ExternalPlaceId = "hc_001",
                VenueName = "Nhà hàng Chay Thiền Tâm",
                Address = "123 Nam Kỳ Khởi Nghĩa, Q.3, TP.HCM",
                Latitude = 10.78m,
                Longitude = 106.69m,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            },
            new VeganVenue
            {
                VenueId = 2,
                ExternalSource = "GOOGLE_PLACES",
                ExternalPlaceId = "gp_002",
                VenueName = "Quán Cơm Chay Bồ Đề",
                Address = "456 Lê Văn Việt, TP.Thủ Đức",
                Latitude = 10.84m,
                Longitude = 106.78m,
                IsActive = false,
                CreatedAt = DateTime.UtcNow
            }
        );

        _db.SaveChanges();
    }

    #region 1. GetVenuesAsync

    [Fact]
    public async Task GetVenuesAsync_NoFilter_ReturnsAllWithPagination()
    {
        var request = new AdminVenueFilterRequest
        {
            Page = 1,
            Size = 10
        };

        var (items, meta) = await _service.GetVenuesAsync(request);

        meta.Total.Should().Be(2);
        items.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetVenuesAsync_FilterByIsActive_ReturnsOnlyActive()
    {
        var request = new AdminVenueFilterRequest
        {
            IsActive = true,
            Page = 1,
            Size = 10
        };

        var (items, meta) = await _service.GetVenuesAsync(request);

        meta.Total.Should().Be(1);
        items.Should().HaveCount(1);
        items[0].VenueId.Should().Be(1);
        items[0].IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task GetVenuesAsync_FilterBySource_ReturnsMatchingSource()
    {
        var request = new AdminVenueFilterRequest
        {
            Source = "GOOGLE_PLACES",
            Page = 1,
            Size = 10
        };

        var (items, meta) = await _service.GetVenuesAsync(request);

        meta.Total.Should().Be(1);
        items.Should().HaveCount(1);
        items[0].VenueId.Should().Be(2);
        items[0].Source.Should().Be("GOOGLE_PLACES");
    }

    [Fact]
    public async Task GetVenuesAsync_FilterByKeyword_ReturnsMatchingNameOrAddress()
    {
        var request = new AdminVenueFilterRequest
        {
            Keyword = "Lê Văn Việt",
            Page = 1,
            Size = 10
        };

        var (items, meta) = await _service.GetVenuesAsync(request);

        meta.Total.Should().Be(1);
        items.Should().HaveCount(1);
        items[0].Name.Should().Be("Quán Cơm Chay Bồ Đề");
    }

    #endregion

    #region 2. UpdateVenueStatusAsync

    [Fact]
    public async Task UpdateVenueStatusAsync_VenueNotFound_ThrowsNotFound()
    {
        var request = new UpdateAdminVenueRequest
        {
            IsActive = false
        };

        var act = async () => await _service.UpdateVenueStatusAsync(99999, request, AdminId);

        var ex = await act.Should().ThrowAsync<AppException>();
        ex.Which.StatusCode.Should().Be(404);
        ex.Which.ErrorCode.Should().Be(ErrorCodes.ErrAdmNotFound);
    }

    [Fact]
    public async Task UpdateVenueStatusAsync_ValidVenue_UpdatesStatus_WritesAuditLog_AndInvalidatesCache()
    {
        var request = new UpdateAdminVenueRequest
        {
            IsActive = false
        };

        var response = await _service.UpdateVenueStatusAsync(1, request, AdminId);

        response.VenueId.Should().Be(1);
        response.IsActive.Should().BeFalse();

        var venue = await _db.VeganVenues.FindAsync(1);
        venue!.IsActive.Should().BeFalse();

        var auditLog = await _db.AuditLogs.FirstOrDefaultAsync(a => a.TargetTable == "VeganVenues" && a.TargetId == "1");
        auditLog.Should().NotBeNull();
        auditLog!.ActorUserId.Should().Be(AdminId);
        auditLog.Action.Should().Be("UPDATE_VENUE_STATUS");

        _mockRedis.Verify(
            r => r.RemoveAsync("venue_detail:1", It.IsAny<CancellationToken>()),
            Times.Once);

        _mockRedis.Verify(
            r => r.RemoveAsync("venue_geo:*", It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion

    #region 3. SyncVenuesAsync

    [Fact]
    public async Task SyncVenuesAsync_ValidProvince_QueuesJob_WritesAuditLog_AndInvalidatesGeoCache()
    {
        var request = new SyncAdminVenuesRequest
        {
            Province = "Hồ Chí Minh"
        };

        var response = await _service.SyncVenuesAsync(request, AdminId);

        response.JobId.Should().StartWith("sync_");

        var auditLog = await _db.AuditLogs.FirstOrDefaultAsync(a => a.Action == "SYNC_VENUES");
        auditLog.Should().NotBeNull();
        auditLog!.TargetId.Should().Be(response.JobId);
        auditLog.Note.Should().Contain("Hồ Chí Minh");

        _mockRedis.Verify(
            r => r.RemoveAsync("venue_geo:*", It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion
}
