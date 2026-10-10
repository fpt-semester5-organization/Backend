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

namespace VeggieLife.BLL.Services.Posts;

/// <summary>
/// Dịch vụ xử lý tìm kiếm, lọc danh sách và xem chi tiết bài viết cộng đồng (FN-POST-05, FN-POST-06).
/// Tuân thủ các quy tắc nghiệp vụ: BR-GUEST-01, BR-ADM-CNT-001, BR-AUTH-03, BR-GUEST-AUTH-01.
/// </summary>
public class PostService : IPostService
{
    private readonly AppDbContext _db;
    private readonly IRedisService _redis;
    private readonly ILogger<PostService> _logger;

    private static readonly TimeSpan PostListCacheTtl = TimeSpan.FromMinutes(5); // BR-SYS-CACHE (TTL 5m)
    private static readonly TimeSpan GuestHistoryTtl = TimeSpan.FromHours(24);   // BR-GUEST-AUTH-01

    public PostService(AppDbContext db, IRedisService redis, ILogger<PostService> logger)
    {
        _db = db;
        _redis = redis;
        _logger = logger;
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
    /// Lớp bọc nội bộ phục vụ việc lưu trữ kết quả phân trang tìm kiếm vào cache Redis.
    /// </summary>
    private class PostSearchResultCache
    {
        public List<PostSummaryDto> Items { get; set; } = new();
        public int Total { get; set; }
    }
}
