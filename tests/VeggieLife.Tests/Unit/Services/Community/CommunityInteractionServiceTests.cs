using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using VeggieLife.BLL.Common.Constants;
using VeggieLife.BLL.Common.Exceptions;
using VeggieLife.BLL.DTOs.Community.Requests;
using VeggieLife.BLL.Services.Cache;
using VeggieLife.BLL.Services.Community;
using VeggieLife.DAL.Context;
using VeggieLife.DAL.Entities;
using Xunit;

namespace VeggieLife.Tests.Unit.Services.Community;

/// <summary>
/// Bộ kiểm thử đơn vị cho CommunityInteractionService (FN-COMM-04, FN-COMM-05).
/// Kiểm tra:
/// - Thả tim / bỏ tim bài viết và công thức (thả lần 2 = bỏ tim, mỗi người 1 tim).
/// - Từ chối thả tim nội dung nháp, đã xóa, bị ẩn (BR-INT-LIKE-02).
/// - Báo cáo vi phạm: tối đa 10 báo cáo/ngày (BR-USR-04), không báo trùng (ERR_REPORT_DUPLICATE).
/// - Ẩn ngay nội dung bị báo cáo (BR-INT-REPORT-01, BR-ADM-CNT-001, BR-ADM-CMT-001).
/// </summary>
public class CommunityInteractionServiceTests : IDisposable
{
    private readonly AppDbContext _db;
    private readonly Mock<IRedisService> _mockRedis;
    private readonly Mock<ILogger<CommunityInteractionService>> _mockLogger;
    private readonly CommunityInteractionService _service;

    public CommunityInteractionServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _db = new AppDbContext(options);
        _mockRedis = new Mock<IRedisService>();
        _mockLogger = new Mock<ILogger<CommunityInteractionService>>();

        _service = new CommunityInteractionService(_db, _mockRedis.Object, _mockLogger.Object);

        SeedTestData();
    }

    public void Dispose()
    {
        _db.Database.EnsureDeleted();
        _db.Dispose();
    }

    private void SeedTestData()
    {
        var user = new User
        {
            UserId = 1,
            FullName = "Nguyễn Văn A",
            Email = "user1@example.com",
            PasswordHash = "hashed",
            Role = "MEMBER",
            AccountStatus = "ACTIVE",
            CreatedAt = DateTime.UtcNow
        };
        _db.Users.Add(user);

        var school = new DietarySchool
        {
            DietarySchoolId = 1,
            SchoolCode = "VEGAN",
            SchoolName = "Thuần chay",
            ForbiddenFoodRules = "Không thịt cá trứng sữa",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _db.DietarySchools.Add(school);

        var category = new FoodCategory
        {
            CategoryId = 1,
            CategoryName = "Món Canh",
            IsCore = true,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        _db.FoodCategories.Add(category);

        // Bài viết hợp lệ APPROVED_ACTIVE & PUBLIC
        var activePost = new Post
        {
            PostId = 10,
            UserId = 1,
            Title = "Bài viết món canh chay",
            Content = "Nội dung chia sẻ canh nấm đậu hũ",
            Visibility = "PUBLIC",
            ModerationStatus = "APPROVED_ACTIVE",
            CreatedAt = DateTime.UtcNow
        };
        _db.Posts.Add(activePost);

        // Bài viết nháp DRAFT
        var draftPost = new Post
        {
            PostId = 11,
            UserId = 1,
            Title = "Bài viết nháp",
            Content = "Nội dung nháp",
            Visibility = "PUBLIC",
            ModerationStatus = "DRAFT",
            CreatedAt = DateTime.UtcNow
        };
        _db.Posts.Add(draftPost);

        // Công thức hợp lệ APPROVED_ACTIVE
        var activeRecipe = new Recipe
        {
            RecipeId = 20,
            UserId = 1,
            CategoryId = 1,
            DietarySchoolId = 1,
            Title = "Công thức canh rong biển",
            Instructions = "Nấu rong biển với đậu hũ",
            PrepTimeMinutes = 10,
            CookTimeMinutes = 15,
            Servings = 2,
            CaloriesKcal = 120,
            DifficultyLevel = "EASY",
            Status = "APPROVED_ACTIVE",
            Source = "USER_CREATED",
            CreatedAt = DateTime.UtcNow
        };
        _db.Recipes.Add(activeRecipe);

        // Video hợp lệ READY
        var activeVideo = new Video
        {
            VideoId = 30,
            PostId = 10,
            Title = "Video hướng dẫn",
            VideoUrl = "https://example.com/video.mp4",
            DurationSeconds = 120,
            Status = "READY",
            CreatedAt = DateTime.UtcNow
        };
        _db.Videos.Add(activeVideo);

        // Bình luận hợp lệ
        var activeComment = new Comment
        {
            CommentId = 40,
            UserId = 1,
            PostId = 10,
            Content = "Bình luận hay quá",
            Status = "APPROVED_ACTIVE",
            IsSoftDeleted = false,
            CreatedAt = DateTime.UtcNow
        };
        _db.Comments.Add(activeComment);

        // Bình luận phản hồi con (Reply)
        var replyComment = new Comment
        {
            CommentId = 41,
            UserId = 1,
            PostId = 10,
            ParentCommentId = 40,
            Content = "Phản hồi cho bình luận 40",
            Status = "APPROVED_ACTIVE",
            IsSoftDeleted = false,
            CreatedAt = DateTime.UtcNow
        };
        _db.Comments.Add(replyComment);

        _db.SaveChanges();
    }

    // ==========================================
    // FN-COMM-04: Thả tim / Bỏ tim cho bài viết
    // ==========================================

    [Fact]
    public async Task VotePostAsync_FirstTime_ShouldAddVoteAndReturnVotedTrue()
    {
        // Act: Thả tim lần đầu
        var result = await _service.VotePostAsync(10, 1, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Voted.Should().BeTrue();
        result.VoteCount.Should().Be(1);

        var voteInDb = await _db.Votes.FirstOrDefaultAsync(v => v.UserId == 1 && v.PostId == 10);
        voteInDb.Should().NotBeNull();
    }

    [Fact]
    public async Task VotePostAsync_SecondTime_ShouldRemoveVoteAndReturnVotedFalse()
    {
        // Arrange: Thả tim lần 1
        await _service.VotePostAsync(10, 1, CancellationToken.None);

        // Act: Thả tim lần 2 (Bỏ tim)
        var result = await _service.VotePostAsync(10, 1, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Voted.Should().BeFalse();
        result.VoteCount.Should().Be(0);

        var voteInDb = await _db.Votes.FirstOrDefaultAsync(v => v.UserId == 1 && v.PostId == 10);
        voteInDb.Should().BeNull();
    }

    [Fact]
    public async Task VotePostAsync_PostNotFound_ShouldThrowContentNotFoundException()
    {
        // Act & Assert
        var act = () => _service.VotePostAsync(999, 1, CancellationToken.None);
        var ex = await act.Should().ThrowAsync<AppException>();
        ex.Which.StatusCode.Should().Be(404);
        ex.Which.ErrorCode.Should().Be(ErrorCodes.ErrContentNotFound);
    }

    [Fact]
    public async Task VotePostAsync_DraftPost_ShouldThrowContentNotFoundException()
    {
        // Act & Assert (BR-INT-LIKE-02: Không cho like bài nháp)
        var act = () => _service.VotePostAsync(11, 1, CancellationToken.None);
        var ex = await act.Should().ThrowAsync<AppException>();
        ex.Which.StatusCode.Should().Be(404);
        ex.Which.ErrorCode.Should().Be(ErrorCodes.ErrContentNotFound);
    }

    [Fact]
    public async Task VotePostAsync_InvalidId_ShouldThrowValidationException()
    {
        var act = () => _service.VotePostAsync(0, 1, CancellationToken.None);
        var ex = await act.Should().ThrowAsync<AppException>();
        ex.Which.StatusCode.Should().Be(400);
        ex.Which.ErrorCode.Should().Be(ErrorCodes.ErrValidation);
    }

    // ==========================================
    // FN-COMM-04: Thả tim / Bỏ tim cho công thức
    // ==========================================

    [Fact]
    public async Task VoteRecipeAsync_FirstTime_ShouldCreateRepPostIfMissingAndAddVote()
    {
        // Act: Thả tim công thức lần đầu
        var result = await _service.VoteRecipeAsync(20, 1, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Voted.Should().BeTrue();
        result.VoteCount.Should().Be(1);

        // Kiểm tra bài viết đại diện được tạo tự động
        var repPost = await _db.Posts.FirstOrDefaultAsync(p => p.RecipeId == 20);
        repPost.Should().NotBeNull();

        // Kiểm tra vote được liên kết qua repPost
        var voteInDb = await _db.Votes.FirstOrDefaultAsync(v => v.UserId == 1 && v.PostId == repPost!.PostId);
        voteInDb.Should().NotBeNull();
    }

    [Fact]
    public async Task VoteRecipeAsync_SecondTime_ShouldRemoveVoteAndReturnVotedFalse()
    {
        // Arrange: Thả tim lần 1
        await _service.VoteRecipeAsync(20, 1, CancellationToken.None);

        // Act: Thả tim lần 2 (Bỏ tim)
        var result = await _service.VoteRecipeAsync(20, 1, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Voted.Should().BeFalse();
        result.VoteCount.Should().Be(0);
    }

    [Fact]
    public async Task VoteRecipeAsync_RecipeNotFound_ShouldThrowContentNotFoundException()
    {
        var act = () => _service.VoteRecipeAsync(999, 1, CancellationToken.None);
        var ex = await act.Should().ThrowAsync<AppException>();
        ex.Which.StatusCode.Should().Be(404);
        ex.Which.ErrorCode.Should().Be(ErrorCodes.ErrContentNotFound);
    }

    // ==========================================
    // FN-COMM-05: Báo cáo vi phạm nội dung
    // ==========================================

    [Fact]
    public async Task CreateReportAsync_ValidPostReport_ShouldCreateReportAndQuarantinePost()
    {
        // Arrange
        var request = new CreateReportRequest
        {
            TargetType = "POST",
            TargetId = 10,
            ViolationType = "VIO_MEAT",
            EvidenceSummary = "Bài viết này có hình ảnh thịt động vật vi phạm quy tắc ăn chay."
        };

        // Act
        var result = await _service.CreateReportAsync(request, 1, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.ReportId.Should().BeGreaterThan(0);
        result.Status.Should().Be("PENDING");

        // Kiểm tra bản ghi ViolationReports được tạo
        var reportInDb = await _db.ViolationReports.FirstOrDefaultAsync(r => r.ReportId == result.ReportId);
        reportInDb.Should().NotBeNull();
        reportInDb!.ReporterUserId.Should().Be(1);
        reportInDb.PostId.Should().Be(10);
        reportInDb.ViolationType.Should().Be("VIO_MEAT");
        reportInDb.ReportSource.Should().Be("USER_REPORT");
        reportInDb.Status.Should().Be("PENDING");

        // Kiểm tra bài viết bị ẩn ngay lập tức (BR-INT-REPORT-01, BR-ADM-CNT-001)
        var postInDb = await _db.Posts.FirstOrDefaultAsync(p => p.PostId == 10);
        postInDb!.ModerationStatus.Should().Be("QUARANTINED_HIDDEN");
    }

    [Fact]
    public async Task CreateReportAsync_DuplicateReport_ShouldThrowReportDuplicateException()
    {
        // Arrange: Đã báo cáo 1 lần
        var request = new CreateReportRequest
        {
            TargetType = "POST",
            TargetId = 10,
            ViolationType = "VIO_SPAM",
            EvidenceSummary = "Nội dung quảng cáo spam liên tục."
        };
        await _service.CreateReportAsync(request, 1, CancellationToken.None);

        // Act: Báo cáo trùng lặp lần 2
        var act = () => _service.CreateReportAsync(request, 1, CancellationToken.None);

        // Assert: Báo cáo trùng bị từ chối 409 ERR_REPORT_DUPLICATE
        var ex = await act.Should().ThrowAsync<AppException>();
        ex.Which.StatusCode.Should().Be(409);
        ex.Which.ErrorCode.Should().Be(ErrorCodes.ErrReportDuplicate);
        ex.Which.Message.Should().Be("Bạn đã báo cáo nội dung này.");
    }

    [Fact]
    public async Task CreateReportAsync_DailyLimitExceeded_ShouldThrowReportDailyLimitException()
    {
        // Arrange: Tạo sẵn 10 báo cáo trong ngày của user 1
        for (int i = 1; i <= 10; i++)
        {
            _db.ViolationReports.Add(new ViolationReport
            {
                ReporterUserId = 1,
                ViolationType = "VIO_SPAM",
                EvidenceSummary = $"Báo cáo thứ {i} trong ngày để kiểm tra hạn mức",
                ReportSource = "USER_REPORT",
                Status = "PENDING",
                CreatedAt = DateTime.UtcNow
            });
        }
        await _db.SaveChangesAsync();

        var request = new CreateReportRequest
        {
            TargetType = "POST",
            TargetId = 10,
            ViolationType = "VIO_SPAM",
            EvidenceSummary = "Báo cáo thứ 11 vượt quá giới hạn ngày cho phép."
        };

        // Act & Assert (BR-USR-04: Tối đa 10 báo cáo/ngày)
        var act = () => _service.CreateReportAsync(request, 1, CancellationToken.None);
        var ex = await act.Should().ThrowAsync<AppException>();
        ex.Which.StatusCode.Should().Be(429);
        ex.Which.ErrorCode.Should().Be(ErrorCodes.ErrReportDailyLimit);
        ex.Which.Message.Should().Be("Quá 10 báo cáo trong 1 ngày.");
    }

    [Fact]
    public async Task CreateReportAsync_CommentReport_ShouldQuarantineCommentAndCascadeToReplies()
    {
        // Arrange
        var request = new CreateReportRequest
        {
            TargetType = "COMMENT",
            TargetId = 40,
            ViolationType = "VIO_HATE",
            EvidenceSummary = "Bình luận chứa ngôn từ thù địch xúc phạm người khác."
        };

        // Act
        var result = await _service.CreateReportAsync(request, 1, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();

        // Bình luận cha bị ẩn (BR-INT-REPORT-01)
        var parentComment = await _db.Comments.FirstOrDefaultAsync(c => c.CommentId == 40);
        parentComment!.Status.Should().Be("QUARANTINED_HIDDEN");

        // Bình luận phản hồi con cũng bị cascade ẩn (BR-ADM-CMT-001)
        var childComment = await _db.Comments.FirstOrDefaultAsync(c => c.CommentId == 41);
        childComment!.Status.Should().Be("QUARANTINED_HIDDEN");
    }

    [Fact]
    public async Task CreateReportAsync_VideoReport_ShouldBanVideo()
    {
        // Arrange
        var request = new CreateReportRequest
        {
            TargetType = "VIDEO",
            TargetId = 30,
            ViolationType = "VIO_MEAT",
            EvidenceSummary = "Video nấu nướng thịt động vật trái phép trên nền tảng."
        };

        // Act
        var result = await _service.CreateReportAsync(request, 1, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();

        var video = await _db.Videos.FirstOrDefaultAsync(v => v.VideoId == 30);
        video!.Status.Should().Be("BANNED");
    }

    [Fact]
    public async Task CreateReportAsync_InvalidEvidenceSummaryLength_ShouldThrowValidationException()
    {
        // Dưới 10 ký tự
        var requestShort = new CreateReportRequest
        {
            TargetType = "POST",
            TargetId = 10,
            ViolationType = "VIO_SPAM",
            EvidenceSummary = "Ngắn quá"
        };

        var actShort = () => _service.CreateReportAsync(requestShort, 1, CancellationToken.None);
        var exShort = await actShort.Should().ThrowAsync<AppException>();
        exShort.Which.StatusCode.Should().Be(400);
        exShort.Which.ErrorCode.Should().Be(ErrorCodes.ErrValidation);

        // Quá 500 ký tự
        var requestLong = new CreateReportRequest
        {
            TargetType = "POST",
            TargetId = 10,
            ViolationType = "VIO_SPAM",
            EvidenceSummary = new string('A', 501)
        };

        var actLong = () => _service.CreateReportAsync(requestLong, 1, CancellationToken.None);
        var exLong = await actLong.Should().ThrowAsync<AppException>();
        exLong.Which.StatusCode.Should().Be(400);
        exLong.Which.ErrorCode.Should().Be(ErrorCodes.ErrValidation);
    }

    [Fact]
    public async Task CreateReportAsync_TargetNotFound_ShouldThrowContentNotFoundException()
    {
        var request = new CreateReportRequest
        {
            TargetType = "RECIPE",
            TargetId = 9999,
            ViolationType = "VIO_SPAM",
            EvidenceSummary = "Công thức không tồn tại để báo cáo."
        };

        var act = () => _service.CreateReportAsync(request, 1, CancellationToken.None);
        var ex = await act.Should().ThrowAsync<AppException>();
        ex.Which.StatusCode.Should().Be(404);
        ex.Which.ErrorCode.Should().Be(ErrorCodes.ErrContentNotFound);
    }
}
