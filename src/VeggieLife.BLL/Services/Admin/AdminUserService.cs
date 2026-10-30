using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using VeggieLife.BLL.Common.Constants;
using VeggieLife.BLL.Common.Exceptions;
using VeggieLife.BLL.Common.Responses;
using VeggieLife.BLL.DTOs.Admin.Users;
using VeggieLife.BLL.Services.Cache;
using VeggieLife.BLL.Services.Email;
using VeggieLife.BLL.Services.Realtime;
using VeggieLife.BLL.Validators.Admin;
using VeggieLife.DAL.Context;
using VeggieLife.DAL.Entities;

namespace VeggieLife.BLL.Services.Admin;

/// <summary>
/// Dịch vụ Quản trị người dùng dành cho Quản trị viên (FN-ADM-01, FN-ADM-02, FN-ADM-03).
/// Hiện thực đầy đủ:
/// - BR-ADM-USR-001: Bảo toàn tối thiểu 01 Administrator Active, cấm Admin duy nhất tự vô hiệu hóa/khóa tài khoản.
/// - BR-ADM-USR-002: Thang chế tài kỷ luật lũy tiến theo số vi phạm và các trạng thái chuẩn hóa.
/// - BR-ADM-SYS-002: Ghi nhật ký kiểm toán AuditLogs hành động USER_SANCTION.
/// - BR-AUTH-004: Thu hồi toàn bộ refresh token và đưa vào danh sách đen (Blacklist) khi tài khoản bị kỷ luật.
/// </summary>
public class AdminUserService : IAdminUserService
{
    private readonly AppDbContext _db;
    private readonly IRedisService _redis;
    private readonly IEmailService _emailService;
    private readonly IUserRealtimeNotifier? _realtimeNotifier;
    private readonly ILogger<AdminUserService> _logger;

    public AdminUserService(
        AppDbContext db,
        IRedisService redis,
        IEmailService emailService,
        ILogger<AdminUserService> logger,
        IUserRealtimeNotifier? realtimeNotifier = null)
    {
        _db = db;
        _redis = redis;
        _emailService = emailService;
        _logger = logger;
        _realtimeNotifier = realtimeNotifier;
    }

    /// <summary>
    /// FN-ADM-01: Tra cứu, lọc và phân trang danh sách người dùng trong hệ thống.
    /// </summary>
    public async Task<(IReadOnlyList<AdminUserListItemDto> Items, PagedMeta Meta)> GetUsersAsync(
        AdminUserListFilterRequest filter,
        CancellationToken ct = default)
    {
        AdminUserValidator.ValidateFilter(filter);

        var query = _db.Users.IgnoreQueryFilters().AsNoTracking();

        if (!string.IsNullOrWhiteSpace(filter.Keyword))
        {
            var keyword = filter.Keyword.Trim();
            query = query.Where(u => u.Email.Contains(keyword) || u.FullName.Contains(keyword));
        }

        if (!string.IsNullOrWhiteSpace(filter.AccountStatus))
        {
            var status = filter.AccountStatus.Trim().ToUpperInvariant();
            query = query.Where(u => u.AccountStatus == status);
        }

        if (!string.IsNullOrWhiteSpace(filter.Role))
        {
            var role = filter.Role.Trim().ToUpperInvariant();
            query = query.Where(u => u.Role == role);
        }

        if (filter.DietarySchoolId.HasValue)
        {
            var schoolId = filter.DietarySchoolId.Value;
            query = query.Where(u => u.HealthProfile != null && u.HealthProfile.DietarySchoolId == schoolId);
        }

        if (filter.CreatedFrom.HasValue)
        {
            query = query.Where(u => u.CreatedAt >= filter.CreatedFrom.Value);
        }

        if (filter.CreatedTo.HasValue)
        {
            query = query.Where(u => u.CreatedAt <= filter.CreatedTo.Value);
        }

        var total = await query.CountAsync(ct);

        var users = await query
            .OrderByDescending(u => u.CreatedAt)
            .Skip((filter.Page - 1) * filter.Size)
            .Take(filter.Size)
            .Select(u => new AdminUserListItemDto
            {
                UserId = u.UserId,
                Email = u.Email,
                FullName = u.FullName,
                Role = u.Role,
                AccountStatus = u.AccountStatus,
                CreatedAt = u.CreatedAt,
                DietarySchoolId = u.HealthProfile != null ? u.HealthProfile.DietarySchoolId : null,
                DietarySchoolName = u.HealthProfile != null && u.HealthProfile.DietarySchool != null ? u.HealthProfile.DietarySchool.SchoolName : null,
                LockedUntil = u.LockedUntil,
                CountViolationReports = u.CountViolationReports,
                VipEndsAt = u.PaymentTransactions
                    .Where(pt => pt.TransactionStatus == "SUCCESS" && pt.EndsAt > DateTime.UtcNow)
                    .OrderByDescending(pt => pt.EndsAt)
                    .Select(pt => (DateTime?)pt.EndsAt)
                    .FirstOrDefault()
            })
            .ToListAsync(ct);

        var meta = new PagedMeta(filter.Page, filter.Size, total);
        return (users, meta);
    }

    /// <summary>
    /// FN-ADM-02: Cập nhật trạng thái và áp dụng chế tài kỷ luật tài khoản (Cảnh cáo, Khóa bình luận, Tạm khóa, Cấm vĩnh viễn, Mở lại).
    /// </summary>
    public async Task<UpdateAdminUserStatusResponseData> UpdateUserStatusAsync(
        int userId,
        UpdateAdminUserStatusRequest request,
        int adminId,
        CancellationToken ct = default)
    {
        AdminUserValidator.ValidateUpdateStatus(request);
        var normalizedAction = request.Action.Trim().ToUpperInvariant();

        var targetUser = await _db.Users.IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.UserId == userId, ct);

        if (targetUser == null)
        {
            throw AppException.NotFound(ErrorCodes.ErrAdmNotFound, $"Không tìm thấy người dùng có mã ID #{userId}.");
        }

        // 1. Kiểm tra quy tắc BR-ADM-USR-001: Bảo vệ Admin duy nhất còn hoạt động
        if (string.Equals(targetUser.Role, UserRoles.Admin, StringComparison.OrdinalIgnoreCase))
        {
            if (normalizedAction != AdminSanctionActions.Reactivate)
            {
                var activeAdminCount = await _db.Users.IgnoreQueryFilters()
                    .CountAsync(u => u.Role == UserRoles.Admin && u.AccountStatus == AccountStatuses.Active, ct);

                if (string.Equals(targetUser.AccountStatus, AccountStatuses.Active, StringComparison.OrdinalIgnoreCase) && activeAdminCount <= 1)
                {
                    _logger.LogWarning("Từ chối kỷ luật/vô hiệu hóa Administrator ID {UserId} vì đây là Admin duy nhất đang Active.", userId);
                    throw AppException.Conflict(ErrorCodes.ErrAdmLastAdmin,
                        "Không thể vô hiệu hóa hoặc kỷ luật tài khoản Administrator duy nhất còn lại đang hoạt động trong hệ thống (BR-ADM-USR-001).");
                }
            }
        }

        // 2. Kiểm tra chuyển đổi trạng thái hợp lệ (Invalid State Transition)
        if (normalizedAction == AdminSanctionActions.Reactivate &&
            string.Equals(targetUser.AccountStatus, AccountStatuses.Active, StringComparison.OrdinalIgnoreCase))
        {
            throw AppException.Conflict(ErrorCodes.ErrAdmInvalidTransition,
                "Tài khoản người dùng hiện đang ở trạng thái hoạt động (ACTIVE), không cần mở lại.");
        }

        if (normalizedAction == AdminSanctionActions.Ban &&
            string.Equals(targetUser.AccountStatus, AccountStatuses.Banned, StringComparison.OrdinalIgnoreCase))
        {
            throw AppException.Conflict(ErrorCodes.ErrAdmInvalidTransition,
                "Tài khoản người dùng đã bị cấm vĩnh viễn (BANNED) từ trước.");
        }

        if (string.Equals(targetUser.AccountStatus, AccountStatuses.AnonymizedDeleted, StringComparison.OrdinalIgnoreCase) &&
            normalizedAction != AdminSanctionActions.Reactivate)
        {
            throw AppException.Conflict(ErrorCodes.ErrAdmInvalidTransition,
                "Không thể xử phạt tài khoản đã bị xóa và ẩn danh hóa vĩnh viễn (ANONYMIZED_DELETED).");
        }

        var oldValue = targetUser.AccountStatus;
        string newStatus = targetUser.AccountStatus;
        DateTime? newLockedUntil = targetUser.LockedUntil;

        // 3. Xử lý chế tài theo thang kỷ luật BR-ADM-USR-002 & BR-ADM-SOC-001
        switch (normalizedAction)
        {
            case AdminSanctionActions.Warn:
                // Cảnh cáo: giữ nguyên trạng thái tài khoản (hoặc chuyển sang ACTIVE nếu đang bình thường), không khóa thời gian
                if (targetUser.AccountStatus == AccountStatuses.PendingActivation)
                {
                    newStatus = AccountStatuses.Active;
                }
                break;

            case AdminSanctionActions.RestrictComment:
                // Khóa quyền bình luận (trạng thái QUARANTINED)
                newStatus = AccountStatuses.Quarantined;
                var restrictDays = request.DurationDays ?? 7;
                newLockedUntil = DateTime.UtcNow.AddDays(restrictDays);
                break;

            case AdminSanctionActions.Suspend:
                // Tạm khóa tài khoản (trạng thái SUSPENDED)
                newStatus = AccountStatuses.Suspended;
                var suspendDays = request.DurationDays ?? 3;
                newLockedUntil = DateTime.UtcNow.AddDays(suspendDays);
                break;

            case AdminSanctionActions.Ban:
                // Cấm vĩnh viễn (trạng thái BANNED)
                newStatus = AccountStatuses.Banned;
                newLockedUntil = null;
                break;

            case AdminSanctionActions.Reactivate:
                // Khôi phục tài khoản (trạng thái ACTIVE)
                newStatus = AccountStatuses.Active;
                newLockedUntil = null;
                targetUser.FailedLoginCount = 0;
                break;
        }

        targetUser.AccountStatus = newStatus;
        targetUser.LockedUntil = newLockedUntil;
        targetUser.UpdatedAt = DateTime.UtcNow;

        // 4. Thu hồi toàn bộ refresh token và blacklist access token (BR-AUTH-004)
        if (normalizedAction is AdminSanctionActions.Suspend or AdminSanctionActions.Ban or AdminSanctionActions.RestrictComment)
        {
            try
            {
                var userSessionsKey = $"user_sessions:{targetUser.UserId}";
                var activeHashes = await _redis.GetAsync<List<string>>(userSessionsKey, ct);
                if (activeHashes != null)
                {
                    foreach (var hash in activeHashes)
                    {
                        await _redis.RemoveAsync($"refresh:{hash}", ct);
                    }
                }
                await _redis.RemoveAsync(userSessionsKey, ct);
                await _redis.RemoveAsync($"admin_session:{targetUser.UserId}", ct);

                // Blacklist tài khoản trong Redis
                var blacklistDuration = request.DurationDays.HasValue
                    ? TimeSpan.FromDays(request.DurationDays.Value)
                    : TimeSpan.FromDays(30);

                await _redis.SetAsync($"user_blacklist:{targetUser.UserId}", true, blacklistDuration, ct);

                // Gửi thông báo SignalR ForceLogout realtime tới client bị cấm
                if (_realtimeNotifier != null && normalizedAction is AdminSanctionActions.Suspend or AdminSanctionActions.Ban)
                {
                    await _realtimeNotifier.NotifyForceLogoutAsync(
                        targetUser.UserId,
                        request.Reason ?? "Tài khoản của bạn đã bị tạm khóa hoặc cấm do vi phạm điều khoản dịch vụ.",
                        normalizedAction,
                        ct);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Lỗi khi thu hồi các phiên đăng nhập Redis cho User {UserId}.", targetUser.UserId);
            }
        }
        else if (normalizedAction == AdminSanctionActions.Reactivate)
        {
            try
            {
                await _redis.RemoveAsync($"user_blacklist:{targetUser.UserId}", ct);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Lỗi khi gỡ blacklist Redis cho User {UserId}.", targetUser.UserId);
            }
        }

        // 5. Ghi nhật ký kiểm toán AuditLogs (BR-ADM-SYS-002, ISJSON constraints)
        var auditLog = new AuditLog
        {
            ActorUserId = adminId,
            ActorRole = UserRoles.Admin,
            Action = "USER_SANCTION",
            TargetTable = "Users",
            TargetId = targetUser.UserId.ToString(),
            OldValue = JsonSerializer.Serialize(new { accountStatus = oldValue }),
            NewValue = JsonSerializer.Serialize(new
            {
                accountStatus = newStatus,
                action = normalizedAction,
                lockedUntil = newLockedUntil,
                durationDays = request.DurationDays,
                reason = request.Reason
            }),
            Note = $"[Hành động: {normalizedAction}] {request.Reason}",
            CreatedAt = DateTime.UtcNow
        };
        _db.AuditLogs.Add(auditLog);

        await _db.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Admin {AdminId} đã áp dụng chế tài '{Action}' lên User {UserId} ({OldStatus} -> {NewStatus}). Lý do: {Reason}",
            adminId, normalizedAction, targetUser.UserId, oldValue, newStatus, request.Reason);

        // 6. Gửi email thông báo kỷ luật (không chặn flow nếu SMTP gặp sự cố)
        try
        {
            var emailSubject = normalizedAction switch
            {
                AdminSanctionActions.Warn => "[VeggieLife] Cảnh cáo vi phạm tiêu chuẩn cộng đồng",
                AdminSanctionActions.RestrictComment => "[VeggieLife] Tạm khóa quyền bình luận tài khoản",
                AdminSanctionActions.Suspend => "[VeggieLife] Thông báo tạm khóa tài khoản",
                AdminSanctionActions.Ban => "[VeggieLife] Thông báo đình chỉ vĩnh viễn tài khoản",
                AdminSanctionActions.Reactivate => "[VeggieLife] Thông báo khôi phục tài khoản",
                _ => "[VeggieLife] Thay đổi trạng thái tài khoản"
            };

            var emailBody = $"<p>Xin chào <strong>{targetUser.FullName}</strong>,</p>" +
                            $"<p>Tài khoản của bạn đã được cập nhật trạng thái: <strong>{newStatus}</strong>.</p>" +
                            $"<p><strong>Lý do:</strong> {request.Reason}</p>" +
                            (newLockedUntil.HasValue ? $"<p><strong>Thời hạn đến:</strong> {newLockedUntil.Value:dd/MM/yyyy HH:mm:ss} UTC</p>" : "") +
                            $"<p>Trân trọng,<br/>Đội ngũ Quản trị VeggieLife</p>";

            await _emailService.SendNotificationEmailAsync(targetUser.Email, emailSubject, emailBody, ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Không thể gửi email thông báo kỷ luật tới {Email}.", targetUser.Email);
        }

        return new UpdateAdminUserStatusResponseData
        {
            UserId = targetUser.UserId,
            AccountStatus = targetUser.AccountStatus,
            LockedUntil = targetUser.LockedUntil,
            ViolationCount = targetUser.CountViolationReports
        };
    }

    /// <summary>
    /// FN-ADM-03: Xem tổng hợp các nội dung và lịch sử vi phạm của một người dùng.
    /// </summary>
    public async Task<(AdminUserContentsResponseData Data, PagedMeta Meta)> GetUserContentsAsync(
        int userId,
        AdminUserContentsFilterRequest filter,
        CancellationToken ct = default)
    {
        AdminUserValidator.ValidateContentsFilter(filter);

        var targetUser = await _db.Users.IgnoreQueryFilters().AsNoTracking()
            .FirstOrDefaultAsync(u => u.UserId == userId, ct);

        if (targetUser == null)
        {
            throw AppException.NotFound(ErrorCodes.ErrAdmNotFound, $"Không tìm thấy người dùng có mã ID #{userId}.");
        }

        // 1. Thống kê ViolationReports liên quan đến các nội dung của tác giả
        var postIds = _db.Posts.IgnoreQueryFilters().Where(p => p.UserId == userId).Select(p => p.PostId);
        var recipeIds = _db.Recipes.IgnoreQueryFilters().Where(r => r.UserId == userId).Select(r => r.RecipeId);
        var commentIds = _db.Comments.IgnoreQueryFilters().Where(c => c.UserId == userId).Select(c => c.CommentId);
        var videoIds = _db.Videos.IgnoreQueryFilters().Where(v => v.Post.UserId == userId).Select(v => v.VideoId);

        var reportsQuery = _db.ViolationReports.AsNoTracking().Where(vr =>
            (vr.PostId != null && postIds.Contains(vr.PostId.Value)) ||
            (vr.RecipeId != null && recipeIds.Contains(vr.RecipeId.Value)) ||
            (vr.CommentId != null && commentIds.Contains(vr.CommentId.Value)) ||
            (vr.VideoId != null && videoIds.Contains(vr.VideoId.Value)));

        var resolvedCount = await reportsQuery.CountAsync(vr => vr.Status == "APPROVE", ct);
        var dismissedCount = await reportsQuery.CountAsync(vr => vr.Status == "REJECT", ct);

        // 2. Lấy danh sách nội dung theo loại (hoặc gộp toàn bộ nếu không lọc)
        var normalizedType = filter.Type?.Trim().ToUpperInvariant();

        IQueryable<AdminUserContentItemDto>? unifiedQuery = null;

        var postsStream = _db.Posts.IgnoreQueryFilters().Where(p => p.UserId == userId)
            .Select(p => new AdminUserContentItemDto
            {
                Type = AdminUserContentTypes.Post,
                Id = p.PostId,
                Title = p.Title,
                Status = p.ModerationStatus,
                CreatedAt = p.CreatedAt
            });

        var recipesStream = _db.Recipes.IgnoreQueryFilters().Where(r => r.UserId == userId)
            .Select(r => new AdminUserContentItemDto
            {
                Type = AdminUserContentTypes.Recipe,
                Id = r.RecipeId,
                Title = r.Title,
                Status = r.Status,
                CreatedAt = r.CreatedAt
            });

        var videosStream = _db.Videos.IgnoreQueryFilters().Where(v => v.Post.UserId == userId)
            .Select(v => new AdminUserContentItemDto
            {
                Type = AdminUserContentTypes.Video,
                Id = v.VideoId,
                Title = v.Title,
                Status = v.Status,
                CreatedAt = v.CreatedAt
            });

        var commentsStream = _db.Comments.IgnoreQueryFilters().Where(c => c.UserId == userId)
            .Select(c => new AdminUserContentItemDto
            {
                Type = AdminUserContentTypes.Comment,
                Id = c.CommentId,
                Title = c.Content,
                Status = c.Status,
                CreatedAt = c.CreatedAt
            });

        switch (normalizedType)
        {
            case AdminUserContentTypes.Post:
                unifiedQuery = postsStream;
                break;
            case AdminUserContentTypes.Recipe:
                unifiedQuery = recipesStream;
                break;
            case AdminUserContentTypes.Video:
                unifiedQuery = videosStream;
                break;
            case AdminUserContentTypes.Comment:
                unifiedQuery = commentsStream;
                break;
            default:
                unifiedQuery = postsStream
                    .Concat(recipesStream)
                    .Concat(videosStream)
                    .Concat(commentsStream);
                break;
        }

        var totalItems = await unifiedQuery.CountAsync(ct);

        var pagedItems = await unifiedQuery
            .OrderByDescending(x => x.CreatedAt)
            .Skip((filter.Page - 1) * filter.Size)
            .Take(filter.Size)
            .ToListAsync(ct);

        var result = new AdminUserContentsResponseData
        {
            UserId = targetUser.UserId,
            AccountStatus = targetUser.AccountStatus,
            LockedUntil = targetUser.LockedUntil,
            CountViolationReports = targetUser.CountViolationReports,
            ViolationSummary = new AdminUserViolationSummaryDto
            {
                Resolved = resolvedCount,
                Dismissed = dismissedCount
            },
            Items = pagedItems
        };

        var meta = new PagedMeta(filter.Page, filter.Size, totalItems);
        return (result, meta);
    }
}
