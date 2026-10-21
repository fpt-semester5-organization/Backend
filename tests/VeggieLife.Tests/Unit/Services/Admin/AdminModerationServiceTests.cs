using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using VeggieLife.BLL.Common.Constants;
using VeggieLife.BLL.Common.Exceptions;
using VeggieLife.BLL.DTOs.Admin.Moderation;
using VeggieLife.BLL.Services.Admin;
using VeggieLife.BLL.Services.Cache;
using VeggieLife.BLL.Services.Knowledge;
using VeggieLife.DAL.Context;
using VeggieLife.DAL.Entities;
using Xunit;

namespace VeggieLife.Tests.Unit.Services.Admin;

/// <summary>
/// Bộ kiểm thử đơn vị cho AdminModerationService (FN-ADM-04, FN-ADM-05, FN-ADM-06).
/// Kiểm tra:
/// 1. Lọc hàng đợi kiểm duyệt theo ReportSource, ViolationType, TargetType, phân trang.
/// 2. Quyết định báo cáo: UPHOLD (thu hồi nội dung, tăng vi phạm tác giả, áp chế tài, thu hồi phiên, gỡ khỏi kho AI).
/// 3. Quyết định báo cáo: DISMISS (bác bỏ vi phạm, phục hồi nội dung, đồng bộ lại AI).
/// 4. Xử lý báo cáo đã đóng (ERR_ADM_REPORT_CLOSED / 409 Conflict).
/// 5. Xóa bình luận cấp quản trị (FN-ADM-06): Xóa mềm phân cấp toàn bộ cây con, ghi audit log, đồng bộ AI.
/// </summary>
public class AdminModerationServiceTests : IDisposable
{
    private readonly AppDbContext _db;
    private readonly Mock<IRedisService> _mockRedis = new();
    private readonly Mock<IKnowledgeSyncQueue> _mockKnowledgeSyncQueue = new();
    private readonly AdminModerationService _service;
    private const int AdminId = 999;

    public AdminModerationServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase("AdminModerationServiceTestsDb_" + Guid.NewGuid())
            .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        _db = new AppDbContext(options);
        _service = new AdminModerationService(
            _db,
            _mockRedis.Object,
            _mockKnowledgeSyncQueue.Object,
            NullLogger<AdminModerationService>.Instance);

        SeedTestData();
    }

    public void Dispose()
    {
        _db.Database.EnsureDeleted();
        _db.Dispose();
    }

    private void SeedTestData()
    {
        var author1 = new User
        {
            UserId = 10,
            Email = "author1@veggielife.vn",
            FullName = "Tác Giả Một",
            Role = UserRoles.Member,
            AccountStatus = AccountStatuses.Active,
            CountViolationReports = 0,
            CreatedAt = DateTime.UtcNow
        };

        var author2 = new User
        {
            UserId = 11,
            Email = "author2@veggielife.vn",
            FullName = "Tác Giả Hai",
            Role = UserRoles.Member,
            AccountStatus = AccountStatuses.Active,
            CountViolationReports = 12, // Vi phạm lần 13 sẽ bị BAN vĩnh viễn
            CreatedAt = DateTime.UtcNow
        };

        _db.Users.AddRange(author1, author2);

        var recipe = new Recipe
        {
            RecipeId = 201,
            UserId = author1.UserId,
            Title = "Món chay nghi vấn thịt bò",
            Status = "PENDING_REVIEW",
            Instructions = "Hướng dẫn nấu",
            DifficultyLevel = "MEDIUM",
            Source = "USER",
            CreatedAt = DateTime.UtcNow
        };

        var post = new Post
        {
            PostId = 301,
            UserId = author1.UserId,
            Title = "Bài viết quảng cáo spam",
            Content = "Nội dung bài viết",
            Visibility = "PUBLIC",
            ModerationStatus = "PENDING",
            CreatedAt = DateTime.UtcNow
        };

        var commentParent = new Comment
        {
            CommentId = 401,
            UserId = author1.UserId,
            PostId = 301,
            Content = "Bình luận độc hại",
            Status = "VISIBLE",
            IsSoftDeleted = false,
            CreatedAt = DateTime.UtcNow
        };

        var commentChild = new Comment
        {
            CommentId = 402,
            UserId = author2.UserId,
            PostId = 301,
            ParentCommentId = 401,
            Content = "Bình luận con phản hồi",
            Status = "VISIBLE",
            IsSoftDeleted = false,
            CreatedAt = DateTime.UtcNow
        };

        _db.Recipes.Add(recipe);
        _db.Posts.Add(post);
        _db.Comments.AddRange(commentParent, commentChild);

        _db.ViolationReports.AddRange(
            new ViolationReport
            {
                ReportId = 1,
                RecipeId = recipe.RecipeId,
                ReportSource = "AI_SCAN",
                ViolationType = "VIO_MEAT",
                EvidenceSummary = "Phát hiện thành phần thịt trong công thức",
                Status = "PENDING",
                CreatedAt = DateTime.UtcNow.AddMinutes(-10)
            },
            new ViolationReport
            {
                ReportId = 2,
                PostId = post.PostId,
                ReportSource = "USER_REPORT",
                ViolationType = "VIO_SPAM",
                EvidenceSummary = "Spam quảng cáo cờ bạc",
                Status = "PENDING",
                CreatedAt = DateTime.UtcNow.AddMinutes(-5)
            },
            new ViolationReport
            {
                ReportId = 3,
                CommentId = commentParent.CommentId,
                ReportSource = "USER_REPORT",
                ViolationType = "VIO_HATE",
                EvidenceSummary = "Ngôn từ thô tục",
                Status = "PENDING",
                CreatedAt = DateTime.UtcNow.AddMinutes(-1)
            },
            new ViolationReport
            {
                ReportId = 4,
                PostId = post.PostId,
                ReportSource = "USER_REPORT",
                ViolationType = "VIO_SPAM",
                EvidenceSummary = "Đã xử lý xong",
                Status = "RESOLVED",
                CreatedAt = DateTime.UtcNow.AddHours(-1)
            }
        );

        _db.SaveChanges();
    }

    #region 1. GetModerationQueueAsync

    [Fact]
    public async Task GetModerationQueueAsync_NoFilter_ReturnsPendingReportsOrderedByCreatedAtDesc()
    {
        var request = new AdminModerationQueueFilterRequest
        {
            Page = 1,
            Size = 10
        };

        var (items, meta) = await _service.GetModerationQueueAsync(request);

        meta.Total.Should().Be(3); // Báo cáo số 4 đã RESOLVED không nằm trong PENDING queue
        items.Should().HaveCount(3);
        items[0].ReportId.Should().Be(3); // Mới nhất trước
        items[1].ReportId.Should().Be(2);
        items[2].ReportId.Should().Be(1);
    }

    [Fact]
    public async Task GetModerationQueueAsync_FilterByReportSource_ReturnsMatchingReports()
    {
        var request = new AdminModerationQueueFilterRequest
        {
            ReportSource = "AI_SCAN",
            Page = 1,
            Size = 10
        };

        var (items, meta) = await _service.GetModerationQueueAsync(request);

        meta.Total.Should().Be(1);
        items.Should().HaveCount(1);
        items[0].ReportId.Should().Be(1);
        items[0].ReportSource.Should().Be("AI_SCAN");
    }

    [Fact]
    public async Task GetModerationQueueAsync_FilterByViolationType_ReturnsMatchingReports()
    {
        var request = new AdminModerationQueueFilterRequest
        {
            ViolationType = "VIO_SPAM",
            Page = 1,
            Size = 10
        };

        var (items, meta) = await _service.GetModerationQueueAsync(request);

        meta.Total.Should().Be(1);
        items.Should().HaveCount(1);
        items[0].ReportId.Should().Be(2);
        items[0].ViolationType.Should().Be("VIO_SPAM");
    }

    [Fact]
    public async Task GetModerationQueueAsync_FilterByTargetType_ReturnsCorrectTarget()
    {
        var request = new AdminModerationQueueFilterRequest
        {
            TargetType = "COMMENT",
            Page = 1,
            Size = 10
        };

        var (items, meta) = await _service.GetModerationQueueAsync(request);

        meta.Total.Should().Be(1);
        items.Should().HaveCount(1);
        items[0].TargetType.Should().Be("COMMENT");
        items[0].TargetId.Should().Be(401);
    }

    #endregion

    #region 2. DecideReportAsync

    [Fact]
    public async Task DecideReportAsync_ReportNotFound_ThrowsNotFound()
    {
        var request = new DecideAdminModerationReportRequest
        {
            Decision = "UPHOLD",
            Note = "Xác nhận vi phạm"
        };

        var act = async () => await _service.DecideReportAsync(99999, request, AdminId);

        var ex = await act.Should().ThrowAsync<AppException>();
        ex.Which.StatusCode.Should().Be(404);
        ex.Which.ErrorCode.Should().Be(ErrorCodes.ErrAdmNotFound);
    }

    [Fact]
    public async Task DecideReportAsync_ReportAlreadyClosed_ThrowsConflict()
    {
        var request = new DecideAdminModerationReportRequest
        {
            Decision = "UPHOLD",
            Note = "Đã xử lý lại"
        };

        // ReportId 4 có status là RESOLVED
        var act = async () => await _service.DecideReportAsync(4, request, AdminId);

        var ex = await act.Should().ThrowAsync<AppException>();
        ex.Which.StatusCode.Should().Be(409);
        ex.Which.ErrorCode.Should().Be(ErrorCodes.ErrAdmReportClosed);
    }

    [Fact]
    public async Task DecideReportAsync_UpholdRecipe_UpdatesRecipeAndAuthorViolationCount_EnqueuesDelete()
    {
        var request = new DecideAdminModerationReportRequest
        {
            Decision = "UPHOLD",
            Note = "Công thức có chứa thịt động vật",
            ApplySanction = true
        };

        var response = await _service.DecideReportAsync(1, request, AdminId);

        response.ReportId.Should().Be(1);
        response.Status.Should().Be("RESOLVED");
        response.TargetStatus.Should().Be("REJECTED");

        var report = await _db.ViolationReports.FindAsync(1);
        report!.Status.Should().Be("APPROVE");
        report.AdminId.Should().Be(AdminId);

        var recipe = await _db.Recipes.FindAsync(201);
        recipe!.Status.Should().Be("REJECTED");

        var author = await _db.Users.FindAsync(10);
        author!.CountViolationReports.Should().Be(1);

        _mockKnowledgeSyncQueue.Verify(
            q => q.EnqueueDeleteAsync("RECIPE", 201, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task DecideReportAsync_UpholdWithSanction_AuthorExceeds13Violations_BansAccountAndRevokesSessions()
    {
        // Gán report cho user 11 đã có 12 vi phạm
        var recipe2 = new Recipe
        {
            RecipeId = 202,
            UserId = 11,
            Title = "Công thức vi phạm lần 13",
            Status = "PENDING_REVIEW",
            Instructions = "B1",
            DifficultyLevel = "EASY",
            Source = "USER",
            CreatedAt = DateTime.UtcNow
        };
        _db.Recipes.Add(recipe2);

        var report5 = new ViolationReport
        {
            ReportId = 5,
            RecipeId = 202,
            ReportSource = "USER_REPORT",
            ViolationType = "VIO_MEAT",
            EvidenceSummary = "Nội dung vi phạm nghiêm trọng",
            Status = "PENDING",
            CreatedAt = DateTime.UtcNow
        };
        _db.ViolationReports.Add(report5);
        await _db.SaveChangesAsync();

        var request = new DecideAdminModerationReportRequest
        {
            Decision = "UPHOLD",
            Note = "Tái phạm nhiều lần, tiến hành khóa vĩnh viễn",
            ApplySanction = true
        };

        var response = await _service.DecideReportAsync(5, request, AdminId);

        response.Status.Should().Be("RESOLVED");

        var author = await _db.Users.FindAsync(11);
        author!.CountViolationReports.Should().Be(13);
        author.AccountStatus.Should().Be(AccountStatuses.Banned);

        _mockRedis.Verify(
            r => r.RemoveAsync("user_sessions:11", It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task DecideReportAsync_DismissRecipe_RestoresRecipeStatus_EnqueuesUpsert()
    {
        var request = new DecideAdminModerationReportRequest
        {
            Decision = "DISMISS",
            Note = "Báo cáo sai, nguyên liệu là sườn non chay không phải thịt thật",
            ApplySanction = false
        };

        var response = await _service.DecideReportAsync(1, request, AdminId);

        response.ReportId.Should().Be(1);
        response.Status.Should().Be("RESOLVED");
        response.TargetStatus.Should().Be("APPROVED_ACTIVE");

        var report = await _db.ViolationReports.FindAsync(1);
        report!.Status.Should().Be("REJECT");
        report.AdminId.Should().Be(AdminId);

        var recipe = await _db.Recipes.FindAsync(201);
        recipe!.Status.Should().Be("APPROVED_ACTIVE");

        _mockKnowledgeSyncQueue.Verify(
            q => q.EnqueueUpsertAsync("RECIPE", 201, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion

    #region 3. DeleteCommentAsync

    [Fact]
    public async Task DeleteCommentAsync_CommentNotFound_ThrowsNotFound()
    {
        var act = async () => await _service.DeleteCommentAsync(99999, "Vi phạm nội quy", AdminId);

        var ex = await act.Should().ThrowAsync<AppException>();
        ex.Which.StatusCode.Should().Be(404);
        ex.Which.ErrorCode.Should().Be(ErrorCodes.ErrAdmNotFound);
    }

    [Fact]
    public async Task DeleteCommentAsync_CascadesSoftDeleteToChildren_WritesAuditLogAndSyncsAi()
    {
        var response = await _service.DeleteCommentAsync(401, "Bình luận chứa ngôn từ xúc phạm và kích động", AdminId);

        // Đã xóa mềm cả bình luận cha 401 và bình luận con 402
        response.Deleted.Should().Be(2);

        var parent = await _db.Comments.FindAsync(401);
        parent!.IsSoftDeleted.Should().BeTrue();
        parent.Status.Should().Be("HIDDEN");

        var child = await _db.Comments.FindAsync(402);
        child!.IsSoftDeleted.Should().BeTrue();
        child.Status.Should().Be("HIDDEN");

        var auditLog = await _db.AuditLogs.FirstOrDefaultAsync(a => a.TargetTable == "Comments" && a.TargetId == "401");
        auditLog.Should().NotBeNull();
        auditLog!.ActorUserId.Should().Be(AdminId);
        auditLog.Action.Should().Be("COMMENT_DELETE");
        auditLog.Note.Should().Be("Bình luận chứa ngôn từ xúc phạm và kích động");

        _mockKnowledgeSyncQueue.Verify(
            q => q.EnqueueDeleteAsync("COMMENT", 401, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion
}
