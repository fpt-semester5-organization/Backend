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
using VeggieLife.BLL.DTOs.Ai;
using VeggieLife.BLL.DTOs.Community.Requests;
using VeggieLife.BLL.Services.Cache;
using VeggieLife.BLL.Services.Community;
using VeggieLife.BLL.Services.Moderation;
using VeggieLife.DAL.Context;
using VeggieLife.DAL.Entities;
using Xunit;

namespace VeggieLife.Tests.Unit.Services.Community;

/// <summary>
/// Bộ kiểm thử đơn vị cho CommentService (FN-COMM-01, FN-COMM-02, FN-COMM-03).
/// </summary>
public class CommentServiceTests : IDisposable
{
    private readonly AppDbContext _db;
    private readonly Mock<IRedisService> _mockRedis;
    private readonly Mock<IContentModerationService> _mockModeration;
    private readonly Mock<ILogger<CommentService>> _mockLogger;
    private readonly CommentService _service;

    public CommentServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _db = new AppDbContext(options);
        _mockRedis = new Mock<IRedisService>();
        _mockModeration = new Mock<IContentModerationService>();
        _mockLogger = new Mock<ILogger<CommentService>>();

        // Mặc định: AI phê duyệt an toàn (< 85%)
        _mockModeration
            .Setup(m => m.ModeratePostAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ContentModerationResult
            {
                IsApproved = true,
                ViolationScore = 10.0,
                ModerationStatus = "APPROVED_ACTIVE"
            });

        _service = new CommentService(_db, _mockRedis.Object, _mockModeration.Object, _mockLogger.Object);

        SeedTestData();
    }

    public void Dispose()
    {
        _db.Database.EnsureDeleted();
        _db.Dispose();
    }

    private void SeedTestData()
    {
        var user1 = new User
        {
            UserId = 100,
            FullName = "Nguyễn Văn A",
            Email = "user100@example.com",
            PasswordHash = "hash",
            Role = "MEMBER",
            AccountStatus = "ACTIVE",
            CreatedAt = DateTime.UtcNow
        };
        var user2 = new User
        {
            UserId = 200,
            FullName = "Trần Thị B",
            Email = "user200@example.com",
            PasswordHash = "hash",
            Role = "MEMBER",
            AccountStatus = "ACTIVE",
            CreatedAt = DateTime.UtcNow
        };
        _db.Users.AddRange(user1, user2);

        var school = new DietarySchool
        {
            DietarySchoolId = 1,
            SchoolCode = "VEGAN",
            SchoolName = "Thuần chay",
            ForbiddenFoodRules = "Không thịt cá",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _db.DietarySchools.Add(school);

        var category = new FoodCategory
        {
            CategoryId = 1,
            CategoryName = "Món Kho",
            IsCore = true,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        _db.FoodCategories.Add(category);

        // 1. Bài viết APPROVED_ACTIVE
        var activePost = new Post
        {
            PostId = 1,
            UserId = 100,
            Title = "Cách làm đậu hũ kho nấm",
            Content = "Nội dung bài viết chia sẻ đậu hũ kho nấm",
            Visibility = "PUBLIC",
            ModerationStatus = "APPROVED_ACTIVE",
            CreatedAt = DateTime.UtcNow
        };

        // 2. Bài viết DRAFT
        var draftPost = new Post
        {
            PostId = 2,
            UserId = 100,
            Title = "Bản nháp bài viết",
            Content = "Nội dung bản nháp",
            Visibility = "PUBLIC",
            ModerationStatus = "DRAFT",
            CreatedAt = DateTime.UtcNow
        };
        _db.Posts.AddRange(activePost, draftPost);

        // 3. Công thức APPROVED_ACTIVE
        var activeRecipe = new Recipe
        {
            RecipeId = 10,
            UserId = 100,
            CategoryId = 1,
            DietarySchoolId = 1,
            Title = "Công thức nấm kho tiêu",
            Instructions = "Bước 1 kho nấm. Bước 2 nêm tiêu.",
            DifficultyLevel = "EASY",
            Source = "USER",
            Status = "APPROVED_ACTIVE",
            CreatedAt = DateTime.UtcNow
        };

        // 4. Công thức QUARANTINED_HIDDEN
        var hiddenRecipe = new Recipe
        {
            RecipeId = 20,
            UserId = 100,
            CategoryId = 1,
            DietarySchoolId = 1,
            Title = "Công thức cách ly",
            Instructions = "Nội dung",
            DifficultyLevel = "EASY",
            Source = "USER",
            Status = "QUARANTINED_HIDDEN",
            CreatedAt = DateTime.UtcNow
        };
        _db.Recipes.AddRange(activeRecipe, hiddenRecipe);

        _db.SaveChanges();
    }

    // =========================================================================
    // FN-COMM-01: Xem bình luận
    // =========================================================================

    [Fact]
    public async Task ListPostCommentsAsync_InvalidParams_ThrowsBadRequest()
    {
        // postId <= 0
        var act1 = () => _service.ListPostCommentsAsync(0);
        await act1.Should().ThrowAsync<AppException>().Where(e => e.StatusCode == 400 && e.ErrorCode == ErrorCodes.ErrValidation);

        // page < 1
        var act2 = () => _service.ListPostCommentsAsync(1, page: 0);
        await act2.Should().ThrowAsync<AppException>().Where(e => e.StatusCode == 400 && e.ErrorCode == ErrorCodes.ErrValidation);

        // size < 1 or size > 50
        var act3 = () => _service.ListPostCommentsAsync(1, size: 51);
        await act3.Should().ThrowAsync<AppException>().Where(e => e.StatusCode == 400 && e.ErrorCode == ErrorCodes.ErrValidation);
    }

    [Fact]
    public async Task ListPostCommentsAsync_PostNotFoundOrNotActive_ThrowsNotFound()
    {
        // Bài không tồn tại
        var act1 = () => _service.ListPostCommentsAsync(999);
        await act1.Should().ThrowAsync<AppException>().Where(e => e.StatusCode == 404 && e.ErrorCode == ErrorCodes.ErrContentNotFound);

        // Bài ở trạng thái DRAFT (PostId = 2)
        var act2 = () => _service.ListPostCommentsAsync(2);
        await act2.Should().ThrowAsync<AppException>().Where(e => e.StatusCode == 404 && e.ErrorCode == ErrorCodes.ErrContentNotFound);
    }

    [Fact]
    public async Task ListPostCommentsAsync_HierarchicalAndChronologicalOrder_ReturnsProperTree()
    {
        // Tạo bình luận gốc 1 (cũ hơn)
        var root1 = new Comment
        {
            CommentId = 1,
            PostId = 1,
            UserId = 100,
            Content = "Bình luận gốc 1",
            Status = "ACTIVE",
            IsSoftDeleted = false,
            CreatedAt = DateTime.UtcNow.AddMinutes(-30)
        };

        // Bình luận gốc 2 (mới hơn)
        var root2 = new Comment
        {
            CommentId = 2,
            PostId = 1,
            UserId = 200,
            Content = "Bình luận gốc 2",
            Status = "ACTIVE",
            IsSoftDeleted = false,
            CreatedAt = DateTime.UtcNow.AddMinutes(-10)
        };

        // Bình luận con của gốc 1 (cũ hơn)
        var reply1_1 = new Comment
        {
            CommentId = 3,
            PostId = 1,
            UserId = 200,
            ParentCommentId = 1,
            Content = "Trả lời 1 cho gốc 1",
            Status = "ACTIVE",
            IsSoftDeleted = false,
            CreatedAt = DateTime.UtcNow.AddMinutes(-25)
        };

        // Bình luận con của gốc 1 (mới hơn)
        var reply1_2 = new Comment
        {
            CommentId = 4,
            PostId = 1,
            UserId = 100,
            ParentCommentId = 1,
            Content = "Trả lời 2 cho gốc 1",
            Status = "ACTIVE",
            IsSoftDeleted = false,
            CreatedAt = DateTime.UtcNow.AddMinutes(-20)
        };

        // Bình luận đã xóa mềm hoặc bị cách ly (không hiển thị)
        var deletedComment = new Comment
        {
            CommentId = 5,
            PostId = 1,
            UserId = 100,
            Content = "Đã bị xóa mềm",
            Status = "ACTIVE",
            IsSoftDeleted = true,
            CreatedAt = DateTime.UtcNow.AddMinutes(-5)
        };
        var quarantinedComment = new Comment
        {
            CommentId = 6,
            PostId = 1,
            UserId = 100,
            Content = "Bị cách ly",
            Status = "QUARANTINED_HIDDEN",
            IsSoftDeleted = false,
            CreatedAt = DateTime.UtcNow.AddMinutes(-4)
        };

        _db.Comments.AddRange(root1, root2, reply1_1, reply1_2, deletedComment, quarantinedComment);
        await _db.SaveChangesAsync();

        var (items, meta) = await _service.ListPostCommentsAsync(1, page: 1, size: 20);

        meta.Total.Should().Be(2); // Chỉ 2 bình luận gốc ACTIVE
        items.Should().HaveCount(2);

        // Thứ tự tăng dần CreatedAt
        items[0].CommentId.Should().Be(1);
        items[0].Content.Should().Be("Bình luận gốc 1");
        items[0].Author.FullName.Should().Be("Nguyễn Văn A");
        items[0].Replies.Should().HaveCount(2);
        items[0].Replies[0].CommentId.Should().Be(3);
        items[0].Replies[1].CommentId.Should().Be(4);

        items[1].CommentId.Should().Be(2);
        items[1].Content.Should().Be("Bình luận gốc 2");
        items[1].Author.FullName.Should().Be("Trần Thị B");
        items[1].Replies.Should().BeEmpty();
    }

    [Fact]
    public async Task ListRecipeCommentsAsync_RecipeExistsWithoutRepresentativePost_ReturnsEmptyList()
    {
        var (items, meta) = await _service.ListRecipeCommentsAsync(10, page: 1, size: 20);

        meta.Total.Should().Be(0);
        items.Should().BeEmpty();
    }

    [Fact]
    public async Task ListRecipeCommentsAsync_RecipeNotActive_ThrowsNotFound()
    {
        var act = () => _service.ListRecipeCommentsAsync(20);
        await act.Should().ThrowAsync<AppException>().Where(e => e.StatusCode == 404 && e.ErrorCode == ErrorCodes.ErrContentNotFound);
    }

    // =========================================================================
    // FN-COMM-02: Viết bình luận & Chống spam
    // =========================================================================

    [Fact]
    public async Task CreatePostCommentAsync_InvalidContentLength_ThrowsBadRequest()
    {
        var requestEmpty = new CreateCommentRequest { Content = "" };
        var act1 = () => _service.CreatePostCommentAsync(1, requestEmpty, 100);
        await act1.Should().ThrowAsync<AppException>().Where(e => e.StatusCode == 400 && e.ErrorCode == ErrorCodes.ErrValidation);

        var requestTooShort = new CreateCommentRequest { Content = "a" }; // < 2 chars
        var act2 = () => _service.CreatePostCommentAsync(1, requestTooShort, 100);
        await act2.Should().ThrowAsync<AppException>().Where(e => e.StatusCode == 400 && e.ErrorCode == ErrorCodes.ErrValidation);

        var requestTooLong = new CreateCommentRequest { Content = new string('x', 501) }; // > 500 chars
        var act3 = () => _service.CreatePostCommentAsync(1, requestTooLong, 100);
        await act3.Should().ThrowAsync<AppException>().Where(e => e.StatusCode == 400 && e.ErrorCode == ErrorCodes.ErrValidation);
    }

    [Fact]
    public async Task CreatePostCommentAsync_TargetPostNotFoundOrNotActive_ThrowsNotFound()
    {
        var req = new CreateCommentRequest { Content = "Bài viết hay quá!" };
        var act = () => _service.CreatePostCommentAsync(2, req, 100); // Post 2 is DRAFT
        await act.Should().ThrowAsync<AppException>().Where(e => e.StatusCode == 404 && e.ErrorCode == ErrorCodes.ErrContentNotFound);
    }

    [Fact]
    public async Task CreatePostCommentAsync_CooldownViolated_ThrowsTooManyRequests()
    {
        var req = new CreateCommentRequest { Content = "Bình luận lần 1" };

        // Mock: Đang trong cooldown 15s
        _mockRedis
            .Setup(r => r.ExistsAsync("comment_cooldown:100", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var act = () => _service.CreatePostCommentAsync(1, req, 100);

        var ex = await act.Should().ThrowAsync<AppException>();
        ex.Which.StatusCode.Should().Be(429);
        ex.Which.ErrorCode.Should().Be(ErrorCodes.ErrCommentTooFast);
        ex.Which.Message.Should().Contain("15 giây");
    }

    [Fact]
    public async Task CreatePostCommentAsync_Exceeds5Duplicates_ThrowsTooManyRequests()
    {
        var content = "Nội dung lặp lại y hệt";

        // Seed sẵn 5 comment trùng nội dung của user 100 trong bài 1
        for (int i = 0; i < 5; i++)
        {
            _db.Comments.Add(new Comment
            {
                PostId = 1,
                UserId = 100,
                Content = content,
                Status = "ACTIVE",
                IsSoftDeleted = false,
                CreatedAt = DateTime.UtcNow.AddMinutes(-i)
            });
        }
        await _db.SaveChangesAsync();

        var req = new CreateCommentRequest { Content = content };

        var act = () => _service.CreatePostCommentAsync(1, req, 100);

        var ex = await act.Should().ThrowAsync<AppException>();
        ex.Which.StatusCode.Should().Be(429);
        ex.Which.ErrorCode.Should().Be(ErrorCodes.ErrCommentDuplicateLimit);
        ex.Which.Message.Should().Contain("05 bình luận trùng nội dung");
    }

    [Fact]
    public async Task CreatePostCommentAsync_AiDetectsViolation_QuarantinesAndCreatesViolationReport()
    {
        var req = new CreateCommentRequest { Content = "Nội dung chứa từ ngữ tiêu cực vi phạm" };

        // Giả lập AI phát hiện vi phạm >= 85%
        _mockModeration
            .Setup(m => m.ModeratePostAsync("Bình luận", req.Content, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ContentModerationResult
            {
                IsApproved = false,
                ViolationScore = 92.0,
                ModerationStatus = "QUARANTINED_HIDDEN",
                ViolationType = "VIO_HATE",
                Note = "Từ ngữ công kích người khác"
            });

        var result = await _service.CreatePostCommentAsync(1, req, 100);

        result.Status.Should().Be("QUARANTINED_HIDDEN");

        // Kiểm tra trong CSDL: Có 1 ViolationReport AI_SCAN được tạo
        var report = await _db.ViolationReports.FirstOrDefaultAsync(r => r.CommentId == result.CommentId);
        report.Should().NotBeNull();
        report!.ReportSource.Should().Be("AI_SCAN");
        report.ViolationType.Should().Be("VIO_HATE");
        report.Status.Should().Be("PENDING");
    }

    [Fact]
    public async Task CreatePostCommentAsync_ValidParentComment_CreatesReplySuccessfully()
    {
        // Tạo bình luận cha
        var parent = new Comment
        {
            CommentId = 10,
            PostId = 1,
            UserId = 100,
            Content = "Bình luận gốc",
            Status = "ACTIVE",
            IsSoftDeleted = false,
            CreatedAt = DateTime.UtcNow
        };
        _db.Comments.Add(parent);
        await _db.SaveChangesAsync();

        var req = new CreateCommentRequest
        {
            Content = "Mình đồng ý với bạn!",
            ParentCommentId = 10
        };

        var result = await _service.CreatePostCommentAsync(1, req, 200);

        result.CommentId.Should().BeGreaterThan(0);
        result.ParentCommentId.Should().Be(10);
        result.Author.UserId.Should().Be(200);
        result.Author.FullName.Should().Be("Trần Thị B");
        result.Status.Should().Be("ACTIVE");

        // Xác nhận Redis cooldown được ghi nhận 15s
        _mockRedis.Verify(r => r.SetAsync("comment_cooldown:200", "1", TimeSpan.FromSeconds(15), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateRecipeCommentAsync_ValidRecipe_CreatesRepresentativePostAndComment()
    {
        var req = new CreateCommentRequest { Content = "Món nấm kho này ngon tuyệt!" };

        var result = await _service.CreateRecipeCommentAsync(10, req, 100);

        result.Should().NotBeNull();
        result.Content.Should().Be("Món nấm kho này ngon tuyệt!");
        result.Author.FullName.Should().Be("Nguyễn Văn A");

        // Kiểm tra bài viết đại diện được tự động tạo cho recipe 10
        var repPost = await _db.Posts.FirstOrDefaultAsync(p => p.RecipeId == 10);
        repPost.Should().NotBeNull();
        repPost!.ModerationStatus.Should().Be("APPROVED_ACTIVE");
    }

    // =========================================================================
    // FN-COMM-03: Sửa / Xóa bình luận & Cascade Soft-Delete
    // =========================================================================

    [Fact]
    public async Task UpdateCommentAsync_NotAuthor_ThrowsForbidden()
    {
        var comment = new Comment
        {
            CommentId = 50,
            PostId = 1,
            UserId = 100, // Tác giả là user 100
            Content = "Nội dung ban đầu",
            Status = "ACTIVE",
            IsSoftDeleted = false,
            CreatedAt = DateTime.UtcNow
        };
        _db.Comments.Add(comment);
        await _db.SaveChangesAsync();

        var req = new UpdateCommentRequest { Content = "Cố tình sửa nội dung của người khác" };

        var act = () => _service.UpdateCommentAsync(50, req, userId: 200); // User 200 gọi
        var ex = await act.Should().ThrowAsync<AppException>();
        ex.Which.StatusCode.Should().Be(403);
        ex.Which.ErrorCode.Should().Be(ErrorCodes.ErrContentNotOwner);
    }

    [Fact]
    public async Task UpdateCommentAsync_AuthorUpdatesSuccessfully()
    {
        var comment = new Comment
        {
            CommentId = 51,
            PostId = 1,
            UserId = 100,
            Content = "Nội dung ban đầu",
            Status = "ACTIVE",
            IsSoftDeleted = false,
            CreatedAt = DateTime.UtcNow
        };
        _db.Comments.Add(comment);
        await _db.SaveChangesAsync();

        var req = new UpdateCommentRequest { Content = "Nội dung đã được chỉnh sửa chuẩn xác" };

        var result = await _service.UpdateCommentAsync(51, req, userId: 100);

        result.Content.Should().Be("Nội dung đã được chỉnh sửa chuẩn xác");
        var inDb = await _db.Comments.FindAsync(51);
        inDb!.Content.Should().Be("Nội dung đã được chỉnh sửa chuẩn xác");
        inDb.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task DeleteCommentAsync_CascadeSoftDelete_HidesAllDescendantReplies()
    {
        // Cây bình luận:
        // Root (101)
        //   -> Reply 1 (102)
        //        -> Nested Reply (104)
        //   -> Reply 2 (103)
        var root = new Comment
        {
            CommentId = 101,
            PostId = 1,
            UserId = 100,
            Content = "Bình luận gốc",
            Status = "ACTIVE",
            IsSoftDeleted = false,
            CreatedAt = DateTime.UtcNow
        };
        var reply1 = new Comment
        {
            CommentId = 102,
            PostId = 1,
            UserId = 200,
            ParentCommentId = 101,
            Content = "Phản hồi 1",
            Status = "ACTIVE",
            IsSoftDeleted = false,
            CreatedAt = DateTime.UtcNow
        };
        var reply2 = new Comment
        {
            CommentId = 103,
            PostId = 1,
            UserId = 200,
            ParentCommentId = 101,
            Content = "Phản hồi 2",
            Status = "ACTIVE",
            IsSoftDeleted = false,
            CreatedAt = DateTime.UtcNow
        };
        var nestedReply = new Comment
        {
            CommentId = 104,
            PostId = 1,
            UserId = 100,
            ParentCommentId = 102,
            Content = "Phản hồi lồng con",
            Status = "ACTIVE",
            IsSoftDeleted = false,
            CreatedAt = DateTime.UtcNow
        };

        _db.Comments.AddRange(root, reply1, reply2, nestedReply);
        await _db.SaveChangesAsync();

        // User 100 xóa comment gốc 101
        var result = await _service.DeleteCommentAsync(101, userId: 100);

        // 3 bình luận phản hồi con (102, 103, 104) bị ẩn đồng thời
        result.HiddenReplies.Should().Be(3);

        // Kiểm tra trong CSDL: Toàn bộ 4 bình luận đều IsSoftDeleted == true
        var allComments = await _db.Comments.Where(c => c.CommentId >= 101 && c.CommentId <= 104).ToListAsync();
        allComments.Should().HaveCount(4);
        allComments.All(c => c.IsSoftDeleted).Should().BeTrue();
    }

    [Fact]
    public async Task DeleteCommentAsync_NotAuthor_ThrowsForbidden()
    {
        var comment = new Comment
        {
            CommentId = 201,
            PostId = 1,
            UserId = 100,
            Content = "Bình luận",
            Status = "ACTIVE",
            IsSoftDeleted = false,
            CreatedAt = DateTime.UtcNow
        };
        _db.Comments.Add(comment);
        await _db.SaveChangesAsync();

        var act = () => _service.DeleteCommentAsync(201, userId: 200);
        var ex = await act.Should().ThrowAsync<AppException>();
        ex.Which.StatusCode.Should().Be(403);
        ex.Which.ErrorCode.Should().Be(ErrorCodes.ErrContentNotOwner);
    }
}
