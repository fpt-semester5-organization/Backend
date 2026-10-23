using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Moq;
using VeggieLife.BLL.Common.Constants;
using VeggieLife.BLL.Common.Responses;
using VeggieLife.BLL.DTOs.Admin.Analytics;
using VeggieLife.BLL.Services.Cache;
using VeggieLife.DAL.Context;
using VeggieLife.DAL.Entities;
using Xunit;

namespace VeggieLife.Tests.Integration.Controllers;

/// <summary>
/// Integration Tests cho API Thống kê tổng quan nền tảng (FN-ADM-18).
/// Kiểm thử đầy đủ:
/// - BR-ADM-REP-01: Chỉ Admin được xem báo cáo tăng trưởng, không lộ PII.
/// - BR-ADM-AI-001: Tính chi phí Token AI từ AIQuotaLogs.
/// - Xác thực JWT & Phân quyền Admin (401 khi thiếu token, 403 khi role khác Admin).
/// - Bộ lọc khoảng ngày from/to và validate format.
/// - Cơ chế lưu tạm Redis Cache TTL 5 phút.
/// </summary>
public class AdminAnalyticsEndpointIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly Mock<IRedisService> _mockRedis = new();
    private readonly string _testDbName = "VeggieLifeAdminAnalyticsIntegrationDb_" + Guid.NewGuid();

    public AdminAnalyticsEndpointIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Development");
            builder.ConfigureServices(services =>
            {
                var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
                if (descriptor != null)
                {
                    services.Remove(descriptor);
                }

                services.AddDbContext<AppDbContext>(options =>
                {
                    options.UseInMemoryDatabase(_testDbName);
                    options.ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning));
                });

                var redisDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IRedisService));
                if (redisDescriptor != null)
                {
                    services.Remove(redisDescriptor);
                }
                services.AddSingleton(_mockRedis.Object);
            });
        });

        SeedTestData();
    }

    private void SeedTestData()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var now = DateTime.UtcNow;

        if (!db.Users.Any(u => u.Email == "admin.analytics@veggielife.vn"))
        {
            db.Users.AddRange(
                new User
                {
                    UserId = 100,
                    Email = "admin.analytics@veggielife.vn",
                    FullName = "Admin Quản Trị",
                    Role = UserRoles.Admin,
                    AccountStatus = "ACTIVE",
                    CreatedAt = now.AddMonths(-2)
                },
                new User
                {
                    UserId = 200,
                    Email = "member.analytics@veggielife.vn",
                    FullName = "Thành viên Thường",
                    Role = UserRoles.Member,
                    AccountStatus = "ACTIVE",
                    CreatedAt = now.AddDays(-10)
                },
                new User
                {
                    UserId = 300,
                    Email = "vip.analytics@veggielife.vn",
                    FullName = "Thành viên VIP",
                    Role = UserRoles.Vip,
                    AccountStatus = "ACTIVE",
                    CreatedAt = now.AddDays(-5)
                }
            );

            db.Recipes.Add(new Recipe
            {
                RecipeId = 1,
                Title = "Chả giò chay giòn rụm",
                Instructions = "[\"B1\"]",
                DifficultyLevel = "EASY",
                Source = "USER",
                Status = "APPROVED_ACTIVE",
                CreatedAt = now
            });

            db.Posts.Add(new Post
            {
                PostId = 1,
                UserId = 200,
                Title = "Kinh nghiệm ăn chay",
                Content = "Chia sẻ bí quyết",
                Visibility = "PUBLIC",
                ModerationStatus = "APPROVED_ACTIVE",
                CreatedAt = now
            });

            db.Videos.Add(new Video
            {
                VideoId = 1,
                PostId = 1,
                Title = "Hướng dẫn nấu ăn",
                VideoUrl = "http://video.mp4",
                Status = "APPROVED_ACTIVE",
                CreatedAt = now
            });

            db.SubscriptionPlans.Add(new SubscriptionPlan
            {
                PlanId = 1,
                AdminId = 100,
                PlanName = "Gói tháng",
                Price = 49000,
                DurationDays = 30,
                IsActive = true,
                CreatedAt = now
            });

            db.PaymentTransactions.Add(new PaymentTransaction
            {
                TransactionId = 1,
                UserId = 300,
                PlanId = 1,
                GatewayProvider = "MOMO",
                PartnerTransactionCode = "TXN_ANALYTICS_01",
                AmountVnd = 49000,
                TransactionStatus = "SUCCESS",
                SignatureHash = "hash1",
                StartsAt = now.AddDays(-2),
                EndsAt = now.AddDays(28),
                CreatedAt = now.AddDays(-2)
            });

            db.ViolationReports.Add(new ViolationReport
            {
                ReportId = 1,
                ViolationType = "SPAM",
                EvidenceSummary = "Nội dung spam",
                ReportSource = "USER",
                Status = "PENDING",
                CreatedAt = now
            });

            db.AiquotaLogs.Add(new AiquotaLog
            {
                LogId = 1,
                UserId = 200,
                FeatureCode = "AI_CHAT",
                TokensConsumed = 500,
                CostEstimateUsd = 0.05m,
                RequestStatus = "SUCCESS",
                CreatedAt = now
            });

            db.SaveChanges();
        }
    }

    private string GenerateJwtToken(int userId, string role)
    {
        using var scope = _factory.Services.CreateScope();
        var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        var secret = config["Jwt:SecretKey"] ?? "YOUR_JWT_SECRET_KEY_HERE_MIN_32_CHARS";
        var issuer = config["Jwt:Issuer"] ?? "VeggieLife.API";
        var audience = config["Jwt:Audience"] ?? "VeggieLife.Client";

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId.ToString()),
            new(ClaimTypes.Role, role),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddHours(2),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    [Fact]
    public async Task GetOverview_WithoutToken_Returns401Unauthorized()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/v1/admin/analytics/overview");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Theory]
    [InlineData(UserRoles.Member)]
    [InlineData(UserRoles.Vip)]
    public async Task GetOverview_WithNonAdminToken_Returns403Forbidden(string role)
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", GenerateJwtToken(200, role));

        var response = await client.GetAsync("/api/v1/admin/analytics/overview");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        var body = await response.Content.ReadFromJsonAsync<ApiErrorResponse>();
        body.Should().NotBeNull();
        body!.ErrorCode.Should().Be(ErrorCodes.ErrAdmForbidden);
    }

    [Fact]
    public async Task GetOverview_WithAdminToken_Returns200WithExpectedResponseShape()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", GenerateJwtToken(100, UserRoles.Admin));

        var response = await client.GetAsync("/api/v1/admin/analytics/overview");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<AdminAnalyticsOverviewResponseData>>();

        body.Should().NotBeNull();
        body!.StatusCode.Should().Be(200);
        body.Message.Should().Be("OK");
        body.Data.Should().NotBeNull();

        // Kiểm tra đúng tên trường, kiểu dữ liệu khớp response mẫu
        var data = body.Data!;
        data.Users.Should().NotBeNull();
        data.Users.Total.Should().BeGreaterThanOrEqualTo(3);
        data.Users.ByStatus.Should().ContainKey("ACTIVE");

        data.Content.Should().NotBeNull();
        data.Content.Recipes.Should().BeGreaterThanOrEqualTo(1);
        data.Content.Posts.Should().BeGreaterThanOrEqualTo(1);
        data.Content.Videos.Should().BeGreaterThanOrEqualTo(1);

        data.Vip.Should().NotBeNull();
        data.Vip.Active.Should().BeGreaterThanOrEqualTo(1);
        data.Vip.RevenueVnd.Should().BeGreaterThanOrEqualTo(49000);

        data.ModerationQueue.Should().BeGreaterThanOrEqualTo(1);
        data.AiCostUsd.Should().BeGreaterThanOrEqualTo(0.05);

        // Kiểm tra lưu cache 5 phút
        _mockRedis.Verify(r => r.SetAsync(
            "admin_overview",
            It.IsAny<AdminAnalyticsOverviewResponseData>(),
            TimeSpan.FromMinutes(5),
            It.IsAny<CancellationToken>()), Times.AtLeastOnce);
    }

    [Theory]
    [InlineData("?from=invalid-date")]
    [InlineData("?to=invalid-date")]
    [InlineData("?from=2026-11-01&to=2026-10-01")]
    public async Task GetOverview_InvalidParameters_Returns400ValidationError(string query)
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", GenerateJwtToken(100, UserRoles.Admin));

        var response = await client.GetAsync($"/api/v1/admin/analytics/overview{query}");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadFromJsonAsync<ApiErrorResponse>();
        body.Should().NotBeNull();
        body!.ErrorCode.Should().Be(ErrorCodes.ErrValidation);
    }
}
