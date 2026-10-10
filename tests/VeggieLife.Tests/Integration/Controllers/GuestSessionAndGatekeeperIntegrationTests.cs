using System;
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
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using VeggieLife.BLL.Common.Constants;
using VeggieLife.BLL.Common.Responses;
using VeggieLife.BLL.DTOs.Guest.Responses;
using VeggieLife.BLL.Services.Cache;
using VeggieLife.DAL.Context;
using Xunit;

namespace VeggieLife.Tests.Integration.Controllers;

/// <summary>
/// Bộ kiểm thử tích hợp (Integration Tests) cho phân hệ Phiên Khách và Người gác cổng (Gatekeeper):
/// 1. Chặn Khách ghi nội dung cộng đồng / tính năng cá nhân, trả thông báo mời đăng ký.
/// 2. Đếm lượt dùng thử chính xác khi 20 yêu cầu gửi cùng lúc (không vượt hạn mức 3).
/// 3. Giới hạn 10 yêu cầu/phút mỗi IP, vượt thì chặn 10 phút.
/// 4. Giới hạn vị trí chỉ cho phép từ Việt Nam (có công tắc bật/tắt).
/// </summary>
public class GuestSessionAndGatekeeperIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private const string JwtSecret = "VeggieLifeSecretKeyForDevelopmentMustBe32CharsLong!";
    private const string JwtIssuer = "VeggieLife.API";
    private const string JwtAudience = "VeggieLife.Client";

    public GuestSessionAndGatekeeperIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    private (HttpClient Client, IServiceProvider Services) CreateTestContext(Action<Dictionary<string, string?>>? configOverride = null)
    {
        var inMemoryConfig = new Dictionary<string, string?>
        {
            ["Jwt:SecretKey"] = JwtSecret,
            ["Jwt:Issuer"] = JwtIssuer,
            ["Jwt:Audience"] = JwtAudience
        };
        configOverride?.Invoke(inMemoryConfig);

        var customFactory = _factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Development");
            builder.ConfigureAppConfiguration((_, config) =>
            {
                config.AddInMemoryCollection(inMemoryConfig);
            });
            builder.ConfigureServices(services =>
            {
                var dbDesc = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
                if (dbDesc != null) services.Remove(dbDesc);
                services.AddDbContext<AppDbContext>(opt =>
                {
                    opt.UseInMemoryDatabase("GatekeeperTestDb_" + Guid.NewGuid());
                });
            });
        });

        return (customFactory.CreateClient(), customFactory.Services);
    }

    private HttpClient CreateTestClient(Action<Dictionary<string, string?>>? configOverride = null)
    {
        return CreateTestContext(configOverride).Client;
    }

    private static string GenerateJwt(string role, string? guestUuid = null, int? userId = null)
    {
        var tokenHandler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes(JwtSecret);
        var claims = new List<Claim>
        {
            new(ClaimTypes.Role, role),
            new("role", role)
        };

        if (userId.HasValue)
        {
            claims.Add(new(ClaimTypes.NameIdentifier, userId.Value.ToString()));
            claims.Add(new(ClaimTypes.Email, $"{role.ToLower()}@veggielife.vn"));
        }
        else if (!string.IsNullOrEmpty(guestUuid))
        {
            claims.Add(new(ClaimTypes.NameIdentifier, guestUuid));
            claims.Add(new("Guest_UUID", guestUuid));
        }

        var descriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddHours(2),
            Issuer = JwtIssuer,
            Audience = JwtAudience,
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };

        var token = tokenHandler.CreateToken(descriptor);
        return tokenHandler.WriteToken(token);
    }

    // =========================================================================
    // 1. CHẶN KHÁCH DÙNG TÍNH NĂNG THÀNH VIÊN VÀ TRẢ THÔNG BÁO MỜI ĐĂNG KÝ (FN-AUTH-03)
    // =========================================================================

    [Fact]
    public async Task CommunityAction_WhenCalledByGuest_ShouldReturnForbidden403_WithErrGuestCommunityActionDenied()
    {
        // Arrange
        var client = CreateTestClient();
        var guestToken = GenerateJwt(UserRoles.Guest, guestUuid: Guid.NewGuid().ToString());
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", guestToken);

        // Act: Khách bấm thao tác ghi cộng đồng (Thích / Bình luận / Đăng bài)
        var response = await client.PostAsync("/api/v1/test/auth/community-action", null);

        // Assert: Chặn Khách và trả thông báo mời đăng ký kèm login/register URLs
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        var error = await response.Content.ReadFromJsonAsync<ApiErrorResponse>(new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        error.Should().NotBeNull();
        error!.ErrorCode.Should().Be(ErrorCodes.ErrGuestCommunityActionDenied);
        error.Message.Should().Contain("đăng ký");

        var detailsElement = (JsonElement)error.Details!;
        detailsElement.GetProperty("registerUrl").GetString().Should().Be("/register");
        detailsElement.GetProperty("loginUrl").GetString().Should().Be("/login");
    }

    [Fact]
    public async Task CommunityAction_WhenCalledByGuest_WithReturnUrlAndPendingActionHeaders_ShouldStoreContextInRedis()
    {
        // Arrange
        var guestUuid = Guid.NewGuid().ToString();
        var (client, services) = CreateTestContext();
        var guestToken = GenerateJwt(UserRoles.Guest, guestUuid: guestUuid);

        var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/test/auth/community-action");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", guestToken);
        request.Headers.Add("X-Return-Url", "/recipes/10/comments");
        request.Headers.Add("X-Pending-Action", "COMMENT");

        // Act
        var response = await client.SendAsync(request);

        // Assert 1: Chặn 403 Forbidden
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        var error = await response.Content.ReadFromJsonAsync<ApiErrorResponse>(new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        error.Should().NotBeNull();
        error!.ErrorCode.Should().Be(ErrorCodes.ErrGuestCommunityActionDenied);

        // Assert 2: Kiểm tra dữ liệu được lưu vào Redis guest_return:{guestId} (TTL 15m)
        var redis = services.GetRequiredService<IRedisService>();
        var savedContext = await redis.GetAsync<GuestReturnContextDto>($"guest_return:{guestUuid}");
        savedContext.Should().NotBeNull();
        savedContext!.ReturnUrl.Should().Be("/recipes/10/comments");
        savedContext.PendingAction.Should().Be("COMMENT");
    }

    [Fact]
    public async Task CommunityAction_WhenCalledByGuest_WithInvalidReturnUrlOrPendingAction_ShouldNotStoreInRedis()
    {
        // Arrange
        var guestUuid = Guid.NewGuid().ToString();
        var (client, services) = CreateTestContext();
        var guestToken = GenerateJwt(UserRoles.Guest, guestUuid: guestUuid);

        var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/test/auth/community-action");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", guestToken);
        request.Headers.Add("X-Return-Url", "https://malicious.com/phishing"); // Không bắt đầu bằng '/'
        request.Headers.Add("X-Pending-Action", "INVALID_ACTION"); // Không thuộc danh mục enum

        // Act
        var response = await client.SendAsync(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        var redis = services.GetRequiredService<IRedisService>();
        var savedContext = await redis.GetAsync<GuestReturnContextDto>($"guest_return:{guestUuid}");
        savedContext.Should().BeNull("Không được lưu vào Redis nếu returnUrl không an toàn hoặc pendingAction không hợp lệ");
    }

    [Fact]
    public async Task PersonalizationAction_WhenCalledByGuest_ShouldReturnForbidden403_WithErrGuestPersonalizationRestricted()
    {
        // Arrange
        var client = CreateTestClient();
        var guestToken = GenerateJwt(UserRoles.Guest, guestUuid: Guid.NewGuid().ToString());
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", guestToken);

        // Act: Khách bấm tính năng cá nhân hóa (BMI/TDEE, Hồ sơ cá nhân)
        var response = await client.GetAsync("/api/v1/test/auth/personalization-action");

        // Assert: Chặn Khách và trả thông báo mời đăng ký
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        var error = await response.Content.ReadFromJsonAsync<ApiErrorResponse>(new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        error.Should().NotBeNull();
        error!.ErrorCode.Should().Be(ErrorCodes.ErrGuestPersonalizationRestricted);
        error.Message.Should().Contain("đăng ký");

        var detailsElement = (JsonElement)error.Details!;
        detailsElement.GetProperty("registerUrl").GetString().Should().Be("/register");
        detailsElement.GetProperty("loginUrl").GetString().Should().Be("/login");
    }

    [Fact]
    public async Task MemberOnlyAction_WhenCalledByUnauthenticatedUser_ShouldReturnUnauthorized401()
    {
        // Arrange
        var client = CreateTestClient();

        // Act: Người dùng chưa có token gọi tính năng thành viên
        var response = await client.PostAsync("/api/v1/test/auth/community-action", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        var error = await response.Content.ReadFromJsonAsync<ApiErrorResponse>(new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        error.Should().NotBeNull();
        error!.ErrorCode.Should().Be(ErrorCodes.ErrAuthInvalidToken);

        var detailsElement = (JsonElement)error.Details!;
        detailsElement.GetProperty("registerUrl").GetString().Should().Be("/register");
        detailsElement.GetProperty("loginUrl").GetString().Should().Be("/login");
    }

    [Fact]
    public async Task CommunityAndPersonalizationActions_WhenCalledByMember_ShouldReturnOk200()
    {
        // Arrange
        var client = CreateTestClient();
        var memberToken = GenerateJwt(UserRoles.Member, userId: 101);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", memberToken);

        // Act & Assert 1: Thao tác cộng đồng
        var commResp = await client.PostAsync("/api/v1/test/auth/community-action", null);
        commResp.StatusCode.Should().Be(HttpStatusCode.OK);

        // Act & Assert 2: Thao tác cá nhân hóa
        var persResp = await client.GetAsync("/api/v1/test/auth/personalization-action");
        persResp.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // =========================================================================
    // 2. KHỞI TẠO PHIÊN KHÁCH & ĐẾM LƯỢT DÙNG THỬ 20 YÊU CẦU CÙNG LÚC
    // =========================================================================

    [Fact]
    public async Task AnonymousSession_ShouldCreateGuestUuidAndSetCookie()
    {
        // Arrange
        var client = CreateTestClient();

        // Act
        var response = await client.PostAsync("/api/v1/auth/anonymous", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<CreateAnonymousSessionResponse>>();
        body.Should().NotBeNull();
        body!.Data.Should().NotBeNull();
        body.Data.Role.Should().Be("GUEST");
        body.Data.AccessToken.Should().NotBeNullOrEmpty();
        body.Data.GuestId.Should().NotBeNullOrEmpty();
        body.Data.CountryCode.Should().Be("VN");

        // Kiểm tra Cookie guest_uuid được trả về
        response.Headers.Contains("Set-Cookie").Should().BeTrue();
    }

    [Fact]
    public async Task TrialEndpoint_20ConcurrentRequests_ShouldNeverExceedLimitOf3()
    {
        // Arrange: Mục tiêu nghiệm thu: Thử 20 yêu cầu cùng lúc không vượt hạn mức
        var client = CreateTestClient();
        var guestUuid = Guid.NewGuid().ToString();

        var successCount = 0;
        var quotaExceededCount = 0;

        // Act: Bắn đồng thời 20 HTTP requests vào endpoint dùng thử AI
        var tasks = Enumerable.Range(0, 20).Select(async _ =>
        {
            var req = new HttpRequestMessage(HttpMethod.Post, "/api/v1/guest/trial/ai");
            req.Headers.Add("X-Guest-UUID", guestUuid);

            var res = await client.SendAsync(req);
            if (res.StatusCode == HttpStatusCode.OK)
            {
                Interlocked.Increment(ref successCount);
            }
            else if (res.StatusCode == HttpStatusCode.TooManyRequests)
            {
                var json = await res.Content.ReadAsStringAsync();
                if (json.Contains(ErrorCodes.ErrQuotaAiExceeded))
                {
                    Interlocked.Increment(ref quotaExceededCount);
                }
            }
        });

        await Task.WhenAll(tasks);

        // Assert
        successCount.Should().Be(3, "Hạn mức dùng thử tối đa là đúng 3 lượt");
        quotaExceededCount.Should().Be(17, "17 yêu cầu cùng lúc còn lại phải bị từ chối 429 Too Many Requests");

        // Tra cứu lại quota
        var quotaReq = new HttpRequestMessage(HttpMethod.Get, "/api/v1/guest/quota");
        quotaReq.Headers.Add("X-Guest-UUID", guestUuid);
        var quotaResp = await client.SendAsync(quotaReq);

        quotaResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var quotaBody = await quotaResp.Content.ReadFromJsonAsync<ApiResponse<GetGuestQuotaResponse>>();
        quotaBody!.Data!.Ai.Used.Should().Be(3);
        quotaBody.Data.Ai.Remaining.Should().Be(0);
    }

    // =========================================================================
    // 3. GIỚI HẠN 10 YÊU CẦU/PHÚT MỖI IP, VƯỢT THÌ CHẶN 10 PHÚT (BR-GUEST-SEC-01)
    // =========================================================================

    [Fact]
    public async Task RateLimiting_WhenOver10RequestsPerMinute_ShouldBlockFor10Minutes()
    {
        // Arrange: Dùng một IP duy nhất cho test case này
        var client = CreateTestClient();
        var testIp = "203.0.113." + new Random().Next(10, 200);

        // 10 yêu cầu đầu tiên trong 1 phút phải thành công
        for (int i = 1; i <= 10; i++)
        {
            var req = new HttpRequestMessage(HttpMethod.Post, "/api/v1/auth/anonymous");
            req.Headers.Add("X-Forwarded-For", testIp);
            var resp = await client.SendAsync(req);
            resp.StatusCode.Should().Be(HttpStatusCode.OK, $"Yêu cầu thứ {i} trong hạn mức 10 req/phút phải thành công");
        }

        // Act: Yêu cầu thứ 11 vượt quá hạn mức 10 req/phút -> kích hoạt tạm khóa 10 phút
        var req11 = new HttpRequestMessage(HttpMethod.Post, "/api/v1/auth/anonymous");
        req11.Headers.Add("X-Forwarded-For", testIp);
        var resp11 = await client.SendAsync(req11);

        // Assert: Bị từ chối 429 và thông báo khóa 10 phút
        resp11.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
        var err11 = await resp11.Content.ReadFromJsonAsync<ApiErrorResponse>(new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        err11.Should().NotBeNull();
        err11!.ErrorCode.Should().Be(ErrorCodes.ErrGuestRateLimitExceeded);
        err11.Message.Should().Contain("10 phút");

        // Yêu cầu thứ 12 tiếp tục bị chặn bởi lệnh cấm 10 phút và trả header Retry-After
        var req12 = new HttpRequestMessage(HttpMethod.Post, "/api/v1/auth/anonymous");
        req12.Headers.Add("X-Forwarded-For", testIp);
        var resp12 = await client.SendAsync(req12);

        resp12.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
        resp12.Headers.Contains("Retry-After").Should().BeTrue();

        // Thành viên đã đăng nhập từ cùng địa chỉ IP này không bị áp hạn ngạch Rate Limit của Khách
        var memberToken = GenerateJwt(UserRoles.Member, userId: 202);
        var memberReq = new HttpRequestMessage(HttpMethod.Get, "/api/v1/test/auth/member-only");
        memberReq.Headers.Add("X-Forwarded-For", testIp);
        memberReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", memberToken);

        var memberResp = await client.SendAsync(memberReq);
        memberResp.StatusCode.Should().Be(HttpStatusCode.OK, "Thành viên chính thức được miễn trừ Rate Limit của Khách");
    }

    [Fact]
    public async Task RateLimiting_20ConcurrentRequestsFromSameIp_First10Pass_Next10Blocked()
    {
        // Arrange
        var client = CreateTestClient();
        var testIp = "198.51.100." + new Random().Next(10, 200);

        var okCount = 0;
        var blockedCount = 0;

        // Act: 20 request đồng thời từ cùng 1 IP
        var tasks = Enumerable.Range(0, 20).Select(async _ =>
        {
            var req = new HttpRequestMessage(HttpMethod.Post, "/api/v1/auth/anonymous");
            req.Headers.Add("X-Forwarded-For", testIp);
            var res = await client.SendAsync(req);

            if (res.StatusCode == HttpStatusCode.OK)
            {
                Interlocked.Increment(ref okCount);
            }
            else if (res.StatusCode == HttpStatusCode.TooManyRequests)
            {
                Interlocked.Increment(ref blockedCount);
            }
        });

        await Task.WhenAll(tasks);

        // Assert
        okCount.Should().Be(10, "Đúng 10 yêu cầu đầu tiên được chấp nhận trong hạn mức");
        blockedCount.Should().Be(10, "10 yêu cầu vượt hạn mức bị chặn với 429 Too Many Requests");
    }

    // =========================================================================
    // 4. CHỈ CHO DÙNG TỪ VIỆT NAM (CÓ CÔNG TẮC BẬT/TẮT KHI CHẠY THỬ)
    // =========================================================================

    [Fact]
    public async Task RegionCheck_WhenEnforceVietnamOnlyIsFalse_ForeignIpShouldBeAllowed()
    {
        // Arrange: Công tắc EnforceVietnamOnly = false (mặc định môi trường chạy thử)
        var client = CreateTestClient(cfg =>
        {
            cfg["GuestSecurity:EnforceVietnamOnly"] = "false";
        });

        var req = new HttpRequestMessage(HttpMethod.Post, "/api/v1/auth/anonymous");
        req.Headers.Add("X-Country-Code", "US"); // Quốc gia nước ngoài
        req.Headers.Add("X-Forwarded-For", "54.239.28.85");

        // Act
        var res = await client.SendAsync(req);

        // Assert: Được phép truy cập do công tắc đang TẮT
        res.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task RegionCheck_WhenEnforceVietnamOnlyIsTrue_ForeignIpShouldReturnForbidden403()
    {
        // Arrange: Công tắc EnforceVietnamOnly = true (BẬT kiểm soát lãnh thổ)
        var client = CreateTestClient(cfg =>
        {
            cfg["GuestSecurity:EnforceVietnamOnly"] = "true";
            cfg["GuestSecurity:AllowedCountryCodes:0"] = "VN";
        });

        // 1. Gửi request từ Mỹ (US)
        var foreignReq = new HttpRequestMessage(HttpMethod.Post, "/api/v1/auth/anonymous");
        foreignReq.Headers.Add("X-Country-Code", "US");
        foreignReq.Headers.Add("X-Forwarded-For", "54.239.28.85");

        var foreignRes = await client.SendAsync(foreignReq);

        // Assert: Bị từ chối 403 Forbidden với mã ERR_GUEST_REGION_UNSUPPORTED
        foreignRes.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        var foreignErr = await foreignRes.Content.ReadFromJsonAsync<ApiErrorResponse>(new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        foreignErr.Should().NotBeNull();
        foreignErr!.ErrorCode.Should().Be(ErrorCodes.ErrGuestRegionUnsupported);
        foreignErr.Message.Should().Contain("Việt Nam");

        // 2. Gửi request từ Việt Nam (VN)
        var vnReq = new HttpRequestMessage(HttpMethod.Post, "/api/v1/auth/anonymous");
        vnReq.Headers.Add("X-Country-Code", "VN");
        vnReq.Headers.Add("X-Forwarded-For", "118.69.182.10");

        var vnRes = await client.SendAsync(vnReq);

        // Assert: Thành công 200 OK
        vnRes.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
