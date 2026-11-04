using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Moq;
using VeggieLife.BLL.Common.Constants;
using VeggieLife.BLL.Common.Responses;
using VeggieLife.BLL.DTOs.Admin.Moderation;
using VeggieLife.BLL.Services.Cache;
using VeggieLife.BLL.Services.Knowledge;
using VeggieLife.DAL.Context;
using VeggieLife.DAL.Entities;
using Xunit;

namespace VeggieLife.Tests.Integration.Controllers;

/// <summary>
/// Bộ kiểm thử tích hợp (Integration Tests) gọi qua HTTP pipeline cho các endpoint kiểm duyệt nội dung của Admin (FN-ADM-04, FN-ADM-05, FN-ADM-06).
/// </summary>
public class AdminModerationEndpointIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly Mock<IRedisService> _mockRedis = new();
    private readonly Mock<IKnowledgeSyncQueue> _mockKnowledgeSync = new();
    private readonly string _testDbName = "VeggieLifeAdminModerationIntegrationDb_" + Guid.NewGuid();

    public AdminModerationEndpointIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Development");
            builder.ConfigureServices(services =>
            {
                var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
                if (descriptor != null)
                {
                    services.Remove(descriptor);
                }

                services.AddDbContext<AppDbContext>(options =>
                {
                    options.UseInMemoryDatabase(_testDbName);
                    options.ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning));
                });

                var redisDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IRedisService));
                if (redisDescriptor != null)
                {
                    services.Remove(redisDescriptor);
                }
                services.AddSingleton(_mockRedis.Object);

                var knowledgeDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IKnowledgeSyncQueue));
                if (knowledgeDescriptor != null)
                {
                    services.Remove(knowledgeDescriptor);
                }
                services.AddSingleton(_mockKnowledgeSync.Object);

                var sp = services.BuildServiceProvider();
                using var scope = sp.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                db.Database.EnsureDeleted();
                db.Database.EnsureCreated();
                SeedTestData(db);
            });
        });

        _mockRedis.Setup(r => r.RemoveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _mockKnowledgeSync.Setup(k => k.EnqueueDeleteAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _mockKnowledgeSync.Setup(k => k.EnqueueUpsertAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
    }

    private static void SeedTestData(AppDbContext db)
    {
        // 1. Users
        var admin = new User
        {
            UserId = 1,
            Email = "admin.mod@veggielife.vn",
            FullName = "Admin Quản Trị Duyệt",
            Role = UserRoles.Admin,
            AccountStatus = "ACTIVE",
            CountViolationReports = 0,
            CreatedAt = DateTime.UtcNow
        };
        var author1 = new User
        {
            UserId = 10,
            Email = "author1@veggielife.vn",
            FullName = "Tác giả vi phạm 1",
            Role = UserRoles.Member,
            AccountStatus = "ACTIVE",
            CountViolationReports = 0,
            CreatedAt = DateTime.UtcNow
        };
        var author2 = new User
        {
            UserId = 11,
            Email = "author2@veggielife.vn",
            FullName = "Tác giả vi phạm 2",
            Role = UserRoles.Member,
            AccountStatus = "ACTIVE",
            CountViolationReports = 3, // Next violation -> 4 (Suspend 3 days)
            CreatedAt = DateTime.UtcNow
        };
        var normalMember = new User
        {
            UserId = 2,
            Email = "member.mod@veggielife.vn",
            FullName = "Thành viên bình thường",
            Role = UserRoles.Member,
            AccountStatus = "ACTIVE",
            CountViolationReports = 0,
            CreatedAt = DateTime.UtcNow
        };
        db.Users.AddRange(admin, author1, author2, normalMember);

        // 2. Categories & DietarySchool for Recipes
        var cat = new FoodCategory
        {
            CategoryId = 1,
            CategoryName = "Món chính chay",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        var school = new DietarySchool
        {
            DietarySchoolId = 1,
            SchoolName = "Chay Thuần (Vegan)",
            SchoolCode = "VEGAN",
            ForbiddenFoodRules = "Meat, Fish, Egg, Dairy",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        db.FoodCategories.Add(cat);
        db.DietarySchools.Add(school);

        // 3. Target Items: Recipe, Post, Video, Comment
        var recipe1 = new Recipe
        {
            RecipeId = 101,
            UserId = author1.UserId,
            DietarySchoolId = school.DietarySchoolId,
            Title = "Canh nấm chay thanh đạm",
            Instructions = "Nấu nấm với nước dừa tươi trong 20 phút",
            PrepTimeMinutes = 10,
            CookTimeMinutes = 20,
            Servings = 4,
            CaloriesKcal = 150,
            DifficultyLevel = "EASY",
            IsVerified = true,
            Status = "FLAGGED",
            Source = "USER",
            CreatedAt = DateTime.UtcNow
        };
        var recipe2 = new Recipe
        {
            RecipeId = 102,
            UserId = author2.UserId,
            DietarySchoolId = school.DietarySchoolId,
            Title = "Bún riêu chay có thịt heo",
            Instructions = "Nấu với thịt heo ba chỉ",
            PrepTimeMinutes = 15,
            CookTimeMinutes = 30,
            Servings = 2,
            CaloriesKcal = 350,
            DifficultyLevel = "MEDIUM",
            IsVerified = false,
            Status = "PENDING_APPROVAL",
            Source = "USER",
            CreatedAt = DateTime.UtcNow
        };
        db.Recipes.AddRange(recipe1, recipe2);

        var post1 = new Post
        {
            PostId = 201,
            UserId = author1.UserId,
            Title = "Chia sẻ kinh nghiệm ăn chay",
            Content = "Ăn chay giúp thanh lọc cơ thể rất tốt.",
            Visibility = "PUBLIC",
            ModerationStatus = "FLAGGED",
            CreatedAt = DateTime.UtcNow
        };
        db.Posts.Add(post1);

        var post2 = new Post
        {
            PostId = 202,
            UserId = author2.UserId,
            Title = "Video hướng dẫn làm món xào",
            Content = "Xem video hướng dẫn chi tiết bên dưới.",
            Visibility = "PUBLIC",
            ModerationStatus = "APPROVED_ACTIVE",
            CreatedAt = DateTime.UtcNow
        };
        db.Posts.Add(post2);

        var video1 = new Video
        {
            VideoId = 301,
            PostId = post2.PostId,
            Title = "Mẹo làm gỏi chay giòn ngọt",
            VideoUrl = "https://cdn.veggielife.vn/videos/301.mp4",
            DurationSeconds = 120,
            Status = "PENDING",
            CreatedAt = DateTime.UtcNow
        };
        db.Videos.Add(video1);

        var commentRoot = new Comment
        {
            CommentId = 401,
            UserId = author1.UserId,
            PostId = post1.PostId,
            Content = "Bình luận xúc phạm chửi bới người khác",
            Status = "ACTIVE",
            IsSoftDeleted = false,
            CreatedAt = DateTime.UtcNow
        };
        var commentReply1 = new Comment
        {
            CommentId = 402,
            UserId = author2.UserId,
            PostId = post1.PostId,
            ParentCommentId = 401,
            Content = "Đúng rồi, phản hồi bênh vực vi phạm",
            Status = "ACTIVE",
            IsSoftDeleted = false,
            CreatedAt = DateTime.UtcNow
        };
        var commentReply2 = new Comment
        {
            CommentId = 403,
            UserId = normalMember.UserId,
            PostId = post1.PostId,
            ParentCommentId = 402,
            Content = "Trả lời cho cấp 2",
            Status = "ACTIVE",
            IsSoftDeleted = false,
            CreatedAt = DateTime.UtcNow
        };
        db.Comments.AddRange(commentRoot, commentReply1, commentReply2);

        // 4. Violation Reports
        var r1 = new ViolationReport
        {
            ReportId = 501,
            ReporterUserId = null,
            AdminId = null,
            RecipeId = recipe1.RecipeId,
            ViolationType = "VIO_MEAT",
            EvidenceSummary = "AI phát hiện có chứa thành phần thịt lợn",
            ReportSource = "AI_SCAN",
            Status = "PENDING",
            CreatedAt = DateTime.UtcNow.AddMinutes(-30)
        };
        var r2 = new ViolationReport
        {
            ReportId = 502,
            ReporterUserId = normalMember.UserId,
            AdminId = null,
            PostId = post1.PostId,
            ViolationType = "VIO_HATE",
            EvidenceSummary = "Người dùng báo cáo bài viết ngôn từ kích động thù địch",
            ReportSource = "USER_REPORT",
            Status = "PENDING",
            CreatedAt = DateTime.UtcNow.AddMinutes(-20)
        };
        var r3 = new ViolationReport
        {
            ReportId = 503,
            ReporterUserId = null,
            AdminId = null,
            VideoId = video1.VideoId,
            ViolationType = "VIO_SPAM",
            EvidenceSummary = "AI gắn cờ video quảng cáo spam link cá độ",
            ReportSource = "AI_SCAN",
            Status = "PENDING",
            CreatedAt = DateTime.UtcNow.AddMinutes(-10)
        };
        var r4 = new ViolationReport
        {
            ReportId = 504,
            ReporterUserId = normalMember.UserId,
            AdminId = null,
            CommentId = commentRoot.CommentId,
            ViolationType = "VIO_HATE",
            EvidenceSummary = "Bình luận xúc phạm danh dự người khác",
            ReportSource = "USER_REPORT",
            Status = "PENDING",
            CreatedAt = DateTime.UtcNow.AddMinutes(-5)
        };
        var r5Closed = new ViolationReport
        {
            ReportId = 505,
            ReporterUserId = normalMember.UserId,
            AdminId = admin.UserId,
            RecipeId = recipe2.RecipeId,
            ViolationType = "VIO_MEAT",
            EvidenceSummary = "Báo cáo này đã từng được duyệt trước đó",
            ReportSource = "USER_REPORT",
            Status = "APPROVE", // Already closed!
            CreatedAt = DateTime.UtcNow.AddDays(-1)
        };
        db.ViolationReports.AddRange(r1, r2, r3, r4, r5Closed);

        db.SaveChanges();
    }

    private string GenerateJwtToken(int userId, string role)
    {
        using var scope = _factory.Services.CreateScope();
        var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        var secretKey = config["Jwt:SecretKey"]
            ?? config["JwtSettings:SecretKey"]
            ?? "veggielife-development-super-secret-jwt-key-32-chars-long";
        var issuer = config["Jwt:Issuer"] ?? config["JwtSettings:Issuer"] ?? "VeggieLife.API";
        var audience = config["Jwt:Audience"] ?? config["JwtSettings:Audience"] ?? "VeggieLife.Client";

        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes(secretKey);

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                new Claim(ClaimTypes.Role, role),
                new Claim(ClaimTypes.Email, $"user{userId}@veggielife.vn")
            }),
            Expires = DateTime.UtcNow.AddHours(2),
            Issuer = issuer,
            Audience = audience,
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }

    private HttpClient CreateAdminClient()
    {
        var client = _factory.CreateClient();
        var token = GenerateJwtToken(1, UserRoles.Admin);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    private HttpClient CreateMemberClient()
    {
        var client = _factory.CreateClient();
        var token = GenerateJwtToken(2, UserRoles.Member);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    #region GET /api/v1/admin/moderation/queue

    [Fact]
    public async Task GetQueue_WithoutToken_Returns401Unauthorized()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/v1/admin/moderation/queue");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain(ErrorCodes.ErrAuthInvalidToken);
    }

    [Fact]
    public async Task GetQueue_WithMemberToken_Returns403Forbidden()
    {
        var client = CreateMemberClient();
        var response = await client.GetAsync("/api/v1/admin/moderation/queue");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain(ErrorCodes.ErrAdmForbidden);
    }

    [Fact]
    public async Task GetQueue_WithAdminToken_ReturnsPendingListAndMeta()
    {
        var client = CreateAdminClient();
        var response = await client.GetAsync("/api/v1/admin/moderation/queue?page=1&size=10");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<List<AdminModerationQueueItemDto>>>();
        body.Should().NotBeNull();
        body!.StatusCode.Should().Be(200);
        body.Data.Should().NotBeNull();
        // 4 pending reports seeded (501, 502, 503, 504), report 505 is APPROVE so excluded by default
        body.Data.Should().HaveCount(4);
        body.Meta.Should().NotBeNull();
        body.Meta!.Page.Should().Be(1);
        body.Meta.Total.Should().Be(4);
    }

    [Fact]
    public async Task GetQueue_FilterByReportSource_ReturnsMatchingReportsOnly()
    {
        var client = CreateAdminClient();
        var response = await client.GetAsync("/api/v1/admin/moderation/queue?reportSource=AI_SCAN");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<List<AdminModerationQueueItemDto>>>();
        body.Should().NotBeNull();
        body!.Data.Should().AllSatisfy(item => item.ReportSource.Should().Be("AI_SCAN"));
        body.Data.Should().HaveCount(2); // 501 and 503
    }

    [Fact]
    public async Task GetQueue_FilterByViolationType_ReturnsMatchingReportsOnly()
    {
        var client = CreateAdminClient();
        var response = await client.GetAsync("/api/v1/admin/moderation/queue?violationType=VIO_HATE");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<List<AdminModerationQueueItemDto>>>();
        body.Should().NotBeNull();
        body!.Data.Should().AllSatisfy(item => item.ViolationType.Should().Be("VIO_HATE"));
        body.Data.Should().HaveCount(2); // 502 (post) and 504 (comment)
    }

    [Fact]
    public async Task GetQueue_FilterByTargetType_ReturnsMatchingReportsOnly()
    {
        var client = CreateAdminClient();
        var response = await client.GetAsync("/api/v1/admin/moderation/queue?targetType=COMMENT");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<List<AdminModerationQueueItemDto>>>();
        body.Should().NotBeNull();
        body!.Data.Should().HaveCount(1);
        body.Data[0].TargetType.Should().Be("COMMENT");
        body.Data[0].TargetId.Should().Be(401);
    }

    [Fact]
    public async Task GetQueue_WithInvalidQueryParameters_Returns400BadRequest()
    {
        var client = CreateAdminClient();

        // Invalid page
        var responsePage = await client.GetAsync("/api/v1/admin/moderation/queue?page=0");
        responsePage.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        // Invalid violation type
        var responseVio = await client.GetAsync("/api/v1/admin/moderation/queue?violationType=INVALID_VIO");
        responseVio.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        // Invalid target type
        var responseTarget = await client.GetAsync("/api/v1/admin/moderation/queue?targetType=INVALID_TARGET");
        responseTarget.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    #endregion

    #region PUT /api/v1/admin/moderation/reports/{id}/decision

    [Fact]
    public async Task DecideReport_WithoutToken_Returns401Unauthorized()
    {
        var client = _factory.CreateClient();
        var req = new DecideAdminModerationReportRequest
        {
            Decision = "UPHOLD",
            Note = "Xác nhận vi phạm chuẩn mực cộng đồng."
        };
        var response = await client.PutAsJsonAsync("/api/v1/admin/moderation/reports/501/decision", req);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task DecideReport_WithMemberToken_Returns403Forbidden()
    {
        var client = CreateMemberClient();
        var req = new DecideAdminModerationReportRequest
        {
            Decision = "UPHOLD",
            Note = "Thành viên không được phép duyệt."
        };
        var response = await client.PutAsJsonAsync("/api/v1/admin/moderation/reports/501/decision", req);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task DecideReport_NonExistentReport_Returns404NotFound()
    {
        var client = CreateAdminClient();
        var req = new DecideAdminModerationReportRequest
        {
            Decision = "UPHOLD",
            Note = "Báo cáo này không tồn tại trong hệ thống."
        };
        var response = await client.PutAsJsonAsync("/api/v1/admin/moderation/reports/999999/decision", req);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain(ErrorCodes.ErrAdmNotFound);
    }

    [Fact]
    public async Task DecideReport_InvalidRequest_Returns400BadRequest()
    {
        var client = CreateAdminClient();

        // Note too short (< 10 chars)
        var reqShortNote = new DecideAdminModerationReportRequest
        {
            Decision = "UPHOLD",
            Note = "Ngắn"
        };
        var res1 = await client.PutAsJsonAsync("/api/v1/admin/moderation/reports/501/decision", reqShortNote);
        res1.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        // Invalid decision string
        var reqInvalidDecision = new DecideAdminModerationReportRequest
        {
            Decision = "MAYBE",
            Note = "Ghi chú hợp lệ dài trên mười ký tự."
        };
        var res2 = await client.PutAsJsonAsync("/api/v1/admin/moderation/reports/501/decision", reqInvalidDecision);
        res2.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task DecideReport_ReDecidingClosedReport_Returns409Conflict()
    {
        var client = CreateAdminClient();
        var req = new DecideAdminModerationReportRequest
        {
            Decision = "DISMISS",
            Note = "Cố tình xử lý lại báo cáo đã đóng trước đó."
        };

        // Report 505 was seeded with Status = "APPROVE"
        var response = await client.PutAsJsonAsync("/api/v1/admin/moderation/reports/505/decision", req);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain(ErrorCodes.ErrAdmReportClosed);
    }

    [Fact]
    public async Task DecideReport_UpholdRecipeViolation_WithSanction_Success()
    {
        var client = CreateAdminClient();
        var req = new DecideAdminModerationReportRequest
        {
            Decision = "UPHOLD",
            Note = "Công thức có chứa thịt lợn, vi phạm trường phái thuần chay nghiêm trọng.",
            ApplySanction = true
        };

        var response = await client.PutAsJsonAsync("/api/v1/admin/moderation/reports/501/decision", req);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<DecideAdminModerationReportResponseData>>();
        body.Should().NotBeNull();
        body!.StatusCode.Should().Be(200);
        body.Data.Should().NotBeNull();
        body.Data.ReportId.Should().Be(501);
        body.Data.Status.Should().Be("RESOLVED");
        body.Data.TargetStatus.Should().Be("REJECTED");

        // Verify Database state
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var reportInDb = await db.ViolationReports.FindAsync(501);
        reportInDb.Should().NotBeNull();
        reportInDb!.Status.Should().Be("APPROVE");
        reportInDb.AdminId.Should().Be(1);

        var recipeInDb = await db.Recipes.FindAsync(101);
        recipeInDb.Should().NotBeNull();
        recipeInDb!.Status.Should().Be("REJECTED");

        var authorInDb = await db.Users.FindAsync(10);
        authorInDb.Should().NotBeNull();
        authorInDb!.CountViolationReports.Should().Be(1);

        // Verify Audit Log
        var audit = await db.AuditLogs.FirstOrDefaultAsync(a => a.TargetTable == "ViolationReports" && a.TargetId == "501");
        audit.Should().NotBeNull();
        audit!.Action.Should().Be("MODERATION_DECISION");
        audit.ActorUserId.Should().Be(1);

        // Verify Knowledge sync queue called to delete recipe
        _mockKnowledgeSync.Verify(k => k.EnqueueDeleteAsync("RECIPE", 101, It.IsAny<CancellationToken>()), Times.AtLeastOnce());
    }

    [Fact]
    public async Task DecideReport_UpholdPost_TriggersProgressiveSanctionAndRedisRevocation()
    {
        var client = CreateAdminClient();
        // Report 503 is Video on Post 202 belonging to author2 (author2 had CountViolationReports = 3)
        // With +1, CountViolationReports = 4 -> triggers "Suspend 3 days" and Redis session delete
        var req = new DecideAdminModerationReportRequest
        {
            Decision = "UPHOLD",
            Note = "Video chứa quảng cáo cờ bạc, vi phạm điều khoản nền tảng.",
            ApplySanction = true
        };

        var response = await client.PutAsJsonAsync("/api/v1/admin/moderation/reports/503/decision", req);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<DecideAdminModerationReportResponseData>>();
        body.Should().NotBeNull();
        body!.Data.Should().NotBeNull();
        body.Data!.TargetStatus.Should().Be("BANNED");

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var author2 = await db.Users.FindAsync(11);
        author2.Should().NotBeNull();
        author2!.CountViolationReports.Should().Be(4);
        author2.AccountStatus.Should().Be("SUSPENDED");
        author2.LockedUntil.Should().NotBeNull();

        // Verify Redis session revocation
        _mockRedis.Verify(r => r.RemoveAsync("user_sessions:11", It.IsAny<CancellationToken>()), Times.AtLeastOnce());
    }

    [Fact]
    public async Task DecideReport_DismissPostReport_RestoresStatusAndEnqueuesUpsert()
    {
        var client = CreateAdminClient();
        var req = new DecideAdminModerationReportRequest
        {
            Decision = "DISMISS",
            Note = "Nội dung bài viết không vi phạm quy tắc, phục hồi lại bài viết."
        };

        var response = await client.PutAsJsonAsync("/api/v1/admin/moderation/reports/502/decision", req);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<DecideAdminModerationReportResponseData>>();
        body.Should().NotBeNull();
        body!.Data.Should().NotBeNull();
        body.Data!.Status.Should().Be("RESOLVED");
        body.Data.TargetStatus.Should().Be("APPROVED_ACTIVE");

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var report = await db.ViolationReports.FindAsync(502);
        report.Should().NotBeNull();
        report!.Status.Should().Be("REJECT");

        var post = await db.Posts.FindAsync(201);
        post.Should().NotBeNull();
        post!.ModerationStatus.Should().Be("APPROVED_ACTIVE");

        // Verify KnowledgeSync EnqueueUpsertAsync called to restore post knowledge
        _mockKnowledgeSync.Verify(k => k.EnqueueUpsertAsync("POST", 201, It.IsAny<CancellationToken>()), Times.AtLeastOnce());

        // Verify Audit Log
        var audit = await db.AuditLogs.FirstOrDefaultAsync(a => a.TargetTable == "ViolationReports" && a.TargetId == "502");
        audit.Should().NotBeNull();
        audit!.Action.Should().Be("MODERATION_DECISION");
    }

    #endregion

    #region DELETE /api/v1/admin/comments/{id}

    [Fact]
    public async Task DeleteComment_WithoutToken_Returns401Unauthorized()
    {
        var client = _factory.CreateClient();
        var response = await client.DeleteAsync("/api/v1/admin/comments/401?reason=BinhLuanViPhamQuyDinhDauTranh");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task DeleteComment_WithMemberToken_Returns403Forbidden()
    {
        var client = CreateMemberClient();
        var response = await client.DeleteAsync("/api/v1/admin/comments/401?reason=BinhLuanViPhamQuyDinhDauTranh");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task DeleteComment_NonExistent_Returns404NotFound()
    {
        var client = CreateAdminClient();
        var response = await client.DeleteAsync("/api/v1/admin/comments/999999?reason=XoaBinhLuanKhongTonTaiNhe");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain(ErrorCodes.ErrAdmNotFound);
    }

    [Fact]
    public async Task DeleteComment_ReasonTooShort_Returns400BadRequest()
    {
        var client = CreateAdminClient();
        var response = await client.DeleteAsync("/api/v1/admin/comments/401?reason=Ngan");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain(ErrorCodes.ErrValidation);
    }

    [Fact]
    public async Task DeleteComment_CascadeSoftDeletesReplies_Success()
    {
        var client = CreateAdminClient();
        var reason = "Vi phạm chuẩn mực ngôn từ, công kích cá nhân nghiêm trọng";
        var response = await client.DeleteAsync($"/api/v1/admin/comments/401?reason={Uri.EscapeDataString(reason)}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<DeleteAdminCommentResponseData>>();
        body.Should().NotBeNull();
        body!.StatusCode.Should().Be(200);
        body.Data.Should().NotBeNull();
        // Comment 401 has reply 402, which has reply 403 -> Total 3 comments cascaded
        body.Data.Deleted.Should().Be(3);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var c401 = await db.Comments.FindAsync(401);
        var c402 = await db.Comments.FindAsync(402);
        var c403 = await db.Comments.FindAsync(403);

        c401.Should().NotBeNull();
        c401!.IsSoftDeleted.Should().BeTrue();
        c401.Status.Should().Be("HIDDEN");

        c402.Should().NotBeNull();
        c402!.IsSoftDeleted.Should().BeTrue();
        c402.Status.Should().Be("HIDDEN");

        c403.Should().NotBeNull();
        c403!.IsSoftDeleted.Should().BeTrue();
        c403.Status.Should().Be("HIDDEN");

        // Verify KnowledgeSync EnqueueDeleteAsync called
        _mockKnowledgeSync.Verify(k => k.EnqueueDeleteAsync("COMMENT", 401, It.IsAny<CancellationToken>()), Times.AtLeastOnce());

        // Verify Audit Log
        var audit = await db.AuditLogs.FirstOrDefaultAsync(a => a.TargetTable == "Comments" && a.TargetId == "401");
        audit.Should().NotBeNull();
        audit!.Action.Should().Be("COMMENT_DELETE");
        audit.ActorUserId.Should().Be(1);
    }

    #endregion
}
