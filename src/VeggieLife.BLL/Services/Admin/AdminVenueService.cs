using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using VeggieLife.BLL.Common.Constants;
using VeggieLife.BLL.Common.Exceptions;
using VeggieLife.BLL.Common.Responses;
using VeggieLife.BLL.DTOs.Admin.Venues;
using VeggieLife.BLL.Services.Cache;
using VeggieLife.BLL.Validators.Admin;
using VeggieLife.DAL.Context;
using VeggieLife.DAL.Entities;

namespace VeggieLife.BLL.Services.Admin;

/// <summary>
/// Triển khai dịch vụ quản trị quán chay và đồng bộ nguồn dữ liệu bên ngoài (FN-ADM-14).
/// </summary>
public class AdminVenueService : IAdminVenueService
{
    private readonly AppDbContext _db;
    private readonly IRedisService _redis;
    private readonly ILogger<AdminVenueService> _logger;

    public AdminVenueService(
        AppDbContext db,
        IRedisService redis,
        ILogger<AdminVenueService> logger)
    {
        _db = db;
        _redis = redis;
        _logger = logger;
    }

    /// <summary>
    /// Lấy danh sách quán chay theo bộ lọc và phân trang dành cho Quản trị viên (FN-ADM-14).
    /// </summary>
    public async Task<(IReadOnlyList<AdminVenueListItemDto> Items, PagedMeta Meta)> GetVenuesAsync(
        AdminVenueFilterRequest request,
        CancellationToken ct = default)
    {
        AdminVenueValidator.ValidateFilter(request);

        var query = _db.VeganVenues.AsNoTracking();

        if (request.IsActive.HasValue)
        {
            query = query.Where(v => v.IsActive == request.IsActive.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.Source))
        {
            var src = request.Source.Trim().ToUpperInvariant();
            query = query.Where(v => v.ExternalSource == src);
        }

        if (!string.IsNullOrWhiteSpace(request.Keyword))
        {
            var kw = request.Keyword.Trim();
            query = query.Where(v => v.VenueName.Contains(kw) || v.Address.Contains(kw));
        }

        var total = await query.CountAsync(ct);

        var entities = await query
            .OrderByDescending(v => v.VenueId)
            .Skip((request.Page - 1) * request.Size)
            .Take(request.Size)
            .ToListAsync(ct);

        var items = entities.Select(v => new AdminVenueListItemDto
        {
            VenueId = v.VenueId,
            Name = v.VenueName,
            Source = v.ExternalSource,
            IsActive = v.IsActive
        }).ToList();

        var meta = new PagedMeta(request.Page, request.Size, total);
        return (items, meta);
    }

    /// <summary>
    /// Cập nhật trạng thái ẩn/hiện quán chay (BR-ADM-MAP-001, FN-ADM-14).
    /// </summary>
    public async Task<UpdateAdminVenueResponseData> UpdateVenueStatusAsync(
        int id,
        UpdateAdminVenueRequest request,
        int adminId,
        CancellationToken ct = default)
    {
        AdminVenueValidator.ValidateUpdate(id, request);

        var venue = await _db.VeganVenues.FirstOrDefaultAsync(v => v.VenueId == id, ct);
        if (venue == null)
        {
            throw AppException.NotFound(ErrorCodes.ErrAdmNotFound, "Không tìm thấy quán chay.");
        }

        var oldIsActive = venue.IsActive;
        venue.IsActive = request.IsActive;
        await _db.SaveChangesAsync(ct);

        // Ghi AuditLogs
        _db.AuditLogs.Add(new AuditLog
        {
            ActorUserId = adminId,
            ActorRole = UserRoles.Admin,
            Action = "UPDATE_VENUE_STATUS",
            TargetTable = "VeganVenues",
            TargetId = id.ToString(),
            OldValue = JsonSerializer.Serialize(new { venueId = id, isActive = oldIsActive }),
            NewValue = JsonSerializer.Serialize(new { venueId = id, isActive = request.IsActive }),
            Note = request.IsActive ? "Hiện lại quán chay" : "Ẩn quán chay không phù hợp",
            CreatedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync(ct);

        // Xóa bộ nhớ đệm quán và bản đồ
        await InvalidateVenueCachesAsync(id, ct);

        return new UpdateAdminVenueResponseData
        {
            VenueId = id,
            IsActive = venue.IsActive
        };
    }

    /// <summary>
    /// Kích hoạt xếp job đồng bộ dữ liệu quán chay từ nguồn bên ngoài (FN-ADM-14, D92, D100).
    /// </summary>
    public async Task<SyncAdminVenuesResponseData> SyncVenuesAsync(
        SyncAdminVenuesRequest request,
        int adminId,
        CancellationToken ct = default)
    {
        AdminVenueValidator.ValidateSync(request);

        var province = request.Province.Trim();
        var jobId = $"sync_{DateTime.UtcNow:yyyyMMdd}_{Guid.NewGuid():N}";

        // Ghi AuditLogs
        _db.AuditLogs.Add(new AuditLog
        {
            ActorUserId = adminId,
            ActorRole = UserRoles.Admin,
            Action = "SYNC_VENUES",
            TargetTable = "VeganVenues",
            TargetId = jobId,
            NewValue = JsonSerializer.Serialize(new
            {
                jobId,
                province,
                queuedAt = DateTime.UtcNow
            }),
            Note = $"Xếp job đồng bộ quán chay khu vực {province}",
            CreatedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync(ct);

        // Xóa cache bản đồ
        try
        {
            await _redis.RemoveAsync("venue_geo:*", ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Lỗi khi xóa bộ nhớ đệm bản đồ quán chay.");
        }

        return new SyncAdminVenuesResponseData
        {
            JobId = jobId
        };
    }

    private async Task InvalidateVenueCachesAsync(int venueId, CancellationToken ct)
    {
        try
        {
            await _redis.RemoveAsync($"venue_detail:{venueId}", ct);
            await _redis.RemoveAsync("venue_geo:*", ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Lỗi khi xóa bộ nhớ đệm quán chay ID {VenueId}.", venueId);
        }
    }
}
