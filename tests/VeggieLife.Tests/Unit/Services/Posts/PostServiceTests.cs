using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using VeggieLife.BLL.Common.Constants;
using VeggieLife.BLL.Common.Exceptions;
using VeggieLife.BLL.DTOs.Posts.Requests;
using VeggieLife.BLL.DTOs.Posts.Responses;
using VeggieLife.BLL.Services.Cache;
using VeggieLife.BLL.Services.Posts;
using VeggieLife.DAL.Context;
using VeggieLife.DAL.Entities;
using Xunit;

namespace VeggieLife.Tests.Unit.Services.Posts;

/// <summary>
/// Bộ kiểm thử đơn vị toàn diện cho PostService (FN-POST-05, FN-POST-06).
/// Kiểm tra tìm kiếm bài viết, lọc theo tác giả, từ khóa, sắp xếp POPULAR/NEWEST, phân trang,
/// điều kiện hiển thị APPROVED_ACTIVE & PUBLIC, quyền xem của tác giả, memberActions cho Khách và Thành viên,
/// và bộ đếm lượt xem ViewCount.
/// </summary>
public class PostServiceTests : IDisposable
{
    private readonly AppDbContext _db;
    private readonly Mock<IRedisService> _mockRedis;
    private readonly Mock<ILogger<PostService>> _mockLogger;
    private readonly PostService _service;

    public PostServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _db = new AppDbContext(options);
        _mockRedis = new Mock<IRedisService>();
        _mockLogger = new Mock<ILogger<PostService>>();

        _service = new PostService(_db, _mockRedis.Object, _mockLogger.Object);

        SeedTestData();
    }

    public void Dispose()
    {
        _db.Database.EnsureDeleted();
        _db.Dispose();
    }

    private void SeedTestData()
    {
        // 1. Tạo người dùng (Users)
        var user1 = new User
        {
            UserId = 1024,
            FullName = "Nguyễn An",
            Email = "nguyenan@example.com",
            Role = "MEMBER",
            AccountStatus = "ACTIVE",
            CreatedAt = DateTime.UtcNow.AddDays(-10)
        };

        var user2 = new User
        {
            UserId = 2048,
            FullName = "Trần Bình",
            Email = "tranbinh@example.com",
            Role = "MEMBER",
            AccountStatus = "ACTIVE",
            CreatedAt = DateTime.UtcNow.AddDays(-10)
        };

        _db.Users.AddRange(user1, user2);

        // 2. Tạo trường phái, danh mục và công thức liên kết (Recipes)
        var school = new DietarySchool
        {
            DietarySchoolId = 7,
            SchoolCode = "VEGAN",
            SchoolName = "Thuần chay",
            ForbiddenFoodRules = "{}",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _db.DietarySchools.Add(school);

        var category = new FoodCategory
        {
            CategoryId = 1,
            CategoryName = "Món canh",
            IsCore = true,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _db.FoodCategories.Add(category);

        var recipe = new Recipe
        {
            RecipeId = 50,
            UserId = 1024,
            CategoryId = 1,
            Category = category,
            DietarySchoolId = 7,
            DietarySchool = school,
            Title = "Canh chua chay thanh đạm",
            Instructions = "Nấu canh chua",
            PrepTimeMinutes = 10,
            CookTimeMinutes = 20,
            Servings = 4,
            CaloriesKcal = 200m,
            DifficultyLevel = "EASY",
            IsVerified = true,
            Status = "APPROVED_ACTIVE",
            Source = "USER",
            CreatedAt = DateTime.UtcNow.AddDays(-5)
        };
        _db.Recipes.Add(recipe);

        // 3. Tạo bài viết (Posts)
        // Bài 1: APPROVED_ACTIVE, PUBLIC của user 1024, có recipe và video
        var post1 = new Post
        {
            PostId = 1,
            UserId = 1024,
            User = user1,
            RecipeId = 50,
            Recipe = recipe,
            Title = "Một tuần ăn chay đầu tiên",
            Content = "Chia sẻ cảm nhận sau một tuần ăn thuần chay lành mạnh...",
            ImageUrl = "https://res.cloudinary.com/veggielife/p/1.webp",
            Visibility = "PUBLIC",
            ModerationStatus = "APPROVED_ACTIVE",
            ViewCount = 10,
            CreatedAt = DateTime.UtcNow.AddDays(-5)
        };

        // Video gắn vào Post 1
        var video1 = new Video
        {
            VideoId = 100,
            PostId = 1,
            Title = "Video hành trình ăn chay",
            VideoUrl = "https://res.cloudinary.com/veggielife/v/100.mp4",
            ThumbnailUrl = "https://res.cloudinary.com/veggielife/v/100.webp",
            DurationSeconds = 120,
            Status = "READY",
            CreatedAt = DateTime.UtcNow
        };
        post1.Videos.Add(video1);

        // Thêm Votes và Comments cho Post 1
        post1.Votes.Add(new Vote { VoteId = 1, UserId = 2048, PostId = 1, CreatedAt = DateTime.UtcNow });
        post1.Votes.Add(new Vote { VoteId = 2, UserId = 1024, PostId = 1, CreatedAt = DateTime.UtcNow });
        post1.Comments.Add(new Comment { CommentId = 1, UserId = 2048, PostId = 1, Content = "Hay quá!", Status = "ACTIVE", IsSoftDeleted = false, CreatedAt = DateTime.UtcNow });
        post1.Comments.Add(new Comment { CommentId = 2, UserId = 1024, PostId = 1, Content = "Cảm ơn bạn!", Status = "ACTIVE", IsSoftDeleted = false, CreatedAt = DateTime.UtcNow });
        post1.Comments.Add(new Comment { CommentId = 3, UserId = 2048, PostId = 1, Content = "Bình luận bị xóa", Status = "DELETED", IsSoftDeleted = true, CreatedAt = DateTime.UtcNow });

        // Bài 2: APPROVED_ACTIVE, PUBLIC của user 2048, không có vote, tạo mới hơn
        var post2 = new Post
        {
            PostId = 2,
            UserId = 2048,
            User = user2,
            Title = "Mẹo chọn nấm tươi ngon",
            Content = "Cách chọn nấm đùi gà, nấm hương tại chợ truyền thống...",
            ImageUrl = null,
            Visibility = "PUBLIC",
            ModerationStatus = "APPROVED_ACTIVE",
            ViewCount = 5,
            CreatedAt = DateTime.UtcNow.AddDays(-1)
        };

        // Bài 3: PRIVATE của user 1024 (APPROVED_ACTIVE nhưng Visibility = PRIVATE)
        var post3 = new Post
        {
            PostId = 3,
            UserId = 1024,
            User = user1,
            Title = "Nhật ký ăn chay cá nhân",
            Content = "Nội dung riêng tư của tôi...",
            ImageUrl = null,
            Visibility = "PRIVATE",
            ModerationStatus = "APPROVED_ACTIVE",
            ViewCount = 2,
            CreatedAt = DateTime.UtcNow.AddDays(-2)
        };

        // Bài 4: PENDING_REVIEW của user 1024 (chưa duyệt)
        var post4 = new Post
        {
            PostId = 4,
            UserId = 1024,
            User = user1,
            Title = "Bài viết đang chờ duyệt",
            Content = "Nội dung đang kiểm duyệt AI...",
            ImageUrl = null,
            Visibility = "PUBLIC",
            ModerationStatus = "PENDING_REVIEW",
            ViewCount = 0,
            CreatedAt = DateTime.UtcNow.AddDays(-2)
        };

        // Bài 5: DELETED (đã xóa mềm)
        var post5 = new Post
        {
            PostId = 5,
            UserId = 1024,
            User = user1,
            Title = "Bài viết đã bị xóa",
            Content = "Nội dung vi phạm đã gỡ...",
            ImageUrl = null,
            Visibility = "PUBLIC",
            ModerationStatus = "DELETED",
            ViewCount = 0,
            CreatedAt = DateTime.UtcNow.AddDays(-2)
        };

        _db.Posts.AddRange(post1, post2, post3, post4, post5);
        _db.SaveChanges();
    }

    [Theory]
    [InlineData("a", 0)] // authorId <= 0
    [InlineData("a", -5)]
    public async Task ListPosts_InvalidAuthorId_ThrowsAppException(string keyword, int authorId)
    {
        var filter = new PostFilterRequest { Keyword = keyword, AuthorId = authorId };
        var act = () => _service.ListPostsAsync(filter, true, null);
        await act.Should().ThrowAsync<AppException>()
            .Where(ex => ex.StatusCode == 400 && ex.ErrorCode == ErrorCodes.ErrValidation);
    }

    [Fact]
    public async Task ListPosts_KeywordTooLong_ThrowsAppException()
    {
        var longKeyword = new string('x', 101);
        var filter = new PostFilterRequest { Keyword = longKeyword };
        var act = () => _service.ListPostsAsync(filter, true, null);
        await act.Should().ThrowAsync<AppException>()
            .Where(ex => ex.StatusCode == 400 && ex.ErrorCode == ErrorCodes.ErrValidation);
    }

    [Fact]
    public async Task ListPosts_KeywordEmptyString_ThrowsAppException()
    {
        var filter = new PostFilterRequest { Keyword = "" };
        var act = () => _service.ListPostsAsync(filter, true, null);
        await act.Should().ThrowAsync<AppException>()
            .Where(ex => ex.StatusCode == 400 && ex.ErrorCode == ErrorCodes.ErrValidation);
    }

    [Theory]
    [InlineData("INVALID_SORT")]
    [InlineData("OLDEST")]
    public async Task ListPosts_InvalidSort_ThrowsAppException(string sort)
    {
        var filter = new PostFilterRequest { Sort = sort };
        var act = () => _service.ListPostsAsync(filter, true, null);
        await act.Should().ThrowAsync<AppException>()
            .Where(ex => ex.StatusCode == 400 && ex.ErrorCode == ErrorCodes.ErrValidation);
    }

    [Theory]
    [InlineData(0, 10)]
    [InlineData(-1, 10)]
    [InlineData(1, 0)]
    [InlineData(1, 21)]
    public async Task ListPosts_InvalidPagination_ThrowsAppException(int page, int size)
    {
        var filter = new PostFilterRequest { Page = page, Size = size };
        var act = () => _service.ListPostsAsync(filter, true, null);
        await act.Should().ThrowAsync<AppException>()
            .Where(ex => ex.StatusCode == 400 && ex.ErrorCode == ErrorCodes.ErrValidation);
    }

    [Fact]
    public async Task ListPosts_GuestUser_ReturnsOnlyApprovedActiveAndPublicWithSignUpPrompt()
    {
        // Arrange
        var filter = new PostFilterRequest { Page = 1, Size = 10 };

        // Act
        var (items, meta) = await _service.ListPostsAsync(filter, isGuest: true, guestId: "guest-uuid-1");

        // Assert: Guest only sees Post 1 and Post 2 (Post 3 is private, 4 is pending, 5 is deleted)
        items.Should().HaveCount(2);
        items.Select(i => i.PostId).Should().Contain(new[] { 1, 2 });
        meta.Total.Should().Be(2);
        meta.SignUpPromptAtItem.Should().Be(5); // BR-GUEST-01
    }

    [Fact]
    public async Task ListPosts_AuthorLoggedIn_SeesApprovedPublicPlusOwnPrivateAndDraftPosts()
    {
        // Arrange
        var filter = new PostFilterRequest { Page = 1, Size = 10 };

        // Act: User 1024 is logged in
        var (items, meta) = await _service.ListPostsAsync(filter, isGuest: false, guestId: null, currentUserId: 1024);

        // Assert: User 1024 sees Post 1 (approved public), Post 2 (approved public),
        // PLUS Post 3 (own private), Post 4 (own pending); Post 5 (deleted) is NOT shown
        items.Should().HaveCount(4);
        items.Select(i => i.PostId).Should().Contain(new[] { 1, 2, 3, 4 });
        items.Select(i => i.PostId).Should().NotContain(5);
        meta.SignUpPromptAtItem.Should().BeNull(); // Member does not receive guest signup prompt
    }

    [Fact]
    public async Task ListPosts_FilterByKeyword_MatchesTitleOrContent()
    {
        // Arrange: Tìm theo từ khóa "nấm"
        var filter = new PostFilterRequest { Keyword = "nấm" };

        // Act
        var (items, meta) = await _service.ListPostsAsync(filter, isGuest: true, guestId: null);

        // Assert: Post 2 có "nấm" trong tiêu đề
        items.Should().HaveCount(1);
        items[0].PostId.Should().Be(2);
        items[0].Title.Should().Be("Mẹo chọn nấm tươi ngon");
    }

    [Fact]
    public async Task ListPosts_FilterByAuthorId_ReturnsOnlyPostsOfSpecifiedAuthor()
    {
        // Arrange: Lọc theo tác giả 2048
        var filter = new PostFilterRequest { AuthorId = 2048 };

        // Act
        var (items, meta) = await _service.ListPostsAsync(filter, isGuest: true, guestId: null);

        // Assert: Chỉ trả Post 2 của user 2048
        items.Should().HaveCount(1);
        items[0].PostId.Should().Be(2);
        items[0].AuthorName.Should().Be("Trần Bình");
    }

    [Fact]
    public async Task ListPosts_SortByPopular_OrdersByVoteCountDescending()
    {
        // Arrange: Sắp xếp POPULAR (Post 1 có 2 votes, Post 2 có 0 votes)
        var filter = new PostFilterRequest { Sort = "POPULAR" };

        // Act
        var (items, meta) = await _service.ListPostsAsync(filter, isGuest: true, guestId: null);

        // Assert: Post 1 phải đứng đầu
        items.Should().HaveCount(2);
        items[0].PostId.Should().Be(1);
        items[0].VoteCount.Should().Be(2);
        items[1].PostId.Should().Be(2);
        items[1].VoteCount.Should().Be(0);
    }

    [Fact]
    public async Task ListPosts_SortByNewest_OrdersByCreatedAtDescending()
    {
        // Arrange: Sắp xếp NEWEST (Post 2 tạo 1 ngày trước, Post 1 tạo 5 ngày trước)
        var filter = new PostFilterRequest { Sort = "NEWEST" };

        // Act
        var (items, meta) = await _service.ListPostsAsync(filter, isGuest: true, guestId: null);

        // Assert: Post 2 phải đứng trước Post 1
        items.Should().HaveCount(2);
        items[0].PostId.Should().Be(2);
        items[1].PostId.Should().Be(1);
    }

    [Fact]
    public async Task GetPost_InvalidId_ThrowsAppException()
    {
        var act = () => _service.GetPostByIdAsync(0, true, null);
        await act.Should().ThrowAsync<AppException>()
            .Where(ex => ex.StatusCode == 400 && ex.ErrorCode == ErrorCodes.ErrValidation);

        var actNegative = () => _service.GetPostByIdAsync(-99, true, null);
        await actNegative.Should().ThrowAsync<AppException>()
            .Where(ex => ex.StatusCode == 400 && ex.ErrorCode == ErrorCodes.ErrValidation);
    }

    [Fact]
    public async Task GetPost_NonExistent_ThrowsNotFound()
    {
        var act = () => _service.GetPostByIdAsync(9999, true, null);
        await act.Should().ThrowAsync<AppException>()
            .Where(ex => ex.StatusCode == 404 && ex.ErrorCode == ErrorCodes.ErrContentNotFound);
    }

    [Fact]
    public async Task GetPost_DeletedPost_ThrowsNotFoundForEveryone()
    {
        // Post 5 is DELETED: guest or author both get 404
        var actGuest = () => _service.GetPostByIdAsync(5, isGuest: true, currentUserId: null);
        await actGuest.Should().ThrowAsync<AppException>()
            .Where(ex => ex.StatusCode == 404 && ex.ErrorCode == ErrorCodes.ErrContentNotFound);

        var actAuthor = () => _service.GetPostByIdAsync(5, isGuest: false, currentUserId: 1024);
        await actAuthor.Should().ThrowAsync<AppException>()
            .Where(ex => ex.StatusCode == 404 && ex.ErrorCode == ErrorCodes.ErrContentNotFound);
    }

    [Fact]
    public async Task GetPost_PrivateOrPendingPost_OtherUserOrGuest_ThrowsNotFound()
    {
        // Post 3 is PRIVATE of user 1024: Guest gets 404
        var actGuest = () => _service.GetPostByIdAsync(3, isGuest: true, currentUserId: null);
        await actGuest.Should().ThrowAsync<AppException>()
            .Where(ex => ex.StatusCode == 404 && ex.ErrorCode == ErrorCodes.ErrContentNotFound);

        // Other user 2048 also gets 404
        var actOtherUser = () => _service.GetPostByIdAsync(3, isGuest: false, currentUserId: 2048);
        await actOtherUser.Should().ThrowAsync<AppException>()
            .Where(ex => ex.StatusCode == 404 && ex.ErrorCode == ErrorCodes.ErrContentNotFound);

        // Post 4 is PENDING_REVIEW: Other user gets 404
        var actPending = () => _service.GetPostByIdAsync(4, isGuest: false, currentUserId: 2048);
        await actPending.Should().ThrowAsync<AppException>()
            .Where(ex => ex.StatusCode == 404 && ex.ErrorCode == ErrorCodes.ErrContentNotFound);
    }

    [Fact]
    public async Task GetPost_PrivatePost_AuthorCanView()
    {
        // Post 3 is PRIVATE of user 1024: Author viewing their own post succeeds
        var post = await _service.GetPostByIdAsync(3, isGuest: false, currentUserId: 1024);

        post.Should().NotBeNull();
        post.PostId.Should().Be(3);
        post.Title.Should().Be("Nhật ký ăn chay cá nhân");
        post.Author.UserId.Should().Be(1024);
    }

    [Fact]
    public async Task GetPost_GuestUser_MemberActionsCannotVoteOrComment()
    {
        // Arrange & Act
        var post = await _service.GetPostByIdAsync(1, isGuest: true, currentUserId: null);

        // Assert (BR-AUTH-03)
        post.Should().NotBeNull();
        post.MemberActions.CanVote.Should().BeFalse();
        post.MemberActions.CanComment.Should().BeFalse();
    }

    [Fact]
    public async Task GetPost_AuthenticatedMember_MemberActionsCanVoteAndComment()
    {
        // Arrange & Act
        var post = await _service.GetPostByIdAsync(1, isGuest: false, currentUserId: 2048);

        // Assert (BR-AUTH-03)
        post.Should().NotBeNull();
        post.MemberActions.CanVote.Should().BeTrue();
        post.MemberActions.CanComment.Should().BeTrue();
    }

    [Fact]
    public async Task GetPost_IncludesAttachedVideosRecipesAndCounts()
    {
        // Arrange & Act
        var post = await _service.GetPostByIdAsync(1, isGuest: false, currentUserId: 1024);

        // Assert
        post.Should().NotBeNull();
        post.PostId.Should().Be(1);
        post.Title.Should().Be("Một tuần ăn chay đầu tiên");
        post.Author.UserId.Should().Be(1024);
        post.Author.FullName.Should().Be("Nguyễn An");
        post.VoteCount.Should().Be(2);
        post.CommentCount.Should().Be(2); // Only non-soft-deleted comments (Comment 3 was deleted)

        // Videos
        post.Videos.Should().HaveCount(1);
        post.Videos[0].VideoId.Should().Be(100);
        post.Videos[0].Title.Should().Be("Video hành trình ăn chay");
        post.Videos[0].VideoUrl.Should().Be("https://res.cloudinary.com/veggielife/v/100.mp4");
        post.Videos[0].DurationSeconds.Should().Be(120);

        // Recipes
        post.Recipes.Should().HaveCount(1);
        post.Recipes[0].RecipeId.Should().Be(50);
        post.Recipes[0].Title.Should().Be("Canh chua chay thanh đạm");
        post.Recipes[0].DietarySchool.Should().Be("VEGAN");
        post.Recipes[0].CookTimeMinutes.Should().Be(20);
    }

    [Fact]
    public async Task GetPost_IncrementsViewCountInDatabaseAndRedis()
    {
        // Arrange
        var initialViewCount = (await _db.Posts.FindAsync(1))!.ViewCount;

        // Act
        await _service.GetPostByIdAsync(1, isGuest: true, currentUserId: null);

        // Assert
        var updatedPost = await _db.Posts.FindAsync(1);
        updatedPost!.ViewCount.Should().Be(initialViewCount + 1);

        _mockRedis.Verify(r => r.IncrementAsync(
            "view_counter:post:1",
            1,
            It.IsAny<TimeSpan?>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    // =========================================================================
    // FN-POST-07: ĐĂNG, SỬA, GỠ VÀ XÓA BÀI VIẾT (BR-USR-03, BR-USR-04, BR-ADM-CNT-001/002)
    // =========================================================================

    [Fact]
    public async Task CreatePost_DraftOnly_ShouldCreateDraftWithoutCheckingDailyQuotaOrAI()
    {
        // Arrange: Người dùng mới tạo tài khoản
        var request = new CreatePostRequest
        {
            Title = "Bản nháp chia sẻ ăn chay tuần đầu",
            Content = "Nội dung đang viết dở dang...",
            Visibility = "PUBLIC",
            Submit = false
        };

        // Act
        var result = await _service.CreatePostAsync(request, 1024, "MEMBER");

        // Assert
        result.Should().NotBeNull();
        result.PostId.Should().BeGreaterThan(0);
        result.ModerationStatus.Should().Be("DRAFT");

        var dbPost = await _db.Posts.FindAsync(result.PostId);
        dbPost.Should().NotBeNull();
        dbPost!.ModerationStatus.Should().Be("DRAFT");
        dbPost.Visibility.Should().Be("PUBLIC");

        // Không ghi AIQuotaLogs cho bản nháp
        var quotaLogs = await _db.AiquotaLogs.Where(l => l.UserId == 1024).ToListAsync();
        quotaLogs.Should().BeEmpty();
    }

    [Fact]
    public async Task CreatePost_SubmitCleanContent_ShouldApproveActive_AndLogAiQuota()
    {
        // Arrange
        var request = new CreatePostRequest
        {
            Title = "Hành trình một tháng ăn chay thuần thực vật",
            Content = "Mình cảm thấy cơ thể nhẹ nhàng hơn rất nhiều sau khi chuyển sang chế độ thuần chay.",
            Visibility = "PUBLIC",
            Submit = true
        };

        // Act
        var result = await _service.CreatePostAsync(request, 1024, "MEMBER");

        // Assert
        result.Should().NotBeNull();
        result.ModerationStatus.Should().Be("APPROVED_ACTIVE");

        var dbPost = await _db.Posts.FindAsync(result.PostId);
        dbPost.Should().NotBeNull();
        dbPost!.ModerationStatus.Should().Be("APPROVED_ACTIVE");

        // Kiểm tra AIQuotaLogs được ghi nhận
        var quotaLog = await _db.AiquotaLogs.FirstOrDefaultAsync(l => l.UserId == 1024 && l.FeatureCode == "CONTENT_MODERATION");
        quotaLog.Should().NotBeNull();
        quotaLog!.RequestStatus.Should().Be("SUCCESS");

        // Redis cache invalidation
        _mockRedis.Verify(r => r.RemoveAsync("post_list:*", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreatePost_SubmitViolatingContent_ShouldQuarantine_AndCreateViolationReport()
    {
        // Arrange: Nội dung chứa từ khóa vi phạm động vật ("thịt bò")
        var request = new CreatePostRequest
        {
            Title = "Chia sẻ món ăn chay nhưng có thịt bò",
            Content = "Bài viết này hướng dẫn nấu món nhưng lại nhắc đến thịt bò xào thơm ngon.",
            Visibility = "PUBLIC",
            Submit = true
        };

        // Act
        var result = await _service.CreatePostAsync(request, 1024, "MEMBER");

        // Assert
        result.Should().NotBeNull();
        result.ModerationStatus.Should().Be("QUARANTINED_HIDDEN");

        var dbPost = await _db.Posts.FindAsync(result.PostId);
        dbPost!.ModerationStatus.Should().Be("QUARANTINED_HIDDEN");

        // Kiểm tra ViolationReports được tạo tự động
        var report = await _db.ViolationReports.FirstOrDefaultAsync(r => r.PostId == result.PostId);
        report.Should().NotBeNull();
        report!.ViolationType.Should().Be("VIO_MEAT");
        report.ReportSource.Should().Be("AI_SCAN");
        report.Status.Should().Be("PENDING");
    }

    [Fact]
    public async Task CreatePost_NewAccount_ThirdPostInDay_ShouldThrowDailyLimitExceeded_429()
    {
        // Arrange: Tạo tài khoản mới dưới 3 ngày (tạo cách đây 1 ngày)
        var newUser = new User
        {
            UserId = 3001,
            FullName = "Tân Binh Ăn Chay",
            Email = "tanbinh@example.com",
            Role = "MEMBER",
            AccountStatus = "ACTIVE",
            CreatedAt = DateTime.UtcNow.AddDays(-1)
        };
        _db.Users.Add(newUser);
        await _db.SaveChangesAsync();

        var req1 = new CreatePostRequest
        {
            Title = "Bài viết số 1 trong ngày của tài khoản mới",
            Content = "Nội dung bài viết số 1...",
            Submit = true
        };
        var req2 = new CreatePostRequest
        {
            Title = "Bài viết số 2 trong ngày của tài khoản mới",
            Content = "Nội dung bài viết số 2...",
            Submit = true
        };
        var req3 = new CreatePostRequest
        {
            Title = "Bài viết số 3 trong ngày của tài khoản mới",
            Content = "Nội dung bài viết số 3 vượt hạn mức...",
            Submit = true
        };

        // Act: Đăng bài 1 và 2 thành công
        var res1 = await _service.CreatePostAsync(req1, 3001, "MEMBER");
        res1.Should().NotBeNull();

        var res2 = await _service.CreatePostAsync(req2, 3001, "MEMBER");
        res2.Should().NotBeNull();

        // Act & Assert: Bài thứ 3 trong ngày của tài khoản mới bị từ chối đúng mã lỗi 429 ERR_POST_DAILY_LIMIT_EXCEEDED
        var act = async () => await _service.CreatePostAsync(req3, 3001, "MEMBER");
        var ex = await act.Should().ThrowAsync<AppException>();
        ex.Which.StatusCode.Should().Be(429);
        ex.Which.ErrorCode.Should().Be(ErrorCodes.ErrPostDailyLimitExceeded);
        ex.Which.Message.Should().Contain("BR-USR-04");
    }

    [Fact]
    public async Task CreatePost_NewAccount_DraftsDoNotCountTowardsDailyLimit()
    {
        // Arrange: Tài khoản mới < 3 ngày
        var newUser = new User
        {
            UserId = 3002,
            FullName = "Người Dùng Thích Nháp",
            Email = "nhap@example.com",
            Role = "MEMBER",
            AccountStatus = "ACTIVE",
            CreatedAt = DateTime.UtcNow.AddDays(-1)
        };
        _db.Users.Add(newUser);
        await _db.SaveChangesAsync();

        // Tạo 3 bản nháp DRAFT (submit = false)
        for (int i = 1; i <= 3; i++)
        {
            await _service.CreatePostAsync(new CreatePostRequest
            {
                Title = $"Bản nháp số {i} không tính vào quota",
                Content = $"Nội dung nháp {i}...",
                Submit = false
            }, 3002, "MEMBER");
        }

        // Đăng bài chính thức thứ 1 (submit = true) -> Thành công
        var res1 = await _service.CreatePostAsync(new CreatePostRequest
        {
            Title = "Bài viết chính thức số 1 hợp lệ",
            Content = "Nội dung bài 1...",
            Submit = true
        }, 3002, "MEMBER");
        res1.Should().NotBeNull();

        // Đăng bài chính thức thứ 2 (submit = true) -> Thành công
        var res2 = await _service.CreatePostAsync(new CreatePostRequest
        {
            Title = "Bài viết chính thức số 2 hợp lệ",
            Content = "Nội dung bài 2...",
            Submit = true
        }, 3002, "MEMBER");
        res2.Should().NotBeNull();
    }

    [Fact]
    public async Task CreatePost_OlderAccount_AllowsUpToTenPostsPerDay()
    {
        // Arrange: Tài khoản tạo hơn 3 ngày trước (5 ngày trước)
        var oldUser = new User
        {
            UserId = 3003,
            FullName = "Thành Viên Lâu Năm",
            Email = "launam@example.com",
            Role = "MEMBER",
            AccountStatus = "ACTIVE",
            CreatedAt = DateTime.UtcNow.AddDays(-5)
        };
        _db.Users.Add(oldUser);
        await _db.SaveChangesAsync();

        // Đăng 10 bài thành công
        for (int i = 1; i <= 10; i++)
        {
            var res = await _service.CreatePostAsync(new CreatePostRequest
            {
                Title = $"Bài viết hợp lệ số {i:D2} trong ngày",
                Content = $"Nội dung chia sẻ số {i}...",
                Submit = true
            }, 3003, "MEMBER");
            res.Should().NotBeNull();
        }

        // Bài thứ 11 trong ngày -> Bị từ chối HTTP 429 ERR_POST_DAILY_LIMIT_EXCEEDED
        var act = async () => await _service.CreatePostAsync(new CreatePostRequest
        {
            Title = "Bài viết số 11 vượt giới hạn 10 bài",
            Content = "Nội dung bài 11...",
            Submit = true
        }, 3003, "MEMBER");

        var ex = await act.Should().ThrowAsync<AppException>();
        ex.Which.StatusCode.Should().Be(429);
        ex.Which.ErrorCode.Should().Be(ErrorCodes.ErrPostDailyLimitExceeded);
    }

    [Fact]
    public async Task CreatePost_ValidationFailures_ShouldThrowBadRequest()
    {
        // Title quá ngắn (< 10 ký tự)
        var shortTitleReq = new CreatePostRequest { Title = "Ngắn", Content = "Nội dung hợp lệ..." };
        var act1 = async () => await _service.CreatePostAsync(shortTitleReq, 1024, "MEMBER");
        (await act1.Should().ThrowAsync<AppException>()).Which.ErrorCode.Should().Be(ErrorCodes.ErrValidation);

        // Content rỗng
        var emptyContentReq = new CreatePostRequest { Title = "Tiêu đề hợp lệ từ 10 ký tự", Content = "   " };
        var act2 = async () => await _service.CreatePostAsync(emptyContentReq, 1024, "MEMBER");
        (await act2.Should().ThrowAsync<AppException>()).Which.ErrorCode.Should().Be(ErrorCodes.ErrValidation);

        // Visibility = FOLLOWERS (chưa hỗ trợ ở Phase 1)
        var followersReq = new CreatePostRequest
        {
            Title = "Tiêu đề hợp lệ từ 10 ký tự",
            Content = "Nội dung hợp lệ...",
            Visibility = "FOLLOWERS"
        };
        var act3 = async () => await _service.CreatePostAsync(followersReq, 1024, "MEMBER");
        var ex3 = await act3.Should().ThrowAsync<AppException>();
        ex3.Which.StatusCode.Should().Be(400);
        ex3.Which.ErrorCode.Should().Be(ErrorCodes.ErrPostVisibilityUnsupported);
    }

    [Fact]
    public async Task UpdatePost_ValidAuthor_ShouldUpdateAndReturnPendingReview_AndWriteAuditLog()
    {
        // Arrange: Sửa bài viết số 1 (do user 1024 tạo)
        var request = new UpdatePostRequest
        {
            Title = "Tiêu đề bài viết đã được chỉnh sửa hợp lệ",
            Content = "Nội dung bài viết mới có thêm chi tiết bổ sung."
        };

        // Act
        var result = await _service.UpdatePostAsync(1, request, 1024, "MEMBER");

        // Assert
        result.Should().NotBeNull();
        result.PostId.Should().Be(1);
        result.ModerationStatus.Should().Be("PENDING_REVIEW");

        var dbPost = await _db.Posts.FindAsync(1);
        dbPost!.Title.Should().Be("Tiêu đề bài viết đã được chỉnh sửa hợp lệ");
        dbPost.ModerationStatus.Should().Be("PENDING_REVIEW");

        // Kiểm tra AuditLogs
        var audit = await _db.AuditLogs.FirstOrDefaultAsync(a => a.TargetTable == "Posts" && a.TargetId == "1" && a.Action == "UPDATE_POST");
        audit.Should().NotBeNull();
        audit!.ActorUserId.Should().Be(1024);
        audit.ActorRole.Should().Be("MEMBER");
    }

    [Fact]
    public async Task UpdatePost_NotAuthor_ShouldThrowForbidden_403()
    {
        // User 2048 cố tình sửa bài 1 của User 1024
        var request = new UpdatePostRequest
        {
            Title = "Tiêu đề bị sửa trái phép",
            Content = "Nội dung hack..."
        };

        var act = async () => await _service.UpdatePostAsync(1, request, 2048, "MEMBER");
        var ex = await act.Should().ThrowAsync<AppException>();
        ex.Which.StatusCode.Should().Be(403);
        ex.Which.ErrorCode.Should().Be(ErrorCodes.ErrContentNotOwner);
    }

    [Fact]
    public async Task UpdatePost_ChangeVisibilityToPrivate_ShouldUnpublishPost()
    {
        // Tác giả gỡ bài khỏi trang cộng đồng bằng cách chuyển sang PRIVATE
        var request = new UpdatePostRequest
        {
            Visibility = "PRIVATE"
        };

        var result = await _service.UpdatePostAsync(1, request, 1024, "MEMBER");
        result.Should().NotBeNull();

        var dbPost = await _db.Posts.FindAsync(1);
        dbPost!.Visibility.Should().Be("PRIVATE");
    }

    [Fact]
    public async Task DeletePost_Success_ShouldSoftDelete_AndWriteAuditLog()
    {
        // Act: User 1024 xóa bài 1 của mình
        await _service.DeletePostAsync(1, 1024, "MEMBER");

        // Assert
        var dbPost = await _db.Posts.FindAsync(1);
        dbPost!.ModerationStatus.Should().Be("DELETED");

        var audit = await _db.AuditLogs.FirstOrDefaultAsync(a => a.TargetTable == "Posts" && a.TargetId == "1" && a.Action == "DELETE_POST");
        audit.Should().NotBeNull();
        audit!.ActorUserId.Should().Be(1024);

        // Thử xóa lại bài đã xóa -> Trả lỗi 404 ERR_CONTENT_NOT_FOUND
        var actAgain = async () => await _service.DeletePostAsync(1, 1024, "MEMBER");
        var ex = await actAgain.Should().ThrowAsync<AppException>();
        ex.Which.StatusCode.Should().Be(404);
        ex.Which.ErrorCode.Should().Be(ErrorCodes.ErrContentNotFound);
    }

    [Fact]
    public async Task DeletePost_NotOwner_ShouldThrowForbidden_403()
    {
        // User 2048 cố tình xóa bài 1 của User 1024
        var act = async () => await _service.DeletePostAsync(1, 2048, "MEMBER");
        var ex = await act.Should().ThrowAsync<AppException>();
        ex.Which.StatusCode.Should().Be(403);
        ex.Which.ErrorCode.Should().Be(ErrorCodes.ErrContentNotOwner);
    }
}

