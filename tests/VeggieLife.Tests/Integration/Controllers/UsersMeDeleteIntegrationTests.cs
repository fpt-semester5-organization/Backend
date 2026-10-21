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
using VeggieLife.BLL.DTOs.Auth;
using VeggieLife.BLL.DTOs.Profile.Requests;
using VeggieLife.BLL.DTOs.Profile.Responses;
using VeggieLife.BLL.Services.Cache;
using VeggieLife.BLL.Services.Email;
using VeggieLife.DAL.Context;
using VeggieLife.DAL.Entities;
using VeggieLife.DAL.External.Google;
using Xunit;

namespace VeggieLife.Tests.Integration.Controllers;

/// <summary>
/// Bộ kiểm thử tích hợp (Integration Tests) cho chức năng Vô hiệu hóa tài khoản (ân hạn 30 ngày) (FN-PROF-07).
/// </summary>
public class UsersMeDeleteIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly Mock<IRedisService> _mockRedis = new();
    private readonly Mock<IEmailService> _mockEmail = new();
    private readonly Mock<IGoogleAuthClient> _mockGoogleAuth = new();
    private readonly string _testDbName = "VeggieLifeUsersMeDeleteIntegrationDb_" + Guid.NewGuid();

    private const string PasswordPlainText = "Veggie@2026";
    private const string GoogleIdTokenValid = "valid-google-id-token-202";

    public UsersMeDeleteIntegrationTests(WebApplicationFactory<Program> factory)
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

                var googleDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IGoogleAuthClient));
                if (googleDescriptor != null)
                {
                    services.Remove(googleDescriptor);
                }
                services.AddSingleton(_mockGoogleAuth.Object);

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
        _mockRedis.Setup(r => r.SetAsync(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockEmail.Setup(e => e.SendNotificationEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockGoogleAuth.Setup(g => g.ValidateIdTokenAsync(GoogleIdTokenValid, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GoogleTokenPayload
            {
                Subject = "google-uid-202",
                Email = "google202@veggielife.vn",
                Name = "Google User 202",
                EmailVerified = true
            });

        _mockGoogleAuth.Setup(g => g.ValidateIdTokenAsync("invalid-google-token", It.IsAny<CancellationToken>()))
            .ReturnsAsync((GoogleTokenPayload?)null);
    }

    private static void SeedTestData(AppDbContext db)
    {
        // 1. User 201: Member tài khoản mật khẩu
        var userWithPassword = new User
        {
            UserId = 201,
            Email = "member201@veggielife.vn",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(PasswordPlainText),
            FullName = "Nguyen Member 201",
            Role = UserRoles.Member,
            AccountStatus = AccountStatuses.Active,
            CreatedAt = DateTime.UtcNow
        };

        // 2. User 202: Member tài khoản chỉ liên kết Google (không có mật khẩu)
        var userGoogleOnly = new User
        {
            UserId = 202,
            Email = "google202@veggielife.vn",
            PasswordHash = null,
            GoogleId = "google-uid-202",
            FullName = "Google User 202",
            Role = UserRoles.Member,
            AccountStatus = AccountStatuses.Active,
            CreatedAt = DateTime.UtcNow
        };

        // 3. User 203: Guest
        var guestUser = new User
        {
            UserId = 203,
            Email = "guest203@veggielife.vn",
            FullName = "Khách Vãng Lai 203",
            Role = UserRoles.Guest,
            AccountStatus = AccountStatuses.Active,
            CreatedAt = DateTime.UtcNow
        };

        db.Users.AddRange(userWithPassword, userGoogleOnly, guestUser);
        db.SaveChanges();
    }

    private string GenerateJwtToken(int userId, string role, string email)
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
                new Claim(ClaimTypes.Email, email),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            }),
            Expires = DateTime.UtcNow.AddHours(2),
            Issuer = issuer,
            Audience = audience,
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }

    private HttpClient CreateClientForUser(int userId, string role, string email)
    {
        var client = _factory.CreateClient();
        var token = GenerateJwtToken(userId, role, email);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    [Fact]
    public async Task DeleteCurrentUser_WithoutToken_Returns401Unauthorized()
    {
        var client = _factory.CreateClient();
        var req = new DeleteCurrentUserRequest
        {
            ConfirmPassword = PasswordPlainText,
            Reason = "Tạm ngừng dùng"
        };

        var response = await client.SendAsync(new HttpRequestMessage(HttpMethod.Delete, "/api/v1/users/me")
        {
            Content = JsonContent.Create(req)
        });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain(ErrorCodes.ErrAuthInvalidToken);
    }

    [Fact]
    public async Task DeleteCurrentUser_WithGuestToken_Returns403Forbidden()
    {
        var client = CreateClientForUser(203, UserRoles.Guest, "guest203@veggielife.vn");
        var req = new DeleteCurrentUserRequest
        {
            Reason = "Khách thử xóa"
        };

        var response = await client.SendAsync(new HttpRequestMessage(HttpMethod.Delete, "/api/v1/users/me")
        {
            Content = JsonContent.Create(req)
        });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain(ErrorCodes.ErrGuestPersonalizationRestricted);
    }

    [Fact]
    public async Task DeleteCurrentUser_ReasonTooLong_Returns400BadRequest()
    {
        var client = CreateClientForUser(201, UserRoles.Member, "member201@veggielife.vn");
        var req = new DeleteCurrentUserRequest
        {
            ConfirmPassword = PasswordPlainText,
            Reason = new string('A', 256) // > 255 chars
        };

        var response = await client.SendAsync(new HttpRequestMessage(HttpMethod.Delete, "/api/v1/users/me")
        {
            Content = JsonContent.Create(req)
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain(ErrorCodes.ErrValidation);
    }

    [Fact]
    public async Task DeleteCurrentUser_PasswordAccount_MissingPassword_Returns400BadRequest()
    {
        var client = CreateClientForUser(201, UserRoles.Member, "member201@veggielife.vn");
        var req = new DeleteCurrentUserRequest
        {
            ConfirmPassword = null, // Thiếu mật khẩu
            Reason = "Lý do hợp lệ"
        };

        var response = await client.SendAsync(new HttpRequestMessage(HttpMethod.Delete, "/api/v1/users/me")
        {
            Content = JsonContent.Create(req)
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain(ErrorCodes.ErrValidation);
    }

    [Fact]
    public async Task DeleteCurrentUser_PasswordAccount_WrongPassword_Returns401Unauthorized()
    {
        var client = CreateClientForUser(201, UserRoles.Member, "member201@veggielife.vn");
        var req = new DeleteCurrentUserRequest
        {
            ConfirmPassword = "WrongPassword@999",
            Reason = "Lý do hợp lệ"
        };

        var response = await client.SendAsync(new HttpRequestMessage(HttpMethod.Delete, "/api/v1/users/me")
        {
            Content = JsonContent.Create(req)
        });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain(ErrorCodes.ErrAuthInvalidCredentials);
    }

    [Fact]
    public async Task DeleteCurrentUser_PasswordAccount_CorrectPassword_Returns200AndSoftDeletes()
    {
        var client = CreateClientForUser(201, UserRoles.Member, "member201@veggielife.vn");
        var req = new DeleteCurrentUserRequest
        {
            ConfirmPassword = PasswordPlainText,
            Reason = "Tạm ngừng sử dụng tài khoản"
        };

        var response = await client.SendAsync(new HttpRequestMessage(HttpMethod.Delete, "/api/v1/users/me")
        {
            Content = JsonContent.Create(req)
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<DeleteCurrentUserResponseData>>();
        body.Should().NotBeNull();
        body!.StatusCode.Should().Be(200);
        body.Message.Should().Be("Tài khoản đã được vô hiệu hóa. Bạn có 30 ngày để khôi phục.");
        body.Data.Should().NotBeNull();
        body.Data.RestoreDeadline.Should().BeAfter(DateTime.UtcNow.AddDays(29));

        // 1. Kiểm tra CSDL
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var userInDb = await db.Users.IgnoreQueryFilters().FirstAsync(u => u.UserId == 201);
        userInDb.AccountStatus.Should().Be(AccountStatuses.SoftDeleted);

        // 2. Kiểm tra AuditLogs
        var audit = await db.AuditLogs.FirstOrDefaultAsync(a => a.TargetTable == "Users" && a.TargetId == "201" && a.Action == "ACCOUNT_SOFT_DELETED");
        audit.Should().NotBeNull();
        audit!.ActorRole.Should().Be(UserRoles.Member);
        audit.Note.Should().Be("Tạm ngừng sử dụng tài khoản");

        // 3. Kiểm tra Redis thu hồi phiên & blacklist
        _mockRedis.Verify(r => r.RemoveAsync("user_sessions:201", It.IsAny<CancellationToken>()), Times.AtLeastOnce());

        // 4. Kiểm tra gửi Email thông báo kèm hạn chót 30 ngày
        _mockEmail.Verify(e => e.SendNotificationEmailAsync(
            "member201@veggielife.vn",
            It.Is<string>(s => s.Contains("vô hiệu hóa")),
            It.Is<string>(b => b.Contains("30 ngày")),
            It.IsAny<CancellationToken>()), Times.AtLeastOnce());
    }

    [Fact]
    public async Task DeleteCurrentUser_GoogleOnlyAccount_MissingGoogleToken_Returns400BadRequest()
    {
        var client = CreateClientForUser(202, UserRoles.Member, "google202@veggielife.vn");
        var req = new DeleteCurrentUserRequest
        {
            GoogleIdToken = null, // Thiếu Google ID Token
            Reason = "Lý do hợp lệ"
        };

        var response = await client.SendAsync(new HttpRequestMessage(HttpMethod.Delete, "/api/v1/users/me")
        {
            Content = JsonContent.Create(req)
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain(ErrorCodes.ErrValidation);
    }

    [Fact]
    public async Task DeleteCurrentUser_GoogleOnlyAccount_InvalidGoogleToken_Returns401Unauthorized()
    {
        var client = CreateClientForUser(202, UserRoles.Member, "google202@veggielife.vn");
        var req = new DeleteCurrentUserRequest
        {
            GoogleIdToken = "invalid-google-token",
            Reason = "Lý do hợp lệ"
        };

        var response = await client.SendAsync(new HttpRequestMessage(HttpMethod.Delete, "/api/v1/users/me")
        {
            Content = JsonContent.Create(req)
        });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain(ErrorCodes.ErrAuthInvalidCredentials);
    }

    [Fact]
    public async Task DeleteCurrentUser_GoogleOnlyAccount_ValidGoogleToken_Returns200AndSoftDeletes()
    {
        var client = CreateClientForUser(202, UserRoles.Member, "google202@veggielife.vn");
        var req = new DeleteCurrentUserRequest
        {
            GoogleIdToken = GoogleIdTokenValid,
            Reason = "Google user xóa tài khoản"
        };

        var response = await client.SendAsync(new HttpRequestMessage(HttpMethod.Delete, "/api/v1/users/me")
        {
            Content = JsonContent.Create(req)
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<DeleteCurrentUserResponseData>>();
        body.Should().NotBeNull();
        body!.StatusCode.Should().Be(200);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var userInDb = await db.Users.IgnoreQueryFilters().FirstAsync(u => u.UserId == 202);
        userInDb.AccountStatus.Should().Be(AccountStatuses.SoftDeleted);
    }

    [Fact]
    public async Task DeleteCurrentUser_RestorationOnLogin_WhenLoginWithin30DaysWithRestoreFlag_RestoresToActive()
    {
        // ARRANGE: Tạo tài khoản 204 và xóa mềm (còn trong 30 ngày)
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var u204 = new User
            {
                UserId = 204,
                Email = "restore204@veggielife.vn",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(PasswordPlainText),
                FullName = "Restore User 204",
                Role = UserRoles.Member,
                AccountStatus = AccountStatuses.SoftDeleted,
                CreatedAt = DateTime.UtcNow.AddDays(-5),
                UpdatedAt = DateTime.UtcNow.AddDays(-2)
            };
            db.Users.Add(u204);
            await db.SaveChangesAsync();
        }

        var client = _factory.CreateClient();

        // 1. Thử đăng nhập không kèm RestoreAccount -> 409 Conflict
        var loginReqWithoutRestore = new LoginRequest
        {
            Email = "restore204@veggielife.vn",
            Password = PasswordPlainText,
            RestoreAccount = false
        };
        var res1 = await client.PostAsJsonAsync("/api/v1/auth/login", loginReqWithoutRestore);
        res1.StatusCode.Should().Be(HttpStatusCode.Conflict);

        // 2. Đăng nhập kèm RestoreAccount = true -> 200 OK và phục hồi ACTIVE
        var loginReqWithRestore = new LoginRequest
        {
            Email = "restore204@veggielife.vn",
            Password = PasswordPlainText,
            RestoreAccount = true
        };
        var res2 = await client.PostAsJsonAsync("/api/v1/auth/login", loginReqWithRestore);
        res2.StatusCode.Should().Be(HttpStatusCode.OK);

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var userAfterLogin = await db.Users.FirstAsync(u => u.UserId == 204);
            userAfterLogin.AccountStatus.Should().Be(AccountStatuses.Active);
        }
    }
}
