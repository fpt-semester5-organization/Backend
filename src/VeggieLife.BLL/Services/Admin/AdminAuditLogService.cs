using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using VeggieLife.BLL.Common.Responses;
using VeggieLife.BLL.Common.Utils;
using VeggieLife.BLL.DTOs.Admin.AuditLogs;
using VeggieLife.BLL.Validators.Admin;
using VeggieLife.DAL.Context;
using VeggieLife.DAL.Entities;

namespace VeggieLife.BLL.Services.Admin;

/// <summary>
/// Triển khai dịch vụ tra cứu nhật ký kiểm toán cho Quản trị viên (FN-ADM-17).
/// Tuân thủ:
/// - BR-ADM-SYS-002: Nhật ký chỉ ghi (Append-only); cấm sửa/xóa; dữ liệu nhạy cảm được che giấu.
/// - Hiệu năng: Đạt &lt; 800 ms với 100.000 bản ghi nhờ AsNoTracking, chỉ số index và phân trang tối ưu.
/// </summary>
public class AdminAuditLogService : IAdminAuditLogService
{
    private readonly AppDbContext _db;
    private readonly ILogger<AdminAuditLogService> _logger;

    public AdminAuditLogService(AppDbContext db, ILogger<AdminAuditLogService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<(IReadOnlyList<AdminAuditLogListItemDto> Logs, PagedMeta Meta)> GetAuditLogsAsync(
        AdminAuditLogFilterRequest filter,
        CancellationToken ct = default)
    {
        var (fromDate, toDate) = AdminAuditLogValidator.ValidateFilter(filter);

        IQueryable<AuditLog> query = _db.AuditLogs.AsNoTracking();

        if (filter.ActorUserId.HasValue)
        {
            query = query.Where(a => a.ActorUserId == filter.ActorUserId.Value);
        }

        if (!string.IsNullOrWhiteSpace(filter.Action))
        {
            var actionTrimmed = filter.Action.Trim();
            query = query.Where(a => a.Action == actionTrimmed);
        }

        if (!string.IsNullOrWhiteSpace(filter.TargetTable))
        {
            var targetTableTrimmed = filter.TargetTable.Trim();
            query = query.Where(a => a.TargetTable == targetTableTrimmed);
        }

        if (!string.IsNullOrWhiteSpace(filter.TargetId))
        {
            var targetIdTrimmed = filter.TargetId.Trim();
            query = query.Where(a => a.TargetId == targetIdTrimmed);
        }

        var vietnamOffset = TimeSpan.FromHours(7);

        if (fromDate.HasValue)
        {
            var fromUtc = fromDate.Value.Date - vietnamOffset;
            query = query.Where(a => a.CreatedAt >= fromUtc);
        }

        if (toDate.HasValue)
        {
            var toUtc = toDate.Value.Date.AddDays(1) - vietnamOffset;
            query = query.Where(a => a.CreatedAt < toUtc);
        }

        var total = await query.CountAsync(ct);

        if (total == 0)
        {
            return (Array.Empty<AdminAuditLogListItemDto>(), new PagedMeta
            {
                Page = filter.Page,
                Size = filter.Size,
                Total = 0
            });
        }

        var skip = (filter.Page - 1) * filter.Size;
        var pagedEntities = await query
            .OrderByDescending(a => a.CreatedAt)
            .ThenByDescending(a => a.AuditId)
            .Skip(skip)
            .Take(filter.Size)
            .ToListAsync(ct);

        var items = pagedEntities.Select(a => new AdminAuditLogListItemDto
        {
            AuditId = a.AuditId,
            ActorUserId = a.ActorUserId,
            ActorRole = a.ActorRole,
            Action = a.Action,
            TargetTable = a.TargetTable,
            TargetId = a.TargetId,
            Note = a.Note,
            CreatedAt = a.CreatedAt,
            OldValue = AuditLogSanitizer.MaskSensitiveData(a.OldValue),
            NewValue = AuditLogSanitizer.MaskSensitiveData(a.NewValue)
        }).ToList();

        return (items, new PagedMeta
        {
            Page = filter.Page,
            Size = filter.Size,
            Total = total
        });
    }
}
