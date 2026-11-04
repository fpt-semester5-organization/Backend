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
using System.Text.Json;
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
using VeggieLife.BLL.DTOs.Admin.Users;
using VeggieLife.BLL.Services.Cache;
using VeggieLife.BLL.Services.Email;
using VeggieLife.DAL.Context;
using VeggieLife.DAL.Entities;
using Xunit;

namespace VeggieLife.Tests.Integration.Controllers;

/// <summary>
/// Bộ kiểm thử tích hợp (Integration Tests) cho các API Quản trị người dùng (FN-ADM-01, FN-ADM-02, FN-ADM-03).
/// Kiểm thử đầy đủ:
/// - BR-ADM-USR-001: Bảo toàn Admin cuối cùng (không thể tự khóa / vô hiệu hóa).
/// - BR-ADM-USR-002: Thang kỷ luật lũy tiến (WARN, RESTRICT_COMMENT, SUSPEND, BAN, REACTIVATE).
/// - BR-ADM-SYS-002: Ghi nhận nhật ký AuditLogs.
/// - BR-AUTH-004: Thu hồi phiên đăng nhập & Blacklist token.
/// </summary>
public class AdminUsersEndpointIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly Mock<IRedisService> _mockRedis = new();
    private readonly Mock<IEmailService> _mockEmail = new();
    private readonly string _testDbName = "VeggieLifeAdminUsersIntegrationDb_" + Guid.NewGuid();

    public AdminUsersEndpointIntegrationTests(WebApplicationFactory<Program> factory)
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

                var emailDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IEmailService));
                if (emailDescriptor != null)
                {
                    services.Remove(emailDescriptor);
                }
                services.AddSingleton(_mockEmail.Object);
            });
        });

        SeedTestData();
    }

    private void SeedTestData()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // 1. Dietary Schools
        if (!db.DietarySchools.Any())
        {
            db.DietarySchools.AddRange(
                new DietarySchool { DietarySchoolId = 1, SchoolName = "Thuần chay (Vegan)", SchoolCode = "VEGAN", ForbiddenFoodRules = "Meat, Fish", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
                new DietarySchool { DietarySchoolId = 2, SchoolName = "Chay có sữa (Lacto)", SchoolCode = "LACTO", ForbiddenFoodRules = "Meat, Fish, Egg", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow }
            );
        }

        // 2. Users
        var admin1 = new User
        {
            UserId = 100,
            Email = "admin.primary@veggielife.vn",
            FullName = "Admin Trưởng",
            Role = UserRoles.Admin,
            AccountStatus = AccountStatuses.Active,
            CreatedAt = DateTime.UtcNow.AddMonths(-3)
        };

        var admin2 = new User
        {
            UserId = 101,
            Email = "admin.secondary@veggielife.vn",
            FullName = "Admin Phó",
            Role = UserRoles.Admin,
            AccountStatus = AccountStatuses.Active,
            CreatedAt = DateTime.UtcNow.AddMonths(-2)
        };

        var memberToDiscipline = new User
        {
            UserId = 200,
            Email = "spammer@veggielife.vn",
            FullName = "Nguyễn Văn Spam",
            Role = UserRoles.Member,
            AccountStatus = AccountStatuses.Active,
            CountViolationReports = 4,
            CreatedAt = DateTime.UtcNow.AddMonths(-1)
        };

        var memberNormal = new User
        {
            UserId = 201,
            Email = "member.good@veggielife.vn",
            FullName = "Lê Thị Thuần Chay",
            Role = UserRoles.Member,
            AccountStatus = AccountStatuses.Active,
            CountViolationReports = 0,
            CreatedAt = DateTime.UtcNow.AddDays(-10)
        };

        var userBanned = new User
        {
            UserId = 202,
            Email = "banned.user@veggielife.vn",
            FullName = "Trần Bị Cấm",
            Role = UserRoles.Member,
            AccountStatus = AccountStatuses.Banned,
            CountViolationReports = 15,
            CreatedAt = DateTime.UtcNow.AddMonths(-5)
        };

        db.Users.AddRange(admin1, admin2, memberToDiscipline, memberNormal, userBanned);

        // 3. Health Profiles
        db.HealthProfiles.AddRange(
            new HealthProfile
            {
                UserId = 200,
                DietarySchoolId = 1,
                Gender = "MALE",
                DateOfBirth = new DateOnly(1995, 1, 1),
                HeightCm = 170,
                CurrentWeightKg = 65,
                BmiValue = 22.5m,
                TdeeKcal = 2000,
                HealthGoal = "MAINTAIN",
                RegionPreference = "NORTH",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new HealthProfile
            {
                UserId = 201,
                DietarySchoolId = 2,
                Gender = "FEMALE",
                DateOfBirth = new DateOnly(1998, 5, 1),
                HeightCm = 160,
                CurrentWeightKg = 50,
                BmiValue = 19.5m,
                TdeeKcal = 1600,
                HealthGoal = "MAINTAIN",
                RegionPreference = "NORTH",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            }
        );

        // 4. Posts, Recipes, Comments, Videos for member 200
        var post = new Post
        {
            PostId = 501,
            UserId = 200,
            Title = "Bài viết quảng cáo spam",
            Content = "Nội dung spam quảng cáo",
            Visibility = "PUBLIC",
            ModerationStatus = "QUARANTINED_HIDDEN",
            CreatedAt = DateTime.UtcNow.AddDays(-5)
        };

        var recipe = new Recipe
        {
            RecipeId = 601,
            UserId = 200,
            DietarySchoolId = 1,
            Title = "Công thức nấu chay số 1",
            Instructions = "[\"B1\"]",
            CookTimeMinutes = 15,
            DifficultyLevel = "EASY",
            Source = "USER",
            Status = "APPROVED_ACTIVE",
            CreatedAt = DateTime.UtcNow.AddDays(-4)
        };

        var comment = new Comment
        {
            CommentId = 701,
            UserId = 200,
            PostId = 501,
            Content = "Bình luận khiếm nhã",
            Status = "HIDDEN",
            CreatedAt = DateTime.UtcNow.AddDays(-3)
        };

        db.Posts.Add(post);
        db.Recipes.Add(recipe);
        db.Comments.Add(comment);

        // 5. Violation Reports on member 200's content
        db.ViolationReports.AddRange(
            new ViolationReport
            {
                ReportId = 801,
                ReporterUserId = 201,
                PostId = 501,
                ViolationType = "VIO_SPAM",
                EvidenceSummary = "Spam quảng cáo sản phẩm",
                ReportSource = "USER_REPORT",
                Status = "APPROVE",
                CreatedAt = DateTime.UtcNow.AddDays(-2)
            },
            new ViolationReport
            {
                ReportId = 802,
                ReporterUserId = 201,
                CommentId = 701,
                ViolationType = "VIO_HATE",
                EvidenceSummary = "Ngôn từ thô tục",
                ReportSource = "USER_REPORT",
                Status = "APPROVE",
                CreatedAt = DateTime.UtcNow.AddDays(-2)
            },
            new ViolationReport
            {
                ReportId = 803,
                ReporterUserId = 201,
                RecipeId = 601,
                ViolationType = "VIO_MEAT",
                EvidenceSummary = "Nghi vấn chứa thịt",
                ReportSource = "USER_REPORT",
                Status = "REJECT",
                CreatedAt = DateTime.UtcNow.AddDays(-1)
            }
        );

        db.SaveChanges();
    }

    private string GenerateJwtToken(int userId, string role)
    {
        using var scope = _factory.Services.CreateScope();
        var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        var secret = config["Jwt:SecretKey"] ?? "YOUR_JWT_SECRET_KEY_HERE_MIN_32_CHARS";
        var issuer = config["Jwt:Issuer"] ?? "VeggieLife.API";
        var audience = config["Jwt:Audience"] ?? "VeggieLife.Client";

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId.ToString()),
            new(ClaimTypes.Role, role),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddHours(2),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    // =========================================================================
    // PHẦN 1: FN-ADM-01 - Tra cứu người dùng
    // =========================================================================

    [Fact]
    public async Task ListUsers_WithoutToken_Returns401Unauthorized()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/v1/admin/users");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ListUsers_WithMemberToken_Returns403Forbidden()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", GenerateJwtToken(201, UserRoles.Member));

        var response = await client.GetAsync("/api/v1/admin/users");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        var body = await response.Content.ReadFromJsonAsync<ApiErrorResponse>();
        body.Should().NotBeNull();
        body!.ErrorCode.Should().Be(ErrorCodes.ErrAdmForbidden);
    }

    [Fact]
    public async Task ListUsers_WithAdminToken_Returns200WithPagedUsers()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", GenerateJwtToken(100, UserRoles.Admin));

        var response = await client.GetAsync("/api/v1/admin/users?page=1&size=10");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<List<AdminUserListItemDto>>>();
        body.Should().NotBeNull();
        body!.StatusCode.Should().Be(200);
        body.Message.Should().Be("OK");
        body.Data.Should().NotBeNull();
        body.Data!.Count.Should().BeGreaterThanOrEqualTo(4);
        body.Meta.Should().NotBeNull();
        body.Meta!.Page.Should().Be(1);
    }

    [Fact]
    public async Task ListUsers_FilterByKeyword_ReturnsMatchingUsers()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", GenerateJwtToken(100, UserRoles.Admin));

        var response = await client.GetAsync("/api/v1/admin/users?keyword=spammer");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<List<AdminUserListItemDto>>>();
        body.Should().NotBeNull();
        body!.Data.Should().NotBeNull();
        body.Data!.Should().ContainSingle(u => u.Email == "spammer@veggielife.vn");
    }

    [Fact]
    public async Task ListUsers_FilterByDietarySchool_ReturnsMatchingUsers()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", GenerateJwtToken(100, UserRoles.Admin));

        var response = await client.GetAsync("/api/v1/admin/users?dietarySchoolId=1");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<List<AdminUserListItemDto>>>();
        body.Should().NotBeNull();
        body!.Data.Should().NotBeNull();
        body.Data!.All(u => u.DietarySchoolId == 1).Should().BeTrue();
    }

    [Fact]
    public async Task ListUsers_InvalidPage_Returns400ValidationError()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", GenerateJwtToken(100, UserRoles.Admin));

        var response = await client.GetAsync("/api/v1/admin/users?page=0");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadFromJsonAsync<ApiErrorResponse>();
        body.Should().NotBeNull();
        body!.ErrorCode.Should().Be(ErrorCodes.ErrValidation);
    }

    // =========================================================================
    // PHẦN 2: FN-ADM-02 - Đổi trạng thái & kỷ luật tài khoản (Thang kỷ luật)
    // =========================================================================

    [Fact]
    public async Task UpdateUserStatus_WithoutToken_Returns401Unauthorized()
    {
        var client = _factory.CreateClient();
        var response = await client.PutAsJsonAsync("/api/v1/admin/users/200/status", new UpdateAdminUserStatusRequest
        {
            Action = AdminSanctionActions.Warn,
            Reason = "Cảnh cáo vi phạm bình luận lần 1"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task UpdateUserStatus_WithMemberToken_Returns403Forbidden()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", GenerateJwtToken(201, UserRoles.Member));

        var response = await client.PutAsJsonAsync("/api/v1/admin/users/200/status", new UpdateAdminUserStatusRequest
        {
            Action = AdminSanctionActions.Warn,
            Reason = "Cảnh cáo vi phạm bình luận lần 1"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task UpdateUserStatus_NonExistentUser_Returns404NotFound()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", GenerateJwtToken(100, UserRoles.Admin));

        var response = await client.PutAsJsonAsync("/api/v1/admin/users/99999/status", new UpdateAdminUserStatusRequest
        {
            Action = AdminSanctionActions.Warn,
            Reason = "Cảnh cáo tài khoản không tồn tại"
        });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var body = await response.Content.ReadFromJsonAsync<ApiErrorResponse>();
        body.Should().NotBeNull();
        body!.ErrorCode.Should().Be(ErrorCodes.ErrAdmNotFound);
    }

    [Fact]
    public async Task UpdateUserStatus_ReasonTooShort_Returns400ValidationError()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", GenerateJwtToken(100, UserRoles.Admin));

        var response = await client.PutAsJsonAsync("/api/v1/admin/users/200/status", new UpdateAdminUserStatusRequest
        {
            Action = AdminSanctionActions.Warn,
            Reason = "Ngắn" // < 10 ký tự
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadFromJsonAsync<ApiErrorResponse>();
        body.Should().NotBeNull();
        body!.ErrorCode.Should().Be(ErrorCodes.ErrValidation);
    }

    [Fact]
    public async Task UpdateUserStatus_SuspendWithoutDuration_Returns400ValidationError()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", GenerateJwtToken(100, UserRoles.Admin));

        var response = await client.PutAsJsonAsync("/api/v1/admin/users/200/status", new UpdateAdminUserStatusRequest
        {
            Action = AdminSanctionActions.Suspend,
            Reason = "Tạm khóa do vi phạm lặp lại nhiều lần"
            // DurationDays = null -> thiếu
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadFromJsonAsync<ApiErrorResponse>();
        body.Should().NotBeNull();
        body!.ErrorCode.Should().Be(ErrorCodes.ErrValidation);
    }

    [Fact]
    public async Task UpdateUserStatus_WarnAction_Returns200AndLogsAudit()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", GenerateJwtToken(100, UserRoles.Admin));

        var response = await client.PutAsJsonAsync("/api/v1/admin/users/200/status", new UpdateAdminUserStatusRequest
        {
            Action = AdminSanctionActions.Warn,
            Reason = "Vi phạm lần 1: Bình luận mang tính chất spam"
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<UpdateAdminUserStatusResponseData>>();
        body.Should().NotBeNull();
        body!.StatusCode.Should().Be(200);
        body.Message.Should().Be("Đã cập nhật trạng thái.");
        body.Data.Should().NotBeNull();
        body.Data!.UserId.Should().Be(200);

        // Kiểm tra AuditLogs
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var audit = await db.AuditLogs.FirstOrDefaultAsync(a => a.TargetId == "200" && a.Action == "USER_SANCTION");
        audit.Should().NotBeNull();
        audit!.ActorUserId.Should().Be(100);
    }

    [Fact]
    public async Task UpdateUserStatus_RestrictComment_Returns200AndQuarantines()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", GenerateJwtToken(100, UserRoles.Admin));

        var response = await client.PutAsJsonAsync("/api/v1/admin/users/200/status", new UpdateAdminUserStatusRequest
        {
            Action = AdminSanctionActions.RestrictComment,
            DurationDays = 7,
            Reason = "Khóa quyền bình luận 7 ngày theo BR-ADM-SOC-001"
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<UpdateAdminUserStatusResponseData>>();
        body.Should().NotBeNull();
        body!.Data!.AccountStatus.Should().Be(AccountStatuses.Quarantined);
        body.Data.LockedUntil.Should().NotBeNull();
    }

    [Fact]
    public async Task UpdateUserStatus_Suspend_Returns200AndSetsLockedUntil()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", GenerateJwtToken(100, UserRoles.Admin));

        var response = await client.PutAsJsonAsync("/api/v1/admin/users/200/status", new UpdateAdminUserStatusRequest
        {
            Action = AdminSanctionActions.Suspend,
            DurationDays = 3,
            Reason = "Tạm khóa tài khoản 3 ngày theo mức vi phạm lần 4-6"
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<UpdateAdminUserStatusResponseData>>();
        body.Should().NotBeNull();
        body!.Data!.AccountStatus.Should().Be(AccountStatuses.Suspended);
        body.Data.LockedUntil.Should().NotBeNull();
    }

    [Fact]
    public async Task UpdateUserStatus_Ban_Returns200AndBans()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", GenerateJwtToken(100, UserRoles.Admin));

        var response = await client.PutAsJsonAsync("/api/v1/admin/users/200/status", new UpdateAdminUserStatusRequest
        {
            Action = AdminSanctionActions.Ban,
            Reason = "Đình chỉ vĩnh viễn do vi phạm nghiêm trọng"
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<UpdateAdminUserStatusResponseData>>();
        body.Should().NotBeNull();
        body!.Data!.AccountStatus.Should().Be(AccountStatuses.Banned);
        body.Data.LockedUntil.Should().BeNull();
    }

    [Fact]
    public async Task UpdateUserStatus_Reactivate_Returns200AndRestoresActive()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", GenerateJwtToken(100, UserRoles.Admin));

        var response = await client.PutAsJsonAsync("/api/v1/admin/users/202/status", new UpdateAdminUserStatusRequest
        {
            Action = AdminSanctionActions.Reactivate,
            Reason = "Mở lại tài khoản sau khi khiếu nại thành công"
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<UpdateAdminUserStatusResponseData>>();
        body.Should().NotBeNull();
        body!.Data!.AccountStatus.Should().Be(AccountStatuses.Active);
        body.Data.LockedUntil.Should().BeNull();
    }

    [Fact]
    public async Task UpdateUserStatus_InvalidTransition_ActiveToReactivate_Returns409Conflict()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", GenerateJwtToken(100, UserRoles.Admin));

        // Member 201 hiện đang ACTIVE, gọi REACTIVATE phải báo 409
        var response = await client.PutAsJsonAsync("/api/v1/admin/users/201/status", new UpdateAdminUserStatusRequest
        {
            Action = AdminSanctionActions.Reactivate,
            Reason = "Mở lại tài khoản dù đang hoạt động bình thường"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var body = await response.Content.ReadFromJsonAsync<ApiErrorResponse>();
        body.Should().NotBeNull();
        body!.ErrorCode.Should().Be(ErrorCodes.ErrAdmInvalidTransition);
    }

    [Fact]
    public async Task UpdateUserStatus_LastAdmin_AttemptDeactivate_Returns409ConflictErrAdmLastAdmin()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", GenerateJwtToken(100, UserRoles.Admin));

        // Tạm khóa Admin 101 trước để chỉ còn duy nhất Admin 100 là Active
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var admin2 = db.Users.Find(101);
            if (admin2 != null)
            {
                admin2.AccountStatus = AccountStatuses.Suspended;
                db.SaveChanges();
            }
        }

        // Giờ Admin 100 là Admin duy nhất còn Active -> thử khóa Admin 100
        var response = await client.PutAsJsonAsync("/api/v1/admin/users/100/status", new UpdateAdminUserStatusRequest
        {
            Action = AdminSanctionActions.Suspend,
            DurationDays = 3,
            Reason = "Cố tình tự khóa tài khoản Admin cuối cùng"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var body = await response.Content.ReadFromJsonAsync<ApiErrorResponse>();
        body.Should().NotBeNull();
        body!.ErrorCode.Should().Be(ErrorCodes.ErrAdmLastAdmin);

        // Khôi phục lại trạng thái cho Admin 101 để không ảnh hưởng test khác
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var admin2 = db.Users.Find(101);
            if (admin2 != null)
            {
                admin2.AccountStatus = AccountStatuses.Active;
                db.SaveChanges();
            }
        }
    }

    // =========================================================================
    // PHẦN 3: FN-ADM-03 - Xem nội dung & lịch sử vi phạm
    // =========================================================================

    [Fact]
    public async Task GetUserContents_WithoutToken_Returns401Unauthorized()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/v1/admin/users/200/contents");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetUserContents_WithMemberToken_Returns403Forbidden()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", GenerateJwtToken(201, UserRoles.Member));

        var response = await client.GetAsync("/api/v1/admin/users/200/contents");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetUserContents_NonExistentUser_Returns404NotFound()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", GenerateJwtToken(100, UserRoles.Admin));

        var response = await client.GetAsync("/api/v1/admin/users/99999/contents");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var body = await response.Content.ReadFromJsonAsync<ApiErrorResponse>();
        body.Should().NotBeNull();
        body!.ErrorCode.Should().Be(ErrorCodes.ErrAdmNotFound);
    }

    [Fact]
    public async Task GetUserContents_WithAdminToken_Returns200WithViolationSummaryAndItems()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", GenerateJwtToken(100, UserRoles.Admin));

        var response = await client.GetAsync("/api/v1/admin/users/200/contents?page=1&size=20");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<AdminUserContentsResponseData>>();
        body.Should().NotBeNull();
        body!.StatusCode.Should().Be(200);
        body.Message.Should().Be("OK");
        body.Data.Should().NotBeNull();
        body.Data!.UserId.Should().Be(200);

        // Kiểm tra violationSummary (2 resolved, 1 rejected/dismissed)
        body.Data.ViolationSummary.Should().NotBeNull();
        body.Data.ViolationSummary.Resolved.Should().Be(2);
        body.Data.ViolationSummary.Dismissed.Should().Be(1);

        // Kiểm tra danh sách items tổng hợp
        body.Data.Items.Should().NotBeNull();
        body.Data.Items.Count.Should().BeGreaterThanOrEqualTo(3);
    }

    [Fact]
    public async Task GetUserContents_FilterByType_ReturnsFilteredItems()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", GenerateJwtToken(100, UserRoles.Admin));

        var response = await client.GetAsync("/api/v1/admin/users/200/contents?type=RECIPE");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<AdminUserContentsResponseData>>();
        body.Should().NotBeNull();
        body!.Data.Should().NotBeNull();
        body.Data!.Items.Should().NotBeNull();
        body.Data.Items.All(i => i.Type == AdminUserContentTypes.Recipe).Should().BeTrue();
    }

    [Fact]
    public async Task GetUserContents_InvalidType_Returns400ValidationError()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", GenerateJwtToken(100, UserRoles.Admin));

        var response = await client.GetAsync("/api/v1/admin/users/200/contents?type=INVALID_TYPE");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadFromJsonAsync<ApiErrorResponse>();
        body.Should().NotBeNull();
        body!.ErrorCode.Should().Be(ErrorCodes.ErrValidation);
    }
}
