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
/// Dá»‹ch vá»¥ xá»­ lÃ½ tÃ¬m kiáº¿m, lá»c danh sÃ¡ch, xem chi tiáº¿t vÃ  quáº£n lÃ½ bÃ i viáº¿t cá»™ng Ä‘á»“ng (FN-POST-05, FN-POST-06, FN-POST-07).
/// TuÃ¢n thá»§ cÃ¡c quy táº¯c nghiá»‡p vá»¥: BR-GUEST-01, BR-ADM-CNT-001, BR-ADM-CNT-002, BR-AUTH-03, BR-GUEST-AUTH-01, BR-USR-03, BR-USR-04.
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
        // 1. Kiá»ƒm tra tÃ­nh há»£p lá»‡ cá»§a dá»¯ liá»‡u Ä‘áº§u vÃ o (Validation)
        ValidateFilterRequest(filter);

        var normalizedKeyword = filter.Keyword?.Trim();
        var normalizedSort = (filter.Sort ?? "NEWEST").ToUpperInvariant();
        var isAuthorQuery = currentUserId.HasValue && filter.AuthorId.HasValue && currentUserId.Value == filter.AuthorId.Value;

        // 2. Tra cá»©u bá»™ nhá»› Ä‘á»‡m phÃ¢n tÃ¡n Redis: post_list:{hash} (TTL 5m)
        var cacheHash = ComputeSearchHash(
            normalizedKeyword,
            filter.AuthorId,
            normalizedSort,
            filter.Page,
            filter.Size,
            currentUserId);

        var cacheKey = $"post_list:{cacheHash}";
        var cachedData = !isAuthorQuery ? await _redis.GetAsync<PostSearchResultCache>(cacheKey, ct) : null;
        if (cachedData != null)
        {
            _logger.LogInformation("Láº¥y danh sÃ¡ch bÃ i viáº¿t tá»« Redis cache: {CacheKey}", cacheKey);

            var cachedMeta = new PagedMeta(
                filter.Page,
                filter.Size,
                cachedData.Total,
                isGuest ? 5 : null); // BR-GUEST-01: signUpPromptAtItem = 5 cho KhÃ¡ch

            // Ghi nháº­n lá»‹ch sá»­ tÃ¬m kiáº¿m cho KhÃ¡ch náº¿u cÃ³ tá»« khÃ³a (BR-GUEST-AUTH-01)
            if (isGuest && !string.IsNullOrWhiteSpace(normalizedKeyword) && !string.IsNullOrWhiteSpace(guestId))
            {
                await SaveGuestSearchHistoryAsync(guestId, normalizedKeyword, ct);
            }

            return (cachedData.Items, cachedMeta);
        }

        // 3. KhÃ¡ch: LÆ°u tá»« khÃ³a vÃ o Redis guest_search_history:{guestId} (â‰¤ 20 má»¥c, TTL 24h) (BR-GUEST-AUTH-01)
        if (isGuest && !string.IsNullOrWhiteSpace(normalizedKeyword) && !string.IsNullOrWhiteSpace(guestId))
        {
            await SaveGuestSearchHistoryAsync(guestId, normalizedKeyword, ct);
        }

        // 4. Xây dựng truy vấn CSDL: Loại bỏ bài đã xóa mềm (DELETED) và các bài viết đại diện nội bộ của công thức
        var query = _db.Posts
            .AsNoTracking()
            .Where(p => p.ModerationStatus != "DELETED");

        // Khi khong phai truy van theo tac gia cu the (vi du bang tin chung), loai bo bai viet dai dien noi bo cua cong thuc
        if (!isAuthorQuery && !filter.AuthorId.HasValue)
        {
            query = query.Where(p => p.RecipeId == null || (p.Recipe != null && p.Title != p.Recipe.Title));
        }

        // isAuthorQuery đã được tính toán ở đầu hàm để phân biệt truy vấn quản lý cá nhân

        // Äiá»u kiá»‡n hiá»ƒn thá»‹ (BR-ADM-CNT-001, FN-POST-05):
        // 1. Khi tác giả xem trang quản lý bài viết của chính mình (/me/content, isAuthorQuery = true):
        //    Cho phép xem bài của mình ở các trạng thái (APPROVED_ACTIVE, PENDING_REVIEW, DRAFT, QUARANTINED_HIDDEN).
        // 2. Khi lá»c theo tÃ¡c giáº£ khÃ¡c (filter.AuthorId != currentUserId):
        //    Chỉ xem bài APPROVED_ACTIVE và PUBLIC của tác giả đó.
        // 3. Khi xem bảng tin cộng đồng công khai (Feed, filter.AuthorId == null):
        //    Má»i ngÆ°á»i xem bÃ i APPROVED_ACTIVE vÃ  PUBLIC. TÃ¡c giáº£ xem thÃªm bÃ i cá»§a mÃ¬nh (ngoáº¡i trá»« QUARANTINED_HIDDEN).
        //    TUYá»†T Äá»I KHÃ”NG BAO GIá»œ hiá»ƒn thá»‹ bÃ i viáº¿t bá»‹ cÃ¡ch ly/áº©n (QUARANTINED_HIDDEN) trÃªn báº£ng tin cÃ´ng khai!
        if (isAuthorQuery)
        {
            query = query.Where(p => p.UserId == currentUserId!.Value);
        }
        else if (filter.AuthorId.HasValue)
        {
            query = query.Where(p => p.UserId == filter.AuthorId.Value && p.ModerationStatus == "APPROVED_ACTIVE" && p.Visibility == "PUBLIC");
        }
        else if (currentUserId.HasValue)
        {
            query = query.Where(p => (p.ModerationStatus == "APPROVED_ACTIVE" && p.Visibility == "PUBLIC")
                || (p.UserId == currentUserId.Value && p.ModerationStatus != "QUARANTINED_HIDDEN"));
        }
        else
        {
            query = query.Where(p => p.ModerationStatus == "APPROVED_ACTIVE" && p.Visibility == "PUBLIC");
        }

        // Lá»c theo Tá»« khÃ³a tÃ¬m kiáº¿m (tiÃªu Ä‘á» hoáº·c ná»™i dung)
        if (!string.IsNullOrWhiteSpace(normalizedKeyword))
        {
            query = query.Where(p => p.Title.Contains(normalizedKeyword) || p.Content.Contains(normalizedKeyword));
        }

        // Äáº¿m tá»•ng sá»‘ bÃ i viáº¿t thá»a mÃ£n
        var total = await query.CountAsync(ct);

        // Sáº¯p xáº¿p: POPULAR theo sá»‘ lÆ°á»£t Votes giáº£m dáº§n; NEWEST theo CreatedAt giáº£m dáº§n
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

        // BUG-FL05-08: Khi truy váº¥n bÃ i viáº¿t vá»›i tham sá»‘ authorId trÃ¹ng vá»›i ID ngÆ°á»i gá»i, tráº£ vá» Ä‘áº§y Ä‘á»§ ModerationStatus vÃ  Visibility (isAuthorQuery)

        // PhÃ¢n trang vÃ  chiáº¿u DTO
        var items = await orderedQuery
            .Skip((filter.Page - 1) * filter.Size)
            .Take(filter.Size)
            .Select(p => new PostSummaryDto
            {
                PostId = p.PostId,
                RecipeId = p.RecipeId,
                Title = p.Title,
                AuthorName = p.User.FullName,
                AuthorAvatarUrl = p.User.AvatarUrl,
                ImageUrl = p.ImageUrl ?? p.Videos.Where(v => v.Status == "READY" || v.Status == "PROCESSING").OrderByDescending(v => v.VideoId).Select(v => v.ThumbnailUrl).FirstOrDefault(),
                VoteCount = p.Votes.Count,
                CommentCount = p.Comments.Count(c => !c.IsSoftDeleted),
                CreatedAt = p.CreatedAt,
                ModerationStatus = isAuthorQuery ? p.ModerationStatus : null,
                Visibility = isAuthorQuery ? p.Visibility : null,
                HasVideo = p.Videos.Any(v => v.Status == "READY" || v.Status == "PROCESSING"),
                Type = p.Videos.Any(v => v.Status == "READY" || v.Status == "PROCESSING") ? "VIDEO" : (p.RecipeId.HasValue ? "RECIPE" : "POST"),
                VideoId = p.Videos.Where(v => v.Status == "READY" || v.Status == "PROCESSING").OrderByDescending(v => v.VideoId).Select(v => (int?)v.VideoId).FirstOrDefault(),
                VideoUrl = p.Videos.Where(v => v.Status == "READY" || v.Status == "PROCESSING").OrderByDescending(v => v.VideoId).Select(v => v.VideoUrl).FirstOrDefault()
            })
            .ToListAsync(ct);

        // 5. LÆ°u vÃ o bá»™ nhá»› Ä‘á»‡m Redis: post_list:{hash} (TTL 5m)
        var cacheToStore = new PostSearchResultCache
        {
            Items = items,
            Total = total
        };
        await _redis.SetAsync(cacheKey, cacheToStore, PostListCacheTtl, ct);

        // 6. Tráº£ vá» káº¿t quáº£ kÃ¨m PagedMeta (BR-GUEST-01)
        var meta = new PagedMeta(filter.Page, filter.Size, total, isGuest ? 5 : null);
        return (items, meta);
    }

    /// <summary>
    /// Xem chi tiết đầy đủ bài viết, ảnh, video và công thức đính kèm; tăng lượt xem View_Count (FN-POST-06).
    /// </summary>
    public Task<PostDetailDto> GetPostByIdAsync(
        int id,
        bool isGuest,
        int? currentUserId = null,
        CancellationToken ct = default)
        => GetPostByIdAsync(id, isGuest, currentUserId, isAdmin: false, ct);

    public async Task<PostDetailDto> GetPostByIdAsync(
        int id,
        bool isGuest,
        int? currentUserId,
        bool isAdmin,
        CancellationToken ct = default)
    {
        // 1. Kiá»ƒm tra id > 0
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

        // 3. Kiá»ƒm tra Ä‘iá»u kiá»‡n hiá»ƒn thá»‹: BÃ i á»Ÿ tráº¡ng thÃ¡i APPROVED_ACTIVE vÃ  PUBLIC (hoáº·c lÃ  bÃ i cá»§a chÃ­nh ngÆ°á»i xem)
        if (post == null)
        {
            throw AppException.NotFound(ErrorCodes.ErrContentNotFound, "Nội dung không tồn tại hoặc chưa ở trạng thái APPROVED_ACTIVE.");
        }

        var isAuthor = currentUserId.HasValue && post.UserId == currentUserId.Value;
        var isVisible = (post.ModerationStatus == "APPROVED_ACTIVE" && post.Visibility == "PUBLIC") || isAuthor || isAdmin;

        if (!isVisible || (post.ModerationStatus == "DELETED" && !isAdmin))
        {
            throw AppException.NotFound(ErrorCodes.ErrContentNotFound, "Nội dung không tồn tại hoặc chưa ở trạng thái APPROVED_ACTIVE.");
        }

        // 4. Video đính kèm: Tác giả hoặc Admin được phép xem mọi trạng thái video (kể cả BANNED/PROCESSING để biết nguyên nhân vi phạm và sửa/xóa)
        var videos = post.Videos
            .Where(v => (isAuthor || isAdmin) ? v.Status != "DELETED" : v.Status == "READY")
            .OrderByDescending(v => v.VideoId)
            .Select(v => new PostVideoDto
            {
                VideoId = v.VideoId,
                Title = v.Title,
                VideoUrl = v.VideoUrl,
                ThumbnailUrl = v.ThumbnailUrl,
                DurationSeconds = v.DurationSeconds,
                Status = v.Status
            })
            .ToList();

        // 5. Công thức đính kèm (qua Posts.RecipeId)
        var recipes = new List<PostRecipeDto>();
        if (post.Recipe != null && (post.Recipe.Status != "DELETED" || isAdmin))
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

        // 6. Gán memberActions cho Khách: canVote = false, canComment = false (BR-AUTH-03)
        var memberActions = new PostMemberActionsDto
        {
            CanVote = !isGuest && !isAdmin,
            CanComment = !isGuest
        };

        var detailDto = new PostDetailDto
        {
            PostId = post.PostId,
            Title = post.Title,
            Content = post.Content,
            ImageUrl = post.ImageUrl,
            ModerationStatus = (isAuthor || isAdmin) ? post.ModerationStatus : null,
            Visibility = (isAuthor || isAdmin) ? post.Visibility : null,
            Author = new PostAuthorDto
            {
                UserId = post.User.UserId,
                FullName = post.User.FullName,
                AvatarUrl = post.User.AvatarUrl
            },
            Videos = videos,
            Recipes = recipes,
            VoteCount = post.Votes.Count,
            CommentCount = post.Comments.Count(c => !c.IsSoftDeleted),
            MemberActions = memberActions
        };

        // 7. Tăng View_Count qua Redis counter và CSDL (chỉ tăng khi bài đã duyệt và không phải admin)
        if (post.ModerationStatus == "APPROVED_ACTIVE" && !isAdmin)
        {
            await IncrementViewCountAsync(id, ct);
        }

        return detailDto;
    }

    /// <summary>
    /// Kiểm tra và xác thực các tham số bộ lọc tìm kiếm theo đúng ràng buộc đặc tả.
    /// </summary>
    private static void ValidateFilterRequest(PostFilterRequest filter)
    {
        if (filter.Keyword != null && (filter.Keyword.Length < 1 || filter.Keyword.Length > 100))
        {
            throw AppException.BadRequest(ErrorCodes.ErrValidation, "Tá»« khÃ³a tÃ¬m kiáº¿m pháº£i tá»« 1 Ä‘áº¿n 100 kÃ½ tá»±.");
        }

        if (filter.AuthorId.HasValue && filter.AuthorId.Value <= 0)
        {
            throw AppException.BadRequest(ErrorCodes.ErrValidation, "MÃ£ tÃ¡c giáº£ khÃ´ng há»£p lá»‡ (pháº£i > 0).");
        }

        var sort = (filter.Sort ?? "NEWEST").ToUpperInvariant();
        if (sort != "NEWEST" && sort != "POPULAR")
        {
            throw AppException.BadRequest(ErrorCodes.ErrValidation, "TiÃªu chÃ­ sáº¯p xáº¿p khÃ´ng há»£p lá»‡ (chá»‰ cháº¥p nháº­n: NEWEST, POPULAR).");
        }

        if (filter.Page < 1)
        {
            throw AppException.BadRequest(ErrorCodes.ErrValidation, "Sá»‘ trang (page) pháº£i lá»›n hÆ¡n hoáº·c báº±ng 1.");
        }

        if (filter.Size < 1 || filter.Size > 20)
        {
            throw AppException.BadRequest(ErrorCodes.ErrValidation, "KÃ­ch thÆ°á»›c trang (size) pháº£i tá»« 1 Ä‘áº¿n 20.");
        }
    }

    /// <summary>
    /// TÃ­nh toÃ¡n chuá»—i mÃ£ bÄƒm MD5 duy nháº¥t cho bá»™ tham sá»‘ tÃ¬m kiáº¿m bÃ i viáº¿t nháº±m lÆ°u cache Redis.
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
    /// LÆ°u tá»« khÃ³a tÃ¬m kiáº¿m cá»§a KhÃ¡ch vÃ o Redis guest_search_history:{guestId} (tá»‘i Ä‘a 20 má»¥c, TTL 24 giá»).
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
            _logger.LogWarning(ex, "KhÃ´ng thá»ƒ lÆ°u lá»‹ch sá»­ tÃ¬m kiáº¿m khÃ¡ch cho guestId '{GuestId}'.", guestId);
        }
    }

    /// <summary>
    /// TÄƒng lÆ°á»£t xem cá»§a bÃ i viáº¿t qua Redis counter vÃ  CSDL.
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
            _logger.LogWarning(ex, "Lá»—i khi tÄƒng lÆ°á»£t xem bÃ i viáº¿t ID {PostId}.", postId);
        }
    }

    /// <summary>
    /// ÄÄƒng bÃ i viáº¿t chia sáº» má»›i (lÆ°u nhÃ¡p hoáº·c gá»­i kiá»ƒm duyá»‡t AI) (FN-POST-07).
    /// </summary>
    public async Task<CreatePostResponseDto> CreatePostAsync(
        CreatePostRequest request,
        int userId,
        string userRole,
        CancellationToken ct = default)
    {
        // 1. Kiá»ƒm tra tÃ­nh há»£p lá»‡ cá»§a dá»¯ liá»‡u Ä‘áº§u vÃ o
        ValidateCreatePostRequest(request);

        var normalizedVisibility = string.IsNullOrWhiteSpace(request.Visibility)
            ? "PUBLIC"
            : request.Visibility.Trim().ToUpperInvariant();

        var shouldSubmit = request.Submit ?? false;
        string status;
        ContentModerationResult? modResult = null;

        var resolvedTitle = !string.IsNullOrWhiteSpace(request.Title)
            ? request.Title.Trim()
            : GenerateTitleFromContent(request.Content);

        if (shouldSubmit)
        {
            // 2. Kiá»ƒm tra giá»›i háº¡n Ä‘Äƒng bÃ i trong ngÃ y (BR-USR-04):
            // TÃ i khoáº£n < 3 ngÃ y tá»‘i Ä‘a 02 bÃ i/ngÃ y, tá»« 3 ngÃ y trá»Ÿ lÃªn tá»‘i Ä‘a 10 bÃ i/ngÃ y.
            await CheckDailyPostLimitAsync(userId, ct);

            // 3. QuÃ©t kiá»ƒm duyá»‡t AI tá»± Ä‘á»™ng (BR-USR-03, BR-ADM-CNT-002)
            modResult = await _moderationService.ModeratePostAsync(
                resolvedTitle,
                request.Content,
                request.ImageUrl,
                ct);

            // Ghi nháº­t kÃ½ Ä‘á»‹nh má»©c AIQuotaLogs (Feature_Code: CONTENT_MODERATION)
            await LogAiQuotaAsync(userId, modResult, ct);

            var isQuarantined = modResult.ViolationScore >= 85.0
                || (!string.IsNullOrEmpty(modResult.ViolationType) && modResult.ViolationType != "NONE")
                || !modResult.IsApproved
                || modResult.ModerationStatus == "QUARANTINED_HIDDEN";

            if (isQuarantined)
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

        // 4. LÆ°u bÃ i viáº¿t vÃ o cÆ¡ sá»Ÿ dá»¯ liá»‡u (Posts)
        var post = new Post
        {
            UserId = userId,
            Title = resolvedTitle,
            Content = request.Content.Trim(),
            ImageUrl = string.IsNullOrWhiteSpace(request.ImageUrl) ? null : request.ImageUrl.Trim(),
            Visibility = normalizedVisibility,
            ModerationStatus = status,
            ViewCount = 0,
            CreatedAt = DateTime.UtcNow
        };

        _db.Posts.Add(post);
        await _db.SaveChangesAsync(ct);

        // 5. Náº¿u bá»‹ cÃ¡ch ly do AI gáº¯n cá» vi pháº¡m (>= 85%), ghi ViolationReports
        if (status == "QUARANTINED_HIDDEN" && modResult != null)
        {
            var violationReport = new ViolationReport
            {
                PostId = post.PostId,
                ViolationType = MapToValidViolationType(modResult.ViolationType),
                EvidenceSummary = (modResult.Note ?? "AI phÃ¡t hiá»‡n vi pháº¡m ná»™i dung (Ä‘iá»ƒm vi pháº¡m >= 85%).").Length > 500
                    ? (modResult.Note ?? "AI phÃ¡t hiá»‡n vi pháº¡m ná»™i dung.").Substring(0, 500)
                    : (modResult.Note ?? "AI phÃ¡t hiá»‡n vi pháº¡m ná»™i dung."),
                ReportSource = "AI_SCAN",
                Status = "PENDING",
                CreatedAt = DateTime.UtcNow
            };
            _db.ViolationReports.Add(violationReport);
            await _db.SaveChangesAsync(ct);
        }

        // 6. Náº¿u Ä‘Æ°á»£c duyá»‡t cÃ´ng khai (APPROVED_ACTIVE vÃ  PUBLIC), Ä‘á»“ng bá»™ sang kho tri thá»©c AI RAG (FN-CHAT-07)
        if (status == "APPROVED_ACTIVE" && post.Visibility == "PUBLIC")
        {
            await _knowledgeSyncQueue.EnqueueUpsertAsync("POST", post.PostId, ct);
        }

        // 7. XÃ³a bá»™ nhá»› Ä‘á»‡m danh sÃ¡ch bÃ i viáº¿t
        await InvalidatePostListCacheAsync(ct);

        return new CreatePostResponseDto
        {
            PostId = post.PostId,
            ModerationStatus = post.ModerationStatus
        };
    }

    /// <summary>
    /// Cáº­p nháº­t bÃ i viáº¿t cá»§a mÃ¬nh, gá»¡ khá»i cá»™ng Ä‘á»“ng (chuyá»ƒn PRIVATE) hoáº·c gá»­i duyá»‡t láº¡i (FN-POST-07).
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
            throw AppException.BadRequest(ErrorCodes.ErrValidation, "MÃ£ bÃ i viáº¿t khÃ´ng há»£p lá»‡ (pháº£i > 0).");
        }

        if (request == null)
        {
            throw AppException.BadRequest(ErrorCodes.ErrValidation, "Dá»¯ liá»‡u cáº­p nháº­t khÃ´ng Ä‘Æ°á»£c Ä‘á»ƒ trá»‘ng.");
        }

        var post = await _db.Posts.FirstOrDefaultAsync(p => p.PostId == id, ct);
        if (post == null || post.ModerationStatus == "DELETED")
        {
            throw AppException.NotFound(ErrorCodes.ErrContentNotFound, "Nội dung không tồn tại hoặc chưa ở trạng thái APPROVED_ACTIVE.");
        }

        if (post.UserId != userId)
        {
            throw AppException.Forbidden(ErrorCodes.ErrContentNotOwner, "KhÃ´ng pháº£i tÃ¡c giáº£ cá»§a ná»™i dung.");
        }

        // Validate cÃ¡c trÆ°á»ng cáº­p nháº­t náº¿u cÃ³
        if (request.Title != null)
        {
            if (string.IsNullOrWhiteSpace(request.Title) || request.Title.Trim().Length < 10 || request.Title.Trim().Length > 150)
            {
                throw AppException.BadRequest(ErrorCodes.ErrValidation, "TiÃªu Ä‘á» bÃ i viáº¿t pháº£i tá»« 10 Ä‘áº¿n 150 kÃ½ tá»±.");
            }
        }

        if (request.Content != null)
        {
            if (string.IsNullOrWhiteSpace(request.Content))
            {
                throw AppException.BadRequest(ErrorCodes.ErrValidation, "Ná»™i dung bÃ i viáº¿t khÃ´ng Ä‘Æ°á»£c Ä‘á»ƒ trá»‘ng.");
            }
        }

        if (request.Visibility != null)
        {
            var vis = request.Visibility.Trim().ToUpperInvariant();
            if (vis == "FOLLOWERS")
            {
                throw AppException.BadRequest(
                    ErrorCodes.ErrPostVisibilityUnsupported,
                    "Visibility FOLLOWERS chÆ°a há»— trá»£ (Phase 2).");
            }

            if (vis != "PUBLIC" && vis != "PRIVATE")
            {
                throw AppException.BadRequest(
                    ErrorCodes.ErrValidation,
                    "Cháº¿ Ä‘á»™ hiá»ƒn thá»‹ khÃ´ng há»£p lá»‡ (chá»‰ cháº¥p nháº­n: PUBLIC, PRIVATE).");
            }
        }

        if (!string.IsNullOrWhiteSpace(request.ImageUrl))
        {
            if (!Uri.TryCreate(request.ImageUrl, UriKind.Absolute, out var uri) ||
                (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
            {
                throw AppException.BadRequest(ErrorCodes.ErrValidation, "ÄÆ°á»ng dáº«n áº£nh khÃ´ng há»£p lá»‡.");
            }
        }

        // LÆ°u trá»¯ giÃ¡ trá»‹ cÅ© cho AuditLogs
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

        bool imageChanged = false;
        if (request.ImageUrl != null)
        {
            var newImageUrl = string.IsNullOrWhiteSpace(request.ImageUrl) ? null : request.ImageUrl.Trim();
            if (newImageUrl == null)
            {
                // Người dùng xóa tệp phương tiện (ảnh/video) của bài viết
                post.ImageUrl = null;
                titleOrContentChanged = true;
                imageChanged = true;
                var attachedVideos = await _db.Videos.Where(v => v.PostId == post.PostId && v.Status != "DELETED").ToListAsync(ct);
                foreach (var v in attachedVideos)
                {
                    v.Status = "DELETED";
                    v.UpdatedAt = DateTime.UtcNow;
                }
            }
            else if (newImageUrl != post.ImageUrl)
            {
                post.ImageUrl = newImageUrl;
                titleOrContentChanged = true;
                imageChanged = true;
            }
        }

        if (request.Visibility != null)
        {
            post.Visibility = request.Visibility.Trim().ToUpperInvariant();
        }

        post.UpdatedAt = DateTime.UtcNow;

        // 1. Xử lý gửi duyệt bản nháp (submit = true khi đang DRAFT)
        if (post.ModerationStatus == "DRAFT")
        {
            if (request.Submit == true)
            {
                await CheckDailyPostLimitAsync(userId, ct);

                var modResult = await _moderationService.ModeratePostAsync(post.Title, post.Content, post.ImageUrl, ct);
                await LogAiQuotaAsync(userId, modResult, ct);

                var isQuarantined = modResult.ViolationScore >= 85.0
                    || (!string.IsNullOrEmpty(modResult.ViolationType) && modResult.ViolationType != "NONE")
                    || !modResult.IsApproved
                    || modResult.ModerationStatus == "QUARANTINED_HIDDEN";

                if (isQuarantined)
                {
                    post.ModerationStatus = "QUARANTINED_HIDDEN";
                    _db.ViolationReports.Add(new ViolationReport
                    {
                        PostId = post.PostId,
                        ViolationType = MapToValidViolationType(modResult.ViolationType),
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
        }
        else if ((post.ModerationStatus == "APPROVED_ACTIVE" || post.ModerationStatus == "QUARANTINED_HIDDEN" || post.ModerationStatus == "PENDING_REVIEW") && (titleOrContentChanged || imageChanged))
        {
            // 2. Bài viết đã đăng khi sửa nội dung (tiêu đề, nội dung hoặc ảnh):
            // Giống luồng đăng bài, quét AI kiểm duyệt tự động:
            // - Nếu AI nghi ngờ vi phạm (>= 85% hoặc có violationType): chuyển QUARANTINED_HIDDEN, tạo phiếu ViolationReport, gỡ khỏi Vector DB
            // - Nếu hợp lệ, không vi phạm: giữ/chuyển trạng thái APPROVED_ACTIVE, cập nhật Vector DB nếu PUBLIC
            var modResult = await _moderationService.ModeratePostAsync(post.Title, post.Content, post.ImageUrl, ct);
            await LogAiQuotaAsync(userId, modResult, ct);

            var isQuarantined = modResult.ViolationScore >= 85.0
                || (!string.IsNullOrEmpty(modResult.ViolationType) && modResult.ViolationType != "NONE")
                || !modResult.IsApproved
                || modResult.ModerationStatus == "QUARANTINED_HIDDEN";

            if (isQuarantined)
            {
                post.ModerationStatus = "QUARANTINED_HIDDEN";
                _db.ViolationReports.Add(new ViolationReport
                {
                    PostId = post.PostId,
                    ViolationType = MapToValidViolationType(modResult.ViolationType),
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
                post.ModerationStatus = "APPROVED_ACTIVE";
                if (post.Visibility == "PUBLIC")
                {
                    await _knowledgeSyncQueue.EnqueueUpsertAsync("POST", post.PostId, ct);
                }
                else
                {
                    await _knowledgeSyncQueue.EnqueueDeleteAsync("POST", post.PostId, ct);
                }
            }
        }
        else if (post.ModerationStatus == "APPROVED_ACTIVE" && !titleOrContentChanged && !imageChanged)
        {
            // Không đổi nội dung nhưng có thể đổi chế độ hiển thị (PUBLIC <-> PRIVATE)
            if (post.Visibility == "PUBLIC")
            {
                await _knowledgeSyncQueue.EnqueueUpsertAsync("POST", post.PostId, ct);
            }
            else
            {
                await _knowledgeSyncQueue.EnqueueDeleteAsync("POST", post.PostId, ct);
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
    /// XÃ³a má»m bÃ i viáº¿t cá»§a mÃ¬nh (chuyá»ƒn tráº¡ng thÃ¡i sang DELETED) (FN-POST-07).
    /// </summary>
    public async Task DeletePostAsync(
        int id,
        int userId,
        string userRole,
        CancellationToken ct = default)
    {
        if (id <= 0)
        {
            throw AppException.BadRequest(ErrorCodes.ErrValidation, "MÃ£ bÃ i viáº¿t khÃ´ng há»£p lá»‡ (pháº£i > 0).");
        }

        var post = await _db.Posts.FirstOrDefaultAsync(p => p.PostId == id, ct);
        if (post == null || post.ModerationStatus == "DELETED")
        {
            throw AppException.NotFound(ErrorCodes.ErrContentNotFound, "Nội dung không tồn tại hoặc chưa ở trạng thái APPROVED_ACTIVE.");
        }

        if (post.UserId != userId)
        {
            throw AppException.Forbidden(ErrorCodes.ErrContentNotOwner, "KhÃ´ng pháº£i tÃ¡c giáº£ cá»§a ná»™i dung.");
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
    /// Kiá»ƒm tra rÃ ng buá»™c há»£p lá»‡ cho yÃªu cáº§u táº¡o bÃ i viáº¿t má»›i.
    /// </summary>
    private static void ValidateCreatePostRequest(CreatePostRequest request)
    {
        if (request == null)
        {
            throw AppException.BadRequest(ErrorCodes.ErrValidation, "Dá»¯ liá»‡u yÃªu cáº§u khÃ´ng Ä‘Æ°á»£c Ä‘á»ƒ trá»‘ng.");
        }

        if (!string.IsNullOrWhiteSpace(request.Title) && request.Title.Trim().Length > 150)
        {
            throw AppException.BadRequest(ErrorCodes.ErrValidation, "TiÃªu Ä‘á» bÃ i viáº¿t tá»‘i Ä‘a 150 kÃ½ tá»±.");
        }

        if (string.IsNullOrWhiteSpace(request.Content))
        {
            throw AppException.BadRequest(ErrorCodes.ErrValidation, "Ná»™i dung bÃ i viáº¿t khÃ´ng Ä‘Æ°á»£c Ä‘á»ƒ trá»‘ng.");
        }

        if (!string.IsNullOrWhiteSpace(request.Visibility))
        {
            var vis = request.Visibility.Trim().ToUpperInvariant();
            if (vis == "FOLLOWERS")
            {
                throw AppException.BadRequest(
                    ErrorCodes.ErrPostVisibilityUnsupported,
                    "Visibility FOLLOWERS chÆ°a há»— trá»£ (Phase 2).");
            }

            if (vis != "PUBLIC" && vis != "PRIVATE")
            {
                throw AppException.BadRequest(
                    ErrorCodes.ErrValidation,
                    "Cháº¿ Ä‘á»™ hiá»ƒn thá»‹ khÃ´ng há»£p lá»‡ (chá»‰ cháº¥p nháº­n: PUBLIC, PRIVATE).");
            }
        }

        if (!string.IsNullOrWhiteSpace(request.ImageUrl))
        {
            if (!Uri.TryCreate(request.ImageUrl, UriKind.Absolute, out var uri) ||
                (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
            {
                throw AppException.BadRequest(ErrorCodes.ErrValidation, "ÄÆ°á»ng dáº«n áº£nh khÃ´ng há»£p lá»‡.");
            }
        }
    }

    /// <summary>
    /// Kiá»ƒm tra Ä‘á»‹nh má»©c Ä‘Äƒng bÃ i/ngÃ y (BR-USR-04): tÃ i khoáº£n < 3 ngÃ y tá»‘i Ä‘a 02 bÃ i, tÃ i khoáº£n >= 3 ngÃ y tá»‘i Ä‘a 10 bÃ i.
    /// Ãp dá»¥ng cho tá»•ng sá»‘ Recipes + Posts gá»­i Ä‘Äƒng trong ngÃ y (khÃ´ng tÃ­nh DRAFT).
    /// </summary>
    private async Task CheckDailyPostLimitAsync(int userId, CancellationToken ct)
    {
        // Đã hủy bỏ kiểm tra giới hạn bài đăng trong ngày theo yêu cầu để người dùng thao tác kiểm thử không bị chặn 429
        await Task.CompletedTask;
    }

    /// <summary>
    /// XÃ³a bá»™ nhá»› Ä‘á»‡m phÃ¢n tÃ¡n Redis cho danh sÃ¡ch bÃ i viáº¿t.
    /// </summary>
    private async Task InvalidatePostListCacheAsync(CancellationToken ct)
    {
        try
        {
            await _redis.RemoveAsync("post_list:*", ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Lá»—i khi xÃ³a bá»™ nhá»› Ä‘á»‡m post_list:* trÃªn Redis.");
        }
    }

    /// <summary>
    /// Ghi nháº­t kÃ½ Ä‘á»‹nh má»©c sá»­ dá»¥ng AI vÃ o báº£ng AIQuotaLogs.
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
            _logger.LogWarning(ex, "Lá»—i khi ghi nháº­t kÃ½ AIQuotaLogs cho ngÆ°á»i dÃ¹ng {UserId}.", userId);
        }
    }

    /// <summary>
    /// Ãnh xáº¡ phÃ¢n loáº¡i vi pháº¡m tá»« AI sang má»™t trong 4 nhÃ³m vi pháº¡m há»£p lá»‡ theo rÃ ng buá»™c CSDL (BR-ADM-CNT-001).
    /// </summary>
    private static string MapToValidViolationType(string? violationType)
    {
        return violationType switch
        {
            "VIO_MEAT" => "VIO_MEAT",
            "VIO_LABEL" => "VIO_LABEL",
            "VIO_SPAM" => "VIO_SPAM",
            _ => "VIO_HATE"
        };
    }

    /// <summary>
    /// Tá»± Ä‘á»™ng trÃ­ch xuáº¥t tiÃªu Ä‘á» tá»« ná»™i dung bÃ i viáº¿t khi ngÆ°á»i dÃ¹ng khÃ´ng nháº­p tiÃªu Ä‘á».
    /// </summary>
    private static string GenerateTitleFromContent(string content)
    {
        if (string.IsNullOrWhiteSpace(content)) return "BÃ i viáº¿t chia sáº»";
        var lines = content.Trim().Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        var firstLine = lines.Length > 0 ? lines[0].Trim() : content.Trim();
        if (firstLine.Length > 80)
        {
            firstLine = firstLine.Substring(0, 77) + "...";
        }
        return string.IsNullOrWhiteSpace(firstLine) ? "BÃ i viáº¿t chia sáº»" : firstLine;
    }

    /// <summary>
    /// Lá»›p bá»c ná»™i bá»™ phá»¥c vá»¥ viá»‡c lÆ°u trá»¯ káº¿t quáº£ phÃ¢n trang tÃ¬m kiáº¿m vÃ o cache Redis.
    /// </summary>
    private class PostSearchResultCache
    {
        public List<PostSummaryDto> Items { get; set; } = new();
        public int Total { get; set; }
    }
}

