using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using VeggieLife.BLL.Common.Constants;
using VeggieLife.BLL.Common.Exceptions;
using VeggieLife.BLL.DTOs.Auth;
using VeggieLife.BLL.Services.Auth;
using VeggieLife.BLL.Services.Cache;
using VeggieLife.BLL.Services.Email;
using VeggieLife.DAL.Context;
using VeggieLife.DAL.Entities;
using Xunit;

namespace VeggieLife.Tests.Unit.Services;

/// <summary>
/// Bộ kiểm thử đơn vị cho chức năng Đăng nhập, Làm mới Token và Đăng xuất (FN-AUTH-08, FN-AUTH-09, FN-AUTH-10).
/// </summary>
public class AuthLoginRefreshLogoutTests
{
    private class InMemoryRedisService : IRedisService
    {
        public readonly ConcurrentDictionary<string, (object Value, DateTime ExpireAt)> Storage = new();

        public Task<T?> GetAsync<T>(string key, CancellationToken ct = default)
        {
            if (Storage.TryGetValue(key, out var entry))
            {
                if (DateTime.UtcNow <= entry.ExpireAt)
                {
                    return Task.FromResult((T?)entry.Value);
                }
                Storage.TryRemove(key, out _);
            }
            return Task.FromResult<T?>(default);
        }

        public Task SetAsync<T>(string key, T value, TimeSpan? expiry = null, CancellationToken ct = default)
        {
            var expireAt = DateTime.UtcNow.Add(expiry ?? TimeSpan.FromDays(1));
            Storage[key] = (value!, expireAt);
            return Task.CompletedTask;
        }

        public Task RemoveAsync(string key, CancellationToken ct = default)
        {
            Storage.TryRemove(key, out _);
            return Task.CompletedTask;
        }

        public Task<bool> ExistsAsync(string key, CancellationToken ct = default)
        {
            if (Storage.TryGetValue(key, out var entry))
            {
                if (DateTime.UtcNow <= entry.ExpireAt)
                {
                    return Task.FromResult(true);
                }
                Storage.TryRemove(key, out _);
            }
            return Task.FromResult(false);
        }

        public Task<long> IncrementAsync(string key, long value = 1, TimeSpan? expiry = null, CancellationToken ct = default)
        {
            long current = 0;
            if (Storage.TryGetValue(key, out var entry))
            {
                if (entry.Value is long l) current = l;
                else if (long.TryParse(entry.Value.ToString(), out var parsed)) current = parsed;
            }
            current += value;
            var expireAt = DateTime.UtcNow.Add(expiry ?? TimeSpan.FromMinutes(5));
            Storage[key] = (current, expireAt);
            return Task.FromResult(current);
        }

        public Task<(bool Allowed, long Current, long Remaining)> TryConsumeQuotaAsync(string key, int maxLimit, TimeSpan? expiry = null, CancellationToken ct = default)
        {
            return Task.FromResult((true, 1L, (long)(maxLimit - 1)));
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

    private static (AppDbContext DbContext, AuthService AuthService, InMemoryRedisService RedisService, IJwtTokenService JwtTokenService) CreateTestHarness(IRedisService? customRedis = null)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        var dbContext = new AppDbContext(options);
        var redisService = new InMemoryRedisService();
        var emailService = new Mock<IEmailService>().Object;

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:SecretKey"] = "SuperSecretKeyForTestingAtLeast32CharsLength!",
                ["Jwt:Issuer"] = "VeggieLife.API",
                ["Jwt:Audience"] = "VeggieLife.Client",
                ["Jwt:AccessTokenExpiryMinutes"] = "15"
            })
            .Build();

        var jwtTokenService = new JwtTokenService(config);
        var authService = new AuthService(
            dbContext,
            customRedis ?? redisService,
            emailService,
            jwtTokenService,
            NullLogger<AuthService>.Instance);

        return (dbContext, authService, redisService, jwtTokenService);
    }

    [Fact]
    public async Task Login_WithValidCredentials_Returns200_AndResetsFailedCount()
    {
        var (db, service, redis, _) = CreateTestHarness();

        var user = new User
        {
            Email = "member@veggielife.vn",
            FullName = "Nguyễn Thành Viên",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("ValidPassword@2026", workFactor: 4),
            Role = UserRoles.Member,
            AccountStatus = AccountStatuses.Active,
            FailedLoginCount = 2,
            CreatedAt = DateTime.UtcNow
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var response = await service.LoginAsync(new LoginRequest
        {
            Email = "member@veggielife.vn",
            Password = "ValidPassword@2026"
        }, clientIp: "127.0.0.1");

        Assert.NotNull(response);
        Assert.Equal("Bearer", response.TokenType);
        Assert.NotEmpty(response.AccessToken);
        Assert.NotEmpty(response.RefreshToken);
        Assert.Equal(900, response.ExpiresIn);
        Assert.Equal("member@veggielife.vn", response.User.Email);
        Assert.Equal(UserRoles.Member, response.User.Role);
        Assert.False(response.User.IsVip);

        // Kiểm tra User trong CSDL: FailedLoginCount reset về 0
        var updatedUser = await db.Users.FindAsync(user.UserId);
        Assert.NotNull(updatedUser);
        Assert.Equal(0, updatedUser.FailedLoginCount);
        Assert.Null(updatedUser.LockedUntil);
        Assert.NotNull(updatedUser.LastLoginAt);

        // Kiểm tra AuditLog được ghi nhận
        var audit = await db.AuditLogs.FirstOrDefaultAsync(a => a.ActorUserId == user.UserId && a.Action == "LOGIN");
        Assert.NotNull(audit);

        // Kiểm tra Refresh Token lưu trên Redis
        var userSessions = await redis.GetAsync<List<string>>($"user_sessions:{user.UserId}");
        Assert.NotNull(userSessions);
        Assert.Single(userSessions);
    }

    [Fact]
    public async Task Login_WithWrongPassword_IncrementsFailedCount_AndReturnsRemainingAttempts()
    {
        var (db, service, _, _) = CreateTestHarness();

        var user = new User
        {
            Email = "wrongpass@veggielife.vn",
            FullName = "Người Dùng Thử",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("CorrectPassword@2026", workFactor: 4),
            Role = UserRoles.Member,
            AccountStatus = AccountStatuses.Active,
            FailedLoginCount = 0,
            CreatedAt = DateTime.UtcNow
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var ex = await Assert.ThrowsAsync<AppException>(() => service.LoginAsync(new LoginRequest
        {
            Email = "wrongpass@veggielife.vn",
            Password = "WrongPassword@2026"
        }));

        Assert.Equal(401, ex.StatusCode);
        Assert.Equal(ErrorCodes.ErrAuthInvalidCredentials, ex.ErrorCode);

        var updatedUser = await db.Users.FindAsync(user.UserId);
        Assert.NotNull(updatedUser);
        Assert.Equal(1, updatedUser.FailedLoginCount);
    }

    [Fact]
    public async Task Login_WrongPassword5Times_LocksAccountFor15Minutes()
    {
        var (db, service, _, _) = CreateTestHarness();

        var user = new User
        {
            Email = "lockout@veggielife.vn",
            FullName = "Người Dùng Bị Khóa",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("CorrectPassword@2026", workFactor: 4),
            Role = UserRoles.Member,
            AccountStatus = AccountStatuses.Active,
            FailedLoginCount = 4, // Đã sai 4 lần trước đó
            CreatedAt = DateTime.UtcNow
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();

        // Lần thứ 5 sai -> Khóa 15 phút
        var ex = await Assert.ThrowsAsync<AppException>(() => service.LoginAsync(new LoginRequest
        {
            Email = "lockout@veggielife.vn",
            Password = "WrongPassword@2026"
        }));

        Assert.Equal(429, ex.StatusCode);
        Assert.Equal(ErrorCodes.ErrAuthLoginLocked, ex.ErrorCode);

        var updatedUser = await db.Users.FindAsync(user.UserId);
        Assert.NotNull(updatedUser);
        Assert.Equal(5, updatedUser.FailedLoginCount);
        Assert.NotNull(updatedUser.LockedUntil);
        Assert.True(updatedUser.LockedUntil.Value > DateTime.UtcNow.AddMinutes(14));
    }

    [Fact]
    public async Task Login_WhenAccountIsLocked_RejectsWith429_EvenIfPasswordIsCorrect()
    {
        var (db, service, _, _) = CreateTestHarness();

        var user = new User
        {
            Email = "locked@veggielife.vn",
            FullName = "Tài Khoản Đang Khóa",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("CorrectPassword@2026", workFactor: 4),
            Role = UserRoles.Member,
            AccountStatus = AccountStatuses.Active,
            FailedLoginCount = 5,
            LockedUntil = DateTime.UtcNow.AddMinutes(10), // Còn 10 phút khóa
            CreatedAt = DateTime.UtcNow
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var ex = await Assert.ThrowsAsync<AppException>(() => service.LoginAsync(new LoginRequest
        {
            Email = "locked@veggielife.vn",
            Password = "CorrectPassword@2026"
        }));

        Assert.Equal(429, ex.StatusCode);
        Assert.Equal(ErrorCodes.ErrAuthLoginLocked, ex.ErrorCode);
    }

    [Fact]
    public async Task Login_WhenLockExpired_AllowsLogin_AndResetsCounters()
    {
        var (db, service, _, _) = CreateTestHarness();

        var user = new User
        {
            Email = "expiredlock@veggielife.vn",
            FullName = "Đã Hết Thời Gian Khóa",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("CorrectPassword@2026", workFactor: 4),
            Role = UserRoles.Member,
            AccountStatus = AccountStatuses.Active,
            FailedLoginCount = 5,
            LockedUntil = DateTime.UtcNow.AddMinutes(-1), // Đã hết hạn khóa 1 phút trước
            CreatedAt = DateTime.UtcNow
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var res = await service.LoginAsync(new LoginRequest
        {
            Email = "expiredlock@veggielife.vn",
            Password = "CorrectPassword@2026"
        });

        Assert.NotNull(res);
        var updatedUser = await db.Users.FindAsync(user.UserId);
        Assert.NotNull(updatedUser);
        Assert.Equal(0, updatedUser.FailedLoginCount);
        Assert.Null(updatedUser.LockedUntil);
    }

    [Fact]
    public async Task Login_WhenAccountPendingActivation_Throws403_EmailNotVerified()
    {
        var (db, service, _, _) = CreateTestHarness();

        var user = new User
        {
            Email = "pending@veggielife.vn",
            FullName = "Chưa Kích Hoạt",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("ValidPassword@2026", workFactor: 4),
            Role = UserRoles.Member,
            AccountStatus = AccountStatuses.PendingActivation,
            CreatedAt = DateTime.UtcNow
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var ex = await Assert.ThrowsAsync<AppException>(() => service.LoginAsync(new LoginRequest
        {
            Email = "pending@veggielife.vn",
            Password = "ValidPassword@2026"
        }));

        Assert.Equal(403, ex.StatusCode);
        Assert.Equal(ErrorCodes.ErrAuthEmailNotVerified, ex.ErrorCode);
    }

    [Theory]
    [InlineData(AccountStatuses.Suspended)]
    [InlineData(AccountStatuses.Banned)]
    public async Task Login_WhenAccountBannedOrSuspended_Throws403_AccountBanned(string status)
    {
        var (db, service, _, _) = CreateTestHarness();

        var user = new User
        {
            Email = $"{status.ToLower()}@veggielife.vn",
            FullName = "Tài Khoản Vi Phạm",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("ValidPassword@2026", workFactor: 4),
            Role = UserRoles.Member,
            AccountStatus = status,
            CreatedAt = DateTime.UtcNow
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var ex = await Assert.ThrowsAsync<AppException>(() => service.LoginAsync(new LoginRequest
        {
            Email = user.Email,
            Password = "ValidPassword@2026"
        }));

        Assert.Equal(403, ex.StatusCode);
        Assert.Equal(ErrorCodes.ErrAuthAccountBanned, ex.ErrorCode);
    }

    [Fact]
    public async Task Login_WhenAccountSoftDeleted_WithoutRestore_Throws409()
    {
        var (db, service, _, _) = CreateTestHarness();

        var user = new User
        {
            Email = "softdeleted@veggielife.vn",
            FullName = "Người Dùng Đã Xóa",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("ValidPassword@2026", workFactor: 4),
            Role = UserRoles.Member,
            AccountStatus = AccountStatuses.SoftDeleted,
            UpdatedAt = DateTime.UtcNow.AddDays(-10), // Trong 30 ngày ân hạn
            CreatedAt = DateTime.UtcNow.AddDays(-20)
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var ex = await Assert.ThrowsAsync<AppException>(() => service.LoginAsync(new LoginRequest
        {
            Email = "softdeleted@veggielife.vn",
            Password = "ValidPassword@2026",
            RestoreAccount = false
        }));

        Assert.Equal(409, ex.StatusCode);
        Assert.Equal(ErrorCodes.ErrAuthAccountPendingDeletion, ex.ErrorCode);
    }

    [Fact]
    public async Task Login_WhenAccountSoftDeleted_WithRestore_RestoresToActive()
    {
        var (db, service, _, _) = CreateTestHarness();

        var user = new User
        {
            Email = "restoreme@veggielife.vn",
            FullName = "Khôi Phục Tài Khoản",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("ValidPassword@2026", workFactor: 4),
            Role = UserRoles.Member,
            AccountStatus = AccountStatuses.SoftDeleted,
            UpdatedAt = DateTime.UtcNow.AddDays(-5),
            CreatedAt = DateTime.UtcNow.AddDays(-30)
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var response = await service.LoginAsync(new LoginRequest
        {
            Email = "restoreme@veggielife.vn",
            Password = "ValidPassword@2026",
            RestoreAccount = true
        });

        Assert.NotNull(response);
        Assert.Equal(AccountStatuses.Active, response.User.AccountStatus);

        var updatedUser = await db.Users.FindAsync(user.UserId);
        Assert.NotNull(updatedUser);
        Assert.Equal(AccountStatuses.Active, updatedUser.AccountStatus);
    }

    [Fact]
    public async Task Login_AsAdmin_RejectsConcurrentLoginFromDifferentIp()
    {
        var (db, service, redis, _) = CreateTestHarness();

        var admin = new User
        {
            Email = "admin@veggielife.vn",
            FullName = "Quản Trị Viên",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("AdminPass@2026", workFactor: 4),
            Role = UserRoles.Admin,
            AccountStatus = AccountStatuses.Active,
            CreatedAt = DateTime.UtcNow
        };
        db.Users.Add(admin);
        await db.SaveChangesAsync();

        // Phiên 1 từ IP A thành công
        var res1 = await service.LoginAsync(new LoginRequest
        {
            Email = "admin@veggielife.vn",
            Password = "AdminPass@2026"
        }, clientIp: "10.0.0.1");
        Assert.NotNull(res1);

        // Phiên 2 từ IP B khác trong 30 phút -> Bị từ chối theo BR-ADM-SYS-003
        var ex = await Assert.ThrowsAsync<AppException>(() => service.LoginAsync(new LoginRequest
        {
            Email = "admin@veggielife.vn",
            Password = "AdminPass@2026"
        }, clientIp: "10.0.0.2"));

        Assert.Equal(403, ex.StatusCode);
        Assert.Equal(ErrorCodes.ErrAdmForbidden, ex.ErrorCode);
    }

    [Fact]
    public async Task RefreshToken_WithValidToken_RotatesToken_AndReturnsNewTokens()
    {
        var (db, service, redis, _) = CreateTestHarness();

        var user = new User
        {
            Email = "refresh@veggielife.vn",
            FullName = "Người Dùng Refresh",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Pass@2026", workFactor: 4),
            Role = UserRoles.Member,
            AccountStatus = AccountStatuses.Active,
            CreatedAt = DateTime.UtcNow
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();

        // 1. Đăng nhập để lấy Refresh Token ban đầu
        var loginRes = await service.LoginAsync(new LoginRequest
        {
            Email = "refresh@veggielife.vn",
            Password = "Pass@2026"
        });

        var initialRefreshToken = loginRes.RefreshToken;

        // 2. Làm mới Access Token
        var refreshRes = await service.RefreshTokenAsync(initialRefreshToken, clientIp: "127.0.0.1");

        Assert.NotNull(refreshRes);
        Assert.Equal("Bearer", refreshRes.TokenType);
        Assert.NotEmpty(refreshRes.AccessToken);
        Assert.NotEmpty(refreshRes.RefreshToken);
        Assert.NotEqual(initialRefreshToken, refreshRes.RefreshToken); // Mã mới đã xoay vòng
        Assert.Equal(900, refreshRes.ExpiresIn);

        // 3. Kiểm tra token cũ đã bị đánh dấu IsRevoked
        var oldHash = System.Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(initialRefreshToken)));
        var oldSession = await redis.GetAsync<RefreshTokenSession>($"refresh:{oldHash}");
        Assert.NotNull(oldSession);
        Assert.True(oldSession.IsRevoked);

        // 4. Kiểm tra token mới có trạng thái hợp lệ trên Redis
        var newHash = System.Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(refreshRes.RefreshToken)));
        var newSession = await redis.GetAsync<RefreshTokenSession>($"refresh:{newHash}");
        Assert.NotNull(newSession);
        Assert.False(newSession.IsRevoked);
    }

    [Fact]
    public async Task RefreshToken_TokenReuseDetection_RevokesAllUserSessions_AndWritesAuditLog()
    {
        var (db, service, redis, _) = CreateTestHarness();

        var user = new User
        {
            Email = "reuse@veggielife.vn",
            FullName = "Người Dùng Bị Tấn Công",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Pass@2026", workFactor: 4),
            Role = UserRoles.Member,
            AccountStatus = AccountStatuses.Active,
            CreatedAt = DateTime.UtcNow
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();

        // Đăng nhập lần 1
        var loginRes = await service.LoginAsync(new LoginRequest
        {
            Email = "reuse@veggielife.vn",
            Password = "Pass@2026"
        });
        var originalRefreshToken = loginRes.RefreshToken;

        // Xoay vòng lần 1 hợp lệ
        var firstRefreshRes = await service.RefreshTokenAsync(originalRefreshToken);
        var rotatedRefreshToken = firstRefreshRes.RefreshToken;

        // Kẻ gian cố gắng sử dụng lại token cũ (originalRefreshToken) -> TOKEN REUSE DETECTED!
        var ex = await Assert.ThrowsAsync<AppException>(() => service.RefreshTokenAsync(originalRefreshToken));

        Assert.Equal(401, ex.StatusCode);
        Assert.Equal(ErrorCodes.ErrAuthRefreshTokenReused, ex.ErrorCode);

        // Kiểm tra toàn bộ phiên của tài khoản đã bị hủy trên Redis
        var rotatedHash = System.Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(rotatedRefreshToken)));
        var rotatedSession = await redis.GetAsync<RefreshTokenSession>($"refresh:{rotatedHash}");
        Assert.Null(rotatedSession); // Đã bị xóa sạch

        var userSessions = await redis.GetAsync<List<string>>($"user_sessions:{user.UserId}");
        Assert.Null(userSessions);

        // Kiểm tra AuditLog TOKEN_REUSE_DETECTED
        var audit = await db.AuditLogs.FirstOrDefaultAsync(a => a.ActorUserId == user.UserId && a.Action == "TOKEN_REUSE_DETECTED");
        Assert.NotNull(audit);

        // Phiên mới vừa cấp trước đó cũng không dùng được nữa
        var failEx = await Assert.ThrowsAsync<AppException>(() => service.RefreshTokenAsync(rotatedRefreshToken));
        Assert.Equal(401, failEx.StatusCode);
        Assert.Equal(ErrorCodes.ErrAuthRefreshTokenInvalid, failEx.ErrorCode);
    }

    [Fact]
    public async Task Logout_RevokesRefreshToken_AndBlacklistsAccessTokenJti()
    {
        var (db, service, redis, _) = CreateTestHarness();

        var user = new User
        {
            Email = "logout@veggielife.vn",
            FullName = "Người Dùng Đăng Xuất",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Pass@2026", workFactor: 4),
            Role = UserRoles.Member,
            AccountStatus = AccountStatuses.Active,
            CreatedAt = DateTime.UtcNow
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var loginRes = await service.LoginAsync(new LoginRequest
        {
            Email = "logout@veggielife.vn",
            Password = "Pass@2026"
        });

        var refreshToken = loginRes.RefreshToken;
        var jti = "test-jti-12345";
        var tokenExpiry = DateTime.UtcNow.AddMinutes(15);

        // Đăng xuất
        await service.LogoutAsync(user.UserId, user.Role, jti, tokenExpiry, refreshToken);

        // Kiểm tra jti đã được đưa vào blacklist
        var isBlacklisted = await redis.ExistsAsync($"blacklist:{jti}");
        Assert.True(isBlacklisted);

        // Kiểm tra refresh token đã bị xóa khỏi Redis
        var refreshHash = System.Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(refreshToken)));
        var session = await redis.GetAsync<RefreshTokenSession>($"refresh:{refreshHash}");
        Assert.Null(session);

        // Kiểm tra AuditLog USER_LOGOUT
        var audit = await db.AuditLogs.FirstOrDefaultAsync(a => a.ActorUserId == user.UserId && a.Action == "USER_LOGOUT");
        Assert.NotNull(audit);
    }

    [Fact]
    public async Task RefreshToken_WhenRedisThrowsException_Returns503_ErrSysRedisUnavailable()
    {
        var mockRedis = new Mock<IRedisService>();
        mockRedis.Setup(r => r.GetAsync<RefreshTokenSession>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Redis connection timed out"));

        var (_, service, _, _) = CreateTestHarness(mockRedis.Object);

        var ex = await Assert.ThrowsAsync<AppException>(() => service.RefreshTokenAsync("dummy_refresh_token"));

        Assert.Equal(503, ex.StatusCode);
        Assert.Equal(ErrorCodes.ErrSysRedisUnavailable, ex.ErrorCode);
    }

    [Fact]
    public async Task Logout_WhenRedisThrowsException_Returns503_ErrSysRedisUnavailable()
    {
        var mockRedis = new Mock<IRedisService>();
        mockRedis.Setup(r => r.SetAsync(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<TimeSpan?>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Redis down"));

        var (_, service, _, _) = CreateTestHarness(mockRedis.Object);

        var ex = await Assert.ThrowsAsync<AppException>(() => service.LogoutAsync(
            userId: 1,
            userRole: UserRoles.Member,
            jti: "test-jti",
            tokenExpiryUtc: DateTime.UtcNow.AddMinutes(10),
            refreshToken: "dummy_refresh"));

        Assert.Equal(503, ex.StatusCode);
        Assert.Equal(ErrorCodes.ErrSysRedisUnavailable, ex.ErrorCode);
    }
}
