using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
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
using VeggieLife.DAL.External.Google;
using Xunit;

namespace VeggieLife.Tests.Unit.Services.Auth;

/// <summary>
/// Bộ kiểm thử đơn vị cho chức năng Đăng nhập / Đăng ký bằng Google OAuth (FN-AUTH-07).
/// Tuân thủ các quy tắc nghiệp vụ BR-USR-AUTH-02, BR-AUTH-004, BR-ADM-USR-002, BR-GUEST-AUTH-01.
/// </summary>
public class AuthGoogleServiceTests
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

        public Task SetStringAsync(string key, string value, TimeSpan? expiry = null, CancellationToken ct = default)
            => SetAsync(key, value, expiry, ct);

        public Task<string?> GetStringAsync(string key, CancellationToken ct = default)
            => GetAsync<string>(key, ct);

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

    private static (AppDbContext DbContext, AuthService AuthService, InMemoryRedisService RedisService, Mock<IGoogleAuthClient> MockGoogleClient) CreateTestHarness(AppDbContext? customDb = null)
    {
        var dbContext = customDb ?? new AppDbContext(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options);

        var redisService = new InMemoryRedisService();
        var emailService = new Mock<IEmailService>().Object;
        var mockGoogleClient = new Mock<IGoogleAuthClient>();

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
            redisService,
            emailService,
            jwtTokenService,
            NullLogger<AuthService>.Instance,
            mockGoogleClient.Object);

        return (dbContext, authService, redisService, mockGoogleClient);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task LoginWithGoogle_WhenIdTokenEmpty_Throws400ErrValidation(string? emptyToken)
    {
        var (_, service, _, _) = CreateTestHarness();

        var ex = await Assert.ThrowsAsync<AppException>(() =>
            service.LoginWithGoogleAsync(new LoginWithGoogleRequest { IdToken = emptyToken! }));

        Assert.Equal(400, ex.StatusCode);
        Assert.Equal(ErrorCodes.ErrValidation, ex.ErrorCode);
    }

    [Fact]
    public async Task LoginWithGoogle_WhenGoogleClientReturnsNull_Throws401ErrAuthOauthFailed()
    {
        var (_, service, _, mockGoogle) = CreateTestHarness();

        mockGoogle.Setup(g => g.ValidateIdTokenAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((GoogleTokenPayload?)null);

        var ex = await Assert.ThrowsAsync<AppException>(() =>
            service.LoginWithGoogleAsync(new LoginWithGoogleRequest { IdToken = "invalid_token" }));

        Assert.Equal(401, ex.StatusCode);
        Assert.Equal(ErrorCodes.ErrAuthOauthFailed, ex.ErrorCode);
    }

    [Fact]
    public async Task LoginWithGoogle_WhenEmailNotVerified_Throws401ErrAuthOauthFailed()
    {
        var (_, service, _, mockGoogle) = CreateTestHarness();

        mockGoogle.Setup(g => g.ValidateIdTokenAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GoogleTokenPayload
            {
                Subject = "google_sub_123",
                Email = "test@example.com",
                EmailVerified = false,
                Name = "Test User"
            });

        var ex = await Assert.ThrowsAsync<AppException>(() =>
            service.LoginWithGoogleAsync(new LoginWithGoogleRequest { IdToken = "unverified_token" }));

        Assert.Equal(401, ex.StatusCode);
        Assert.Equal(ErrorCodes.ErrAuthOauthFailed, ex.ErrorCode);
    }

    [Fact]
    public async Task LoginWithGoogle_WhenGoogleClientThrowsException_Throws401ErrAuthOauthFailed()
    {
        var (_, service, _, mockGoogle) = CreateTestHarness();

        mockGoogle.Setup(g => g.ValidateIdTokenAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Network connection failed"));

        var ex = await Assert.ThrowsAsync<AppException>(() =>
            service.LoginWithGoogleAsync(new LoginWithGoogleRequest { IdToken = "error_token" }));

        Assert.Equal(401, ex.StatusCode);
        Assert.Equal(ErrorCodes.ErrAuthOauthFailed, ex.ErrorCode);
    }

    [Fact]
    public async Task LoginWithGoogle_WhenEmailNotExists_CreatesActiveUser_AndReturns200_IsNewAccountTrue()
    {
        // ARRANGE: Email chưa tồn tại trong hệ thống
        var (db, service, redis, mockGoogle) = CreateTestHarness();

        mockGoogle.Setup(g => g.ValidateIdTokenAsync("valid_google_token", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GoogleTokenPayload
            {
                Subject = "google_sub_9999",
                Email = "new.user@gmail.com",
                EmailVerified = true,
                Name = "Nguyễn Văn Mới",
                Picture = "https://lh3.googleusercontent.com/avatar.jpg"
            });

        // ACT: Thực hiện đăng ký / đăng nhập nhanh bằng Google
        var response = await service.LoginWithGoogleAsync(new LoginWithGoogleRequest
        {
            IdToken = "valid_google_token"
        }, clientIp: "192.168.1.1");

        // ASSERT: Phản hồi HTTP 200 và DTO khớp chuẩn
        Assert.NotNull(response);
        Assert.Equal("Bearer", response.TokenType);
        Assert.NotEmpty(response.AccessToken);
        Assert.NotEmpty(response.RefreshToken);
        Assert.Equal(900, response.ExpiresIn);
        Assert.True(response.IsNewAccount);

        Assert.Equal("new.user@gmail.com", response.User.Email);
        Assert.Equal("Nguyễn Văn Mới", response.User.FullName);
        Assert.Equal(UserRoles.Member, response.User.Role);
        Assert.Equal(AccountStatuses.Active, response.User.AccountStatus);
        Assert.False(response.User.IsVip);
        Assert.False(response.User.IsProfileCompleted);

        // Kiểm tra User trong CSDL
        var createdUser = await db.Users.FirstOrDefaultAsync(u => u.Email == "new.user@gmail.com");
        Assert.NotNull(createdUser);
        Assert.Equal("google_sub_9999", createdUser.GoogleId);
        Assert.Null(createdUser.PasswordHash);
        Assert.Equal(AccountStatuses.Active, createdUser.AccountStatus);
        Assert.Equal("https://lh3.googleusercontent.com/avatar.jpg", createdUser.AvatarUrl);
        Assert.Equal(0, createdUser.FailedLoginCount);

        // Kiểm tra AuditLog được tạo (USER_REGISTERED)
        var audit = await db.AuditLogs.FirstOrDefaultAsync(a => a.ActorUserId == createdUser.UserId && a.Action == "USER_REGISTERED");
        Assert.NotNull(audit);

        // Kiểm tra Refresh Token được lưu trong Redis
        var userSessions = await redis.GetAsync<List<string>>($"user_sessions:{createdUser.UserId}");
        Assert.NotNull(userSessions);
        Assert.Single(userSessions);
    }

    [Fact]
    public async Task LoginWithGoogle_WhenExistingUserWithSameGoogleId_LogsInSuccessfully_IsNewAccountFalse()
    {
        // ARRANGE: Người dùng đã liên kết Google_ID từ trước
        var (db, service, _, mockGoogle) = CreateTestHarness();

        var existingUser = new User
        {
            Email = "existing.google@gmail.com",
            FullName = "Người Dùng Cũ",
            GoogleId = "google_sub_existing_111",
            PasswordHash = null,
            Role = UserRoles.Member,
            AccountStatus = AccountStatuses.Active,
            CreatedAt = DateTime.UtcNow.AddDays(-10)
        };
        db.Users.Add(existingUser);
        await db.SaveChangesAsync();

        mockGoogle.Setup(g => g.ValidateIdTokenAsync("token_existing", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GoogleTokenPayload
            {
                Subject = "google_sub_existing_111",
                Email = "existing.google@gmail.com",
                EmailVerified = true,
                Name = "Người Dùng Cũ"
            });

        // ACT
        var response = await service.LoginWithGoogleAsync(new LoginWithGoogleRequest
        {
            IdToken = "token_existing"
        });

        // ASSERT
        Assert.NotNull(response);
        Assert.False(response.IsNewAccount);
        Assert.Equal(existingUser.UserId, response.User.UserId);
        Assert.Equal("existing.google@gmail.com", response.User.Email);

        // Kiểm tra cập nhật LastLoginAt
        var updated = await db.Users.FindAsync(existingUser.UserId);
        Assert.NotNull(updated);
        Assert.NotNull(updated.LastLoginAt);
    }

    [Fact]
    public async Task LoginWithGoogle_WhenEmailExistsWithPassword_NoCurrentPassword_Throws409ErrAuthAccountLinkRequired()
    {
        // ARRANGE: Email đã có tài khoản mật khẩu nhưng chưa có Google_ID (BR-USR-AUTH-02)
        var (db, service, _, mockGoogle) = CreateTestHarness();

        var passwordUser = new User
        {
            Email = "password.user@gmail.com",
            FullName = "User Đã Có Pass",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("SecuredPassword@2026", workFactor: 4),
            GoogleId = null,
            Role = UserRoles.Member,
            AccountStatus = AccountStatuses.Active,
            CreatedAt = DateTime.UtcNow.AddMonths(-1)
        };
        db.Users.Add(passwordUser);
        await db.SaveChangesAsync();

        mockGoogle.Setup(g => g.ValidateIdTokenAsync("token_link_req", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GoogleTokenPayload
            {
                Subject = "google_sub_link_999",
                Email = "password.user@gmail.com",
                EmailVerified = true,
                Name = "User Đã Có Pass"
            });

        // ACT: Gửi request không có currentPassword
        var ex = await Assert.ThrowsAsync<AppException>(() =>
            service.LoginWithGoogleAsync(new LoginWithGoogleRequest
            {
                IdToken = "token_link_req",
                CurrentPassword = null
            }));

        // ASSERT: Trả 409 ERR_AUTH_ACCOUNT_LINK_REQUIRED
        Assert.Equal(409, ex.StatusCode);
        Assert.Equal(ErrorCodes.ErrAuthAccountLinkRequired, ex.ErrorCode);
    }

    [Fact]
    public async Task LoginWithGoogle_WhenEmailExistsWithPassword_WrongCurrentPassword_Throws401ErrAuthInvalidCredentials()
    {
        // ARRANGE: Email đã có mật khẩu, truyền sai currentPassword (BR-USR-AUTH-02)
        var (db, service, _, mockGoogle) = CreateTestHarness();

        var passwordUser = new User
        {
            Email = "password.user@gmail.com",
            FullName = "User Đã Có Pass",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("CorrectPassword@2026", workFactor: 4),
            GoogleId = null,
            Role = UserRoles.Member,
            AccountStatus = AccountStatuses.Active,
            CreatedAt = DateTime.UtcNow.AddMonths(-1)
        };
        db.Users.Add(passwordUser);
        await db.SaveChangesAsync();

        mockGoogle.Setup(g => g.ValidateIdTokenAsync("token_wrong_pass", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GoogleTokenPayload
            {
                Subject = "google_sub_link_999",
                Email = "password.user@gmail.com",
                EmailVerified = true,
                Name = "User Đã Có Pass"
            });

        // ACT: Gửi sai CurrentPassword
        var ex = await Assert.ThrowsAsync<AppException>(() =>
            service.LoginWithGoogleAsync(new LoginWithGoogleRequest
            {
                IdToken = "token_wrong_pass",
                CurrentPassword = "WrongPassword@2026"
            }));

        // ASSERT: Trả 401 ERR_AUTH_INVALID_CREDENTIALS
        Assert.Equal(401, ex.StatusCode);
        Assert.Equal(ErrorCodes.ErrAuthInvalidCredentials, ex.ErrorCode);
    }

    [Fact]
    public async Task LoginWithGoogle_WhenEmailExistsWithPassword_CorrectCurrentPassword_LinksGoogleId_Returns200()
    {
        // ARRANGE: Email đã có mật khẩu, truyền đúng currentPassword (BR-USR-AUTH-02)
        var (db, service, _, mockGoogle) = CreateTestHarness();

        var passwordUser = new User
        {
            Email = "password.user@gmail.com",
            FullName = "User Đã Có Pass",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("CorrectPassword@2026", workFactor: 4),
            GoogleId = null,
            Role = UserRoles.Member,
            AccountStatus = AccountStatuses.Active,
            CreatedAt = DateTime.UtcNow.AddMonths(-1)
        };
        db.Users.Add(passwordUser);
        await db.SaveChangesAsync();

        mockGoogle.Setup(g => g.ValidateIdTokenAsync("token_correct_pass", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GoogleTokenPayload
            {
                Subject = "google_sub_linked_888",
                Email = "password.user@gmail.com",
                EmailVerified = true,
                Name = "User Đã Có Pass",
                Picture = "https://lh3.googleusercontent.com/avatar_new.jpg"
            });

        // ACT: Gửi đúng CurrentPassword để liên kết
        var response = await service.LoginWithGoogleAsync(new LoginWithGoogleRequest
        {
            IdToken = "token_correct_pass",
            CurrentPassword = "CorrectPassword@2026"
        });

        // ASSERT: Thành công 200, isNewAccount = false, GoogleId được lưu
        Assert.NotNull(response);
        Assert.False(response.IsNewAccount);
        Assert.Equal(passwordUser.UserId, response.User.UserId);

        var updatedUser = await db.Users.FindAsync(passwordUser.UserId);
        Assert.NotNull(updatedUser);
        Assert.Equal("google_sub_linked_888", updatedUser.GoogleId);
        Assert.Equal("https://lh3.googleusercontent.com/avatar_new.jpg", updatedUser.AvatarUrl);

        // Kiểm tra AuditLog ACCOUNT_LINKED
        var audit = await db.AuditLogs.FirstOrDefaultAsync(a => a.ActorUserId == passwordUser.UserId && a.Action == "ACCOUNT_LINKED");
        Assert.NotNull(audit);
    }

    [Fact]
    public async Task LoginWithGoogle_WhenEmailLinkedToDifferentGoogleId_Throws409ErrAuthAccountLinkRequired()
    {
        // ARRANGE: Email đã liên kết với Google_ID khác
        var (db, service, _, mockGoogle) = CreateTestHarness();

        var existingUser = new User
        {
            Email = "diff.google@gmail.com",
            FullName = "Tài khoản khác",
            GoogleId = "google_sub_first_111",
            Role = UserRoles.Member,
            AccountStatus = AccountStatuses.Active,
            CreatedAt = DateTime.UtcNow.AddDays(-5)
        };
        db.Users.Add(existingUser);
        await db.SaveChangesAsync();

        mockGoogle.Setup(g => g.ValidateIdTokenAsync("token_diff", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GoogleTokenPayload
            {
                Subject = "google_sub_second_222",
                Email = "diff.google@gmail.com",
                EmailVerified = true,
                Name = "Tài khoản khác"
            });

        // ACT
        var ex = await Assert.ThrowsAsync<AppException>(() =>
            service.LoginWithGoogleAsync(new LoginWithGoogleRequest
            {
                IdToken = "token_diff"
            }));

        // ASSERT
        Assert.Equal(409, ex.StatusCode);
        Assert.Equal(ErrorCodes.ErrAuthAccountLinkRequired, ex.ErrorCode);
    }

    [Theory]
    [InlineData(AccountStatuses.Suspended)]
    [InlineData(AccountStatuses.Banned)]
    public async Task LoginWithGoogle_WhenAccountSuspendedOrBanned_Throws403ErrAuthAccountBanned(string accountStatus)
    {
        // ARRANGE: Tài khoản bị đình chỉ hoặc cấm (BR-ADM-USR-002)
        var (db, service, _, mockGoogle) = CreateTestHarness();

        var bannedUser = new User
        {
            Email = "banned.user@gmail.com",
            FullName = "Người Bị Cấm",
            GoogleId = "google_banned_333",
            Role = UserRoles.Member,
            AccountStatus = accountStatus,
            CreatedAt = DateTime.UtcNow.AddMonths(-2)
        };
        db.Users.Add(bannedUser);
        await db.SaveChangesAsync();

        mockGoogle.Setup(g => g.ValidateIdTokenAsync("token_banned", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GoogleTokenPayload
            {
                Subject = "google_banned_333",
                Email = "banned.user@gmail.com",
                EmailVerified = true,
                Name = "Người Bị Cấm"
            });

        // ACT & ASSERT
        var ex = await Assert.ThrowsAsync<AppException>(() =>
            service.LoginWithGoogleAsync(new LoginWithGoogleRequest
            {
                IdToken = "token_banned"
            }));

        Assert.Equal(403, ex.StatusCode);
        Assert.Equal(ErrorCodes.ErrAuthAccountBanned, ex.ErrorCode);
    }

    [Fact]
    public async Task LoginWithGoogle_WhenAccountSoftDeleted_Within30Days_RestoresAndLogsIn()
    {
        // ARRANGE: Tài khoản SOFT_DELETED trong vòng 30 ngày -> tự động phục hồi ACTIVE
        var (db, service, _, mockGoogle) = CreateTestHarness();

        var softDeletedUser = new User
        {
            Email = "soft.deleted@gmail.com",
            FullName = "Người Dùng Xóa Mềm",
            GoogleId = "google_soft_444",
            Role = UserRoles.Member,
            AccountStatus = AccountStatuses.SoftDeleted,
            CreatedAt = DateTime.UtcNow.AddDays(-10),
            UpdatedAt = DateTime.UtcNow.AddDays(-10)
        };
        db.Users.Add(softDeletedUser);
        await db.SaveChangesAsync();

        mockGoogle.Setup(g => g.ValidateIdTokenAsync("token_soft", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GoogleTokenPayload
            {
                Subject = "google_soft_444",
                Email = "soft.deleted@gmail.com",
                EmailVerified = true,
                Name = "Người Dùng Xóa Mềm"
            });

        // ACT
        var response = await service.LoginWithGoogleAsync(new LoginWithGoogleRequest
        {
            IdToken = "token_soft"
        });

        // ASSERT
        Assert.NotNull(response);
        Assert.Equal(AccountStatuses.Active, response.User.AccountStatus);

        var userInDb = await db.Users.FindAsync(softDeletedUser.UserId);
        Assert.NotNull(userInDb);
        Assert.Equal(AccountStatuses.Active, userInDb.AccountStatus);
    }

    [Fact]
    public async Task LoginWithGoogle_WhenAccountSoftDeleted_Over30Days_Throws403ErrAuthAccountBanned()
    {
        // ARRANGE: Tài khoản SOFT_DELETED quá 30 ngày -> bị xóa vĩnh viễn
        var (db, service, _, mockGoogle) = CreateTestHarness();

        var expiredUser = new User
        {
            Email = "expired.soft@gmail.com",
            FullName = "Người Dùng Hết Hạn",
            GoogleId = "google_soft_expired_555",
            Role = UserRoles.Member,
            AccountStatus = AccountStatuses.SoftDeleted,
            CreatedAt = DateTime.UtcNow.AddDays(-40),
            UpdatedAt = DateTime.UtcNow.AddDays(-35)
        };
        db.Users.Add(expiredUser);
        await db.SaveChangesAsync();

        mockGoogle.Setup(g => g.ValidateIdTokenAsync("token_expired_soft", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GoogleTokenPayload
            {
                Subject = "google_soft_expired_555",
                Email = "expired.soft@gmail.com",
                EmailVerified = true,
                Name = "Người Dùng Hết Hạn"
            });

        // ACT & ASSERT
        var ex = await Assert.ThrowsAsync<AppException>(() =>
            service.LoginWithGoogleAsync(new LoginWithGoogleRequest
            {
                IdToken = "token_expired_soft"
            }));

        Assert.Equal(403, ex.StatusCode);
        Assert.Equal(ErrorCodes.ErrAuthAccountBanned, ex.ErrorCode);
    }

    [Fact]
    public async Task LoginWithGoogle_WhenGuestUuidProvided_MergesChatSessions_BR_GUEST_AUTH_01()
    {
        // ARRANGE: Phiên khách có phiên chat AI vãng lai (BR-GUEST-AUTH-01)
        var (db, service, _, mockGoogle) = CreateTestHarness();
        var guestUuid = "guest-session-uuid-777";

        var chatSession = new ChatSession
        {
            GuestUuid = guestUuid,
            UserId = null,
            SessionTitle = "Hỏi thực đơn thuần chay",
            ContextType = "NUTRITION",
            StartedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        db.ChatSessions.Add(chatSession);
        await db.SaveChangesAsync();

        mockGoogle.Setup(g => g.ValidateIdTokenAsync("token_guest_merge", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GoogleTokenPayload
            {
                Subject = "google_guest_merge_sub",
                Email = "guest.merged@gmail.com",
                EmailVerified = true,
                Name = "Khách Hợp Nhất"
            });

        // ACT: Đăng nhập kèm GuestUuid
        var response = await service.LoginWithGoogleAsync(new LoginWithGoogleRequest
        {
            IdToken = "token_guest_merge",
            GuestUuid = guestUuid
        });

        // ASSERT: Chat session được gán UserId của người dùng mới
        Assert.NotNull(response);
        var mergedSession = await db.ChatSessions.FindAsync(chatSession.ChatSessionId);
        Assert.NotNull(mergedSession);
        Assert.Equal(response.User.UserId, mergedSession.UserId);
    }

    [Fact]
    public async Task LoginWithGoogle_WhenActiveVipPaymentExists_ReturnsIsVipTrue_BR_AUTH_04()
    {
        // ARRANGE: Người dùng có giao dịch VIP còn hiệu lực (D104)
        var (db, service, _, mockGoogle) = CreateTestHarness();

        var vipUser = new User
        {
            Email = "vip.google@gmail.com",
            FullName = "VIP Member",
            GoogleId = "google_vip_777",
            Role = UserRoles.Member,
            AccountStatus = AccountStatuses.Active,
            CreatedAt = DateTime.UtcNow.AddMonths(-1)
        };
        db.Users.Add(vipUser);
        await db.SaveChangesAsync();

        db.PaymentTransactions.Add(new PaymentTransaction
        {
            UserId = vipUser.UserId,
            PlanId = 1,
            GatewayProvider = "MOMO",
            PartnerTransactionCode = "MOMO_TRX_12345",
            AmountVnd = 99000,
            SignatureHash = "sig_hash_123",
            TransactionStatus = "SUCCESS",
            StartsAt = DateTime.UtcNow.AddDays(-15),
            EndsAt = DateTime.UtcNow.AddDays(15),
            CreatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        mockGoogle.Setup(g => g.ValidateIdTokenAsync("token_vip", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GoogleTokenPayload
            {
                Subject = "google_vip_777",
                Email = "vip.google@gmail.com",
                EmailVerified = true,
                Name = "VIP Member"
            });

        // ACT
        var response = await service.LoginWithGoogleAsync(new LoginWithGoogleRequest
        {
            IdToken = "token_vip"
        });

        // ASSERT
        Assert.NotNull(response);
        Assert.True(response.User.IsVip);
    }

    [Fact]
    public async Task LoginWithGoogle_WhenHealthProfileExists_SetsIsProfileCompletedTrue()
    {
        // ARRANGE: Người dùng đã hoàn thành hồ sơ dinh dưỡng
        var (db, service, _, mockGoogle) = CreateTestHarness();

        var profileUser = new User
        {
            Email = "profile.completed@gmail.com",
            FullName = "Profile Completed",
            GoogleId = "google_profile_888",
            Role = UserRoles.Member,
            AccountStatus = AccountStatuses.Active,
            CreatedAt = DateTime.UtcNow.AddMonths(-1)
        };
        db.Users.Add(profileUser);
        await db.SaveChangesAsync();

        db.HealthProfiles.Add(new HealthProfile
        {
            UserId = profileUser.UserId,
            DietarySchoolId = 1,
            Gender = "OTHER",
            HealthGoal = "MAINTAIN",
            RegionPreference = "ALL",
            DateOfBirth = new DateOnly(1995, 1, 1),
            HeightCm = 170,
            CurrentWeightKg = 65,
            BmiValue = 22.5m,
            BmrKcal = 1500,
            TdeeKcal = 2000,
            TargetDailyCalories = 2000,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        mockGoogle.Setup(g => g.ValidateIdTokenAsync("token_profile", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GoogleTokenPayload
            {
                Subject = "google_profile_888",
                Email = "profile.completed@gmail.com",
                EmailVerified = true,
                Name = "Profile Completed"
            });

        // ACT
        var response = await service.LoginWithGoogleAsync(new LoginWithGoogleRequest
        {
            IdToken = "token_profile"
        });

        // ASSERT
        Assert.NotNull(response);
        Assert.True(response.User.IsProfileCompleted);
    }
}
