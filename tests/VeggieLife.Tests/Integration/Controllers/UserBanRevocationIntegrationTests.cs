using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Moq;
using VeggieLife.BLL.Common.Constants;
using VeggieLife.BLL.Common.Responses;
using VeggieLife.BLL.DTOs.Admin.Users;
using VeggieLife.BLL.DTOs.Auth;
using VeggieLife.BLL.Services.Cache;
using VeggieLife.BLL.Services.Email;
using VeggieLife.BLL.Services.Realtime;
using VeggieLife.DAL.Context;
using VeggieLife.DAL.Entities;
using Xunit;

namespace VeggieLife.Tests.Integration.Controllers;

/// <summary>
/// Kiểm thử tích hợp toàn diện cho cơ chế:
/// [BE - Bảo mật] Thu hồi JWT tức thì khi Ban user (Redis Blacklist) & WebSocket/SignalR xử lý realtime
/// Phụ trách: Nguyễn Thị Hồng Anh
/// </summary>
public class UserBanRevocationIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private class TestRedisService : IRedisService
    {
        public readonly ConcurrentDictionary<string, (object Value, DateTime ExpireAt)> Store = new();

        public Task<T?> GetAsync<T>(string key, CancellationToken ct = default)
        {
            if (Store.TryGetValue(key, out var entry) && DateTime.UtcNow <= entry.ExpireAt)
            {
                if (entry.Value is T direct) return Task.FromResult<T?>(direct);
                var json = System.Text.Json.JsonSerializer.Serialize(entry.Value);
                return Task.FromResult(System.Text.Json.JsonSerializer.Deserialize<T>(json));
            }
            return Task.FromResult<T?>(default);
        }

        public Task SetAsync<T>(string key, T value, TimeSpan? expiry = null, CancellationToken ct = default)
        {
            var exp = DateTime.UtcNow.Add(expiry ?? TimeSpan.FromDays(30));
            Store[key] = (value!, exp);
            return Task.CompletedTask;
        }

        public Task RemoveAsync(string key, CancellationToken ct = default)
        {
            Store.TryRemove(key, out _);
            return Task.CompletedTask;
        }

        public Task<bool> ExistsAsync(string key, CancellationToken ct = default)
        {
            if (Store.TryGetValue(key, out var entry) && DateTime.UtcNow <= entry.ExpireAt)
            {
                return Task.FromResult(true);
            }
            return Task.FromResult(false);
        }

        public Task<long> IncrementAsync(string key, long value = 1, TimeSpan? expiry = null, CancellationToken ct = default)
            => Task.FromResult(1L);

        public Task<(bool Allowed, long Current, long Remaining)> TryConsumeQuotaAsync(string key, int maxLimit, TimeSpan? expiry = null, CancellationToken ct = default)
            => Task.FromResult((true, 1L, (long)maxLimit - 1));

        public Task<(bool Allowed, bool IsBanned, long CurrentCount, TimeSpan? RetryAfter)> CheckIpRateLimitAsync(string clientIp, int maxRequestsPerMinute = 10, int banDurationMinutes = 10, CancellationToken ct = default)
            => Task.FromResult((true, false, 1L, (TimeSpan?)null));
    }

    private readonly WebApplicationFactory<Program> _factory;
    private readonly TestRedisService _testRedis = new();
    private readonly Mock<IUserRealtimeNotifier> _mockRealtimeNotifier = new();
    private readonly Mock<IEmailService> _mockEmail = new();
    private readonly string _testDbName = "VeggieLifeBanRevocationDb_" + Guid.NewGuid();
    private const string JwtSecret = "VeggieLifeSecretKeyForDevelopmentMustBe32CharsLong!";

    public UserBanRevocationIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Development");
            builder.ConfigureAppConfiguration((ctx, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Jwt:SecretKey"] = JwtSecret,
                    ["Jwt:Issuer"] = "VeggieLife.API",
                    ["Jwt:Audience"] = "VeggieLife.Client",
                    ["Jwt:AccessTokenExpiryMinutes"] = "15"
                });
            });

            builder.ConfigureServices(services =>
            {
                var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
                if (descriptor != null) services.Remove(descriptor);

                services.AddDbContext<AppDbContext>(options =>
                {
                    options.UseInMemoryDatabase(_testDbName);
                    options.ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning));
                });

                var redisDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IRedisService));
                if (redisDescriptor != null) services.Remove(redisDescriptor);
                services.AddSingleton<IRedisService>(_testRedis);

                var realtimeDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IUserRealtimeNotifier));
                if (realtimeDescriptor != null) services.Remove(realtimeDescriptor);
                services.AddSingleton(_mockRealtimeNotifier.Object);

                var emailDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IEmailService));
                if (emailDescriptor != null) services.Remove(emailDescriptor);
                services.AddSingleton(_mockEmail.Object);
            });
        });

        SeedTestData();
    }

    private void SeedTestData()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // Admin User (ID 100)
        var admin = new User
        {
            UserId = 100,
            Email = "admin@veggielife.vn",
            FullName = "Admin Quản Trị",
            Role = UserRoles.Admin,
            AccountStatus = AccountStatuses.Active,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin@123456"),
            CreatedAt = DateTime.UtcNow
        };

        // Member User (ID 300)
        var member = new User
        {
            UserId = 300,
            Email = "member.ban.test@veggielife.vn",
            FullName = "Nguyễn Văn Vi Phạm",
            Role = UserRoles.Member,
            AccountStatus = AccountStatuses.Active,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Member@123456"),
            CreatedAt = DateTime.UtcNow
        };

        // Banned User (ID 301)
        var bannedUser = new User
        {
            UserId = 301,
            Email = "already.banned@veggielife.vn",
            FullName = "Lê Đã Bị Cấm",
            Role = UserRoles.Member,
            AccountStatus = AccountStatuses.Banned,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Banned@123456"),
            CreatedAt = DateTime.UtcNow
        };

        db.Users.AddRange(admin, member, bannedUser);
        db.SaveChanges();
    }

    private static string GenerateJwt(long userId, string role)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(JwtSecret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var jti = Guid.NewGuid().ToString("N");

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId.ToString()),
            new(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new(ClaimTypes.Role, role),
            new("role", role),
            new(JwtRegisteredClaimNames.Jti, jti)
        };

        var token = new JwtSecurityToken(
            issuer: "VeggieLife.API",
            audience: "VeggieLife.Client",
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(15),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    [Fact]
    public async Task WhenUserIsBlacklisted_SubsequentRequest_Returns403ForbiddenWithErrAuthAccountBanned()
    {
        // ARRANGE: Tạo token hợp lệ cho Member 300
        var client = _factory.CreateClient();
        var memberToken = GenerateJwt(300, UserRoles.Member);

        // Giả lập user 300 bị đưa vào blacklist Redis
        await _testRedis.SetAsync("user_blacklist:300", true, TimeSpan.FromDays(30));

        // ACT: Gửi request tới một endpoint yêu cầu xác thực
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/auth/logout");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", memberToken);

        var response = await client.SendAsync(request);

        // ASSERT: Phải chặn ngay lập tức với 403 Forbidden
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        var errorBody = await response.Content.ReadFromJsonAsync<ApiErrorResponse>();
        errorBody.Should().NotBeNull();
        errorBody!.StatusCode.Should().Be(StatusCodes.Status403Forbidden);
        errorBody.ErrorCode.Should().Be(ErrorCodes.ErrAuthAccountBanned);
    }

    [Fact]
    public async Task AdminBansUser_SetsRedisBlacklist_And_TriggersRealtimeForceLogout()
    {
        // ARRANGE
        var client = _factory.CreateClient();
        var adminToken = GenerateJwt(100, UserRoles.Admin);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

        var memberJwt = GenerateJwt(300, UserRoles.Member);

        // ACT 1: Admin thực hiện cấm tài khoản 300
        var banRequest = new UpdateAdminUserStatusRequest
        {
            Action = AdminSanctionActions.Ban,
            Reason = "Vi phạm nhiều lần tiêu chuẩn cộng đồng theo yêu cầu ban quản trị"
        };

        var banResponse = await client.PutAsJsonAsync("/api/v1/admin/users/300/status", banRequest);
        banResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // ASSERT 1: Kiểm tra Redis đã lưu key user_blacklist:300
        var isBlacklisted = await _testRedis.ExistsAsync("user_blacklist:300");
        isBlacklisted.Should().BeTrue();

        // ASSERT 2: Kiểm tra SignalR realtime notifier đã được gọi với event ForceLogout
        _mockRealtimeNotifier.Verify(n => n.NotifyForceLogoutAsync(
            300,
            It.Is<string>(r => r.Contains("Vi phạm")),
            AdminSanctionActions.Ban,
            It.IsAny<CancellationToken>()), Times.Once);

        // ACT 2: Ngay sau khi bị cấm, user 300 gửi request bằng Access Token cũ
        var memberClient = _factory.CreateClient();
        var followUpReq = new HttpRequestMessage(HttpMethod.Post, "/api/v1/auth/logout");
        followUpReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", memberJwt);

        var followUpRes = await memberClient.SendAsync(followUpReq);

        // ASSERT 3: Request ngay tiếp theo bị chặn với 403 Forbidden
        followUpRes.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        var err = await followUpRes.Content.ReadFromJsonAsync<ApiErrorResponse>();
        err.Should().NotBeNull();
        err!.ErrorCode.Should().Be(ErrorCodes.ErrAuthAccountBanned);
    }

    [Fact]
    public async Task BannedUser_AttemptLogin_IsBlockedWith403Forbidden()
    {
        // ARRANGE
        var client = _factory.CreateClient();
        var loginReq = new LoginRequest
        {
            Email = "already.banned@veggielife.vn",
            Password = "Banned@123456"
        };

        // ACT: Đăng nhập bằng tài khoản BANNED
        var response = await client.PostAsJsonAsync("/api/v1/auth/login", loginReq);

        // ASSERT: Bị chặn ngay từ API Login với 403 Forbidden và thông báo rõ ràng
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        var errorBody = await response.Content.ReadFromJsonAsync<ApiErrorResponse>();
        errorBody.Should().NotBeNull();
        errorBody!.StatusCode.Should().Be(StatusCodes.Status403Forbidden);
        errorBody.ErrorCode.Should().Be(ErrorCodes.ErrAuthAccountBanned);
        errorBody.Message.Should().Contain("bị đình chỉ hoặc cấm");
    }
}
