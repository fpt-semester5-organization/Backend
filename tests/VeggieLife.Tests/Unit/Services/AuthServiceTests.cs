using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Security.Claims;
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
/// Bộ kiểm thử đơn vị cho chức năng Đăng ký tài khoản và Xác thực OTP (FN-AUTH-04, FN-AUTH-05, FN-AUTH-06).
/// </summary>
public class AuthServiceTests
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
            long current = 0;
            if (_storage.TryGetValue(key, out var entry))
            {
                if (entry.Value is long l) current = l;
                else if (long.TryParse(entry.Value.ToString(), out var parsed)) current = parsed;
            }
            current += value;
            var expireAt = DateTime.UtcNow.Add(expiry ?? TimeSpan.FromMinutes(5));
            _storage[key] = (current, expireAt);
            return Task.FromResult(current);
        }

        public Task<(bool Allowed, long Current, long Remaining)> TryConsumeQuotaAsync(string key, int maxLimit, TimeSpan? expiry = null, CancellationToken ct = default)
        {
            lock (_storage)
            {
                long current = 0;
                if (_storage.TryGetValue(key, out var entry))
                {
                    if (entry.Value is long l) current = l;
                    else if (long.TryParse(entry.Value.ToString(), out var parsed)) current = parsed;
                }

                if (current >= maxLimit)
                {
                    return Task.FromResult((false, current, 0L));
                }

                current++;
                var expireAt = DateTime.UtcNow.Add(expiry ?? TimeSpan.FromDays(1));
                _storage[key] = (current, expireAt);
                return Task.FromResult((true, current, (long)(maxLimit - current)));
            }
        }

        public Task<(bool Allowed, bool IsBanned, long CurrentCount, TimeSpan? RetryAfter)> CheckIpRateLimitAsync(string clientIp, int maxRequestsPerMinute = 10, int banDurationMinutes = 10, CancellationToken ct = default)
        {
            lock (_storage)
            {
                var banKey = $"ban:{clientIp}";
                if (_storage.TryGetValue(banKey, out var banEntry) && DateTime.UtcNow <= banEntry.ExpireAt)
                {
                    return Task.FromResult((false, true, 0L, (TimeSpan?)(banEntry.ExpireAt - DateTime.UtcNow)));
                }

                var countKey = $"count:{clientIp}";
                long count = 0;
                if (_storage.TryGetValue(countKey, out var countEntry) && DateTime.UtcNow <= countEntry.ExpireAt)
                {
                    if (countEntry.Value is long l) count = l;
                    else if (long.TryParse(countEntry.Value.ToString(), out var parsed)) count = parsed;
                }

                count++;
                _storage[countKey] = (count, DateTime.UtcNow.AddMinutes(1));

                if (count > maxRequestsPerMinute)
                {
                    var banUntil = DateTime.UtcNow.AddMinutes(banDurationMinutes);
                    _storage[banKey] = ("banned", banUntil);
                    return Task.FromResult((false, true, count, (TimeSpan?)TimeSpan.FromMinutes(banDurationMinutes)));
                }

                return Task.FromResult((true, false, count, (TimeSpan?)null));
            }
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
            NullLogger<AuthService>.Instance);

        return (dbContext, authService, emailService, redisService);
    }

    [Theory]
    [InlineData("short1!")] // < 8 ký tự
    [InlineData("nouppercase123!")] // Không có hoa
    [InlineData("NOLOWERCASE123!")] // Không có thường
    [InlineData("NoSpecialChar123")] // Không có ký tự đặc biệt
    [InlineData("NoDigitsInPassword!")] // Không có số
    public async Task Register_WithWeakPassword_ThrowsAppException_WeakPassword(string weakPassword)
    {
        var (_, authService, _, _) = CreateTestHarness();

        var request = new RegisterRequest
        {
            FullName = "Nguyễn Văn Test",
            Email = "test@gmail.com",
            Password = weakPassword,
            ConfirmPassword = weakPassword
        };

        var ex = await Assert.ThrowsAsync<AppException>(() => authService.RegisterAsync(request));
        Assert.Equal(400, ex.StatusCode);
        Assert.Equal(ErrorCodes.ErrAuthWeakPassword, ex.ErrorCode);
    }

    [Fact]
    public async Task Register_WithValidInfo_CreatesUserInPendingActivation_AndSendsOtp()
    {
        var (dbContext, authService, emailService, redisService) = CreateTestHarness();

        var request = new RegisterRequest
        {
            FullName = "Trần An",
            Email = "an.tran@example.com",
            Password = "StrongPassword@2026",
            ConfirmPassword = "StrongPassword@2026"
        };

        var response = await authService.RegisterAsync(request);

        Assert.Equal("an.tran@example.com", response.Email);
        Assert.Equal(300, response.OtpExpiresIn);

        // Kiểm tra User trong CSDL
        var user = await dbContext.Users.FirstOrDefaultAsync(u => u.Email == "an.tran@example.com");
        Assert.NotNull(user);
        Assert.Equal(AccountStatuses.PendingActivation, user.AccountStatus);
        Assert.Equal(UserRoles.Member, user.Role);
        Assert.True(BCrypt.Net.BCrypt.Verify("StrongPassword@2026", user.PasswordHash));

        // Kiểm tra OTP đã gửi qua email và lưu Redis
        Assert.Single(emailService.SentOtps);
        var sentOtp = emailService.SentOtps[0].Otp;
        Assert.Equal(6, sentOtp.Length);

        var redisItem = await redisService.GetAsync<OtpCacheItem>("otp:EMAIL_VERIFY:an.tran@example.com");
        Assert.NotNull(redisItem);
        Assert.Equal(0, redisItem.FailedAttempts);
    }

    [Fact]
    public async Task Register_WithAlreadyActiveEmail_ThrowsConflict()
    {
        var (dbContext, authService, _, _) = CreateTestHarness();

        dbContext.Users.Add(new User
        {
            FullName = "Người Cũ",
            Email = "existing@example.com",
            Role = UserRoles.Member,
            AccountStatus = AccountStatuses.Active,
            CreatedAt = DateTime.UtcNow
        });
        await dbContext.SaveChangesAsync();

        var request = new RegisterRequest
        {
            FullName = "Người Mới Trùng Email",
            Email = "existing@example.com",
            Password = "StrongPassword@2026",
            ConfirmPassword = "StrongPassword@2026"
        };

        var ex = await Assert.ThrowsAsync<AppException>(() => authService.RegisterAsync(request));
        Assert.Equal(409, ex.StatusCode);
        Assert.Equal(ErrorCodes.ErrAuthEmailExists, ex.ErrorCode);
    }

    [Fact]
    public async Task VerifyOtp_WithIncorrectOtp_DecrementsAttempts_AndLocksAfter5Times()
    {
        var (_, authService, emailService, redisService) = CreateTestHarness();

        // 1. Đăng ký trước để có OTP
        var reg = new RegisterRequest
        {
            FullName = "Lê Thử Sai",
            Email = "wrong@example.com",
            Password = "StrongPassword@2026",
            ConfirmPassword = "StrongPassword@2026"
        };
        await authService.RegisterAsync(reg);
        var correctOtp = emailService.SentOtps[0].Otp;

        // 2. Nhập sai 4 lần đầu: Mong đợi 400 ERR_AUTH_OTP_INCORRECT
        for (int i = 1; i <= 4; i++)
        {
            var ex = await Assert.ThrowsAsync<AppException>(() => authService.VerifyOtpAsync(new VerifyOtpRequest
            {
                Email = "wrong@example.com",
                Otp = "000000" // sai
            }));

            Assert.Equal(400, ex.StatusCode);
            Assert.Equal(ErrorCodes.ErrAuthOtpIncorrect, ex.ErrorCode);
        }

        // 3. Nhập sai lần thứ 5: Khóa 15 phút, trả về HTTP 429 ERR_AUTH_OTP_LOCKED
        var lockEx = await Assert.ThrowsAsync<AppException>(() => authService.VerifyOtpAsync(new VerifyOtpRequest
        {
            Email = "wrong@example.com",
            Otp = "000000" // sai lần 5
        }));

        Assert.Equal(429, lockEx.StatusCode);
        Assert.Equal(ErrorCodes.ErrAuthOtpLocked, lockEx.ErrorCode);

        // 4. Kiểm tra mã OTP cũ đã bị hủy và cờ khóa 15 phút tồn tại trên Redis
        var otpInRedis = await redisService.ExistsAsync("otp:EMAIL_VERIFY:wrong@example.com");
        Assert.False(otpInRedis);

        var lockInRedis = await redisService.ExistsAsync("otp_lock:wrong@example.com");
        Assert.True(lockInRedis);

        // 5. Ngay cả khi sau đó nhập đúng OTP, hệ thống vẫn từ chối vì đang bị khóa 15 phút
        var attemptAfterLock = await Assert.ThrowsAsync<AppException>(() => authService.VerifyOtpAsync(new VerifyOtpRequest
        {
            Email = "wrong@example.com",
            Otp = correctOtp
        }));
        Assert.Equal(429, attemptAfterLock.StatusCode);
        Assert.Equal(ErrorCodes.ErrAuthOtpLocked, attemptAfterLock.ErrorCode);
    }

    [Fact]
    public async Task VerifyOtp_WithCorrectOtp_ActivatesAccount_AndReturnsTokens()
    {
        var (dbContext, authService, emailService, redisService) = CreateTestHarness();

        var reg = new RegisterRequest
        {
            FullName = "Nguyễn Thành Công",
            Email = "success@example.com",
            Password = "StrongPassword@2026",
            ConfirmPassword = "StrongPassword@2026"
        };
        await authService.RegisterAsync(reg);
        var correctOtp = emailService.SentOtps[0].Otp;

        // Xác thực đúng OTP
        var verifyRes = await authService.VerifyOtpAsync(new VerifyOtpRequest
        {
            Email = "success@example.com",
            Otp = correctOtp
        });

        Assert.NotNull(verifyRes);
        Assert.NotEmpty(verifyRes.AccessToken);
        Assert.Equal(AccountStatuses.Active, verifyRes.User.AccountStatus);
        Assert.Equal("success@example.com", verifyRes.User.Email);
        Assert.Equal("/onboarding/health-profile", verifyRes.RedirectUrl);

        // Kiểm tra User trong CSDL đã chuyển sang ACTIVE
        var userInDb = await dbContext.Users.FirstOrDefaultAsync(u => u.Email == "success@example.com");
        Assert.NotNull(userInDb);
        Assert.Equal(AccountStatuses.Active, userInDb.AccountStatus);

        // Kiểm tra OTP đã được xóa khỏi Redis sau khi kích hoạt thành công
        var otpExists = await redisService.ExistsAsync("otp:EMAIL_VERIFY:success@example.com");
        Assert.False(otpExists);
    }

    [Fact]
    public async Task ResendOtp_WhenEmailNotExists_DoesNotLeakExistence_AndCompletesSilently()
    {
        var (_, authService, emailService, _) = CreateTestHarness();

        // Gửi lại OTP cho email chưa từng đăng ký
        await authService.ResendOtpAsync(new ResendOtpRequest
        {
            Email = "ghost@example.com",
            Purpose = OtpPurposes.EmailVerify
        });

        // Không gửi email nào và không ném lỗi ra ngoài (Chống User Enumeration)
        Assert.Empty(emailService.SentOtps);
    }

    [Fact]
    public async Task ResendOtp_WhenValidPendingUser_IssuesNewOtp()
    {
        var (_, authService, emailService, redisService) = CreateTestHarness();

        var reg = new RegisterRequest
        {
            FullName = "Trần Gửi Lại",
            Email = "resend@example.com",
            Password = "StrongPassword@2026",
            ConfirmPassword = "StrongPassword@2026"
        };
        await authService.RegisterAsync(reg);
        var firstOtp = emailService.SentOtps[0].Otp;

        // Gọi gửi lại OTP
        await authService.ResendOtpAsync(new ResendOtpRequest
        {
            Email = "resend@example.com",
            Purpose = OtpPurposes.EmailVerify
        });

        Assert.Equal(2, emailService.SentOtps.Count);
        var secondOtp = emailService.SentOtps[1].Otp;
        Assert.Equal(6, secondOtp.Length);

        // Xác thực bằng mã mới thành công
        var verifyRes = await authService.VerifyOtpAsync(new VerifyOtpRequest
        {
            Email = "resend@example.com",
            Otp = secondOtp
        });
        Assert.Equal(AccountStatuses.Active, verifyRes.User.AccountStatus);
    }
}
