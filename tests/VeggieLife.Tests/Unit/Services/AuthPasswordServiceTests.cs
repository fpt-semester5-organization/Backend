using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
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
/// Bộ kiểm thử đơn vị cho chức năng Quên mật khẩu (FN-AUTH-11), Đặt lại mật khẩu (FN-AUTH-12),
/// và Đổi mật khẩu (FN-AUTH-13).
/// Kiểm chứng các quy tắc nghiệp vụ BR-AUTH-001, BR-USR-AUTH-01, BR-AUTH-004.
/// </summary>
public class AuthPasswordServiceTests
{
    private class InMemoryRedisService : IRedisService
    {
        private readonly ConcurrentDictionary<string, (object Value, DateTime ExpireAt)> _storage = new();

        public Task<T?> GetAsync<T>(string key, CancellationToken ct = default)
        {
            if (_storage.TryGetValue(key, out var entry))
            {
                if (DateTime.UtcNow <= entry.ExpireAt)
                {
                    return Task.FromResult((T?)entry.Value);
                }
                _storage.TryRemove(key, out _);
            }
            return Task.FromResult<T?>(default);
        }

        public Task SetAsync<T>(string key, T value, TimeSpan? expiry = null, CancellationToken ct = default)
        {
            var expireAt = DateTime.UtcNow.Add(expiry ?? TimeSpan.FromDays(1));
            _storage[key] = (value!, expireAt);
            return Task.CompletedTask;
        }

        public Task RemoveAsync(string key, CancellationToken ct = default)
        {
            _storage.TryRemove(key, out _);
            return Task.CompletedTask;
        }

        public Task<bool> ExistsAsync(string key, CancellationToken ct = default)
        {
            if (_storage.TryGetValue(key, out var entry))
            {
                if (DateTime.UtcNow <= entry.ExpireAt)
                {
                    return Task.FromResult(true);
                }
                _storage.TryRemove(key, out _);
            }
            return Task.FromResult(false);
        }

        public Task<long> IncrementAsync(string key, long value = 1, TimeSpan? expiry = null, CancellationToken ct = default)
        {
            return Task.FromResult(1L);
        }

        public Task<(bool Allowed, long Current, long Remaining)> TryConsumeQuotaAsync(string key, int maxLimit, TimeSpan? expiry = null, CancellationToken ct = default)
        {
            return Task.FromResult((true, 1L, 1L));
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

    private class TestEmailService : IEmailService
    {
        public readonly List<(string Email, string Otp, string Purpose)> SentOtps = new();

        public Task SendOtpEmailAsync(string toEmail, string otpCode, string purpose, CancellationToken ct = default)
        {
            SentOtps.Add((toEmail, otpCode, purpose));
            return Task.CompletedTask;
        }

        public Task SendNotificationEmailAsync(string toEmail, string subject, string htmlBody, CancellationToken ct = default)
        {
            return Task.CompletedTask;
        }
    }

    private static (AppDbContext DbContext, AuthService AuthService, TestEmailService EmailService, InMemoryRedisService RedisService) CreateTestHarness()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        var dbContext = new AppDbContext(options);
        var redisService = new InMemoryRedisService();
        var emailService = new TestEmailService();

        var inMemorySettings = new Dictionary<string, string?>
        {
            { "Jwt:SecretKey", "TestVeryLongAndSuperSecureKey1234567890!@#$%" },
            { "Jwt:Issuer", "VeggieLifeTest" },
            { "Jwt:Audience", "VeggieLifeTestAudience" },
            { "Jwt:AccessTokenExpiryMinutes", "15" },
            { "Jwt:RefreshTokenExpiryDays", "7" }
        };
        var config = new ConfigurationBuilder().AddInMemoryCollection(inMemorySettings).Build();
        var jwtTokenService = new JwtTokenService(config);

        var authService = new AuthService(
            dbContext,
            redisService,
            emailService,
            jwtTokenService,
            NullLogger<AuthService>.Instance);

        return (dbContext, authService, emailService, redisService);
    }

    private static string HashSha256(string raw)
    {
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(raw)));
    }

    // =========================================================================
    // 1. FN-AUTH-11: Quên mật khẩu (ForgotPasswordAsync)
    // =========================================================================

    [Fact]
    public async Task ForgotPassword_WhenEmailActive_SendsOtpAndStoresInRedis()
    {
        // ARRANGE
        var (db, authService, emailService, redis) = CreateTestHarness();
        var email = "active_user@veggielife.vn";
        db.Users.Add(new User
        {
            Email = email,
            FullName = "Nguyễn Văn A",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("OldPass@2026"),
            Role = UserRoles.Member,
            AccountStatus = AccountStatuses.Active
        });
        await db.SaveChangesAsync();

        // ACT
        await authService.ForgotPasswordAsync(new ForgotPasswordRequest { Email = email });

        // ASSERT: Email được gửi và Redis lưu OTP
        Assert.Single(emailService.SentOtps);
        var sent = emailService.SentOtps[0];
        Assert.Equal(email, sent.Email);
        Assert.Equal(OtpPurposes.PasswordReset, sent.Purpose);
        Assert.Matches(@"^\d{6}$", sent.Otp);

        var redisKey = $"otp:{OtpPurposes.PasswordReset}:{email}";
        var cached = await redis.GetAsync<OtpCacheItem>(redisKey);
        Assert.NotNull(cached);
        Assert.Equal(HashSha256(sent.Otp), cached.OtpHash);
        Assert.Equal(OtpPurposes.PasswordReset, cached.Purpose);
    }

    [Fact]
    public async Task ForgotPassword_WhenUserNotFound_ReturnsSilentlyWithoutSendingEmail()
    {
        // ARRANGE: Email không tồn tại
        var (db, authService, emailService, redis) = CreateTestHarness();

        // ACT: Không ném lỗi để chống user enumeration
        await authService.ForgotPasswordAsync(new ForgotPasswordRequest { Email = "nonexistent@veggielife.vn" });

        // ASSERT: Không gửi email, không lưu Redis
        Assert.Empty(emailService.SentOtps);
        var cached = await redis.GetAsync<OtpCacheItem>("otp:PASSWORD_RESET:nonexistent@veggielife.vn");
        Assert.Null(cached);
    }

    [Fact]
    public async Task ForgotPassword_WhenUserNotActive_ReturnsSilentlyWithoutSendingEmail()
    {
        // ARRANGE: Tài khoản ở trạng thái PENDING_ACTIVATION
        var (db, authService, emailService, redis) = CreateTestHarness();
        var email = "pending@veggielife.vn";
        db.Users.Add(new User
        {
            Email = email,
            FullName = "Chưa kích hoạt",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("OldPass@2026"),
            Role = UserRoles.Member,
            AccountStatus = AccountStatuses.PendingActivation
        });
        await db.SaveChangesAsync();

        // ACT
        await authService.ForgotPasswordAsync(new ForgotPasswordRequest { Email = email });

        // ASSERT: Bỏ qua không gửi OTP vì chưa ACTIVE
        Assert.Empty(emailService.SentOtps);
    }

    [Fact]
    public async Task ForgotPassword_WhenOtpLocked_Throws429ErrAuthOtpLocked()
    {
        // ARRANGE: Email đang trong 15 phút khóa
        var (db, authService, emailService, redis) = CreateTestHarness();
        var email = "locked@veggielife.vn";
        await redis.SetAsync($"otp_lock:{email}", "LOCKED", TimeSpan.FromMinutes(15));

        // ACT & ASSERT
        var ex = await Assert.ThrowsAsync<AppException>(() =>
            authService.ForgotPasswordAsync(new ForgotPasswordRequest { Email = email }));

        Assert.Equal(429, ex.StatusCode);
        Assert.Equal(ErrorCodes.ErrAuthOtpLocked, ex.ErrorCode);
    }

    // =========================================================================
    // 2. FN-AUTH-12: Đặt lại mật khẩu bằng OTP (ResetPasswordAsync)
    // =========================================================================

    [Fact]
    public async Task ResetPassword_WhenSuccessful_UpdatesPasswordHash_UnlocksAccount_RevokesAllSessions()
    {
        // ARRANGE
        var (db, authService, emailService, redis) = CreateTestHarness();
        var email = "reset_test@veggielife.vn";
        var oldPassword = "OldPassword@2026";
        var newPassword = "NewSecurePassword@2026";
        var otpCode = "654321";

        var user = new User
        {
            Email = email,
            FullName = "Test Reset",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(oldPassword),
            Role = UserRoles.Member,
            AccountStatus = AccountStatuses.Active,
            FailedLoginCount = 4,
            LockedUntil = DateTime.UtcNow.AddMinutes(10)
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();

        // Tạo 2 refresh token giả lập của user trên Redis
        var sessionHash1 = HashSha256("old_refresh_token_1");
        var sessionHash2 = HashSha256("old_refresh_token_2");
        await redis.SetAsync($"refresh:{sessionHash1}", new RefreshTokenSession { UserId = user.UserId, TokenHash = sessionHash1 });
        await redis.SetAsync($"refresh:{sessionHash2}", new RefreshTokenSession { UserId = user.UserId, TokenHash = sessionHash2 });
        await redis.SetAsync($"user_sessions:{user.UserId}", new List<string> { sessionHash1, sessionHash2 });

        // Lưu OTP vào Redis
        var otpKey = $"otp:{OtpPurposes.PasswordReset}:{email}";
        await redis.SetAsync(otpKey, new OtpCacheItem
        {
            OtpHash = HashSha256(otpCode),
            FailedAttempts = 0,
            CreatedAt = DateTime.UtcNow,
            Purpose = OtpPurposes.PasswordReset
        });

        // ACT
        await authService.ResetPasswordAsync(new ResetPasswordRequest
        {
            Email = email,
            Otp = otpCode,
            NewPassword = newPassword
        });

        // ASSERT 1: Mật khẩu mới có hiệu lực, mật khẩu cũ KHÔNG CÒN DÙNG ĐƯỢC
        var updatedUser = await db.Users.FirstAsync(u => u.Email == email);
        Assert.False(BCrypt.Net.BCrypt.Verify(oldPassword, updatedUser.PasswordHash), "Mật khẩu cũ không được phép xác thực thành công.");
        Assert.True(BCrypt.Net.BCrypt.Verify(newPassword, updatedUser.PasswordHash), "Mật khẩu mới phải xác thực thành công.");

        // ASSERT 2: Tài khoản được mở khóa
        Assert.Equal(0, updatedUser.FailedLoginCount);
        Assert.Null(updatedUser.LockedUntil);

        // ASSERT 3: Mọi session cũ trên Redis bị thu hồi hoàn toàn (BR-AUTH-004)
        Assert.Null(await redis.GetAsync<RefreshTokenSession>($"refresh:{sessionHash1}"));
        Assert.Null(await redis.GetAsync<RefreshTokenSession>($"refresh:{sessionHash2}"));
        Assert.Null(await redis.GetAsync<List<string>>($"user_sessions:{user.UserId}"));

        // ASSERT 4: OTP đã bị xóa khỏi Redis
        Assert.Null(await redis.GetAsync<OtpCacheItem>(otpKey));

        // ASSERT 5: Có ghi AuditLog PASSWORD_RESET
        var audit = await db.AuditLogs.FirstOrDefaultAsync(a => a.Action == "PASSWORD_RESET" && a.ActorUserId == user.UserId);
        Assert.NotNull(audit);
    }

    [Fact]
    public async Task ResetPassword_WhenWeakPassword_Throws400ErrAuthWeakPassword()
    {
        // ARRANGE
        var (db, authService, _, redis) = CreateTestHarness();

        // ACT & ASSERT: Mật khẩu thiếu ký tự đặc biệt
        var ex = await Assert.ThrowsAsync<AppException>(() =>
            authService.ResetPasswordAsync(new ResetPasswordRequest
            {
                Email = "test@veggielife.vn",
                Otp = "123456",
                NewPassword = "WeakPassword123"
            }));

        Assert.Equal(400, ex.StatusCode);
        Assert.Equal(ErrorCodes.ErrAuthWeakPassword, ex.ErrorCode);
    }

    [Fact]
    public async Task ResetPassword_WhenOtpExpiredOrMissing_Throws400ErrAuthOtpExpired()
    {
        // ARRANGE: Không có OTP trong Redis
        var (db, authService, _, _) = CreateTestHarness();

        // ACT & ASSERT
        var ex = await Assert.ThrowsAsync<AppException>(() =>
            authService.ResetPasswordAsync(new ResetPasswordRequest
            {
                Email = "test@veggielife.vn",
                Otp = "123456",
                NewPassword = "ValidNewPassword@2026"
            }));

        Assert.Equal(400, ex.StatusCode);
        Assert.Equal(ErrorCodes.ErrAuthOtpExpired, ex.ErrorCode);
    }

    [Fact]
    public async Task ResetPassword_WhenOtpIncorrect_Throws400ErrAuthOtpIncorrect()
    {
        // ARRANGE
        var (db, authService, _, redis) = CreateTestHarness();
        var email = "otp_inc@veggielife.vn";
        var otpKey = $"otp:{OtpPurposes.PasswordReset}:{email}";
        await redis.SetAsync(otpKey, new OtpCacheItem
        {
            OtpHash = HashSha256("123456"),
            FailedAttempts = 1,
            CreatedAt = DateTime.UtcNow,
            Purpose = OtpPurposes.PasswordReset
        });

        // ACT & ASSERT: Nhập sai mã OTP
        var ex = await Assert.ThrowsAsync<AppException>(() =>
            authService.ResetPasswordAsync(new ResetPasswordRequest
            {
                Email = email,
                Otp = "999999",
                NewPassword = "ValidNewPassword@2026"
            }));

        Assert.Equal(400, ex.StatusCode);
        Assert.Equal(ErrorCodes.ErrAuthOtpIncorrect, ex.ErrorCode);

        // Kiểm tra FailedAttempts tăng lên 2
        var cached = await redis.GetAsync<OtpCacheItem>(otpKey);
        Assert.NotNull(cached);
        Assert.Equal(2, cached.FailedAttempts);
    }

    [Fact]
    public async Task ResetPassword_WhenOtpIncorrect5Times_LocksAccountFor15Minutes()
    {
        // ARRANGE: Đã sai 4 lần trước đó
        var (db, authService, _, redis) = CreateTestHarness();
        var email = "lock_target@veggielife.vn";
        var otpKey = $"otp:{OtpPurposes.PasswordReset}:{email}";
        await redis.SetAsync(otpKey, new OtpCacheItem
        {
            OtpHash = HashSha256("123456"),
            FailedAttempts = 4,
            CreatedAt = DateTime.UtcNow,
            Purpose = OtpPurposes.PasswordReset
        });

        // ACT: Lần sai thứ 5
        var ex = await Assert.ThrowsAsync<AppException>(() =>
            authService.ResetPasswordAsync(new ResetPasswordRequest
            {
                Email = email,
                Otp = "000000",
                NewPassword = "ValidNewPassword@2026"
            }));

        // ASSERT: Bị khóa 429 ERR_AUTH_OTP_LOCKED
        Assert.Equal(429, ex.StatusCode);
        Assert.Equal(ErrorCodes.ErrAuthOtpLocked, ex.ErrorCode);

        // OTP bị xóa và lockKey 15 phút được kích hoạt
        Assert.Null(await redis.GetAsync<OtpCacheItem>(otpKey));
        Assert.True(await redis.ExistsAsync($"otp_lock:{email}"));
    }

    // =========================================================================
    // 3. FN-AUTH-13: Đổi mật khẩu (ChangePasswordAsync)
    // =========================================================================

    [Fact]
    public async Task ChangePassword_WhenSuccessful_UpdatesHash_RevokesOtherSessions_KeepsCurrentSession()
    {
        // ARRANGE
        var (db, authService, _, redis) = CreateTestHarness();
        var oldPassword = "CurrentPassword@2026";
        var newPassword = "BrandNewPassword@2026";
        var currentRefreshToken = "current_device_refresh_token";
        var otherRefreshToken = "other_device_refresh_token";

        var user = new User
        {
            Email = "member_change@veggielife.vn",
            FullName = "Member Change",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(oldPassword),
            Role = UserRoles.Member,
            AccountStatus = AccountStatuses.Active
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var currentHash = HashSha256(currentRefreshToken);
        var otherHash = HashSha256(otherRefreshToken);

        await redis.SetAsync($"refresh:{currentHash}", new RefreshTokenSession { UserId = user.UserId, TokenHash = currentHash });
        await redis.SetAsync($"refresh:{otherHash}", new RefreshTokenSession { UserId = user.UserId, TokenHash = otherHash });
        await redis.SetAsync($"user_sessions:{user.UserId}", new List<string> { currentHash, otherHash });

        // ACT
        await authService.ChangePasswordAsync(user.UserId, new ChangePasswordRequest
        {
            CurrentPassword = oldPassword,
            NewPassword = newPassword
        }, currentRefreshToken);

        // ASSERT 1: Mật khẩu mới có hiệu lực, mật khẩu cũ KHÔNG CÒN DÙNG ĐƯỢC
        var updated = await db.Users.FindAsync(user.UserId);
        Assert.NotNull(updated);
        Assert.False(BCrypt.Net.BCrypt.Verify(oldPassword, updated.PasswordHash), "Mật khẩu cũ không còn dùng được.");
        Assert.True(BCrypt.Net.BCrypt.Verify(newPassword, updated.PasswordHash), "Mật khẩu mới phải hợp lệ.");

        // ASSERT 2: Phiên hiện tại còn giữ, phiên khác bị thu hồi (BR-AUTH-004)
        Assert.NotNull(await redis.GetAsync<RefreshTokenSession>($"refresh:{currentHash}"));
        Assert.Null(await redis.GetAsync<RefreshTokenSession>($"refresh:{otherHash}"));

        var sessions = await redis.GetAsync<List<string>>($"user_sessions:{user.UserId}");
        Assert.NotNull(sessions);
        Assert.Single(sessions);
        Assert.Contains(currentHash, sessions);

        // ASSERT 3: Ghi AuditLog PASSWORD_CHANGED
        var audit = await db.AuditLogs.FirstOrDefaultAsync(a => a.Action == "PASSWORD_CHANGED" && a.ActorUserId == user.UserId);
        Assert.NotNull(audit);
    }

    [Fact]
    public async Task ChangePassword_WhenCurrentPasswordIncorrect_Throws401ErrAuthInvalidCredentials()
    {
        // ARRANGE
        var (db, authService, _, _) = CreateTestHarness();
        var user = new User
        {
            Email = "test_wrong_pass@veggielife.vn",
            FullName = "Test User",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("CorrectPassword@2026"),
            Role = UserRoles.Member,
            AccountStatus = AccountStatuses.Active
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();

        // ACT & ASSERT: Mật khẩu hiện tại sai
        var ex = await Assert.ThrowsAsync<AppException>(() =>
            authService.ChangePasswordAsync(user.UserId, new ChangePasswordRequest
            {
                CurrentPassword = "WrongPassword@9999",
                NewPassword = "NewValidPassword@2026"
            }));

        Assert.Equal(401, ex.StatusCode);
        Assert.Equal(ErrorCodes.ErrAuthInvalidCredentials, ex.ErrorCode);
    }

    [Fact]
    public async Task ChangePassword_WhenNewPasswordSameAsOld_Throws400ErrAuthWeakPassword()
    {
        // ARRANGE
        var (db, authService, _, _) = CreateTestHarness();
        var currentPass = "CurrentPassword@2026";
        var user = new User
        {
            Email = "test_same_pass@veggielife.vn",
            FullName = "Test User",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(currentPass),
            Role = UserRoles.Member,
            AccountStatus = AccountStatuses.Active
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();

        // ACT & ASSERT: Mật khẩu mới trùng với mật khẩu cũ
        var ex = await Assert.ThrowsAsync<AppException>(() =>
            authService.ChangePasswordAsync(user.UserId, new ChangePasswordRequest
            {
                CurrentPassword = currentPass,
                NewPassword = currentPass
            }));

        Assert.Equal(400, ex.StatusCode);
        Assert.Equal(ErrorCodes.ErrAuthWeakPassword, ex.ErrorCode);
    }

    [Fact]
    public async Task ChangePassword_WhenNewPasswordWeak_Throws400ErrAuthWeakPassword()
    {
        // ARRANGE
        var (db, authService, _, _) = CreateTestHarness();
        var currentPass = "CurrentPassword@2026";
        var user = new User
        {
            Email = "test_weak_new@veggielife.vn",
            FullName = "Test User",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(currentPass),
            Role = UserRoles.Member,
            AccountStatus = AccountStatuses.Active
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();

        // ACT & ASSERT: Mật khẩu mới thiếu số và ký tự đặc biệt
        var ex = await Assert.ThrowsAsync<AppException>(() =>
            authService.ChangePasswordAsync(user.UserId, new ChangePasswordRequest
            {
                CurrentPassword = currentPass,
                NewPassword = "short"
            }));

        Assert.Equal(400, ex.StatusCode);
        Assert.Equal(ErrorCodes.ErrAuthWeakPassword, ex.ErrorCode);
    }

    [Fact]
    public async Task ChangePassword_WhenGoogleUserWithoutPasswordHash_Throws400ErrValidation()
    {
        // ARRANGE: Tài khoản đăng nhập Google (PasswordHash null)
        var (db, authService, _, _) = CreateTestHarness();
        var user = new User
        {
            Email = "google_user@gmail.com",
            FullName = "Google User",
            PasswordHash = null,
            GoogleId = "google_123456",
            Role = UserRoles.Member,
            AccountStatus = AccountStatuses.Active
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();

        // ACT & ASSERT
        var ex = await Assert.ThrowsAsync<AppException>(() =>
            authService.ChangePasswordAsync(user.UserId, new ChangePasswordRequest
            {
                CurrentPassword = "AnyPassword@123",
                NewPassword = "NewValidPassword@2026"
            }));

        Assert.Equal(400, ex.StatusCode);
        Assert.Equal(ErrorCodes.ErrValidation, ex.ErrorCode);
    }
}
