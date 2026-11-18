using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using VeggieLife.BLL.Common.Constants;
using VeggieLife.BLL.Common.Responses;
using VeggieLife.BLL.DTOs.Auth;
using VeggieLife.BLL.DTOs.Subscriptions.Requests;
using VeggieLife.BLL.Services.Cache;
using VeggieLife.DAL.Context;
using VeggieLife.DAL.Entities;
using Xunit;

namespace VeggieLife.Tests.Integration.Controllers;

/// <summary>
/// Ca kiểm thử tích hợp bảo mật tự động phục vụ nghiệm thu NFR Sprint 7:
/// 1. SEC-3: Đoán mật khẩu liên tục, khóa tài khoản sau 5 lần sai trong 15 phút, trả lỗi không phân biệt email.
/// 2. SEC-8: Webhook thanh toán an toàn, xác thực chữ ký HMAC-SHA256, đối soát số tiền, chống phát lại (idempotent replay attack).
/// 3. SEC-2, SEC-5, SEC-6: Kiểm tra token giả, token hết hạn, chuỗi token rác, phát hiện dùng lại refresh token và RBAC khách vãng lai.
/// </summary>
public class SecurityNfrAuditIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
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

        public Task<long> IncrementAsync(string key, long value = 1, TimeSpan? expiry = null, CancellationToken ct = default) => Task.FromResult(1L);
        public Task<(bool Allowed, long Current, long Remaining)> TryConsumeQuotaAsync(string key, int maxLimit, TimeSpan? expiry = null, CancellationToken ct = default) => Task.FromResult((true, 1L, 2L));
        public Task EnqueueAsync<T>(string queueName, T item, CancellationToken ct = default) => Task.CompletedTask;
    }

    private readonly WebApplicationFactory<Program> _factory;
    private readonly TestRedisService _redis = new();
    private readonly string _dbName = "SecurityNfrDb_" + Guid.NewGuid();
    private const string JwtSecret = "VeggieLifeSecretKeyForDevelopmentMustBe32CharsLong!";
    private const string MoMoSecret = "veggielife_momo_secret_key_2026";
    private const string VietQrSecret = "veggielife_vietqr_secret_key_2026";

    public SecurityNfrAuditIntegrationTests(WebApplicationFactory<Program> factory)
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
                    ["Jwt:AccessTokenExpiryMinutes"] = "15",
                    ["Payment:MoMo:SecretKey"] = MoMoSecret,
                    ["Payment:VietQR:SecretKey"] = VietQrSecret
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

        SeedDatabase();
    }

    private void SeedDatabase()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        if (!db.Users.Any())
        {
            var targetUser = new User
            {
                UserId = 101,
                Email = "bruteforce.target@veggielife.vn",
                FullName = "Nạn Nhân Đoán Mật Khẩu",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("CorrectPassword@2026", workFactor: 4),
                Role = UserRoles.Member,
                AccountStatus = AccountStatuses.Active,
                FailedLoginCount = 0,
                CreatedAt = DateTime.UtcNow
            };

            var paymentUser = new User
            {
                UserId = 102,
                Email = "vip.buyer@veggielife.vn",
                FullName = "Người Mua Gói VIP",
                Role = UserRoles.Member,
                AccountStatus = AccountStatuses.Active,
                CreatedAt = DateTime.UtcNow
            };

            var plan = new SubscriptionPlan
            {
                PlanId = 1,
                AdminId = 1,
                PlanName = "VIP 1 Tháng",
                Price = 49000,
                DurationDays = 30,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            db.Users.AddRange(targetUser, paymentUser);
            db.SubscriptionPlans.Add(plan);
            db.SaveChanges();
        }
    }

    private HttpClient CreateCustomClient()
    {
        return _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = false
        });
    }

    private static string ComputeHmac(string secretKey, string orderCode, string partnerTransactionId, long amount, int resultCode)
    {
        var dict = new SortedDictionary<string, string>(StringComparer.Ordinal)
        {
            ["amount"] = amount.ToString(),
            ["orderCode"] = orderCode,
            ["partnerTransactionId"] = partnerTransactionId,
            ["resultCode"] = resultCode.ToString()
        };
        var rawData = string.Join("&", dict.Select(kv => $"{kv.Key}={kv.Value}"));

        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secretKey));
        var hashBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(rawData));
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }

    private static string GenerateJwt(int userId, string email, string role, string secretKey, DateTime expires)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes(secretKey);
        var notBefore = expires < DateTime.UtcNow ? expires.AddMinutes(-5) : DateTime.UtcNow;
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                new Claim(ClaimTypes.Role, role),
                new Claim(ClaimTypes.Email, email),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            }),
            NotBefore = notBefore,
            Expires = expires,
            Issuer = "VeggieLife.API",
            Audience = "VeggieLife.Client",
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };
        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }

    // =========================================================================
    // 1. NHÓM KIỂM THỬ: ĐOÁN MẬT KHẨU LIÊN TỤC & KHÓA ĐĂNG NHẬP (SEC-3)
    // =========================================================================

    [Fact]
    public async Task Sec3_BruteForceLogin_Consecutive5Fails_LocksAccount15Minutes_6thAttemptReturns429()
    {
        var client = CreateCustomClient();
        var email = "bruteforce.target@veggielife.vn";

        // Bước 1: Nhập sai mật khẩu liên tiếp 5 lần
        for (int attempt = 1; attempt <= 5; attempt++)
        {
            var res = await client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest
            {
                Email = email,
                Password = $"WrongPassAttempt{attempt}@2026"
            });

            if (attempt < 5)
            {
                res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
                var err = await res.Content.ReadFromJsonAsync<ApiErrorResponse>();
                err.Should().NotBeNull();
                err!.ErrorCode.Should().Be(ErrorCodes.ErrAuthInvalidCredentials);
            }
            else
            {
                // Lần sai thứ 5: Khóa 15 phút, trả về 429 ERR_AUTH_LOGIN_LOCKED
                res.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
                var err = await res.Content.ReadFromJsonAsync<ApiErrorResponse>();
                err.Should().NotBeNull();
                err!.ErrorCode.Should().Be(ErrorCodes.ErrAuthLoginLocked);
            }
        }

        // Bước 2: Lần thứ 6 gửi tiếp (dù đúng hay sai mật khẩu) -> Vẫn phải bị khóa (429)
        var attempt6 = await client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest
        {
            Email = email,
            Password = "CorrectPassword@2026" // Thử dùng mật khẩu đúng trong khi đang bị khóa
        });

        attempt6.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
        var err6 = await attempt6.Content.ReadFromJsonAsync<ApiErrorResponse>();
        err6.Should().NotBeNull();
        err6!.ErrorCode.Should().Be(ErrorCodes.ErrAuthLoginLocked);

        // Kiểm tra cơ sở dữ liệu: Cột LockedUntil phải lớn hơn thời điểm hiện tại ~13-15 phút
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var user = await db.Users.FirstAsync(u => u.Email == email);
        user.LockedUntil.Should().NotBeNull();
        user.LockedUntil!.Value.Should().BeAfter(DateTime.UtcNow.AddMinutes(13));
    }

    [Fact]
    public async Task Sec3_Login_NonExistentEmail_ReturnsSameGenericErrorWithoutEmailEnumeration()
    {
        var client = CreateCustomClient();

        // Gửi thử với email không tồn tại trong hệ thống
        var res = await client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest
        {
            Email = "nonexistent.attacker@veggielife.vn",
            Password = "RandomPassword@2026"
        });

        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        var err = await res.Content.ReadFromJsonAsync<ApiErrorResponse>();
        err.Should().NotBeNull();
        err!.ErrorCode.Should().Be(ErrorCodes.ErrAuthInvalidCredentials);
        // Thông báo lỗi chung, không tiết lộ email có tồn tại hay không
        err.Message.Should().Be("Email hoặc mật khẩu không chính xác.");
    }

    // =========================================================================
    // 2. NHÓM KIỂM THỬ: WEBHOOK THANH TOÁN, HMAC & CHỐNG PHÁT LẠI REPLAY (SEC-8)
    // =========================================================================

    [Fact]
    public async Task Sec8_PaymentWebhook_ReplayAttack_IsIdempotent_DoesNotExtendVipTwice()
    {
        var client = CreateCustomClient();
        var orderCode = "VL_SEC_REPLAY_" + Guid.NewGuid().ToString("N")[..8];

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.PaymentTransactions.Add(new PaymentTransaction
            {
                PlanId = 1,
                UserId = 102,
                GatewayProvider = "MOMO",
                PartnerTransactionCode = orderCode,
                AmountVnd = 49000,
                TransactionStatus = "PENDING",
                SignatureHash = "init_sig",
                CreatedAt = DateTime.UtcNow,
                StartsAt = DateTime.UtcNow,
                EndsAt = DateTime.UtcNow.AddDays(30)
            });
            await db.SaveChangesAsync();
        }

        var partnerTransId = "MOMO_TRANS_" + Guid.NewGuid().ToString("N")[..6];
        var sig = ComputeHmac(MoMoSecret, orderCode, partnerTransId, 49000, 0);

        var ipnPayload = new HandlePaymentIpnRequest
        {
            OrderCode = orderCode,
            PartnerTransactionId = partnerTransId,
            Amount = 49000,
            ResultCode = 0,
            Signature = sig
        };

        // Lần 1: Cổng gửi webhook thật -> Trả về 200 OK, kích hoạt VIP
        var firstCall = await client.PostAsJsonAsync("/api/v1/payments/momo/ipn", ipnPayload);
        firstCall.StatusCode.Should().Be(HttpStatusCode.OK);

        DateTime originalEndsAt;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var tx = await db.PaymentTransactions.FirstAsync(t => t.PartnerTransactionCode == orderCode);
            tx.TransactionStatus.Should().Be("SUCCESS");
            originalEndsAt = tx.EndsAt;

            var user = await db.Users.FindAsync(102);
            user!.Role.Should().Be(UserRoles.Vip);
        }

        // Lần 2: Kẻ tấn công phát lại (Replay Attack) gói tin Webhook IPN y hệt
        var replayCall = await client.PostAsJsonAsync("/api/v1/payments/momo/ipn", ipnPayload);
        replayCall.StatusCode.Should().Be(HttpStatusCode.OK);

        // Kiểm tra tính bất biến (Idempotent): EndsAt không bị cộng dồn, số lượng giao dịch không tăng
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var tx = await db.PaymentTransactions.FirstAsync(t => t.PartnerTransactionCode == orderCode);
            tx.EndsAt.Should().Be(originalEndsAt);

            var count = await db.PaymentTransactions.CountAsync(t => t.UserId == 102 && t.PartnerTransactionCode == orderCode);
            count.Should().Be(1);
        }
    }

    [Fact]
    public async Task Sec8_PaymentWebhook_ForgedHmacSignature_Returns401Unauthorized()
    {
        var client = CreateCustomClient();
        var orderCode = "VL_SEC_BADSIG_" + Guid.NewGuid().ToString("N")[..8];

        var ipnPayload = new HandlePaymentIpnRequest
        {
            OrderCode = orderCode,
            PartnerTransactionId = "MOMO_FAKE_001",
            Amount = 49000,
            ResultCode = 0,
            Signature = "forged_invalid_signature_hash_1234567890abcdef"
        };

        var res = await client.PostAsJsonAsync("/api/v1/payments/momo/ipn", ipnPayload);
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        var err = await res.Content.ReadFromJsonAsync<ApiErrorResponse>();
        err.Should().NotBeNull();
        err!.ErrorCode.Should().Be(ErrorCodes.ErrPayInvalidSignature);
    }

    [Fact]
    public async Task Sec8_PaymentWebhook_TamperedAmount_MarksFailed_Returns422()
    {
        var client = CreateCustomClient();
        var orderCode = "VL_SEC_TAMPER_" + Guid.NewGuid().ToString("N")[..8];

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.PaymentTransactions.Add(new PaymentTransaction
            {
                PlanId = 1,
                UserId = 102,
                GatewayProvider = "VIETQR",
                PartnerTransactionCode = orderCode,
                AmountVnd = 49000, // Giá đúng là 49.000 VNĐ
                TransactionStatus = "PENDING",
                SignatureHash = "init_sig",
                CreatedAt = DateTime.UtcNow,
                StartsAt = DateTime.UtcNow,
                EndsAt = DateTime.UtcNow.AddDays(30)
            });
            await db.SaveChangesAsync();
        }

        // Kẻ tấn công trả 1.000 VNĐ nhưng gửi webhook kèm chữ ký hợp lệ của số tiền 1.000 VNĐ
        var tamperedAmount = 1000L;
        var sig = ComputeHmac(VietQrSecret, orderCode, "VQR_TAMPERED", tamperedAmount, 0);

        var ipnPayload = new HandlePaymentIpnRequest
        {
            OrderCode = orderCode,
            PartnerTransactionId = "VQR_TAMPERED",
            Amount = tamperedAmount,
            ResultCode = 0,
            Signature = sig
        };

        var res = await client.PostAsJsonAsync("/api/v1/payments/vietqr/ipn", ipnPayload);
        res.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);

        var err = await res.Content.ReadFromJsonAsync<ApiErrorResponse>();
        err.Should().NotBeNull();
        err!.ErrorCode.Should().Be(ErrorCodes.ErrPayAmountMismatch);

        // Kiểm tra cơ sở dữ liệu: Giao dịch bị chuyển sang FAILED
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var tx = await db.PaymentTransactions.FirstAsync(t => t.PartnerTransactionCode == orderCode);
            tx.TransactionStatus.Should().Be("FAILED");
        }
    }

    // =========================================================================
    // 3. NHÓM KIỂM THỬ: TOKEN GIẢ, HẾT HẠN, DÙNG LẠI REFRESH & RBAC (SEC-2, 5, 6)
    // =========================================================================

    [Fact]
    public async Task Sec2_ProtectedEndpoint_ForgedJwtToken_Returns401Unauthorized()
    {
        var client = CreateCustomClient();

        // Tạo JWT với Signing Key giả (không khớp với cấu hình hệ thống)
        var fakeSecret = "ForgedAttackerKeyMustBeAtLeast32CharsLong123456!";
        var forgedJwt = GenerateJwt(101, "victim@veggielife.vn", UserRoles.Member, fakeSecret, DateTime.UtcNow.AddMinutes(15));

        var req = new HttpRequestMessage(HttpMethod.Get, "/api/v1/users/me");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", forgedJwt);

        var res = await client.SendAsync(req);
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        var err = await res.Content.ReadFromJsonAsync<ApiErrorResponse>();
        err.Should().NotBeNull();
        err!.ErrorCode.Should().Be(ErrorCodes.ErrAuthInvalidToken);
    }

    [Fact]
    public async Task Sec2_ProtectedEndpoint_ExpiredJwtToken_Returns401Unauthorized()
    {
        var client = CreateCustomClient();

        // Tạo JWT đã hết hạn 30 phút trước
        var expiredJwt = GenerateJwt(101, "victim@veggielife.vn", UserRoles.Member, JwtSecret, DateTime.UtcNow.AddMinutes(-30));

        var req = new HttpRequestMessage(HttpMethod.Get, "/api/v1/users/me");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", expiredJwt);

        var res = await client.SendAsync(req);
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        var err = await res.Content.ReadFromJsonAsync<ApiErrorResponse>();
        err.Should().NotBeNull();
        err!.ErrorCode.Should().Be(ErrorCodes.ErrAuthInvalidToken);
    }

    [Fact]
    public async Task Sec2_ProtectedEndpoint_MalformedToken_Returns401Unauthorized()
    {
        var client = CreateCustomClient();

        var req = new HttpRequestMessage(HttpMethod.Get, "/api/v1/users/me");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "completely.malformed.garbage.token");

        var res = await client.SendAsync(req);
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        var err = await res.Content.ReadFromJsonAsync<ApiErrorResponse>();
        err.Should().NotBeNull();
        err!.ErrorCode.Should().Be(ErrorCodes.ErrAuthInvalidToken);
    }

    [Fact]
    public async Task Sec2_RefreshToken_ReusingOldRotatedToken_RevokesAllSessions_Returns401()
    {
        var client = CreateCustomClient();
        var email = "token.replay@veggielife.vn";

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Users.Add(new User
            {
                Email = email,
                FullName = "Token Replay Tester",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("TestPass@2026", workFactor: 4),
                Role = UserRoles.Member,
                AccountStatus = AccountStatuses.Active,
                CreatedAt = DateTime.UtcNow
            });
            await db.SaveChangesAsync();
        }

        // 1. Đăng nhập để lấy cặp Access Token và Refresh Token đầu tiên (RT1)
        var loginRes = await client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest
        {
            Email = email,
            Password = "TestPass@2026"
        });
        loginRes.StatusCode.Should().Be(HttpStatusCode.OK);

        var cookieHeader = loginRes.Headers.GetValues("Set-Cookie").First(c => c.Contains("refresh_token="));
        var rt1 = cookieHeader.Split(';')[0].Replace("refresh_token=", "");

        // 2. Làm mới phiên lần 1 bằng RT1 -> Thành công và xoay vòng sang RT2
        var refreshReq1 = new HttpRequestMessage(HttpMethod.Post, "/api/v1/auth/refresh-token");
        refreshReq1.Headers.Add("Cookie", $"refresh_token={rt1}");
        var refreshRes1 = await client.SendAsync(refreshReq1);
        refreshRes1.StatusCode.Should().Be(HttpStatusCode.OK);

        // 3. Tấn công phát lại: Kẻ gian dùng lại RT1 đã qua sử dụng
        var replayReq = new HttpRequestMessage(HttpMethod.Post, "/api/v1/auth/refresh-token");
        replayReq.Headers.Add("Cookie", $"refresh_token={rt1}");
        var replayRes = await client.SendAsync(replayReq);

        // Theo SEC-2 / BR-AUTH-004: Phát hiện dùng lại refresh token, trả về ERR_AUTH_REFRESH_TOKEN_REUSED
        replayRes.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        var replayErr = await replayRes.Content.ReadFromJsonAsync<ApiErrorResponse>();
        replayErr.Should().NotBeNull();
        replayErr!.ErrorCode.Should().Be(ErrorCodes.ErrAuthRefreshTokenReused);
    }

    [Fact]
    public async Task Sec5_Sec6_GuestToken_AccessingProtectedMemberEndpoint_Returns403Forbidden()
    {
        var client = CreateCustomClient();

        // 1. Khách vãng lai lấy Anonymous Guest Token
        var anonRes = await client.PostAsync("/api/v1/auth/anonymous", null);
        anonRes.StatusCode.Should().Be(HttpStatusCode.OK);

        var anonBody = await anonRes.Content.ReadFromJsonAsync<ApiResponse<VeggieLife.BLL.DTOs.Guest.Responses.CreateAnonymousSessionResponse>>();
        var guestJwt = anonBody!.Data!.AccessToken;

        // 2. Khách cố gọi endpoint mua gói VIP (yêu cầu vai trò MEMBER/VIP)
        var checkoutReq = new HttpRequestMessage(HttpMethod.Post, "/api/v1/subscriptions/checkout")
        {
            Content = JsonContent.Create(new CheckoutSubscriptionRequest
            {
                PlanId = 1,
                Gateway = "MOMO"
            })
        };
        checkoutReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", guestJwt);

        var res = await client.SendAsync(checkoutReq);
        // Bị chặn 403 Forbidden theo chính sách RBAC (SEC-5, SEC-6)
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        var err = await res.Content.ReadFromJsonAsync<ApiErrorResponse>();
        err.Should().NotBeNull();
        err!.ErrorCode.Should().Be(ErrorCodes.ErrGuestPersonalizationRestricted);
    }
}
