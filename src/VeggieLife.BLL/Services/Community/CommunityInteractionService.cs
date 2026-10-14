using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using VeggieLife.BLL.Common.Constants;
using VeggieLife.BLL.Common.Exceptions;
using VeggieLife.BLL.DTOs.Community.Requests;
using VeggieLife.BLL.DTOs.Community.Responses;
using VeggieLife.BLL.Services.Cache;
using VeggieLife.DAL.Context;
using VeggieLife.DAL.Entities;

namespace VeggieLife.BLL.Services.Community;

/// <summary>
/// Dịch vụ xử lý tương tác cộng đồng: Thả tim/bỏ tim bài viết, công thức và báo cáo vi phạm nội dung (FN-COMM-04, FN-COMM-05).
/// Tuân thủ nghiêm ngặt các quy tắc nghiệp vụ:
/// - BR-INT-LIKE-01: Mỗi tài khoản Like tối đa 1 lần/nội dung, Like lần 2 = Unlike (bỏ thích).
/// - BR-INT-LIKE-02: Không cho phép Like nội dung Nháp (Draft), đã xóa mềm (Deleted) hoặc bị ẩn bởi Quản trị viên (Quarantined_Hidden).
/// - BR-AUTH-03: Khách vãng lai bị từ chối mọi thao tác tương tác cộng đồng ghi dữ liệu.
/// - BR-USR-04: Tối đa 10 báo cáo vi phạm / ngày / tài khoản.
/// - BR-INT-REPORT-01: Nội dung bị báo cáo bị ẩn ngay lập tức (QUARANTINED_HIDDEN / BANNED).
/// - BR-ADM-CNT-001: Trạng thái vòng đời chuẩn hóa (APPROVED_ACTIVE -> QUARANTINED_HIDDEN).
/// - BR-ADM-CMT-001: Cascade ẩn toàn bộ cây bình luận phản hồi con khi bình luận cha bị báo cáo.
/// </summary>
public class CommunityInteractionService : ICommunityInteractionService, IInteractionService
{
    private readonly AppDbContext _db;
    private readonly IRedisService _redis;
    private readonly ILogger<CommunityInteractionService> _logger;

    private static readonly HashSet<string> ValidTargetTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "POST", "RECIPE", "VIDEO", "COMMENT"
    };

    private static readonly HashSet<string> ValidViolationTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "VIO_MEAT", "VIO_LABEL", "VIO_HATE", "VIO_SPAM"
    };

    public CommunityInteractionService(
        AppDbContext db,
        IRedisService redis,
        ILogger<CommunityInteractionService> logger)
    {
        _db = db;
        _redis = redis;
        _logger = logger;
    }

    /// <summary>
    /// FN-COMM-04: Thả tim hoặc bỏ tim cho bài viết (mỗi thành viên tối đa 1 tim, thả lần 2 = bỏ tim).
    /// </summary>
    public async Task<VoteResponseData> VotePostAsync(int postId, int userId, CancellationToken ct = default)
    {
        // 1. Kiểm tra postId > 0
        if (postId <= 0)
        {
            throw AppException.BadRequest(ErrorCodes.ErrValidation, "Mã bài viết phải lớn hơn 0.");
        }

        // 2. Kiểm tra bài viết đích trong CSDL: Tồn tại và ở trạng thái APPROVED_ACTIVE (BR-INT-LIKE-02)
        var post = await _db.Posts.FirstOrDefaultAsync(p => p.PostId == postId, ct);
        if (post == null || post.ModerationStatus != "APPROVED_ACTIVE" || post.Visibility != "PUBLIC")
        {
            throw AppException.NotFound(ErrorCodes.ErrContentNotFound, "Nội dung không tồn tại hoặc không ở trạng thái hiển thị.");
        }

        // 3. Tra cứu bảng Votes: chưa có thì INSERT (voted = true), đã có thì DELETE (voted = false) (BR-INT-LIKE-01)
        var existingVote = await _db.Votes.FirstOrDefaultAsync(v => v.UserId == userId && v.PostId == postId, ct);
        bool voted;

        if (existingVote == null)
        {
            var vote = new Vote
            {
                UserId = userId,
                PostId = postId,
                CreatedAt = DateTime.UtcNow
            };
            _db.Votes.Add(vote);
            voted = true;
        }
        else
        {
            _db.Votes.Remove(existingVote);
            voted = false;
        }

        try
        {
            await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex)
        {
            _logger.LogWarning(ex, "Xung đột ghi đồng thời khi thả/bỏ tim bài viết {PostId} bởi người dùng {UserId}", postId, userId);
        }
        catch (Exception ex) when (ex is not AppException)
        {
            _logger.LogError(ex, "Lỗi kết nối cơ sở dữ liệu khi thao tác tim trên bài viết {PostId}", postId);
            throw AppException.Internal(ErrorCodes.ErrSysDatabaseUnavailable, "Không kết nối được SQL Server.");
        }

        // 4. Đếm số lượng vote mới nhất từ CSDL
        var voteCount = await _db.Votes.CountAsync(v => v.PostId == postId, ct);

        // 5. Cập nhật bộ nhớ đệm Redis: vote_count:post:{id} và xóa cache chi tiết bài viết
        try
        {
            await _redis.SetAsync($"vote_count:post:{postId}", voteCount, TimeSpan.FromHours(1), ct);
            await _redis.RemoveAsync($"post_detail:{postId}", ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Lỗi cập nhật Redis cache khi thả tim bài viết {PostId}", postId);
        }

        return new VoteResponseData
        {
            Voted = voted,
            VoteCount = voteCount
        };
    }

    /// <summary>
    /// FN-COMM-04: Thả tim hoặc bỏ tim cho công thức nấu ăn (tính qua bài viết đại diện của công thức).
    /// </summary>
    public async Task<VoteResponseData> VoteRecipeAsync(int recipeId, int userId, CancellationToken ct = default)
    {
        // 1. Kiểm tra recipeId > 0
        if (recipeId <= 0)
        {
            throw AppException.BadRequest(ErrorCodes.ErrValidation, "Mã công thức phải lớn hơn 0.");
        }

        // 2. Kiểm tra công thức đích trong CSDL: Tồn tại và ở trạng thái APPROVED_ACTIVE (BR-INT-LIKE-02)
        var recipe = await _db.Recipes.FirstOrDefaultAsync(r => r.RecipeId == recipeId, ct);
        if (recipe == null || recipe.Status != "APPROVED_ACTIVE")
        {
            throw AppException.NotFound(ErrorCodes.ErrContentNotFound, "Nội dung không tồn tại hoặc không ở trạng thái hiển thị.");
        }

        // 3. Tìm hoặc khởi tạo bài viết đại diện (Representative Post) cho công thức
        var repPost = await _db.Posts.FirstOrDefaultAsync(p => p.RecipeId == recipeId, ct);
        if (repPost == null)
        {
            repPost = new Post
            {
                UserId = recipe.UserId ?? userId,
                RecipeId = recipe.RecipeId,
                Title = recipe.Title,
                Content = recipe.Description ?? recipe.Title,
                ImageUrl = recipe.ImageUrl,
                Visibility = "PUBLIC",
                ModerationStatus = "APPROVED_ACTIVE",
                CreatedAt = DateTime.UtcNow
            };
            _db.Posts.Add(repPost);
            try
            {
                await _db.SaveChangesAsync(ct);
            }
            catch (Exception ex) when (ex is not AppException)
            {
                _logger.LogError(ex, "Lỗi tạo bài viết đại diện cho công thức {RecipeId}", recipeId);
                throw AppException.Internal(ErrorCodes.ErrSysDatabaseUnavailable, "Không kết nối được SQL Server.");
            }
        }

        // 4. Tra cứu bảng Votes cho Post đại diện (BR-INT-LIKE-01)
        var existingVote = await _db.Votes.FirstOrDefaultAsync(v => v.UserId == userId && v.PostId == repPost.PostId, ct);
        bool voted;

        if (existingVote == null)
        {
            var vote = new Vote
            {
                UserId = userId,
                PostId = repPost.PostId,
                CreatedAt = DateTime.UtcNow
            };
            _db.Votes.Add(vote);
            voted = true;
        }
        else
        {
            _db.Votes.Remove(existingVote);
            voted = false;
        }

        try
        {
            await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex)
        {
            _logger.LogWarning(ex, "Xung đột ghi đồng thời khi thả/bỏ tim công thức {RecipeId} bởi người dùng {UserId}", recipeId, userId);
        }
        catch (Exception ex) when (ex is not AppException)
        {
            _logger.LogError(ex, "Lỗi kết nối cơ sở dữ liệu khi thao tác tim trên công thức {RecipeId}", recipeId);
            throw AppException.Internal(ErrorCodes.ErrSysDatabaseUnavailable, "Không kết nối được SQL Server.");
        }

        // 5. Đếm số lượng vote mới nhất của bài viết đại diện
        var voteCount = await _db.Votes.CountAsync(v => v.PostId == repPost.PostId, ct);

        // 6. Cập nhật Redis cache
        try
        {
            await _redis.SetAsync($"vote_count:recipe:{recipeId}", voteCount, TimeSpan.FromHours(1), ct);
            await _redis.RemoveAsync($"recipe_detail:{recipeId}", ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Lỗi cập nhật Redis cache khi thả tim công thức {RecipeId}", recipeId);
        }

        return new VoteResponseData
        {
            Voted = voted,
            VoteCount = voteCount
        };
    }

    /// <summary>
    /// FN-COMM-05: Gửi báo cáo vi phạm nội dung (bài viết, công thức, video hoặc bình luận).
    /// </summary>
    public async Task<ReportResponseData> CreateReportAsync(CreateReportRequest request, int userId, CancellationToken ct = default)
    {
        // 1. Kiểm tra tính hợp lệ của dữ liệu đầu vào (Validation)
        if (request == null)
        {
            throw AppException.BadRequest(ErrorCodes.ErrValidation, "Dữ liệu đầu vào không hợp lệ.");
        }

        var normalizedTargetType = request.TargetType?.Trim().ToUpperInvariant();
        if (string.IsNullOrEmpty(normalizedTargetType) || !ValidTargetTypes.Contains(normalizedTargetType))
        {
            throw AppException.BadRequest(ErrorCodes.ErrValidation, "Loại nội dung báo cáo không hợp lệ (POST, RECIPE, VIDEO, COMMENT).");
        }

        if (request.TargetId <= 0)
        {
            throw AppException.BadRequest(ErrorCodes.ErrValidation, "Mã đối tượng báo cáo phải lớn hơn 0.");
        }

        var normalizedViolationType = request.ViolationType?.Trim().ToUpperInvariant();
        if (string.IsNullOrEmpty(normalizedViolationType) || !ValidViolationTypes.Contains(normalizedViolationType))
        {
            throw AppException.BadRequest(ErrorCodes.ErrValidation, "Loại vi phạm không hợp lệ (VIO_MEAT, VIO_LABEL, VIO_HATE, VIO_SPAM).");
        }

        var normalizedEvidenceSummary = request.EvidenceSummary?.Trim();
        if (string.IsNullOrEmpty(normalizedEvidenceSummary) || normalizedEvidenceSummary.Length < 10 || normalizedEvidenceSummary.Length > 500)
        {
            throw AppException.BadRequest(ErrorCodes.ErrValidation, "Nội dung tóm tắt chứng cứ vi phạm phải từ 10 đến 500 ký tự.");
        }

        // 2. Kiểm tra giới hạn tối đa 10 báo cáo/ngày/tài khoản (BR-USR-04)
        var todayUtc = DateTime.UtcNow.Date;
        var tomorrowUtc = todayUtc.AddDays(1);
        var todayReportCount = await _db.ViolationReports
            .CountAsync(r => r.ReporterUserId == userId && r.CreatedAt >= todayUtc && r.CreatedAt < tomorrowUtc, ct);

        if (todayReportCount >= 10)
        {
            throw AppException.TooManyRequests(ErrorCodes.ErrReportDailyLimit, "Quá 10 báo cáo trong 1 ngày.");
        }

        // 3. Kiểm tra đối tượng đích có tồn tại và đang ở trạng thái hiển thị hay không (ERR_CONTENT_NOT_FOUND)
        Post? targetPost = null;
        Recipe? targetRecipe = null;
        Video? targetVideo = null;
        Comment? targetComment = null;

        switch (normalizedTargetType)
        {
            case "POST":
                targetPost = await _db.Posts.FirstOrDefaultAsync(p => p.PostId == request.TargetId, ct);
                if (targetPost == null || targetPost.ModerationStatus == "DELETED")
                {
                    throw AppException.NotFound(ErrorCodes.ErrContentNotFound, "Nội dung không tồn tại hoặc không ở trạng thái hiển thị.");
                }
                break;

            case "RECIPE":
                targetRecipe = await _db.Recipes.FirstOrDefaultAsync(r => r.RecipeId == request.TargetId, ct);
                if (targetRecipe == null || targetRecipe.Status == "DELETED")
                {
                    throw AppException.NotFound(ErrorCodes.ErrContentNotFound, "Nội dung không tồn tại hoặc không ở trạng thái hiển thị.");
                }
                break;

            case "VIDEO":
                targetVideo = await _db.Videos.FirstOrDefaultAsync(v => v.VideoId == request.TargetId, ct);
                if (targetVideo == null || targetVideo.Status == "DELETED" || targetVideo.Status == "BANNED")
                {
                    throw AppException.NotFound(ErrorCodes.ErrContentNotFound, "Nội dung không tồn tại hoặc không ở trạng thái hiển thị.");
                }
                break;

            case "COMMENT":
                targetComment = await _db.Comments.FirstOrDefaultAsync(c => c.CommentId == request.TargetId, ct);
                if (targetComment == null || targetComment.IsSoftDeleted || targetComment.Status == "DELETED")
                {
                    throw AppException.NotFound(ErrorCodes.ErrContentNotFound, "Nội dung không tồn tại hoặc không ở trạng thái hiển thị.");
                }
                break;
        }

        // 4. Kiểm tra báo cáo trùng lặp: một tài khoản không báo cáo trùng lặp cùng một nội dung (ERR_REPORT_DUPLICATE)
        var isDuplicate = await _db.ViolationReports.AnyAsync(r =>
            r.ReporterUserId == userId &&
            (
                (normalizedTargetType == "POST" && r.PostId == request.TargetId) ||
                (normalizedTargetType == "RECIPE" && r.RecipeId == request.TargetId) ||
                (normalizedTargetType == "VIDEO" && r.VideoId == request.TargetId) ||
                (normalizedTargetType == "COMMENT" && r.CommentId == request.TargetId)
            ), ct);

        if (isDuplicate)
        {
            throw AppException.Conflict(ErrorCodes.ErrReportDuplicate, "Bạn đã báo cáo nội dung này.");
        }

        // 5. Khởi tạo bản ghi ViolationReports (Report_Source = USER_REPORT, Status = PENDING)
        var report = new ViolationReport
        {
            ReporterUserId = userId,
            ViolationType = normalizedViolationType,
            EvidenceSummary = normalizedEvidenceSummary,
            ReportSource = "USER_REPORT",
            Status = "PENDING",
            CreatedAt = DateTime.UtcNow,
            PostId = normalizedTargetType == "POST" ? request.TargetId : null,
            RecipeId = normalizedTargetType == "RECIPE" ? request.TargetId : null,
            VideoId = normalizedTargetType == "VIDEO" ? request.TargetId : null,
            CommentId = normalizedTargetType == "COMMENT" ? request.TargetId : null
        };
        _db.ViolationReports.Add(report);

        // 6. Ẩn nội dung ngay lập tức (BR-INT-REPORT-01, BR-ADM-CNT-001, BR-ADM-CMT-001)
        if (normalizedTargetType == "POST" && targetPost != null)
        {
            targetPost.ModerationStatus = "QUARANTINED_HIDDEN";
            targetPost.UpdatedAt = DateTime.UtcNow;
        }
        else if (normalizedTargetType == "RECIPE" && targetRecipe != null)
        {
            targetRecipe.Status = "QUARANTINED_HIDDEN";
            targetRecipe.UpdatedAt = DateTime.UtcNow;

            // Ẩn luôn bài viết đại diện liên kết nếu có
            var repPost = await _db.Posts.FirstOrDefaultAsync(p => p.RecipeId == request.TargetId, ct);
            if (repPost != null)
            {
                repPost.ModerationStatus = "QUARANTINED_HIDDEN";
                repPost.UpdatedAt = DateTime.UtcNow;
            }
        }
        else if (normalizedTargetType == "VIDEO" && targetVideo != null)
        {
            targetVideo.Status = "BANNED";
            targetVideo.UpdatedAt = DateTime.UtcNow;
        }
        else if (normalizedTargetType == "COMMENT" && targetComment != null)
        {
            targetComment.Status = "QUARANTINED_HIDDEN";
            targetComment.UpdatedAt = DateTime.UtcNow;

            // Cascade Soft-Delete / Quarantine toàn bộ cây bình luận phản hồi con (BR-ADM-CMT-001)
            await CascadeQuarantineCommentRepliesAsync(targetComment.CommentId, ct);
        }

        try
        {
            await _db.SaveChangesAsync(ct);
        }
        catch (Exception ex) when (ex is not AppException)
        {
            _logger.LogError(ex, "Lỗi kết nối CSDL khi lưu phiếu báo cáo vi phạm");
            throw AppException.Internal(ErrorCodes.ErrSysDatabaseUnavailable, "Không kết nối được SQL Server.");
        }

        // 7. Cập nhật Redis bộ đếm giới hạn báo cáo trong ngày và xóa cache liên quan
        try
        {
            var dateStr = DateTime.UtcNow.ToString("yyyyMMdd");
            await _redis.IncrementAsync($"report_count:{userId}:{dateStr}", 1, TimeSpan.FromHours(24), ct);

            if (normalizedTargetType == "POST")
            {
                await _redis.RemoveAsync($"post_detail:{request.TargetId}", ct);
            }
            else if (normalizedTargetType == "RECIPE")
            {
                await _redis.RemoveAsync($"recipe_detail:{request.TargetId}", ct);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Lỗi cập nhật Redis cache cho báo cáo vi phạm");
        }

        return new ReportResponseData
        {
            ReportId = report.ReportId,
            Status = report.Status
        };
    }

    /// <summary>
    /// Đệ quy ẩn toàn bộ cây bình luận phản hồi con trực thuộc theo cơ chế Cascade (BR-ADM-CMT-001).
    /// </summary>
    private async Task CascadeQuarantineCommentRepliesAsync(int parentCommentId, CancellationToken ct)
    {
        var replies = await _db.Comments
            .Where(c => c.ParentCommentId == parentCommentId && !c.IsSoftDeleted && c.Status != "QUARANTINED_HIDDEN")
            .ToListAsync(ct);

        foreach (var reply in replies)
        {
            reply.Status = "QUARANTINED_HIDDEN";
            reply.UpdatedAt = DateTime.UtcNow;
            await CascadeQuarantineCommentRepliesAsync(reply.CommentId, ct);
        }
    }
}
