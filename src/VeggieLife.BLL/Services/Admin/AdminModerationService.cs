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
using VeggieLife.BLL.DTOs.Admin.Moderation;
using VeggieLife.BLL.Services.Cache;
using VeggieLife.BLL.Services.Knowledge;
using VeggieLife.BLL.Validators.Admin;
using VeggieLife.DAL.Context;
using VeggieLife.DAL.Entities;

namespace VeggieLife.BLL.Services.Admin;

/// <summary>
/// Triển khai dịch vụ quản trị kiểm duyệt nội dung (FN-ADM-04, FN-ADM-05, FN-ADM-06).
/// </summary>
public class AdminModerationService : IAdminModerationService
{
    private readonly AppDbContext _db;
    private readonly IRedisService _redis;
    private readonly IKnowledgeSyncQueue _knowledgeSyncQueue;
    private readonly ILogger<AdminModerationService> _logger;

    public AdminModerationService(
        AppDbContext db,
        IRedisService redis,
        IKnowledgeSyncQueue knowledgeSyncQueue,
        ILogger<AdminModerationService> logger)
    {
        _db = db;
        _redis = redis;
        _knowledgeSyncQueue = knowledgeSyncQueue;
        _logger = logger;
    }

    /// <summary>
    /// Lấy danh sách nội dung trong hàng đợi chờ duyệt (từ AI_SCAN hoặc USER_REPORT) (FN-ADM-04).
    /// </summary>
    public async Task<(IReadOnlyList<AdminModerationQueueItemDto> Items, PagedMeta Meta)> GetModerationQueueAsync(
        AdminModerationQueueFilterRequest request,
        CancellationToken ct = default)
    {
        AdminModerationValidator.ValidateQueueFilter(request);

        var query = _db.ViolationReports
            .AsNoTracking()
            .Include(r => r.Comment).ThenInclude(c => c!.User)
            .Include(r => r.Comment).ThenInclude(c => c!.Post).ThenInclude(p => p!.User)
            .Include(r => r.Post).ThenInclude(p => p!.User)
            .Include(r => r.Recipe).ThenInclude(rec => rec!.User)
            .Include(r => r.Video).ThenInclude(v => v!.Post).ThenInclude(p => p!.User)
            .Where(r => r.Status == "PENDING");

        if (!string.IsNullOrWhiteSpace(request.ReportSource))
        {
            var src = request.ReportSource.Trim().ToUpperInvariant();
            query = query.Where(r => r.ReportSource == src);
        }

        if (!string.IsNullOrWhiteSpace(request.ViolationType))
        {
            var vType = request.ViolationType.Trim().ToUpperInvariant();
            query = query.Where(r => r.ViolationType == vType);
        }

        if (!string.IsNullOrWhiteSpace(request.TargetType))
        {
            var tType = request.TargetType.Trim().ToUpperInvariant();
            query = tType switch
            {
                "POST" => query.Where(r => r.PostId != null),
                "RECIPE" => query.Where(r => r.RecipeId != null),
                "VIDEO" => query.Where(r => r.VideoId != null),
                "COMMENT" => query.Where(r => r.CommentId != null),
                _ => query
            };
        }

        var total = await query.CountAsync(ct);

        var reports = await query
            .OrderByDescending(r => r.CreatedAt)
            .Skip((request.Page - 1) * request.Size)
            .Take(request.Size)
            .ToListAsync(ct);

        var items = reports.Select(r =>
        {
            string targetType = "POST";
            int targetId = 0;
            int? parentPostId = null;
            string? targetTitle = null;
            string? targetContent = null;
            string? authorName = null;

            if (r.RecipeId.HasValue)
            {
                targetType = "RECIPE";
                targetId = r.RecipeId.Value;
                targetTitle = r.Recipe?.Title;
                targetContent = r.Recipe?.Description;
                authorName = r.Recipe?.User?.FullName;
            }
            else if (r.PostId.HasValue)
            {
                targetType = "POST";
                targetId = r.PostId.Value;
                targetTitle = r.Post?.Title;
                targetContent = r.Post?.Content;
                authorName = r.Post?.User?.FullName;
            }
            else if (r.VideoId.HasValue)
            {
                targetType = "VIDEO";
                targetId = r.VideoId.Value;
                targetTitle = r.Video?.Title;
                authorName = r.Video?.Post?.User?.FullName;
            }
            else if (r.CommentId.HasValue)
            {
                targetType = "COMMENT";
                targetId = r.CommentId.Value;
                targetContent = r.Comment?.Content;
                authorName = r.Comment?.User?.FullName;
                parentPostId = r.Comment?.PostId;
                targetTitle = r.Comment?.Post?.Title;
            }

            return new AdminModerationQueueItemDto
            {
                ReportId = r.ReportId,
                TargetType = targetType,
                TargetId = targetId,
                ParentPostId = parentPostId,
                TargetTitle = targetTitle,
                TargetContent = targetContent,
                AuthorName = authorName,
                ViolationType = r.ViolationType,
                ReportSource = r.ReportSource,
                EvidenceSummary = r.EvidenceSummary,
                Status = r.Status,
                CreatedAt = r.CreatedAt
            };
        }).ToList();

        var meta = new PagedMeta(request.Page, request.Size, total);
        return (items, meta);
    }

    /// <summary>
    /// Quyết định xử lý báo cáo vi phạm nội dung: Xác nhận vi phạm (UPHOLD) hoặc Bác bỏ (DISMISS) (FN-ADM-05).
    /// </summary>
    public async Task<DecideAdminModerationReportResponseData> DecideReportAsync(
        int reportId,
        DecideAdminModerationReportRequest request,
        int adminId,
        CancellationToken ct = default)
    {
        AdminModerationValidator.ValidateDecision(reportId, request);

        var report = await _db.ViolationReports
            .Include(r => r.Recipe)
            .Include(r => r.Post)
            .Include(r => r.Video)
                .ThenInclude(v => v!.Post)
            .Include(r => r.Comment)
            .FirstOrDefaultAsync(r => r.ReportId == reportId, ct);

        if (report == null)
        {
            throw AppException.NotFound(ErrorCodes.ErrAdmNotFound, "Không tìm thấy báo cáo vi phạm.");
        }

        // Báo cáo đã đóng thì từ chối xử lý lại (ERR_ADM_REPORT_CLOSED)
        if (!string.Equals(report.Status, "PENDING", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(report.Status, "INVESTIGATING", StringComparison.OrdinalIgnoreCase))
        {
            throw AppException.Conflict(ErrorCodes.ErrAdmReportClosed, "Báo cáo vi phạm này đã được xử lý.");
        }

        var decisionUpper = request.Decision.Trim().ToUpperInvariant();
        string targetStatus;
        int? authorUserId = null;
        string contentType = string.Empty;
        int contentId = 0;

        if (decisionUpper == "UPHOLD")
        {
            // Xác nhận vi phạm
            report.Status = "APPROVE";
            report.AdminId = adminId;

            if (report.RecipeId.HasValue && report.Recipe != null)
            {
                report.Recipe.Status = "REJECTED";
                report.Recipe.UpdatedAt = DateTime.UtcNow;
                targetStatus = "REJECTED";
                authorUserId = report.Recipe.UserId;
                contentType = "RECIPE";
                contentId = report.Recipe.RecipeId;
            }
            else if (report.PostId.HasValue && report.Post != null)
            {
                report.Post.ModerationStatus = "REJECTED";
                report.Post.UpdatedAt = DateTime.UtcNow;
                targetStatus = "REJECTED";
                authorUserId = report.Post.UserId;
                contentType = "POST";
                contentId = report.Post.PostId;
            }
            else if (report.VideoId.HasValue && report.Video != null)
            {
                report.Video.Status = "BANNED";
                report.Video.UpdatedAt = DateTime.UtcNow;
                targetStatus = "BANNED";
                authorUserId = report.Video.Post?.UserId;
                contentType = "VIDEO";
                contentId = report.Video.VideoId;
            }
            else if (report.CommentId.HasValue && report.Comment != null)
            {
                targetStatus = "HIDDEN";
                authorUserId = report.Comment.UserId;
                contentType = "COMMENT";
                contentId = report.Comment.CommentId;

                // Xóa mềm toàn bộ cây bình luận phản hồi con bên dưới (BR-ADM-CMT-001)
                await CascadeSoftDeleteCommentInternalAsync(report.Comment.CommentId, ct);
            }
            else
            {
                targetStatus = "HIDDEN";
            }

            // Tăng số vi phạm của tác giả nội dung (BR-ADM-USR-002)
            if (authorUserId.HasValue)
            {
                var author = await _db.Users.FirstOrDefaultAsync(u => u.UserId == authorUserId.Value, ct);
                if (author != null)
                {
                    author.CountViolationReports++;
                    author.UpdatedAt = DateTime.UtcNow;

                    if (request.ApplySanction)
                    {
                        ApplyProgressiveSanction(author);
                        if (author.AccountStatus is AccountStatuses.Suspended or AccountStatuses.Banned)
                        {
                            await RevokeUserSessionsAsync(author.UserId, ct);
                        }
                    }
                }
            }

            // Đồng bộ kho tri thức AI: Gỡ nội dung vi phạm khỏi vector database (D99, D118)
            if (!string.IsNullOrEmpty(contentType) && contentId > 0)
            {
                try
                {
                    await _knowledgeSyncQueue.EnqueueDeleteAsync(contentType, contentId, ct);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Lỗi khi đưa yêu cầu gỡ {ContentType} ID {Id} khỏi kho tri thức.", contentType, contentId);
                }
            }
        }
        else // DISMISS
        {
            // Bác bỏ báo cáo / Khôi phục nội dung
            report.Status = "REJECT";
            report.AdminId = adminId;

            if (report.RecipeId.HasValue && report.Recipe != null)
            {
                report.Recipe.Status = "APPROVED_ACTIVE";
                report.Recipe.UpdatedAt = DateTime.UtcNow;
                targetStatus = "APPROVED_ACTIVE";
                contentType = "RECIPE";
                contentId = report.Recipe.RecipeId;
            }
            else if (report.PostId.HasValue && report.Post != null)
            {
                report.Post.ModerationStatus = "APPROVED_ACTIVE";
                report.Post.UpdatedAt = DateTime.UtcNow;
                targetStatus = "APPROVED_ACTIVE";
                contentType = "POST";
                contentId = report.Post.PostId;
            }
            else if (report.VideoId.HasValue && report.Video != null)
            {
                report.Video.Status = "READY";
                report.Video.UpdatedAt = DateTime.UtcNow;
                targetStatus = "READY";
                contentType = "VIDEO";
                contentId = report.Video.VideoId;
            }
            else if (report.CommentId.HasValue && report.Comment != null)
            {
                report.Comment.Status = "ACTIVE";
                report.Comment.IsSoftDeleted = false;
                report.Comment.UpdatedAt = DateTime.UtcNow;
                targetStatus = "ACTIVE";
                contentType = "COMMENT";
                contentId = report.Comment.CommentId;
            }
            else
            {
                targetStatus = "ACTIVE";
            }

            // Đồng bộ kho tri thức AI: Nạp lại nội dung hợp lệ vào vector database (D99, D118)
            if (!string.IsNullOrEmpty(contentType) && contentId > 0 && contentType != "COMMENT")
            {
                try
                {
                    await _knowledgeSyncQueue.EnqueueUpsertAsync(contentType, contentId, ct);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Lỗi khi nạp lại {ContentType} ID {Id} vào kho tri thức.", contentType, contentId);
                }
            }
        }

        await _db.SaveChangesAsync(ct);

        // Ghi AuditLogs
        _db.AuditLogs.Add(new AuditLog
        {
            ActorUserId = adminId,
            ActorRole = UserRoles.Admin,
            Action = "MODERATION_DECISION",
            TargetTable = "ViolationReports",
            TargetId = reportId.ToString(),
            OldValue = JsonSerializer.Serialize(new { status = "PENDING" }),
            NewValue = JsonSerializer.Serialize(new
            {
                reportId,
                decision = decisionUpper,
                status = report.Status,
                targetStatus,
                note = request.Note.Trim(),
                applySanction = request.ApplySanction
            }),
            Note = request.Note.Trim(),
            CreatedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync(ct);

        // Xóa bộ nhớ đệm
        await InvalidateModerationCachesAsync(ct);

        return new DecideAdminModerationReportResponseData
        {
            ReportId = reportId,
            Status = "RESOLVED",
            TargetStatus = targetStatus
        };
    }

    /// <summary>
    /// Xóa mềm một bình luận và toàn bộ cây phản hồi con bên dưới (FN-ADM-06, BR-ADM-CMT-001).
    /// </summary>
    public async Task<DeleteAdminCommentResponseData> DeleteCommentAsync(
        int commentId,
        string reason,
        int adminId,
        CancellationToken ct = default)
    {
        AdminModerationValidator.ValidateDeleteComment(commentId, reason);

        var comment = await _db.Comments.FirstOrDefaultAsync(c => c.CommentId == commentId, ct);
        if (comment == null)
        {
            throw AppException.NotFound(ErrorCodes.ErrAdmNotFound, "Không tìm thấy bình luận.");
        }

        var deletedCount = await CascadeSoftDeleteCommentInternalAsync(commentId, ct);
        await _db.SaveChangesAsync(ct);

        // Ghi AuditLogs
        _db.AuditLogs.Add(new AuditLog
        {
            ActorUserId = adminId,
            ActorRole = UserRoles.Admin,
            Action = "COMMENT_DELETE",
            TargetTable = "Comments",
            TargetId = commentId.ToString(),
            NewValue = JsonSerializer.Serialize(new
            {
                commentId,
                deleted = deletedCount,
                reason = reason.Trim()
            }),
            Note = reason.Trim(),
            CreatedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync(ct);

        // Đồng bộ kho tri thức AI: Gỡ bình luận khỏi vector database
        try
        {
            await _knowledgeSyncQueue.EnqueueDeleteAsync("COMMENT", commentId, ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Lỗi khi đưa yêu cầu gỡ COMMENT ID {Id} khỏi kho tri thức.", commentId);
        }

        return new DeleteAdminCommentResponseData
        {
            Deleted = deletedCount
        };
    }

    private async Task<int> CascadeSoftDeleteCommentInternalAsync(int rootCommentId, CancellationToken ct)
    {
        var allDescendantIds = new HashSet<int> { rootCommentId };
        var currentLevel = new List<int> { rootCommentId };

        while (currentLevel.Count > 0)
        {
            var childIds = await _db.Comments
                .Where(c => c.ParentCommentId.HasValue && currentLevel.Contains(c.ParentCommentId.Value) && !c.IsSoftDeleted)
                .Select(c => c.CommentId)
                .ToListAsync(ct);

            var nextLevel = new List<int>();
            foreach (var childId in childIds)
            {
                if (allDescendantIds.Add(childId))
                {
                    nextLevel.Add(childId);
                }
            }

            currentLevel = nextLevel;
        }

        var commentsToSoftDelete = await _db.Comments
            .Where(c => allDescendantIds.Contains(c.CommentId))
            .ToListAsync(ct);

        foreach (var c in commentsToSoftDelete)
        {
            c.IsSoftDeleted = true;
            c.Status = "HIDDEN";
            c.UpdatedAt = DateTime.UtcNow;
        }

        return commentsToSoftDelete.Count;
    }

    private static void ApplyProgressiveSanction(User author)
    {
        var count = author.CountViolationReports;
        if (count >= 13)
        {
            author.AccountStatus = AccountStatuses.Banned;
            author.LockedUntil = null;
        }
        else if (count >= 10)
        {
            author.AccountStatus = AccountStatuses.Suspended;
            author.LockedUntil = DateTime.UtcNow.AddDays(30);
        }
        else if (count >= 7)
        {
            author.AccountStatus = AccountStatuses.Suspended;
            author.LockedUntil = DateTime.UtcNow.AddDays(10);
        }
        else if (count >= 4)
        {
            author.AccountStatus = AccountStatuses.Suspended;
            author.LockedUntil = DateTime.UtcNow.AddDays(3);
        }
        else
        {
            // 1–3: Cảnh cáo
            if (author.AccountStatus == AccountStatuses.PendingActivation)
            {
                author.AccountStatus = AccountStatuses.Active;
            }
        }
    }

    private async Task RevokeUserSessionsAsync(int userId, CancellationToken ct)
    {
        try
        {
            var userSessionsKey = $"user_sessions:{userId}";
            var activeHashes = await _redis.GetAsync<List<string>>(userSessionsKey, ct);
            if (activeHashes != null)
            {
                foreach (var tokenHash in activeHashes)
                {
                    await _redis.RemoveAsync($"jwt_blacklist:{tokenHash}", ct);
                }
            }
            await _redis.RemoveAsync(userSessionsKey, ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Lỗi khi thu hồi phiên người dùng ID {UserId}.", userId);
        }
    }

    private async Task InvalidateModerationCachesAsync(CancellationToken ct)
    {
        try
        {
            await _redis.RemoveAsync("recipe_detail:*", ct);
            await _redis.RemoveAsync("recipe_search:*", ct);
            await _redis.RemoveAsync("post_list:*", ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Lỗi khi xóa bộ nhớ đệm kiểm duyệt.");
        }
    }
}
