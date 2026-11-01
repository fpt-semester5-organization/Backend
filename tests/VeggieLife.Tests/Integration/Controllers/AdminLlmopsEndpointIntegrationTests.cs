using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
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
using VeggieLife.BLL.DTOs.Admin.Llmops;
using VeggieLife.BLL.Services.Cache;
using VeggieLife.DAL.Context;
using VeggieLife.DAL.Entities;
using Xunit;

namespace VeggieLife.Tests.Integration.Controllers;

/// <summary>
/// Integration Tests cho API Dashboard Token & Chi phí AI (LLMOps) (FN-ADM-16).
/// Kiểm thử:
/// - GET /api/v1/admin/llmops/dashboard
/// - 401 Unauthorized khi thiếu token JWT.
/// - 403 Forbidden khi role không phải ADMIN (MEMBER, VIP).
/// - 200 OK khi role ADMIN, cấu trúc phản hồi đúng DTO GetAdminLlmopsDashboardResponse (Envelope).
/// - Bộ lọc ngày from, to hoạt động chính xác.
/// </summary>
public class AdminLlmopsEndpointIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly Mock<IRedisService> _mockRedis = new();
    private readonly string _testDbName = "VeggieLifeAdminLlmopsIntegrationDb_" + Guid.NewGuid();

    public AdminLlmopsEndpointIntegrationTests(WebApplicationFactory<Program> factory)
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

        if (!db.Users.Any(u => u.Email == "admin.llmops@veggielife.vn"))
        {
            db.Users.AddRange(
                new User
                {
                    UserId = 101,
                    Email = "admin.llmops@veggielife.vn",
                    FullName = "Admin LLMOps",
                    Role = UserRoles.Admin,
                    AccountStatus = "ACTIVE",
                    CreatedAt = now.AddMonths(-1)
                },
                new User
                {
                    UserId = 201,
                    Email = "member.llmops@veggielife.vn",
                    FullName = "Member Normal",
                    Role = UserRoles.Member,
                    AccountStatus = "ACTIVE",
                    CreatedAt = now.AddMonths(-1)
                }
            );

            db.AiquotaLogs.AddRange(
                new AiquotaLog
                {
                    LogId = 1,
                    UserId = 101,
                    FeatureCode = "AI_CHAT",
                    TokensConsumed = 120000,
                    CostEstimateUsd = 4.2m,
                    RequestStatus = "SUCCESS",
                    CreatedAt = new DateTime(2026, 10, 5, 10, 0, 0, DateTimeKind.Utc)
                },
                new AiquotaLog
                {
                    LogId = 2,
                    UserId = 101,
                    FeatureCode = "MEAL_PLAN_GEN",
                    TokensConsumed = 30000,
                    CostEstimateUsd = 1.2m,
                    RequestStatus = "SUCCESS",
                    CreatedAt = new DateTime(2026, 10, 5, 12, 0, 0, DateTimeKind.Utc)
                },
                new AiquotaLog
                {
                    LogId = 3,
                    UserId = 101,
                    FeatureCode = "AI_CHAT",
                    TokensConsumed = 80000,
                    CostEstimateUsd = 2.75m,
                    RequestStatus = "SUCCESS",
                    CreatedAt = new DateTime(2026, 10, 6, 15, 0, 0, DateTimeKind.Utc)
                }
            );

            db.SaveChanges();
        }
    }

    private string GenerateJwtToken(int userId, string role)
    {
        using var scope = _factory.Services.CreateScope();
        var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        var secret = config["Jwt:SecretKey"] ?? "VeggieLifeSecretKeyForDevelopmentMustBe32CharsLong!";
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
    public async Task GetDashboard_WithoutToken_Returns401Unauthorized()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/v1/admin/llmops/dashboard");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Theory]
    [InlineData(UserRoles.Member)]
    [InlineData(UserRoles.Vip)]
    public async Task GetDashboard_WithNonAdminToken_Returns403Forbidden(string role)
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", GenerateJwtToken(201, role));

        var response = await client.GetAsync("/api/v1/admin/llmops/dashboard");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        var body = await response.Content.ReadFromJsonAsync<ApiErrorResponse>();
        body.Should().NotBeNull();
        body!.ErrorCode.Should().Be(ErrorCodes.ErrAdmForbidden);
    }

    [Fact]
    public async Task GetDashboard_WithAdminToken_Returns200AndValidDto()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", GenerateJwtToken(101, UserRoles.Admin));

        var response = await client.GetAsync("/api/v1/admin/llmops/dashboard?from=2026-10-05&to=2026-10-06");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<ApiResponse<AdminLlmopsDashboardResponseData>>();
        body.Should().NotBeNull();
        body!.StatusCode.Should().Be(200);
        body.Data.Should().NotBeNull();

        var data = body.Data!;
        data.Series.Should().HaveCount(3);
        data.DailyBudgetUsd.Should().Be(12.5m);
        data.Total.Tokens.Should().Be(230000);
        data.Total.CostUsd.Should().Be(8.15m);
        data.DayTotals["2026-10-05"].CostUsd.Should().Be(5.4m);
        data.DayTotals["2026-10-06"].CostUsd.Should().Be(2.75m);
        data.FeatureTotals["AI_CHAT"].CostUsd.Should().Be(6.95m);
        data.FeatureTotals["MEAL_PLAN_GEN"].CostUsd.Should().Be(1.2m);
    }

    [Fact]
    public async Task GetLogs_WithAdminToken_Returns200AndPaginatedLogs()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", GenerateJwtToken(101, UserRoles.Admin));

        var response = await client.GetAsync("/api/v1/admin/llmops/logs?from=2026-10-05&to=2026-10-06&page=1&pageSize=10");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<ApiResponse<GetAdminLlmopsLogsResponseData>>();
        body.Should().NotBeNull();
        body!.StatusCode.Should().Be(200);
        body.Data.Should().NotBeNull();

        var data = body.Data!;
        data.Total.Should().Be(3);
        data.Items.Should().HaveCount(3);
        data.Page.Should().Be(1);
    }
}
