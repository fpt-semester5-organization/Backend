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
using VeggieLife.BLL.DTOs.Subscriptions.Requests;
using VeggieLife.BLL.DTOs.Subscriptions.Responses;
using VeggieLife.BLL.Services.Cache;
using VeggieLife.DAL.Context;
using VeggieLife.DAL.Entities;
using Xunit;

namespace VeggieLife.Tests.Integration.Controllers;

/// <summary>
/// Integration Tests kiểm thử toàn bộ đường ống HTTP (Routing, Middleware, Validation, Auth, IPN Webhook)
/// cho Module Gói VIP & Thanh toán (FN-SUB-01, FN-SUB-02, FN-SUB-03, FN-SUB-04).
/// </summary>
public class SubscriptionsAndPaymentsIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
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

        public Task<long> IncrementAsync(string key, long value = 1, TimeSpan? expiry = null, CancellationToken ct = default) => Task.FromResult(1L);
        public Task<(bool Allowed, long Current, long Remaining)> TryConsumeQuotaAsync(string key, int maxLimit, TimeSpan? expiry = null, CancellationToken ct = default) => Task.FromResult((true, 1L, 2L));
        public Task<(bool Allowed, bool IsBanned, long CurrentCount, TimeSpan? RetryAfter)> CheckIpRateLimitAsync(string clientIp, int maxRequestsPerMinute = 10, int banDurationMinutes = 10, CancellationToken ct = default) => Task.FromResult((true, false, 1L, (TimeSpan?)null));
        public Task EnqueueAsync<T>(string queueName, T item, CancellationToken ct = default) => Task.CompletedTask;
    }

    private readonly WebApplicationFactory<Program> _factory;
    private readonly TestRedisService _redis = new();
    private readonly string _dbName = "SubPaymentsIntegrationDb_" + Guid.NewGuid();
    private const string JwtSecret = "VeggieLifeSecretKeyForDevelopmentMustBe32CharsLong!";

    public SubscriptionsAndPaymentsIntegrationTests(WebApplicationFactory<Program> factory)
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
                    ["Jwt:AccessTokenExpiryMinutes"] = "60",
                    ["Payment:MoMo:SecretKey"] = "veggielife_momo_secret_key_2026",
                    ["Payment:VietQR:SecretKey"] = "veggielife_vietqr_secret_key_2026"
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
            var admin = new User
            {
                UserId = 1,
                Email = "admin@veggielife.vn",
                FullName = "System Admin",
                Role = UserRoles.Admin,
                AccountStatus = "ACTIVE",
                CreatedAt = DateTime.UtcNow
            };
            var member = new User
            {
                UserId = 2,
                Email = "member@veggielife.vn",
                FullName = "Member One",
                Role = UserRoles.Member,
                AccountStatus = "ACTIVE",
                CreatedAt = DateTime.UtcNow
            };

            db.Users.AddRange(admin, member);

            db.SubscriptionPlans.AddRange(
                new SubscriptionPlan
                {
                    PlanId = 1,
                    AdminId = 1,
                    PlanName = "Gói VIP 1 Tháng - Mở Khóa AI Dinh Dưỡng",
                    Price = 49000,
                    DurationDays = 30,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                },
                new SubscriptionPlan
                {
                    PlanId = 2,
                    AdminId = 1,
                    PlanName = "Gói VIP 1 Năm - Thuần Chay Bền Vững",
                    Price = 449000,
                    DurationDays = 365,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                }
            );

            db.SaveChanges();
        }
    }

    private string GenerateJwt(int userId, string role, string email)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes(JwtSecret);
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
            Issuer = "VeggieLife.API",
            Audience = "VeggieLife.Client",
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };
        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
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

    // ==========================================
    // 1. FN-SUB-01: GET /api/v1/subscription-plans
    // ==========================================

    [Fact]
    public async Task GetSubscriptionPlans_Anonymous_Returns200WithPlansAndBenefits()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/v1/subscription-plans");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await response.Content.ReadFromJsonAsync<ApiResponse<ListSubscriptionPlansResponseDto>>();

        json.Should().NotBeNull();
        json!.StatusCode.Should().Be(200);
        json.Data.Should().NotBeNull();
        json.Data!.Plans.Should().HaveCount(2);
        json.Data.Plans[0].PlanName.Should().Contain("1 Tháng");
        json.Data.Benefits.Free.MealPlansPerWeek.Should().Be(1);
        json.Data.Benefits.Vip.MealPlansPerWeek.Should().Be(3);
    }

    // ==========================================
    // 2. FN-SUB-02: POST /api/v1/subscriptions/checkout
    // ==========================================

    [Fact]
    public async Task Checkout_WithoutToken_Returns401Unauthorized()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/v1/subscriptions/checkout", new CheckoutSubscriptionRequest
        {
            PlanId = 1,
            Gateway = "MOMO"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Checkout_AsGuest_Returns403Forbidden()
    {
        var client = _factory.CreateClient();
        var guestToken = GenerateJwt(999, UserRoles.Guest, "guest@veggielife.vn");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", guestToken);

        var response = await client.PostAsJsonAsync("/api/v1/subscriptions/checkout", new CheckoutSubscriptionRequest
        {
            PlanId = 1,
            Gateway = "MOMO"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        var error = await response.Content.ReadFromJsonAsync<ApiErrorResponse>();
        error!.ErrorCode.Should().Be(ErrorCodes.ErrGuestPersonalizationRestricted);
    }

    [Fact]
    public async Task Checkout_WithInvalidPlanId_Returns404NotFound()
    {
        var client = _factory.CreateClient();
        var memberToken = GenerateJwt(2, UserRoles.Member, "member@veggielife.vn");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", memberToken);

        var response = await client.PostAsJsonAsync("/api/v1/subscriptions/checkout", new CheckoutSubscriptionRequest
        {
            PlanId = 9999,
            Gateway = "MOMO"
        });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var error = await response.Content.ReadFromJsonAsync<ApiErrorResponse>();
        error!.ErrorCode.Should().Be(ErrorCodes.ErrPlanNotFound);
    }

    [Fact]
    public async Task Checkout_WithInvalidGateway_Returns400BadRequest()
    {
        var client = _factory.CreateClient();
        var memberToken = GenerateJwt(2, UserRoles.Member, "member@veggielife.vn");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", memberToken);

        var response = await client.PostAsJsonAsync("/api/v1/subscriptions/checkout", new CheckoutSubscriptionRequest
        {
            PlanId = 1,
            Gateway = "INVALID_WALLET"
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var error = await response.Content.ReadFromJsonAsync<ApiErrorResponse>();
        error!.ErrorCode.Should().Be(ErrorCodes.ErrValidation);
    }

    // ==========================================
    // 3. Toàn bộ chu trình mua gói & IPN đối soát
    // ==========================================

    [Fact]
    public async Task FullFlow_Checkout_ThenIpnSuccess_UpgradesToVip_AndIdempotentWhenRetried()
    {
        var client = _factory.CreateClient();

        // Tạo user mới cho test này để tránh va chạm PENDING
        int testUserId;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var u = new User
            {
                Email = "flow_test@veggielife.vn",
                FullName = "Flow User",
                Role = UserRoles.Member,
                AccountStatus = "ACTIVE",
                CreatedAt = DateTime.UtcNow
            };
            db.Users.Add(u);
            db.SaveChanges();
            testUserId = u.UserId;
        }

        var memberToken = GenerateJwt(testUserId, UserRoles.Member, "flow_test@veggielife.vn");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", memberToken);

        // Bước A: Kiểm tra ban đầu chưa là VIP (FN-SUB-04)
        var preCheck = await client.GetFromJsonAsync<ApiResponse<CurrentUserSubscriptionResponseDto>>("/api/v1/users/me/subscription");
        preCheck!.Data!.IsVip.Should().BeFalse();
        preCheck.Data.Plan.Should().BeNull();

        // Bước B: Tạo đơn thanh toán MoMo (FN-SUB-02)
        var checkoutResponse = await client.PostAsJsonAsync("/api/v1/subscriptions/checkout", new CheckoutSubscriptionRequest
        {
            PlanId = 1,
            Gateway = "MOMO"
        });

        checkoutResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var checkoutData = await checkoutResponse.Content.ReadFromJsonAsync<ApiResponse<CheckoutSubscriptionResponseDto>>();
        checkoutData!.Data.Should().NotBeNull();
        var orderCode = checkoutData.Data!.OrderCode;
        orderCode.Should().StartWith("VL_");
        checkoutData.Data.AmountVnd.Should().Be(49000);

        // Bước C: Cố gắng tạo đơn thứ 2 ngay lập tức -> Phải bị từ chối 409 ERR_PAYMENT_PENDING_EXISTS
        var secondCheckout = await client.PostAsJsonAsync("/api/v1/subscriptions/checkout", new CheckoutSubscriptionRequest
        {
            PlanId = 1,
            Gateway = "MOMO"
        });
        secondCheckout.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var secondError = await secondCheckout.Content.ReadFromJsonAsync<ApiErrorResponse>();
        secondError!.ErrorCode.Should().Be(ErrorCodes.ErrPaymentPendingExists);

        // Bước D: Giả lập IPN gửi sai chữ ký -> 401 ERR_PAY_INVALID_SIGNATURE
        var badSigResponse = await client.PostAsJsonAsync("/api/v1/payments/momo/ipn", new HandlePaymentIpnRequest
        {
            OrderCode = orderCode,
            PartnerTransactionId = "MOMO_FAKE",
            Amount = 49000,
            ResultCode = 0,
            Signature = "bad_signature"
        });
        badSigResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        // Bước E: Giả lập IPN gửi lệch số tiền -> 422 ERR_PAY_AMOUNT_MISMATCH
        var fakeAmount = 25000L;
        var fakeSig = ComputeHmac("veggielife_momo_secret_key_2026", orderCode, "MOMO_FAKE", fakeAmount, 0);
        var mismatchResponse = await client.PostAsJsonAsync("/api/v1/payments/momo/ipn", new HandlePaymentIpnRequest
        {
            OrderCode = orderCode,
            PartnerTransactionId = "MOMO_FAKE",
            Amount = fakeAmount,
            ResultCode = 0,
            Signature = fakeSig
        });
        mismatchResponse.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);

        // Bước F: Cổng MoMo gửi IPN thanh toán THÀNH CÔNG hợp lệ
        // Cập nhật lại đơn sang PENDING để test thành công (vì bước lệch tiền trước đó đã chuyển FAILED theo quy tắc bảo mật)
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var tx = db.PaymentTransactions.First(t => t.PartnerTransactionCode == orderCode);
            tx.TransactionStatus = "PENDING";
            db.SaveChanges();
        }

        var validSig = ComputeHmac("veggielife_momo_secret_key_2026", orderCode, "MOMO_TRANS_123", 49000, 0);
        var successIpnResponse = await client.PostAsJsonAsync("/api/v1/payments/momo/ipn", new HandlePaymentIpnRequest
        {
            OrderCode = orderCode,
            PartnerTransactionId = "MOMO_TRANS_123",
            Amount = 49000,
            ResultCode = 0,
            Signature = validSig
        });

        successIpnResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Bước G: Kiểm tra tài khoản ĐÃ LÊN VIP (FN-SUB-04)
        var postCheck = await client.GetFromJsonAsync<ApiResponse<CurrentUserSubscriptionResponseDto>>("/api/v1/users/me/subscription");
        postCheck!.Data!.IsVip.Should().BeTrue();
        postCheck.Data.Plan.Should().NotBeNull();
        postCheck.Data.Plan!.PlanName.Should().Contain("1 Tháng");
        postCheck.Data.EndsAt.Should().NotBeNull();
        var endsAtFirst = postCheck.Data.EndsAt;

        // Bước H: Kiểm tra nhận trùng (Idempotent) - Gửi lại IPN lần 2 với cùng nội dung
        var retryIpnResponse = await client.PostAsJsonAsync("/api/v1/payments/momo/ipn", new HandlePaymentIpnRequest
        {
            OrderCode = orderCode,
            PartnerTransactionId = "MOMO_TRANS_123",
            Amount = 49000,
            ResultCode = 0,
            Signature = validSig
        });

        retryIpnResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Bước I: Kiểm tra hạn VIP KHÔNG BỊ CỘNG ĐÚP
        // Xóa cache để đảm bảo đọc trực tiếp DB
        await _redis.RemoveAsync($"vip_status:{testUserId}");
        var finalCheck = await client.GetFromJsonAsync<ApiResponse<CurrentUserSubscriptionResponseDto>>("/api/v1/users/me/subscription");
        finalCheck!.Data!.IsVip.Should().BeTrue();
        finalCheck.Data.EndsAt.Should().Be(endsAtFirst); // Hạn kết thúc giữ nguyên!
    }
}
