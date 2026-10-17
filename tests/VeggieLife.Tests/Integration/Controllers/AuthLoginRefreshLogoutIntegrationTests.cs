using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using VeggieLife.BLL.Common.Constants;
using VeggieLife.BLL.Common.Responses;
using VeggieLife.BLL.DTOs.Auth;
using VeggieLife.BLL.Services.Auth;
using VeggieLife.BLL.Services.Cache;
using VeggieLife.DAL.Context;
using VeggieLife.DAL.Entities;
using Xunit;

namespace VeggieLife.Tests.Integration.Controllers;

public class AuthLoginRefreshLogoutIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private class TestRedisService : IRedisService
    {
        private readonly ConcurrentDictionary<string, (object Value, DateTime ExpireAt)> _store = new();

        public Task<T?> GetAsync<T>(string key, CancellationToken ct = default)
        {
            if (_store.TryGetValue(key, out var entry))
            {
                if (DateTime.UtcNow <= entry.ExpireAt)
                {
                    if (entry.Value is T direct) return Task.FromResult<T?>(direct);
                    var json = JsonSerializer.Serialize(entry.Value);
                    return Task.FromResult(JsonSerializer.Deserialize<T>(json));
                }
                _store.TryRemove(key, out _);
            }
            return Task.FromResult<T?>(default);
        }

        public Task SetAsync<T>(string key, T value, TimeSpan? expiry = null, CancellationToken ct = default)
        {
            var expireAt = DateTime.UtcNow.Add(expiry ?? TimeSpan.FromDays(7));
            _store[key] = (value!, expireAt);
            return Task.CompletedTask;
        }

        public Task RemoveAsync(string key, CancellationToken ct = default)
        {
            _store.TryRemove(key, out _);
            return Task.CompletedTask;
        }

        public Task<bool> ExistsAsync(string key, CancellationToken ct = default)
        {
            if (_store.TryGetValue(key, out var entry))
            {
                if (DateTime.UtcNow <= entry.ExpireAt)
                {
                    return Task.FromResult(true);
                }
                _store.TryRemove(key, out _);
            }
            return Task.FromResult(false);
        }

        public Task<long> IncrementAsync(string key, long value = 1, TimeSpan? expiry = null, CancellationToken ct = default)
        {
            return Task.FromResult(1L);
        }

        public Task<(bool Allowed, long Current, long Remaining)> TryConsumeQuotaAsync(string key, int maxLimit, TimeSpan? expiry = null, CancellationToken ct = default)
        {
            return Task.FromResult((true, 1L, 2L));
        }

        public Task<(bool Allowed, bool IsBanned, long CurrentCount, TimeSpan? RetryAfter)> CheckIpRateLimitAsync(string clientIp, int maxRequestsPerMinute = 10, int banDurationMinutes = 10, CancellationToken ct = default)
        {
            return Task.FromResult((true, false, 1L, (TimeSpan?)null));
        }

        public Task EnqueueAsync<T>(string queueName, T item, CancellationToken ct = default)
        {
            return Task.CompletedTask;
        }
    }

    private readonly WebApplicationFactory<Program> _factory;
    private readonly TestRedisService _redis = new();
    private readonly string _dbName = "AuthIntegrationDb_" + Guid.NewGuid();
    private const string JwtSecret = "VeggieLifeSecretKeyForDevelopmentMustBe32CharsLong!";

    public AuthLoginRefreshLogoutIntegrationTests(WebApplicationFactory<Program> factory)
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
                var dbDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
                if (dbDescriptor != null) services.Remove(dbDescriptor);

                services.AddDbContext<AppDbContext>(options =>
                {
                    options.UseInMemoryDatabase(_dbName);
                    options.ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning));
                });

                var redisDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IRedisService));
                if (redisDescriptor != null) services.Remove(redisDescriptor);

                services.AddSingleton<IRedisService>(_redis);
            });
        });
    }

    private async Task SeedUserAsync(string email, string password, string role = UserRoles.Member, string status = AccountStatuses.Active)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var existing = await db.Users.FirstOrDefaultAsync(u => u.Email == email);
        if (existing == null)
        {
            var user = new User
            {
                Email = email,
                FullName = "Người Dùng Test",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(password, workFactor: 4),
                Role = role,
                AccountStatus = status,
                CreatedAt = DateTime.UtcNow
            };
            db.Users.Add(user);
            await db.SaveChangesAsync();
        }
    }

    [Fact]
    public async Task PostLogin_WithValidCredentials_Returns200_AndSetsHttpOnlyCookie()
    {
        var client = _factory.CreateClient();
        await SeedUserAsync("login.success@veggielife.vn", "Veggie@2026");

        var response = await client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest
        {
            Email = "login.success@veggielife.vn",
            Password = "Veggie@2026"
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<ApiResponse<LoginResponse>>();
        Assert.NotNull(body);
        Assert.Equal(200, body.StatusCode);
        Assert.Equal("Đăng nhập thành công.", body.Message);
        Assert.NotNull(body.Data);
        Assert.Equal("Bearer", body.Data.TokenType);
        Assert.NotEmpty(body.Data.AccessToken);
        Assert.Equal(900, body.Data.ExpiresIn);
        Assert.Equal("login.success@veggielife.vn", body.Data.User.Email);

        // Kiểm tra cookie refresh_token
        var cookies = response.Headers.GetValues("Set-Cookie").ToList();
        var refreshCookie = cookies.FirstOrDefault(c => c.Contains("refresh_token="));
        Assert.NotNull(refreshCookie);
        Assert.Contains("httponly", refreshCookie.ToLowerInvariant());
    }

    [Fact]
    public async Task PostLogin_WithInvalidModel_Returns400_ErrValidation()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/v1/auth/login", new
        {
            Email = "not-an-email",
            Password = "short" // < 8 ký tự
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ApiErrorResponse>();
        Assert.NotNull(body);
        Assert.Equal(ErrorCodes.ErrValidation, body.ErrorCode);
    }

    [Fact]
    public async Task PostLogin_WrongPassword_Returns401_ErrAuthInvalidCredentials()
    {
        var client = _factory.CreateClient();
        await SeedUserAsync("wrongpass.api@veggielife.vn", "Veggie@2026");

        var response = await client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest
        {
            Email = "wrongpass.api@veggielife.vn",
            Password = "WrongPassword@2026"
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ApiErrorResponse>();
        Assert.NotNull(body);
        Assert.Equal(ErrorCodes.ErrAuthInvalidCredentials, body.ErrorCode);
    }

    [Fact]
    public async Task PostRefreshToken_AndPostLogout_EndToEndFlow()
    {
        var client = _factory.CreateClient();
        await SeedUserAsync("e2e.flow@veggielife.vn", "Veggie@2026");

        // 1. Đăng nhập
        var loginResponse = await client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest
        {
            Email = "e2e.flow@veggielife.vn",
            Password = "Veggie@2026"
        });
        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);

        var loginBody = await loginResponse.Content.ReadFromJsonAsync<ApiResponse<LoginResponse>>();
        var accessToken = loginBody!.Data!.AccessToken;

        // Trích xuất Cookie refresh_token từ header
        var setCookieHeader = loginResponse.Headers.GetValues("Set-Cookie").FirstOrDefault(c => c.Contains("refresh_token="));
        Assert.NotNull(setCookieHeader);
        var refreshTokenValue = setCookieHeader.Split(';')[0].Replace("refresh_token=", "");

        // 2. Làm mới phiên (Refresh Token)
        var refreshReq = new HttpRequestMessage(HttpMethod.Post, "/api/v1/auth/refresh-token");
        refreshReq.Headers.Add("Cookie", $"refresh_token={refreshTokenValue}");

        var refreshResponse = await client.SendAsync(refreshReq);
        Assert.Equal(HttpStatusCode.OK, refreshResponse.StatusCode);

        var refreshBody = await refreshResponse.Content.ReadFromJsonAsync<ApiResponse<RefreshTokenResponse>>();
        Assert.NotNull(refreshBody);
        var newAccessToken = refreshBody.Data!.AccessToken;
        Assert.NotEmpty(newAccessToken);

        // Lấy refresh_token xoay vòng mới
        var newSetCookie = refreshResponse.Headers.GetValues("Set-Cookie").FirstOrDefault(c => c.Contains("refresh_token="));
        Assert.NotNull(newSetCookie);
        var newRefreshTokenValue = newSetCookie.Split(';')[0].Replace("refresh_token=", "");

        // 3. Đăng xuất bằng Bearer Token mới
        var logoutReq = new HttpRequestMessage(HttpMethod.Post, "/api/v1/auth/logout");
        logoutReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", newAccessToken);
        logoutReq.Headers.Add("Cookie", $"refresh_token={newRefreshTokenValue}");

        var logoutResponse = await client.SendAsync(logoutReq);
        Assert.Equal(HttpStatusCode.OK, logoutResponse.StatusCode);

        // 4. Gọi lại bằng token vừa đăng xuất -> Bị chặn 401 vì đã nằm trong blacklist
        var protectedReq = new HttpRequestMessage(HttpMethod.Post, "/api/v1/auth/logout");
        protectedReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", newAccessToken);

        var blockedResponse = await client.SendAsync(protectedReq);
        Assert.Equal(HttpStatusCode.Unauthorized, blockedResponse.StatusCode);
    }

    [Fact]
    public async Task PostLogout_WithAnonymousGuestToken_Returns403()
    {
        var client = _factory.CreateClient();

        // 1. Tạo phiên khách vãng lai
        var anonRes = await client.PostAsync("/api/v1/auth/anonymous", null);
        Assert.Equal(HttpStatusCode.OK, anonRes.StatusCode);

        var anonBody = await anonRes.Content.ReadFromJsonAsync<ApiResponse<VeggieLife.BLL.DTOs.Guest.Responses.CreateAnonymousSessionResponse>>();
        var guestJwt = anonBody!.Data!.AccessToken;

        // 2. Cố gắng gọi endpoint logout (chỉ dành cho MEMBER, VIP, ADMIN) bằng Guest Token
        var logoutReq = new HttpRequestMessage(HttpMethod.Post, "/api/v1/auth/logout");
        logoutReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", guestJwt);

        var response = await client.SendAsync(logoutReq);
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);

        var errBody = await response.Content.ReadFromJsonAsync<ApiErrorResponse>();
        Assert.NotNull(errBody);
        Assert.StartsWith("ERR_GUEST_", errBody.ErrorCode);
    }
}
