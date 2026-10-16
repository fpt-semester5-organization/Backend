using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using VeggieLife.BLL.Common.Constants;
using VeggieLife.BLL.Common.Exceptions;
using VeggieLife.BLL.Common.Responses;
using VeggieLife.BLL.DTOs.Community.Requests;
using VeggieLife.BLL.DTOs.Community.Responses;
using VeggieLife.BLL.Services.Cache;
using VeggieLife.BLL.Services.Moderation;
using VeggieLife.DAL.Context;
using VeggieLife.DAL.Entities;

namespace VeggieLife.BLL.Services.Community;

/// <summary>
/// Triển khai dịch vụ quản lý bình luận cộng đồng đa tầng dưới bài viết và công thức nấu ăn.
/// Tuân thủ đầy đủ các chuẩn nghiệp vụ:
/// - BR-USR-03: Kiểm duyệt tự động qua AI (NLP), vi phạm >= 85% thì chuyển QUARANTINED_HIDDEN và tạo ViolationReports.
/// - BR-USR-04: Chống spam - giãn cách tối thiểu 15s giữa 2 lần bình luận, không quá 05 bình luận trùng nội dung trong 1 bài.
/// - BR-ADM-CMT-001: Xóa bình luận thì Cascade Soft-Delete toàn bộ phản hồi con trực thuộc để bảo toàn ngữ cảnh.
/// - BR-INT-LIKE-02: Không thao tác trên nội dung nháp, bị ẩn hoặc đã xóa mềm.
/// - BR-AUTH-03: Khách vãng lai chỉ đọc, thành viên chính thức mới được bình luận.
/// </summary>
public class CommentService : ICommentService
{
    private readonly AppDbContext _db;
    private readonly IRedisService _redisService;
    private readonly IContentModerationService _moderationService;
    private readonly ILogger<CommentService> _logger;

    public CommentService(
        AppDbContext db,
        IRedisService redisService,
        IContentModerationService moderationService,
        ILogger<CommentService> logger)
    {
        _db = db;
        _redisService = redisService;
        _moderationService = moderationService;
        _logger = logger;
    }

    /// <summary>
    /// FN-COMM-01: Lấy danh sách bình luận dạng cây (tối đa 2 cấp hiển thị) của bài viết chia sẻ.
    /// </summary>
    public async Task<(IReadOnlyList<CommentDto> Items, PagedMeta Meta)> ListPostCommentsAsync(
        int postId,
        int page = 1,
        int size = 20,
        CancellationToken ct = default)
    {
        // 1. Kiểm tra tham số đầu vào
        ValidatePagingParams(postId, page, size, "Mã bài viết");

        // 2. Kiểm tra bài viết đích: Phải tồn tại và ở trạng thái APPROVED_ACTIVE (BR-INT-LIKE-02)
        var post = await _db.Posts.AsNoTracking().FirstOrDefaultAsync(p => p.PostId == postId, ct);
        if (post == null || post.ModerationStatus != "APPROVED_ACTIVE")
        {
            throw AppException.NotFound(ErrorCodes.ErrContentNotFound, "Nội dung không tồn tại hoặc không ở trạng thái hiển thị.");
        }

        // 3. Truy vấn các bình luận gốc (ParentCommentId == null), chỉ lấy ACTIVE và chưa xóa mềm
        var rootQuery = _db.Comments
            .AsNoTracking()
            .Include(c => c.User)
            .Where(c => c.PostId == postId && c.ParentCommentId == null && c.Status == "ACTIVE" && !c.IsSoftDeleted);

        var total = await rootQuery.CountAsync(ct);

        // Phân trang theo bình luận gốc, sắp xếp theo thời gian tạo tăng dần (thứ tự hội thoại)
        var rootComments = await rootQuery
            .OrderBy(c => c.CreatedAt)
            .Skip((page - 1) * size)
            .Take(size)
            .ToListAsync(ct);

        if (rootComments.Count == 0)
        {
            return (new List<CommentDto>(), new PagedMeta(page, size, total));
        }

        var rootIds = rootComments.Select(c => c.CommentId).ToList();

        // 4. Lấy các câu trả lời trực thuộc các bình luận gốc đang hiển thị
        var replies = await _db.Comments
            .AsNoTracking()
            .Include(c => c.User)
            .Where(c => c.PostId == postId &&
                        c.ParentCommentId.HasValue &&
                        rootIds.Contains(c.ParentCommentId.Value) &&
                        c.Status == "ACTIVE" &&
                        !c.IsSoftDeleted)
            .OrderBy(c => c.CreatedAt)
            .ToListAsync(ct);

        var repliesLookup = replies.ToLookup(r => r.ParentCommentId!.Value);

        // 5. Gom cây bình luận tối đa 2 cấp hiển thị
        var items = rootComments.Select(root => new CommentDto
        {
            CommentId = root.CommentId,
            Author = new CommentAuthorDto
            {
                UserId = root.User?.UserId ?? root.UserId,
                FullName = root.User?.FullName ?? "Thành viên"
            },
            Content = root.Content,
            ParentCommentId = null,
            Status = root.Status,
            CreatedAt = root.CreatedAt,
            Replies = repliesLookup[root.CommentId].Select(reply => new CommentDto
            {
                CommentId = reply.CommentId,
                Author = new CommentAuthorDto
                {
                    UserId = reply.User?.UserId ?? reply.UserId,
                    FullName = reply.User?.FullName ?? "Thành viên"
                },
                Content = reply.Content,
                ParentCommentId = reply.ParentCommentId,
                Status = reply.Status,
                CreatedAt = reply.CreatedAt,
                Replies = new List<CommentDto>()
            }).ToList()
        }).ToList();

        return (items, new PagedMeta(page, size, total));
    }

    /// <summary>
    /// FN-COMM-01: Lấy danh sách bình luận dạng cây của công thức nấu ăn (tính qua bài viết đại diện).
    /// </summary>
    public async Task<(IReadOnlyList<CommentDto> Items, PagedMeta Meta)> ListRecipeCommentsAsync(
        int recipeId,
        int page = 1,
        int size = 20,
        CancellationToken ct = default)
    {
        // 1. Kiểm tra tham số đầu vào
        ValidatePagingParams(recipeId, page, size, "Mã công thức");

        // 2. Kiểm tra công thức đích: Phải tồn tại và ở trạng thái APPROVED_ACTIVE (BR-INT-LIKE-02)
        var recipe = await _db.Recipes.AsNoTracking().FirstOrDefaultAsync(r => r.RecipeId == recipeId, ct);
        if (recipe == null || recipe.Status != "APPROVED_ACTIVE")
        {
            throw AppException.NotFound(ErrorCodes.ErrContentNotFound, "Nội dung không tồn tại hoặc không ở trạng thái hiển thị.");
        }

        // 3. Tìm bài viết đại diện của công thức
        var repPost = await _db.Posts.AsNoTracking().FirstOrDefaultAsync(p => p.RecipeId == recipeId, ct);
        if (repPost == null)
        {
            return (new List<CommentDto>(), new PagedMeta(page, size, 0));
        }

        return await ListPostCommentsAsync(repPost.PostId, page, size, ct);
    }

    /// <summary>
    /// FN-COMM-02: Bình luận mới hoặc trả lời bình luận trên bài viết chia sẻ.
    /// </summary>
    public async Task<CommentDto> CreatePostCommentAsync(
        int postId,
        CreateCommentRequest request,
        int userId,
        CancellationToken ct = default)
    {
        // 1. Kiểm tra dữ liệu đầu vào
        if (postId <= 0)
        {
            throw AppException.BadRequest(ErrorCodes.ErrValidation, "Mã bài viết phải lớn hơn 0.");
        }

        ValidateContent(request?.Content);

        // 2. Kiểm tra bài viết đích: Phải tồn tại và APPROVED_ACTIVE
        var post = await _db.Posts.FirstOrDefaultAsync(p => p.PostId == postId, ct);
        if (post == null || post.ModerationStatus != "APPROVED_ACTIVE")
        {
            throw AppException.NotFound(ErrorCodes.ErrContentNotFound, "Nội dung không tồn tại hoặc không ở trạng thái hiển thị.");
        }

        // 3. Kiểm tra bình luận cha (nếu có)
        int? resolvedParentId = null;
        if (request!.ParentCommentId.HasValue)
        {
            var parent = await _db.Comments.FirstOrDefaultAsync(c => c.CommentId == request.ParentCommentId.Value, ct);
            if (parent == null || parent.IsSoftDeleted || parent.Status != "ACTIVE")
            {
                throw AppException.BadRequest(ErrorCodes.ErrValidation, "Bình luận cha không tồn tại hoặc đã bị ẩn.");
            }

            if (parent.PostId != postId)
            {
                throw AppException.BadRequest(ErrorCodes.ErrValidation, "Bình luận cha không thuộc cùng bài viết này.");
            }

            // Tối đa 2 cấp hiển thị: Nếu bình luận cha vốn đã là câu trả lời (cấp 2),
            // gán về bình luận gốc của nó để đảm bảo cây không vượt quá 2 cấp
            resolvedParentId = parent.ParentCommentId ?? parent.CommentId;
        }

        var trimmedContent = request.Content.Trim();

        // 4. Chống spam (BR-USR-04):
        // a) Giãn cách tối thiểu 15 giây giữa 2 bình luận liên tiếp
        await EnforceCooldownAsync(userId, ct);

        // b) Không quá 05 bình luận trùng nội dung trong cùng 1 bài
        await EnforceDuplicateLimitAsync(userId, postId, trimmedContent, ct);

        // 5. Quét AI tự động (BR-USR-03)
        var modResult = await _moderationService.ModeratePostAsync("Bình luận", trimmedContent, null, ct);
        var isQuarantined = modResult.ViolationScore >= 85.0 || !modResult.IsApproved;
        var initialStatus = isQuarantined ? "QUARANTINED_HIDDEN" : "ACTIVE";

        // 6. Lưu bình luận vào CSDL
        var comment = new Comment
        {
            UserId = userId,
            PostId = postId,
            ParentCommentId = resolvedParentId,
            Content = trimmedContent,
            Status = initialStatus,
            IsSoftDeleted = false,
            CreatedAt = DateTime.UtcNow
        };

        _db.Comments.Add(comment);
        await _db.SaveChangesAsync(ct);

        // 7. Nếu AI phát hiện vi phạm (>= 85%), tạo phiếu báo cáo ViolationReports (AI_SCAN)
        if (isQuarantined)
        {
            _logger.LogWarning("Bình luận {CommentId} bị cách ly QUARANTINED_HIDDEN do AI phát hiện vi phạm ({Score}%).",
                comment.CommentId, modResult.ViolationScore);

            var report = new ViolationReport
            {
                CommentId = comment.CommentId,
                PostId = postId,
                ViolationType = modResult.ViolationType ?? "VIO_HATE",
                EvidenceSummary = (modResult.Note ?? "AI phát hiện vi phạm nội dung (điểm vi phạm >= 85%).").Length > 500
                    ? (modResult.Note ?? "AI phát hiện vi phạm nội dung.").Substring(0, 500)
                    : (modResult.Note ?? "AI phát hiện vi phạm nội dung."),
                ReportSource = "AI_SCAN",
                Status = "PENDING",
                CreatedAt = DateTime.UtcNow
            };
            _db.ViolationReports.Add(report);
            await _db.SaveChangesAsync(ct);
        }

        // 8. Cập nhật Redis Cooldown (15 giây) và bộ đếm trùng lặp
        await RecordCommentActivityAsync(userId, postId, trimmedContent, ct);

        // 9. Lấy thông tin tác giả trả về DTO
        var author = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.UserId == userId, ct);

        return new CommentDto
        {
            CommentId = comment.CommentId,
            Author = new CommentAuthorDto
            {
                UserId = author?.UserId ?? userId,
                FullName = author?.FullName ?? "Thành viên"
            },
            Content = comment.Content,
            ParentCommentId = comment.ParentCommentId,
            Status = comment.Status,
            CreatedAt = comment.CreatedAt,
            Replies = new List<CommentDto>()
        };
    }

    /// <summary>
    /// FN-COMM-02: Bình luận mới hoặc trả lời bình luận trên công thức nấu ăn.
    /// </summary>
    public async Task<CommentDto> CreateRecipeCommentAsync(
        int recipeId,
        CreateCommentRequest request,
        int userId,
        CancellationToken ct = default)
    {
        // 1. Kiểm tra mã công thức
        if (recipeId <= 0)
        {
            throw AppException.BadRequest(ErrorCodes.ErrValidation, "Mã công thức phải lớn hơn 0.");
        }

        ValidateContent(request?.Content);

        // 2. Kiểm tra công thức đích: Phải tồn tại và APPROVED_ACTIVE (BR-INT-LIKE-02)
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
                throw AppException.Internal(ErrorCodes.ErrSysDatabaseUnavailable, "Không kết nối được cơ sở dữ liệu.");
            }
        }

        // 4. Ủy nhiệm tạo bình luận thông qua Post đại diện
        return await CreatePostCommentAsync(repPost.PostId, request!, userId, ct);
    }

    /// <summary>
    /// FN-COMM-03: Sửa nội dung bình luận của chính mình.
    /// </summary>
    public async Task<CommentDto> UpdateCommentAsync(
        int commentId,
        UpdateCommentRequest request,
        int userId,
        CancellationToken ct = default)
    {
        if (commentId <= 0)
        {
            throw AppException.BadRequest(ErrorCodes.ErrValidation, "Mã bình luận phải lớn hơn 0.");
        }

        ValidateContent(request?.Content);

        // 1. Kiểm tra bình luận tồn tại và chưa bị xóa mềm
        var comment = await _db.Comments
            .Include(c => c.User)
            .FirstOrDefaultAsync(c => c.CommentId == commentId && !c.IsSoftDeleted, ct);

        if (comment == null)
        {
            throw AppException.NotFound(ErrorCodes.ErrContentNotFound, "Nội dung không tồn tại hoặc không ở trạng thái hiển thị.");
        }

        // 2. Kiểm tra quyền sở hữu (User_ID); không phải thì 403 ERR_CONTENT_NOT_OWNER
        if (comment.UserId != userId)
        {
            throw AppException.Forbidden(ErrorCodes.ErrContentNotOwner, "Không phải tác giả của bình luận.");
        }

        var trimmedContent = request!.Content.Trim();

        // 3. Quét lại AI cho nội dung mới (BR-USR-03)
        var modResult = await _moderationService.ModeratePostAsync("Bình luận", trimmedContent, null, ct);
        var isQuarantined = modResult.ViolationScore >= 85.0 || !modResult.IsApproved;
        comment.Status = isQuarantined ? "QUARANTINED_HIDDEN" : "ACTIVE";

        if (isQuarantined)
        {
            var report = new ViolationReport
            {
                CommentId = comment.CommentId,
                PostId = comment.PostId,
                ViolationType = modResult.ViolationType ?? "VIO_HATE",
                EvidenceSummary = (modResult.Note ?? "AI phát hiện vi phạm nội dung khi chỉnh sửa (điểm vi phạm >= 85%).").Length > 500
                    ? (modResult.Note ?? "AI phát hiện vi phạm nội dung.").Substring(0, 500)
                    : (modResult.Note ?? "AI phát hiện vi phạm nội dung."),
                ReportSource = "AI_SCAN",
                Status = "PENDING",
                CreatedAt = DateTime.UtcNow
            };
            _db.ViolationReports.Add(report);
        }

        comment.Content = trimmedContent;
        comment.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);

        return new CommentDto
        {
            CommentId = comment.CommentId,
            Author = new CommentAuthorDto
            {
                UserId = comment.User?.UserId ?? comment.UserId,
                FullName = comment.User?.FullName ?? "Thành viên"
            },
            Content = comment.Content,
            ParentCommentId = comment.ParentCommentId,
            Status = comment.Status,
            CreatedAt = comment.CreatedAt,
            Replies = new List<CommentDto>()
        };
    }

    /// <summary>
    /// FN-COMM-03: Xóa mềm bình luận của chính mình và ẩn đệ quy toàn bộ phản hồi con (Cascade Soft-Delete - BR-ADM-CMT-001).
    /// </summary>
    public async Task<DeleteCommentResponseData> DeleteCommentAsync(
        int commentId,
        int userId,
        CancellationToken ct = default)
    {
        if (commentId <= 0)
        {
            throw AppException.BadRequest(ErrorCodes.ErrValidation, "Mã bình luận phải lớn hơn 0.");
        }

        // 1. Kiểm tra bình luận tồn tại và chưa bị xóa mềm
        var comment = await _db.Comments
            .FirstOrDefaultAsync(c => c.CommentId == commentId && !c.IsSoftDeleted, ct);

        if (comment == null)
        {
            throw AppException.NotFound(ErrorCodes.ErrContentNotFound, "Nội dung không tồn tại hoặc không ở trạng thái hiển thị.");
        }

        // 2. Kiểm tra quyền sở hữu (User_ID); không phải thì 403 ERR_CONTENT_NOT_OWNER
        if (comment.UserId != userId)
        {
            throw AppException.Forbidden(ErrorCodes.ErrContentNotOwner, "Không phải tác giả của bình luận.");
        }

        // 3. Cascade Soft-Delete đệ quy toàn bộ cây phản hồi con (BR-ADM-CMT-001)
        var hiddenCount = 0;
        var queue = new Queue<int>();
        queue.Enqueue(commentId);

        var now = DateTime.UtcNow;

        while (queue.Count > 0)
        {
            var parentId = queue.Dequeue();
            var replies = await _db.Comments
                .Where(c => c.ParentCommentId == parentId && !c.IsSoftDeleted)
                .ToListAsync(ct);

            foreach (var reply in replies)
            {
                reply.IsSoftDeleted = true;
                reply.UpdatedAt = now;
                hiddenCount++;
                queue.Enqueue(reply.CommentId);
            }
        }

        // Đánh dấu xóa mềm chính bình luận gốc
        comment.IsSoftDeleted = true;
        comment.UpdatedAt = now;

        await _db.SaveChangesAsync(ct);

        _logger.LogInformation("Người dùng {UserId} đã xóa bình luận {CommentId}, ẩn đồng thời {HiddenReplies} phản hồi con.",
            userId, commentId, hiddenCount);

        return new DeleteCommentResponseData
        {
            HiddenReplies = hiddenCount
        };
    }

    // =========================================================================
    // Các phương thức trợ giúp & Kiểm tra quy tắc nghiệp vụ
    // =========================================================================

    private static void ValidatePagingParams(int id, int page, int size, string targetName)
    {
        if (id <= 0)
        {
            throw AppException.BadRequest(ErrorCodes.ErrValidation, $"{targetName} phải lớn hơn 0.");
        }

        if (page < 1)
        {
            throw AppException.BadRequest(ErrorCodes.ErrValidation, "Số trang (page) phải lớn hơn hoặc bằng 1.");
        }

        if (size < 1 || size > 50)
        {
            throw AppException.BadRequest(ErrorCodes.ErrValidation, "Kích thước trang (size) phải nằm trong khoảng từ 1 đến 50.");
        }
    }

    private static void ValidateContent(string? content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            throw AppException.BadRequest(ErrorCodes.ErrValidation, "Nội dung bình luận không được để trống.");
        }

        var trimmed = content.Trim();
        if (trimmed.Length < 2 || trimmed.Length > 500)
        {
            throw AppException.BadRequest(ErrorCodes.ErrValidation, "Nội dung bình luận phải từ 2 đến 500 ký tự.");
        }
    }

    private async Task EnforceCooldownAsync(int userId, CancellationToken ct)
    {
        var cooldownKey = $"comment_cooldown:{userId}";
        var inCooldown = await _redisService.ExistsAsync(cooldownKey, ct);
        if (inCooldown)
        {
            throw AppException.TooManyRequests(
                ErrorCodes.ErrCommentTooFast,
                "Chưa đủ 15 giây giữa 2 bình luận.");
        }
    }

    private async Task EnforceDuplicateLimitAsync(int userId, int postId, string content, CancellationToken ct)
    {
        // 1. Kiểm tra số lượng bình luận trùng nội dung trong CSDL
        var dbCount = await _db.Comments.CountAsync(
            c => c.PostId == postId &&
                 c.UserId == userId &&
                 c.Content.Trim() == content &&
                 !c.IsSoftDeleted,
            ct);

        if (dbCount >= 5)
        {
            throw AppException.TooManyRequests(
                ErrorCodes.ErrCommentDuplicateLimit,
                "Quá 05 bình luận trùng nội dung trong 1 bài.");
        }

        // 2. Kiểm tra bộ đếm trên Redis
        var hash = ComputeContentHash(content);
        var dupKey = $"comment_dup:{userId}:{postId}:{hash}";
        var redisVal = await _redisService.GetAsync<long?>(dupKey, ct);
        if (redisVal.HasValue && redisVal.Value >= 5)
        {
            throw AppException.TooManyRequests(
                ErrorCodes.ErrCommentDuplicateLimit,
                "Quá 05 bình luận trùng nội dung trong 1 bài.");
        }
    }

    private async Task RecordCommentActivityAsync(int userId, int postId, string content, CancellationToken ct)
    {
        // Cooldown 15 giây
        var cooldownKey = $"comment_cooldown:{userId}";
        await _redisService.SetAsync(cooldownKey, "1", TimeSpan.FromSeconds(15), ct);

        // Tăng bộ đếm bình luận trùng nội dung
        var hash = ComputeContentHash(content);
        var dupKey = $"comment_dup:{userId}:{postId}:{hash}";
        await _redisService.IncrementAsync(dupKey, 1, TimeSpan.FromDays(1), ct);
    }

    private static string ComputeContentHash(string content)
    {
        using var sha = SHA256.Create();
        var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(content.ToLowerInvariant()));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
