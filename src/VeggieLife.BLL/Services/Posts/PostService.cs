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
using VeggieLife.BLL.DTOs.Posts.Requests;
using VeggieLife.BLL.DTOs.Posts.Responses;
using VeggieLife.BLL.Services.Cache;
using VeggieLife.DAL.Context;
using VeggieLife.DAL.Entities;

using System.Text.Json;
using VeggieLife.BLL.Services.Knowledge;
using VeggieLife.BLL.Services.Moderation;

namespace VeggieLife.BLL.Services.Posts;

/// <summary>
/// Dịch vụ xử lý tìm kiếm, lọc danh sách, xem chi tiết và quản lý bài viết cộng đồng (FN-POST-05, FN-POST-06, FN-POST-07).
/// Tuân thủ các quy tắc nghiệp vụ: BR-GUEST-01, BR-ADM-CNT-001, BR-ADM-CNT-002, BR-AUTH-03, BR-GUEST-AUTH-01, BR-USR-03, BR-USR-04.
/// </summary>
public class PostService : IPostService
{
    private readonly AppDbContext _db;
    private readonly IRedisService _redis;
    private readonly ILogger<PostService> _logger;
    private readonly IContentModerationService _moderationService;
    private readonly IKnowledgeSyncQueue _knowledgeSyncQueue;

    private static readonly TimeSpan PostListCacheTtl = TimeSpan.FromMinutes(5); // BR-SYS-CACHE (TTL 5m)
    private static readonly TimeSpan GuestHistoryTtl = TimeSpan.FromHours(24);   // BR-GUEST-AUTH-01

    public PostService(
        AppDbContext db,
        IRedisService redis,
        ILogger<PostService> logger,
        IContentModerationService? moderationService = null,
        IKnowledgeSyncQueue? knowledgeSyncQueue = null)
    {
        _db = db;
        _redis = redis;
        _logger = logger;
        _moderationService = moderationService ?? new SimulatedContentModerationService(new Microsoft.Extensions.Logging.Abstractions.NullLogger<SimulatedContentModerationService>());
        _knowledgeSyncQueue = knowledgeSyncQueue ?? new KnowledgeSyncQueueStub(new Microsoft.Extensions.Logging.Abstractions.NullLogger<KnowledgeSyncQueueStub>());
    }

    /// <summary>
    /// Tìm kiếm và xem danh sách bài viết chia sẻ đã duyệt, có phân trang, lọc theo từ khóa và tác giả (FN-POST-05).
    /// </summary>
    public async Task<(IReadOnlyList<PostSummaryDto> Items, PagedMeta Meta)> ListPostsAsync(
        PostFilterRequest filter,
        bool isGuest,
        string? guestId,
        int? currentUserId = null,
        CancellationToken ct = default)
    {
        // 1. Kiểm tra tính hợp lệ của dữ liệu đầu vào (Validation)
        ValidateFilterRequest(filter);

        var normalizedKeyword = filter.Keyword?.Trim();
        var normalizedSort = (filter.Sort ?? "NEWEST").ToUpperInvariant();

        // 2. Tra cứu bộ nhớ đệm phân tán Redis: post_list:{hash} (TTL 5m)
        var cacheHash = ComputeSearchHash(
            normalizedKeyword,
            filter.AuthorId,
            normalizedSort,
            filter.Page,
            filter.Size,
            currentUserId);

        var cacheKey = $"post_list:{cacheHash}";
        var cachedData = await _redis.GetAsync<PostSearchResultCache>(cacheKey, ct);
        if (cachedData != null)
        {
            _logger.LogInformation("Lấy danh sách bài viết từ Redis cache: {CacheKey}", cacheKey);

            var cachedMeta = new PagedMeta(
                filter.Page,
                filter.Size,
                cachedData.Total,
                isGuest ? 5 : null); // BR-GUEST-01: signUpPromptAtItem = 5 cho Khách

            // Ghi nhận lịch sử tìm kiếm cho Khách nếu có từ khóa (BR-GUEST-AUTH-01)
            if (isGuest && !string.IsNullOrWhiteSpace(normalizedKeyword) && !string.IsNullOrWhiteSpace(guestId))
            {
                await SaveGuestSearchHistoryAsync(guestId, normalizedKeyword, ct);
            }

            return (cachedData.Items, cachedMeta);
        }

        // 3. Khách: Lưu từ khóa vào Redis guest_search_history:{guestId} (≤ 20 mục, TTL 24h) (BR-GUEST-AUTH-01)
        if (isGuest && !string.IsNullOrWhiteSpace(normalizedKeyword) && !string.IsNullOrWhiteSpace(guestId))
        {
            await SaveGuestSearchHistoryAsync(guestId, normalizedKeyword, ct);
        }

        // 4. Xây dựng truy vấn CSDL: Loại bỏ bài đã xóa mềm (DELETED)
        var query = _db.Posts
            .AsNoTracking()
            .Where(p => p.ModerationStatus != "DELETED");

        // Điều kiện hiển thị (BR-ADM-CNT-001):
        // Mọi người xem bài APPROVED_ACTIVE và PUBLIC. Tác giả đã đăng nhập xem thêm bài của chính mình.
        if (currentUserId.HasValue)
        {
            query = query.Where(p => (p.ModerationStatus == "APPROVED_ACTIVE" && p.Visibility == "PUBLIC") || p.UserId == currentUserId.Value);
        }
        else
        {
            query = query.Where(p => p.ModerationStatus == "APPROVED_ACTIVE" && p.Visibility == "PUBLIC");
        }

        // Lọc theo Tác giả (authorId)
        if (filter.AuthorId.HasValue)
        {
            query = query.Where(p => p.UserId == filter.AuthorId.Value);
        }

        // Lọc theo Từ khóa tìm kiếm (tiêu đề hoặc nội dung)
        if (!string.IsNullOrWhiteSpace(normalizedKeyword))
        {
            query = query.Where(p => p.Title.Contains(normalizedKeyword) || p.Content.Contains(normalizedKeyword));
        }

        // Đếm tổng số bài viết thỏa mãn
        var total = await query.CountAsync(ct);

        // Sắp xếp: POPULAR theo số lượt Votes giảm dần; NEWEST theo CreatedAt giảm dần
        IQueryable<Post> orderedQuery;
        if (normalizedSort == "POPULAR")
        {
            orderedQuery = query
                .OrderByDescending(p => p.Votes.Count)
                .ThenByDescending(p => p.CreatedAt);
        }
        else
        {
            orderedQuery = query
                .OrderByDescending(p => p.CreatedAt);
        }

        // Phân trang và chiếu DTO
        var items = await orderedQuery
            .Skip((filter.Page - 1) * filter.Size)
            .Take(filter.Size)
            .Select(p => new PostSummaryDto
            {
                PostId = p.PostId,
                Title = p.Title,
                AuthorName = p.User.FullName,
                ImageUrl = p.ImageUrl,
                VoteCount = p.Votes.Count,
                CommentCount = p.Comments.Count(c => !c.IsSoftDeleted),
                CreatedAt = p.CreatedAt
            })
            .ToListAsync(ct);

        // 5. Lưu vào bộ nhớ đệm Redis: post_list:{hash} (TTL 5m)
        var cacheToStore = new PostSearchResultCache
        {
            Items = items,
            Total = total
        };
        await _redis.SetAsync(cacheKey, cacheToStore, PostListCacheTtl, ct);

        // 6. Trả về kết quả kèm PagedMeta (BR-GUEST-01)
        var meta = new PagedMeta(filter.Page, filter.Size, total, isGuest ? 5 : null);
        return (items, meta);
    }

    /// <summary>
    /// Xem chi tiết đầy đủ bài viết, ảnh, video và công thức đính kèm; tăng lượt xem View_Count (FN-POST-06).
    /// </summary>
    public async Task<PostDetailDto> GetPostByIdAsync(
        int id,
        bool isGuest,
        int? currentUserId = null,
        CancellationToken ct = default)
    {
        // 1. Kiểm tra id > 0
        if (id <= 0)
        {
            throw AppException.BadRequest(ErrorCodes.ErrValidation, "Mã bài viết phải lớn hơn 0.");
        }

        // 2. Truy vấn CSDL: SELECT Posts + Users (tác giả) + Videos + Recipes (qua Posts.RecipeId) + Votes + Comments
        var post = await _db.Posts
            .Include(p => p.User)
            .Include(p => p.Videos)
            .Include(p => p.Recipe)
                .ThenInclude(r => r!.DietarySchool)
            .Include(p => p.Votes)
            .Include(p => p.Comments)
            .FirstOrDefaultAsync(p => p.PostId == id, ct);

        // 3. Kiểm tra điều kiện hiển thị: Bài ở trạng thái APPROVED_ACTIVE và PUBLIC (hoặc là bài của chính người xem)
        if (post == null)
        {
            throw AppException.NotFound(ErrorCodes.ErrContentNotFound, "Nội dung không tồn tại hoặc chưa ở trạng thái APPROVED_ACTIVE.");
        }

        var isAuthor = currentUserId.HasValue && post.UserId == currentUserId.Value;
        var isVisible = (post.ModerationStatus == "APPROVED_ACTIVE" && post.Visibility == "PUBLIC") || isAuthor;

        if (!isVisible || post.ModerationStatus == "DELETED")
        {
            throw AppException.NotFound(ErrorCodes.ErrContentNotFound, "Nội dung không tồn tại hoặc chưa ở trạng thái APPROVED_ACTIVE.");
        }

        // 4. Video đính kèm
        var videos = post.Videos
            .Where(v => v.Status == "READY")
            .OrderBy(v => v.VideoId)
            .Select(v => new PostVideoDto
            {
                VideoId = v.VideoId,
                Title = v.Title,
                VideoUrl = v.VideoUrl,
                ThumbnailUrl = v.ThumbnailUrl,
                DurationSeconds = v.DurationSeconds
            })
            .ToList();

        // 5. Công thức đính kèm (qua Posts.RecipeId)
        var recipes = new List<PostRecipeDto>();
        if (post.Recipe != null && post.Recipe.Status != "DELETED")
        {
            recipes.Add(new PostRecipeDto
            {
                RecipeId = post.Recipe.RecipeId,
                Title = post.Recipe.Title,
                ImageUrl = post.Recipe.ImageUrl,
                DietarySchool = post.Recipe.DietarySchool?.SchoolCode,
                CookTimeMinutes = post.Recipe.CookTimeMinutes
            });
        }

        // 6. Gắn memberActions cho Khách: canVote = false, canComment = false (BR-AUTH-03)
        var memberActions = new PostMemberActionsDto
        {
            CanVote = !isGuest,
            CanComment = !isGuest
        };

        var detailDto = new PostDetailDto
        {
            PostId = post.PostId,
            Title = post.Title,
            Content = post.Content,
            ImageUrl = post.ImageUrl,
            Author = new PostAuthorDto
            {
                UserId = post.User.UserId,
                FullName = post.User.FullName
            },
            Videos = videos,
            Recipes = recipes,
            VoteCount = post.Votes.Count,
            CommentCount = post.Comments.Count(c => !c.IsSoftDeleted),
            MemberActions = memberActions
        };

        // 7. Tăng View_Count qua Redis counter và CSDL
        await IncrementViewCountAsync(id, ct);

        return detailDto;
    }

    /// <summary>
    /// Kiểm tra và xác thực các tham số bộ lọc tìm kiếm theo đúng ràng buộc đặc tả.
    /// </summary>
    private static void ValidateFilterRequest(PostFilterRequest filter)
    {
        if (filter.Keyword != null && (filter.Keyword.Length < 1 || filter.Keyword.Length > 100))
        {
            throw AppException.BadRequest(ErrorCodes.ErrValidation, "Từ khóa tìm kiếm phải từ 1 đến 100 ký tự.");
        }

        if (filter.AuthorId.HasValue && filter.AuthorId.Value <= 0)
        {
            throw AppException.BadRequest(ErrorCodes.ErrValidation, "Mã tác giả không hợp lệ (phải > 0).");
        }

        var sort = (filter.Sort ?? "NEWEST").ToUpperInvariant();
        if (sort != "NEWEST" && sort != "POPULAR")
        {
            throw AppException.BadRequest(ErrorCodes.ErrValidation, "Tiêu chí sắp xếp không hợp lệ (chỉ chấp nhận: NEWEST, POPULAR).");
        }

        if (filter.Page < 1)
        {
            throw AppException.BadRequest(ErrorCodes.ErrValidation, "Số trang (page) phải lớn hơn hoặc bằng 1.");
        }

        if (filter.Size < 1 || filter.Size > 20)
        {
            throw AppException.BadRequest(ErrorCodes.ErrValidation, "Kích thước trang (size) phải từ 1 đến 20.");
        }
    }

    /// <summary>
    /// Tính toán chuỗi mã băm MD5 duy nhất cho bộ tham số tìm kiếm bài viết nhằm lưu cache Redis.
    /// </summary>
    private static string ComputeSearchHash(
        string? keyword,
        int? authorId,
        string sort,
        int page,
        int size,
        int? currentUserId)
    {
        var rawKey = $"{keyword?.ToLowerInvariant()}|{authorId}|{sort}|{page}|{size}|{currentUserId}";
        using var md5 = MD5.Create();
        var bytes = md5.ComputeHash(Encoding.UTF8.GetBytes(rawKey));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    /// <summary>
    /// Lưu từ khóa tìm kiếm của Khách vào Redis guest_search_history:{guestId} (tối đa 20 mục, TTL 24 giờ).
    /// </summary>
    private async Task SaveGuestSearchHistoryAsync(string guestId, string keyword, CancellationToken ct)
    {
        try
        {
            var key = $"guest_search_history:{guestId}";
            var history = await _redis.GetAsync<List<string>>(key, ct) ?? new List<string>();

            history.RemoveAll(k => string.Equals(k, keyword, StringComparison.OrdinalIgnoreCase));
            history.Insert(0, keyword);

            if (history.Count > 20)
            {
                history = history.Take(20).ToList();
            }

            await _redis.SetAsync(key, history, GuestHistoryTtl, ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Không thể lưu lịch sử tìm kiếm khách cho guestId '{GuestId}'.", guestId);
        }
    }

    /// <summary>
    /// Tăng lượt xem của bài viết qua Redis counter và CSDL.
    /// </summary>
    private async Task IncrementViewCountAsync(int postId, CancellationToken ct)
    {
        try
        {
            await _redis.IncrementAsync($"view_counter:post:{postId}", 1, TimeSpan.FromDays(7), ct);

            var entity = await _db.Posts.FindAsync(new object[] { postId }, ct);
            if (entity != null)
            {
                entity.ViewCount += 1;
                await _db.SaveChangesAsync(ct);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Lỗi khi tăng lượt xem bài viết ID {PostId}.", postId);
        }
    }

    /// <summary>
    /// Đăng bài viết chia sẻ mới (lưu nháp hoặc gửi kiểm duyệt AI) (FN-POST-07).
    /// </summary>
    public async Task<CreatePostResponseDto> CreatePostAsync(
        CreatePostRequest request,
        int userId,
        string userRole,
        CancellationToken ct = default)
    {
        // 1. Kiểm tra tính hợp lệ của dữ liệu đầu vào
        ValidateCreatePostRequest(request);

        var normalizedVisibility = string.IsNullOrWhiteSpace(request.Visibility)
            ? "PUBLIC"
            : request.Visibility.Trim().ToUpperInvariant();

        var shouldSubmit = request.Submit ?? false;
        string status;
        ContentModerationResult? modResult = null;

        if (shouldSubmit)
        {
            // 2. Kiểm tra giới hạn đăng bài trong ngày (BR-USR-04):
            // Tài khoản < 3 ngày tối đa 02 bài/ngày, từ 3 ngày trở lên tối đa 10 bài/ngày.
            await CheckDailyPostLimitAsync(userId, ct);

            // 3. Quét kiểm duyệt AI tự động (BR-USR-03, BR-ADM-CNT-002)
            modResult = await _moderationService.ModeratePostAsync(
                request.Title,
                request.Content,
                request.ImageUrl,
                ct);

            // Ghi nhật ký định mức AIQuotaLogs (Feature_Code: CONTENT_MODERATION)
            await LogAiQuotaAsync(userId, modResult, ct);

            if (modResult.ViolationScore >= 85.0)
            {
                status = "QUARANTINED_HIDDEN";
            }
            else
            {
                status = "APPROVED_ACTIVE";
            }
        }
        else
        {
            status = "DRAFT";
        }

        // 4. Lưu bài viết vào cơ sở dữ liệu (Posts)
        var post = new Post
        {
            UserId = userId,
            Title = request.Title.Trim(),
            Content = request.Content.Trim(),
            ImageUrl = string.IsNullOrWhiteSpace(request.ImageUrl) ? null : request.ImageUrl.Trim(),
            Visibility = normalizedVisibility,
            ModerationStatus = status,
            ViewCount = 0,
            CreatedAt = DateTime.UtcNow
        };

        _db.Posts.Add(post);
        await _db.SaveChangesAsync(ct);

        // 5. Nếu bị cách ly do AI gắn cờ vi phạm (>= 85%), ghi ViolationReports
        if (status == "QUARANTINED_HIDDEN" && modResult != null)
        {
            var violationReport = new ViolationReport
            {
                PostId = post.PostId,
                ViolationType = modResult.ViolationType ?? "VIO_HATE",
                EvidenceSummary = (modResult.Note ?? "AI phát hiện vi phạm nội dung (điểm vi phạm >= 85%).").Length > 500
                    ? (modResult.Note ?? "AI phát hiện vi phạm nội dung.").Substring(0, 500)
                    : (modResult.Note ?? "AI phát hiện vi phạm nội dung."),
                ReportSource = "AI_SCAN",
                Status = "PENDING",
                CreatedAt = DateTime.UtcNow
            };
            _db.ViolationReports.Add(violationReport);
            await _db.SaveChangesAsync(ct);
        }

        // 6. Nếu được duyệt công khai (APPROVED_ACTIVE và PUBLIC), đồng bộ sang kho tri thức AI RAG (FN-CHAT-07)
        if (status == "APPROVED_ACTIVE" && post.Visibility == "PUBLIC")
        {
            await _knowledgeSyncQueue.EnqueueUpsertAsync("POST", post.PostId, ct);
        }

        // 7. Xóa bộ nhớ đệm danh sách bài viết
        await InvalidatePostListCacheAsync(ct);

        return new CreatePostResponseDto
        {
            PostId = post.PostId,
            ModerationStatus = post.ModerationStatus
        };
    }

    /// <summary>
    /// Cập nhật bài viết của mình, gỡ khỏi cộng đồng (chuyển PRIVATE) hoặc gửi duyệt lại (FN-POST-07).
    /// </summary>
    public async Task<UpdatePostResponseDto> UpdatePostAsync(
        int id,
        UpdatePostRequest request,
        int userId,
        string userRole,
        CancellationToken ct = default)
    {
        if (id <= 0)
        {
            throw AppException.BadRequest(ErrorCodes.ErrValidation, "Mã bài viết không hợp lệ (phải > 0).");
        }

        if (request == null)
        {
            throw AppException.BadRequest(ErrorCodes.ErrValidation, "Dữ liệu cập nhật không được để trống.");
        }

        var post = await _db.Posts.FirstOrDefaultAsync(p => p.PostId == id, ct);
        if (post == null || post.ModerationStatus == "DELETED")
        {
            throw AppException.NotFound(ErrorCodes.ErrContentNotFound, "Nội dung không tồn tại hoặc chưa ở trạng thái APPROVED_ACTIVE.");
        }

        if (post.UserId != userId)
        {
            throw AppException.Forbidden(ErrorCodes.ErrContentNotOwner, "Không phải tác giả của nội dung.");
        }

        // Validate các trường cập nhật nếu có
        if (request.Title != null)
        {
            if (string.IsNullOrWhiteSpace(request.Title) || request.Title.Trim().Length < 10 || request.Title.Trim().Length > 150)
            {
                throw AppException.BadRequest(ErrorCodes.ErrValidation, "Tiêu đề bài viết phải từ 10 đến 150 ký tự.");
            }
        }

        if (request.Content != null)
        {
            if (string.IsNullOrWhiteSpace(request.Content))
            {
                throw AppException.BadRequest(ErrorCodes.ErrValidation, "Nội dung bài viết không được để trống.");
            }
        }

        if (request.Visibility != null)
        {
            var vis = request.Visibility.Trim().ToUpperInvariant();
            if (vis == "FOLLOWERS")
            {
                throw AppException.BadRequest(
                    ErrorCodes.ErrPostVisibilityUnsupported,
                    "Visibility FOLLOWERS chưa hỗ trợ (Phase 2).");
            }

            if (vis != "PUBLIC" && vis != "PRIVATE")
            {
                throw AppException.BadRequest(
                    ErrorCodes.ErrValidation,
                    "Chế độ hiển thị không hợp lệ (chỉ chấp nhận: PUBLIC, PRIVATE).");
            }
        }

        if (!string.IsNullOrWhiteSpace(request.ImageUrl))
        {
            if (!Uri.TryCreate(request.ImageUrl, UriKind.Absolute, out var uri) ||
                (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
            {
                throw AppException.BadRequest(ErrorCodes.ErrValidation, "Đường dẫn ảnh không hợp lệ.");
            }
        }

        // Lưu trữ giá trị cũ cho AuditLogs
        var oldValueJson = JsonSerializer.Serialize(new
        {
            title = post.Title,
            content = post.Content,
            imageUrl = post.ImageUrl,
            visibility = post.Visibility,
            moderationStatus = post.ModerationStatus
        });

        bool titleOrContentChanged = false;

        if (request.Title != null && request.Title.Trim() != post.Title)
        {
            post.Title = request.Title.Trim();
            titleOrContentChanged = true;
        }

        if (request.Content != null && request.Content.Trim() != post.Content)
        {
            post.Content = request.Content.Trim();
            titleOrContentChanged = true;
        }

        if (request.ImageUrl != null)
        {
            post.ImageUrl = string.IsNullOrWhiteSpace(request.ImageUrl) ? null : request.ImageUrl.Trim();
        }

        if (request.Visibility != null)
        {
            post.Visibility = request.Visibility.Trim().ToUpperInvariant();
        }

        post.UpdatedAt = DateTime.UtcNow;

        // Xử lý gửi duyệt bản nháp (submit = true khi đang DRAFT)
        if (request.Submit == true && post.ModerationStatus == "DRAFT")
        {
            await CheckDailyPostLimitAsync(userId, ct);

            var modResult = await _moderationService.ModeratePostAsync(post.Title, post.Content, post.ImageUrl, ct);
            await LogAiQuotaAsync(userId, modResult, ct);

            if (modResult.ViolationScore >= 85.0)
            {
                post.ModerationStatus = "QUARANTINED_HIDDEN";
                _db.ViolationReports.Add(new ViolationReport
                {
                    PostId = post.PostId,
                    ViolationType = modResult.ViolationType ?? "VIO_HATE",
                    EvidenceSummary = (modResult.Note ?? "AI phát hiện vi phạm nội dung.").Length > 500
                        ? (modResult.Note ?? "AI phát hiện vi phạm nội dung.").Substring(0, 500)
                        : (modResult.Note ?? "AI phát hiện vi phạm nội dung."),
                    ReportSource = "AI_SCAN",
                    Status = "PENDING",
                    CreatedAt = DateTime.UtcNow
                });
            }
            else
            {
                post.ModerationStatus = "APPROVED_ACTIVE";
                if (post.Visibility == "PUBLIC")
                {
                    await _knowledgeSyncQueue.EnqueueUpsertAsync("POST", post.PostId, ct);
                }
            }
        }
        else if (post.ModerationStatus == "APPROVED_ACTIVE" && titleOrContentChanged)
        {
            // Bài viết đã công khai sửa nội dung -> quét lại AI và chuyển PENDING_REVIEW (hoặc QUARANTINED_HIDDEN nếu vi phạm)
            var modResult = await _moderationService.ModeratePostAsync(post.Title, post.Content, post.ImageUrl, ct);
            await LogAiQuotaAsync(userId, modResult, ct);

            if (modResult.ViolationScore >= 85.0)
            {
                post.ModerationStatus = "QUARANTINED_HIDDEN";
                _db.ViolationReports.Add(new ViolationReport
                {
                    PostId = post.PostId,
                    ViolationType = modResult.ViolationType ?? "VIO_HATE",
                    EvidenceSummary = (modResult.Note ?? "AI phát hiện vi phạm nội dung.").Length > 500
                        ? (modResult.Note ?? "AI phát hiện vi phạm nội dung.").Substring(0, 500)
                        : (modResult.Note ?? "AI phát hiện vi phạm nội dung."),
                    ReportSource = "AI_SCAN",
                    Status = "PENDING",
                    CreatedAt = DateTime.UtcNow
                });
                await _knowledgeSyncQueue.EnqueueDeleteAsync("POST", post.PostId, ct);
            }
            else
            {
                post.ModerationStatus = "PENDING_REVIEW";
            }
        }

        var newValueJson = JsonSerializer.Serialize(new
        {
            title = post.Title,
            content = post.Content,
            imageUrl = post.ImageUrl,
            visibility = post.Visibility,
            moderationStatus = post.ModerationStatus
        });

        // Ghi nhật ký kiểm toán AuditLog
        _db.AuditLogs.Add(new AuditLog
        {
            ActorUserId = userId,
            ActorRole = userRole,
            Action = "UPDATE_POST",
            TargetTable = "Posts",
            TargetId = post.PostId.ToString(),
            OldValue = oldValueJson,
            NewValue = newValueJson,
            Note = "Cập nhật bài viết",
            CreatedAt = DateTime.UtcNow
        });

        await _db.SaveChangesAsync(ct);
        await InvalidatePostListCacheAsync(ct);

        return new UpdatePostResponseDto
        {
            PostId = post.PostId,
            ModerationStatus = post.ModerationStatus
        };
    }

    /// <summary>
    /// Xóa mềm bài viết của mình (chuyển trạng thái sang DELETED) (FN-POST-07).
    /// </summary>
    public async Task DeletePostAsync(
        int id,
        int userId,
        string userRole,
        CancellationToken ct = default)
    {
        if (id <= 0)
        {
            throw AppException.BadRequest(ErrorCodes.ErrValidation, "Mã bài viết không hợp lệ (phải > 0).");
        }

        var post = await _db.Posts.FirstOrDefaultAsync(p => p.PostId == id, ct);
        if (post == null || post.ModerationStatus == "DELETED")
        {
            throw AppException.NotFound(ErrorCodes.ErrContentNotFound, "Nội dung không tồn tại hoặc chưa ở trạng thái APPROVED_ACTIVE.");
        }

        if (post.UserId != userId)
        {
            throw AppException.Forbidden(ErrorCodes.ErrContentNotOwner, "Không phải tác giả của nội dung.");
        }

        var oldStatus = post.ModerationStatus;
        post.ModerationStatus = "DELETED";
        post.UpdatedAt = DateTime.UtcNow;

        // Ghi AuditLog
        _db.AuditLogs.Add(new AuditLog
        {
            ActorUserId = userId,
            ActorRole = userRole,
            Action = "DELETE_POST",
            TargetTable = "Posts",
            TargetId = post.PostId.ToString(),
            OldValue = JsonSerializer.Serialize(new { moderationStatus = oldStatus }),
            NewValue = JsonSerializer.Serialize(new { moderationStatus = "DELETED" }),
            Note = "Xóa mềm bài viết",
            CreatedAt = DateTime.UtcNow
        });

        await _db.SaveChangesAsync(ct);

        if (oldStatus == "APPROVED_ACTIVE")
        {
            await _knowledgeSyncQueue.EnqueueDeleteAsync("POST", post.PostId, ct);
        }

        await InvalidatePostListCacheAsync(ct);
    }

    /// <summary>
    /// Kiểm tra ràng buộc hợp lệ cho yêu cầu tạo bài viết mới.
    /// </summary>
    private static void ValidateCreatePostRequest(CreatePostRequest request)
    {
        if (request == null)
        {
            throw AppException.BadRequest(ErrorCodes.ErrValidation, "Dữ liệu yêu cầu không được để trống.");
        }

        if (string.IsNullOrWhiteSpace(request.Title) || request.Title.Trim().Length < 10 || request.Title.Trim().Length > 150)
        {
            throw AppException.BadRequest(ErrorCodes.ErrValidation, "Tiêu đề bài viết phải từ 10 đến 150 ký tự.");
        }

        if (string.IsNullOrWhiteSpace(request.Content))
        {
            throw AppException.BadRequest(ErrorCodes.ErrValidation, "Nội dung bài viết không được để trống.");
        }

        if (!string.IsNullOrWhiteSpace(request.Visibility))
        {
            var vis = request.Visibility.Trim().ToUpperInvariant();
            if (vis == "FOLLOWERS")
            {
                throw AppException.BadRequest(
                    ErrorCodes.ErrPostVisibilityUnsupported,
                    "Visibility FOLLOWERS chưa hỗ trợ (Phase 2).");
            }

            if (vis != "PUBLIC" && vis != "PRIVATE")
            {
                throw AppException.BadRequest(
                    ErrorCodes.ErrValidation,
                    "Chế độ hiển thị không hợp lệ (chỉ chấp nhận: PUBLIC, PRIVATE).");
            }
        }

        if (!string.IsNullOrWhiteSpace(request.ImageUrl))
        {
            if (!Uri.TryCreate(request.ImageUrl, UriKind.Absolute, out var uri) ||
                (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
            {
                throw AppException.BadRequest(ErrorCodes.ErrValidation, "Đường dẫn ảnh không hợp lệ.");
            }
        }
    }

    /// <summary>
    /// Kiểm tra định mức đăng bài/ngày (BR-USR-04): tài khoản < 3 ngày tối đa 02 bài, tài khoản >= 3 ngày tối đa 10 bài.
    /// Áp dụng cho tổng số Recipes + Posts gửi đăng trong ngày (không tính DRAFT).
    /// </summary>
    private async Task CheckDailyPostLimitAsync(int userId, CancellationToken ct)
    {
        var user = await _db.Users.FindAsync(new object[] { userId }, ct);
        if (user == null)
        {
            throw AppException.Unauthorized(ErrorCodes.ErrAuthInvalidToken, "Người dùng không tồn tại hoặc phiên đăng nhập không hợp lệ.");
        }

        var isNewAccount = (DateTime.UtcNow - user.CreatedAt).TotalDays < 3.0;
        var dailyLimit = isNewAccount ? 2 : 10;

        var todayStart = DateTime.UtcNow.Date;
        var todayEnd = todayStart.AddDays(1);

        var postsToday = await _db.Posts
            .CountAsync(p => p.UserId == userId
                && p.CreatedAt >= todayStart
                && p.CreatedAt < todayEnd
                && p.ModerationStatus != "DRAFT", ct);

        var recipesToday = await _db.Recipes
            .CountAsync(r => r.UserId == userId
                && r.CreatedAt >= todayStart
                && r.CreatedAt < todayEnd
                && r.Status != "DRAFT", ct);

        var totalSubmitted = postsToday + recipesToday;
        if (totalSubmitted >= dailyLimit)
        {
            throw AppException.TooManyRequests(
                ErrorCodes.ErrPostDailyLimitExceeded,
                "Vượt giới hạn số bài đăng trong ngày (BR-USR-04).");
        }
    }

    /// <summary>
    /// Xóa bộ nhớ đệm phân tán Redis cho danh sách bài viết.
    /// </summary>
    private async Task InvalidatePostListCacheAsync(CancellationToken ct)
    {
        try
        {
            await _redis.RemoveAsync("post_list:*", ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Lỗi khi xóa bộ nhớ đệm post_list:* trên Redis.");
        }
    }

    /// <summary>
    /// Ghi nhật ký định mức sử dụng AI vào bảng AIQuotaLogs.
    /// </summary>
    private async Task LogAiQuotaAsync(int userId, ContentModerationResult modResult, CancellationToken ct)
    {
        try
        {
            var quotaLog = new AiquotaLog
            {
                UserId = userId,
                FeatureCode = "CONTENT_MODERATION",
                TokensConsumed = modResult.TokensConsumed,
                CostEstimateUsd = modResult.CostEstimateUsd,
                RequestStatus = "SUCCESS",
                ExecutionTimeMs = modResult.ExecutionTimeMs,
                CreatedAt = DateTime.UtcNow
            };
            _db.AiquotaLogs.Add(quotaLog);
            await _db.SaveChangesAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Lỗi khi ghi nhật ký AIQuotaLogs cho người dùng {UserId}.", userId);
        }
    }

    /// <summary>
    /// Lớp bọc nội bộ phục vụ việc lưu trữ kết quả phân trang tìm kiếm vào cache Redis.
    /// </summary>
    private class PostSearchResultCache
    {
        public List<PostSummaryDto> Items { get; set; } = new();
        public int Total { get; set; }
    }
}

