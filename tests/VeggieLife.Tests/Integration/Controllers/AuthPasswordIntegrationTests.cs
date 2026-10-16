using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
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
using VeggieLife.BLL.Services.Cache;
using VeggieLife.BLL.Services.Email;
using VeggieLife.DAL.Context;
using VeggieLife.DAL.Entities;
using Xunit;

namespace VeggieLife.Tests.Integration.Controllers;

/// <summary>
/// Bộ kiểm thử tích hợp đầu cuối (End-to-End Integration Tests) cho:
/// - FN-AUTH-11: Quên mật khẩu (gửi OTP)
/// - FN-AUTH-12: Đặt lại mật khẩu bằng OTP (thu hồi phiên cũ, mật khẩu cũ vô hiệu)
/// - FN-AUTH-13: Đổi mật khẩu (xác minh mật khẩu cũ, mật khẩu mới mạnh, thu hồi các phiên khác)
/// </summary>
public class AuthPasswordIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
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
    private readonly string _dbName = "AuthPasswordIntegrationDb_" + Guid.NewGuid();
    private const string JwtSecret = "VeggieLifeSecretKeyForDevelopmentMustBe32CharsLong!";

    public AuthPasswordIntegrationTests(WebApplicationFactory<Program> factory)
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

                var emailDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IEmailService));
                if (emailDescriptor != null) services.Remove(emailDescriptor);

                services.AddSingleton<IEmailService, EmailServiceStub>();
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
                CreatedAt = DateTime.UtcNow,
                FailedLoginCount = 0
            };
            db.Users.Add(user);
            await db.SaveChangesAsync();
        }
        else
        {
            existing.PasswordHash = BCrypt.Net.BCrypt.HashPassword(password, workFactor: 4);
            existing.AccountStatus = status;
            await db.SaveChangesAsync();
        }
    }

    private static string ExtractRefreshToken(HttpResponseMessage response)
    {
        if (response.Headers.TryGetValues("Set-Cookie", out var cookies))
        {
            foreach (var cookie in cookies)
            {
                if (cookie.Contains("refresh_token="))
                {
                    var seg = cookie.Split(';')[0];
                    var idx = seg.IndexOf('=');
                    if (idx > 0)
                    {
                        return seg[(idx + 1)..];
                    }
                }
            }
        }
        return string.Empty;
    }

    // =========================================================================
    // 1. FN-AUTH-11: POST /api/v1/auth/forgot-password
    // =========================================================================

    [Fact]
    public async Task PostForgotPassword_WhenEmailExists_Returns200AndSendsOtp()
    {
        var client = _factory.CreateClient();
        var email = "forgot_success@veggielife.vn";
        await SeedUserAsync(email, "InitialPass@2026");

        var response = await client.PostAsJsonAsync("/api/v1/auth/forgot-password", new ForgotPasswordRequest
        {
            Email = email
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<ApiResponse<object?>>();
        Assert.NotNull(body);
        Assert.Equal(200, body.StatusCode);
        Assert.Equal("Nếu email tồn tại, mã OTP đặt lại mật khẩu đã được gửi.", body.Message);
        Assert.Null(body.Data);

        // Kiểm tra mã OTP được lưu vào Redis
        var redisKey = $"otp:PASSWORD_RESET:{email}";
        Assert.True(await _redis.ExistsAsync(redisKey));
    }

    [Fact]
    public async Task PostForgotPassword_WhenEmailInvalidFormat_Returns400ErrValidation()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/v1/auth/forgot-password", new
        {
            email = "not-an-email"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var errBody = await response.Content.ReadFromJsonAsync<ApiErrorResponse>();
        Assert.NotNull(errBody);
        Assert.Equal(ErrorCodes.ErrValidation, errBody.ErrorCode);
    }

    [Fact]
    public async Task PostForgotPassword_WhenEmailNotFound_Returns200GenericMessage()
    {
        var client = _factory.CreateClient();
        var email = "not_found_user@veggielife.vn";

        var response = await client.PostAsJsonAsync("/api/v1/auth/forgot-password", new ForgotPasswordRequest
        {
            Email = email
        });

        // Chống user enumeration: vẫn trả 200
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<object?>>();
        Assert.NotNull(body);
        Assert.Equal(200, body.StatusCode);
        Assert.Equal("Nếu email tồn tại, mã OTP đặt lại mật khẩu đã được gửi.", body.Message);

        // Không có OTP trong Redis
        var redisKey = $"otp:PASSWORD_RESET:{email}";
        Assert.False(await _redis.ExistsAsync(redisKey));
    }

    // =========================================================================
    // 2. FN-AUTH-12: POST /api/v1/auth/reset-password
    // =========================================================================

    [Fact]
    public async Task PostResetPassword_EndToEnd_OldPasswordFails_SessionsRevoked()
    {
        var client = _factory.CreateClient();
        var email = "reset_e2e@veggielife.vn";
        var oldPassword = "OldPassword@2026";
        var newPassword = "BrandNewPassword@2026";

        await SeedUserAsync(email, oldPassword);

        // 1. Đăng nhập để tạo phiên và nhận Refresh Token
        var loginRes = await client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest
        {
            Email = email,
            Password = oldPassword
        });
        Assert.Equal(HttpStatusCode.OK, loginRes.StatusCode);
        var oldRefreshToken = ExtractRefreshToken(loginRes);
        Assert.NotEmpty(oldRefreshToken);

        // 2. Gọi forgot-password để sinh mã OTP
        var forgotRes = await client.PostAsJsonAsync("/api/v1/auth/forgot-password", new ForgotPasswordRequest { Email = email });
        Assert.Equal(HttpStatusCode.OK, forgotRes.StatusCode);

        // Lấy OTP từ Dev stub endpoint
        var devOtpRes = await client.GetAsync($"/api/v1/auth/dev-latest-otp?email={email}");
        Assert.Equal(HttpStatusCode.OK, devOtpRes.StatusCode);
        var devOtpJson = await devOtpRes.Content.ReadFromJsonAsync<JsonElement>();
        var otpCode = devOtpJson.GetProperty("data").GetProperty("otp").GetString()!;
        Assert.Matches(@"^\d{6}$", otpCode);

        // 3. Đặt lại mật khẩu bằng mã OTP
        var resetRes = await client.PostAsJsonAsync("/api/v1/auth/reset-password", new ResetPasswordRequest
        {
            Email = email,
            Otp = otpCode,
            NewPassword = newPassword
        });

        Assert.Equal(HttpStatusCode.OK, resetRes.StatusCode);
        var resetBody = await resetRes.Content.ReadFromJsonAsync<ApiResponse<object?>>();
        Assert.NotNull(resetBody);
        Assert.Equal("Đặt lại mật khẩu thành công. Vui lòng đăng nhập lại.", resetBody.Message);

        // 4. KIỂM CHỨNG: Mật khẩu cũ KHÔNG CÒN DÙNG ĐƯỢC
        var failLoginRes = await client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest
        {
            Email = email,
            Password = oldPassword
        });
        Assert.Equal(HttpStatusCode.Unauthorized, failLoginRes.StatusCode);

        // 5. KIỂM CHỨNG: Mật khẩu mới ĐĂNG NHẬP THÀNH CÔNG
        var successLoginRes = await client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest
        {
            Email = email,
            Password = newPassword
        });
        Assert.Equal(HttpStatusCode.OK, successLoginRes.StatusCode);

        // 6. KIỂM CHỨNG: Phiên refresh token cũ BỊ THU HỒI HOÀN TOÀN
        var refreshClient = _factory.CreateClient();
        var refreshReq = new HttpRequestMessage(HttpMethod.Post, "/api/v1/auth/refresh-token");
        refreshReq.Headers.Add("X-Refresh-Token", oldRefreshToken);
        var refreshRes = await refreshClient.SendAsync(refreshReq);
        Assert.Equal(HttpStatusCode.Unauthorized, refreshRes.StatusCode);
    }

    [Fact]
    public async Task PostResetPassword_WhenWeakPassword_Returns400ErrAuthWeakPassword()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/v1/auth/reset-password", new ResetPasswordRequest
        {
            Email = "any@veggielife.vn",
            Otp = "123456",
            NewPassword = "weak"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var errBody = await response.Content.ReadFromJsonAsync<ApiErrorResponse>();
        Assert.NotNull(errBody);
        Assert.Equal(ErrorCodes.ErrAuthWeakPassword, errBody.ErrorCode);
    }

    [Fact]
    public async Task PostResetPassword_WhenOtpIncorrect_Returns400ErrAuthOtpIncorrect()
    {
        var client = _factory.CreateClient();
        var email = "test_wrong_otp@veggielife.vn";
        await SeedUserAsync(email, "CurrentPass@2026");

        // Gọi forgot password để sinh OTP
        await client.PostAsJsonAsync("/api/v1/auth/forgot-password", new ForgotPasswordRequest { Email = email });

        // Nhập sai OTP
        var response = await client.PostAsJsonAsync("/api/v1/auth/reset-password", new ResetPasswordRequest
        {
            Email = email,
            Otp = "000000",
            NewPassword = "NewValidPassword@2026"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var errBody = await response.Content.ReadFromJsonAsync<ApiErrorResponse>();
        Assert.NotNull(errBody);
        Assert.Equal(ErrorCodes.ErrAuthOtpIncorrect, errBody.ErrorCode);
    }

    // =========================================================================
    // 3. FN-AUTH-13: PUT /api/v1/auth/password
    // =========================================================================

    [Fact]
    public async Task PutChangePassword_EndToEnd_OldPasswordFails_OtherSessionsRevoked()
    {
        var client1 = _factory.CreateClient();
        var client2 = _factory.CreateClient();
        var email = "change_e2e@veggielife.vn";
        var oldPassword = "InitialPassword@2026";
        var newPassword = "UpdatedPassword@2026";

        await SeedUserAsync(email, oldPassword);

        // 1. Thiết bị 1 đăng nhập
        var loginRes1 = await client1.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest
        {
            Email = email,
            Password = oldPassword
        });
        var body1 = await loginRes1.Content.ReadFromJsonAsync<ApiResponse<LoginResponse>>();
        var token1 = body1!.Data!.AccessToken;
        var refreshToken1 = ExtractRefreshToken(loginRes1);
        Assert.NotEmpty(refreshToken1);

        // 2. Thiết bị 2 đăng nhập
        var loginRes2 = await client2.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest
        {
            Email = email,
            Password = oldPassword
        });
        var refreshToken2 = ExtractRefreshToken(loginRes2);
        Assert.NotEmpty(refreshToken2);

        // 3. Thiết bị 1 thực hiện đổi mật khẩu (kèm header X-Refresh-Token của thiết bị 1)
        var changeReq = new HttpRequestMessage(HttpMethod.Put, "/api/v1/auth/password");
        changeReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token1);
        changeReq.Headers.Add("X-Refresh-Token", refreshToken1);
        changeReq.Content = JsonContent.Create(new ChangePasswordRequest
        {
            CurrentPassword = oldPassword,
            NewPassword = newPassword
        });

        var changeRes = await client1.SendAsync(changeReq);
        Assert.Equal(HttpStatusCode.OK, changeRes.StatusCode);
        var changeBody = await changeRes.Content.ReadFromJsonAsync<ApiResponse<object?>>();
        Assert.NotNull(changeBody);
        Assert.Equal("Đổi mật khẩu thành công.", changeBody.Message);

        // 4. KIỂM CHỨNG: Mật khẩu cũ KHÔNG CÒN DÙNG ĐƯỢC
        var failLoginRes = await client1.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest
        {
            Email = email,
            Password = oldPassword
        });
        Assert.Equal(HttpStatusCode.Unauthorized, failLoginRes.StatusCode);

        // 5. KIỂM CHỨNG: Mật khẩu mới ĐĂNG NHẬP THÀNH CÔNG
        var newLoginRes = await client1.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest
        {
            Email = email,
            Password = newPassword
        });
        Assert.Equal(HttpStatusCode.OK, newLoginRes.StatusCode);

        // 6. KIỂM CHỨNG: Phiên thiết bị 2 bị thu hồi (Không thể refresh)
        var refreshReq2 = new HttpRequestMessage(HttpMethod.Post, "/api/v1/auth/refresh-token");
        refreshReq2.Headers.Add("X-Refresh-Token", refreshToken2);
        var refreshRes2 = await client2.SendAsync(refreshReq2);
        Assert.Equal(HttpStatusCode.Unauthorized, refreshRes2.StatusCode);

        // 7. KIỂM CHỨNG: Phiên thiết bị 1 vẫn hợp lệ
        var refreshReq1 = new HttpRequestMessage(HttpMethod.Post, "/api/v1/auth/refresh-token");
        refreshReq1.Headers.Add("X-Refresh-Token", refreshToken1);
        var refreshRes1 = await client1.SendAsync(refreshReq1);
        Assert.Equal(HttpStatusCode.OK, refreshRes1.StatusCode);
    }

    [Fact]
    public async Task PutChangePassword_WhenCurrentPasswordIncorrect_Returns401ErrAuthInvalidCredentials()
    {
        var client = _factory.CreateClient();
        var email = "wrong_cur_pass@veggielife.vn";
        await SeedUserAsync(email, "CorrectPass@2026");

        // Đăng nhập lấy Bearer token
        var loginRes = await client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest
        {
            Email = email,
            Password = "CorrectPass@2026"
        });
        var body = await loginRes.Content.ReadFromJsonAsync<ApiResponse<LoginResponse>>();
        var token = body!.Data!.AccessToken;

        // Gọi đổi mật khẩu với CurrentPassword sai
        var changeReq = new HttpRequestMessage(HttpMethod.Put, "/api/v1/auth/password");
        changeReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        changeReq.Content = JsonContent.Create(new ChangePasswordRequest
        {
            CurrentPassword = "WrongPass@2026",
            NewPassword = "NewValidPassword@2026"
        });

        var response = await client.SendAsync(changeReq);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);

        var errBody = await response.Content.ReadFromJsonAsync<ApiErrorResponse>();
        Assert.NotNull(errBody);
        Assert.Equal(ErrorCodes.ErrAuthInvalidCredentials, errBody.ErrorCode);
    }

    [Fact]
    public async Task PutChangePassword_WhenNewPasswordWeak_Returns400ErrAuthWeakPassword()
    {
        var client = _factory.CreateClient();
        var email = "weak_new_pass@veggielife.vn";
        await SeedUserAsync(email, "CorrectPass@2026");

        var loginRes = await client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest
        {
            Email = email,
            Password = "CorrectPass@2026"
        });
        var body = await loginRes.Content.ReadFromJsonAsync<ApiResponse<LoginResponse>>();
        var token = body!.Data!.AccessToken;

        // Gọi đổi mật khẩu với NewPassword yếu
        var changeReq = new HttpRequestMessage(HttpMethod.Put, "/api/v1/auth/password");
        changeReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        changeReq.Content = JsonContent.Create(new ChangePasswordRequest
        {
            CurrentPassword = "CorrectPass@2026",
            NewPassword = "weak"
        });

        var response = await client.SendAsync(changeReq);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var errBody = await response.Content.ReadFromJsonAsync<ApiErrorResponse>();
        Assert.NotNull(errBody);
        Assert.Equal(ErrorCodes.ErrAuthWeakPassword, errBody.ErrorCode);
    }

    [Fact]
    public async Task PutChangePassword_WhenNewPasswordSameAsOld_Returns400ErrAuthWeakPassword()
    {
        var client = _factory.CreateClient();
        var email = "same_pass@veggielife.vn";
        var currentPass = "CorrectPass@2026";
        await SeedUserAsync(email, currentPass);

        var loginRes = await client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest
        {
            Email = email,
            Password = currentPass
        });
        var body = await loginRes.Content.ReadFromJsonAsync<ApiResponse<LoginResponse>>();
        var token = body!.Data!.AccessToken;

        // Gọi đổi mật khẩu với NewPassword trùng CurrentPassword
        var changeReq = new HttpRequestMessage(HttpMethod.Put, "/api/v1/auth/password");
        changeReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        changeReq.Content = JsonContent.Create(new ChangePasswordRequest
        {
            CurrentPassword = currentPass,
            NewPassword = currentPass
        });

        var response = await client.SendAsync(changeReq);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var errBody = await response.Content.ReadFromJsonAsync<ApiErrorResponse>();
        Assert.NotNull(errBody);
        Assert.Equal(ErrorCodes.ErrAuthWeakPassword, errBody.ErrorCode);
    }

    [Fact]
    public async Task PutChangePassword_WhenUnauthenticated_Returns401()
    {
        var client = _factory.CreateClient();

        // Không truyền Authorization header
        var changeReq = new HttpRequestMessage(HttpMethod.Put, "/api/v1/auth/password");
        changeReq.Content = JsonContent.Create(new ChangePasswordRequest
        {
            CurrentPassword = "AnyPassword@2026",
            NewPassword = "NewPassword@2026"
        });

        var response = await client.SendAsync(changeReq);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task PutChangePassword_WhenCalledByGuest_Returns403()
    {
        var client = _factory.CreateClient();

        // Tạo phiên khách vãng lai (Anonymous JWT)
        var anonRes = await client.PostAsync("/api/v1/auth/anonymous", null);
        var anonBody = await anonRes.Content.ReadFromJsonAsync<ApiResponse<VeggieLife.BLL.DTOs.Guest.Responses.CreateAnonymousSessionResponse>>();
        var guestToken = anonBody!.Data!.AccessToken;

        // Khách cố gọi PUT /api/v1/auth/password
        var changeReq = new HttpRequestMessage(HttpMethod.Put, "/api/v1/auth/password");
        changeReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", guestToken);
        changeReq.Content = JsonContent.Create(new ChangePasswordRequest
        {
            CurrentPassword = "AnyPassword@2026",
            NewPassword = "NewPassword@2026"
        });

        var response = await client.SendAsync(changeReq);
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
