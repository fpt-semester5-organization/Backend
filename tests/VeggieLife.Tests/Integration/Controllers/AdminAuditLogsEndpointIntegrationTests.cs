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
using VeggieLife.BLL.DTOs.Admin.AuditLogs;
using VeggieLife.BLL.Services.Cache;
using VeggieLife.DAL.Context;
using VeggieLife.DAL.Entities;
using Xunit;

namespace VeggieLife.Tests.Integration.Controllers;

/// <summary>
/// Integration Tests cho API Quản trị nhật ký kiểm toán (FN-ADM-17).
/// Kiểm thử đầy đủ:
/// - BR-ADM-SYS-002: Bảng chỉ ghi (Append-only); cấm PUT/DELETE; dữ liệu nhạy cảm được che giấu.
/// - Xác thực JWT & Phân quyền Admin (401 khi thiếu token, 403 khi role khác Admin).
/// - Bộ lọc theo actorUserId, action, targetTable, targetId, from/to.
/// - Phân trang chuẩn Envelope D52 (page, size, total).
/// </summary>
public class AdminAuditLogsEndpointIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly Mock<IRedisService> _mockRedis = new();
    private readonly string _testDbName = "VeggieLifeAdminAuditLogsIntegrationDb_" + Guid.NewGuid();

    public AdminAuditLogsEndpointIntegrationTests(WebApplicationFactory<Program> factory)
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

        if (!db.AuditLogs.Any())
        {
            db.AuditLogs.AddRange(
                new AuditLog
                {
                    AuditId = 5001,
                    ActorUserId = 100,
                    ActorRole = "ADMIN",
                    Action = "USER_SANCTION",
                    TargetTable = "Users",
                    TargetId = "1024",
                    Note = "Vi phạm lần 4",
                    OldValue = "{\"accountStatus\":\"ACTIVE\",\"passwordHash\":\"$2a$secretHashOld\"}",
                    NewValue = "{\"accountStatus\":\"SUSPENDED\",\"token\":\"jwt_secret_token\"}",
                    CreatedAt = new DateTime(2026, 10, 5, 10, 0, 0, DateTimeKind.Utc)
                },
                new AuditLog
                {
                    AuditId = 5002,
                    ActorUserId = 100,
                    ActorRole = "ADMIN",
                    Action = "RECIPE_VERIFY",
                    TargetTable = "Recipes",
                    TargetId = "200",
                    Note = "Kiểm duyệt công thức",
                    OldValue = "{\"isVerified\":false}",
                    NewValue = "{\"isVerified\":true}",
                    CreatedAt = new DateTime(2026, 10, 6, 11, 0, 0, DateTimeKind.Utc)
                },
                new AuditLog
                {
                    AuditId = 5003,
                    ActorUserId = 101,
                    ActorRole = "ADMIN",
                    Action = "CATEGORY_CREATE",
                    TargetTable = "FoodCategories",
                    TargetId = "10",
                    Note = "Tạo danh mục mới",
                    CreatedAt = new DateTime(2026, 10, 7, 14, 0, 0, DateTimeKind.Utc)
                }
            );

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
    public async Task ListAuditLogs_WithoutToken_Returns401Unauthorized()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/v1/admin/audit-logs");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Theory]
    [InlineData(UserRoles.Member)]
    [InlineData(UserRoles.Vip)]
    public async Task ListAuditLogs_WithNonAdminToken_Returns403Forbidden(string role)
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", GenerateJwtToken(200, role));

        var response = await client.GetAsync("/api/v1/admin/audit-logs");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        var body = await response.Content.ReadFromJsonAsync<ApiErrorResponse>();
        body.Should().NotBeNull();
        body!.ErrorCode.Should().Be(ErrorCodes.ErrAdmForbidden);
    }

    [Fact]
    public async Task ListAuditLogs_WithAdminToken_Returns200WithMatchingEnvelope()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", GenerateJwtToken(100, UserRoles.Admin));

        var response = await client.GetAsync("/api/v1/admin/audit-logs?page=1&size=20");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<List<AdminAuditLogListItemDto>>>();

        body.Should().NotBeNull();
        body!.StatusCode.Should().Be(200);
        body.Message.Should().Be("OK");
        body.Data.Should().NotBeNull();
        body.Data!.Count.Should().BeGreaterThanOrEqualTo(3);
        body.Meta.Should().NotBeNull();
        body.Meta!.Page.Should().Be(1);
        body.Meta.Size.Should().Be(20);
        body.Meta.Total.Should().BeGreaterThanOrEqualTo(3);

        // Kiểm tra cấu trúc item đầu tiên khớp đặc tả
        var first = body.Data.First(d => d.AuditId == 5001);
        first.ActorUserId.Should().Be(100);
        first.ActorRole.Should().Be("ADMIN");
        first.Action.Should().Be("USER_SANCTION");
        first.TargetTable.Should().Be("Users");
        first.TargetId.Should().Be("1024");
        first.Note.Should().Be("Vi phạm lần 4");

        // Kiểm tra che giấu dữ liệu nhạy cảm
        if (first.OldValue != null)
        {
            first.OldValue.Should().NotContain("secretHashOld");
            first.OldValue.Should().Contain("\"passwordHash\":\"***\"");
        }
        if (first.NewValue != null)
        {
            first.NewValue.Should().NotContain("jwt_secret_token");
            first.NewValue.Should().Contain("\"token\":\"***\"");
        }
    }

    [Fact]
    public async Task ListAuditLogs_FilterByAction_ReturnsMatchingLogs()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", GenerateJwtToken(100, UserRoles.Admin));

        var response = await client.GetAsync("/api/v1/admin/audit-logs?action=USER_SANCTION");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<List<AdminAuditLogListItemDto>>>();

        body.Should().NotBeNull();
        body!.Data.Should().NotBeNull();
        body.Data!.All(l => l.Action == "USER_SANCTION").Should().BeTrue();
    }

    [Fact]
    public async Task ListAuditLogs_FilterByDateRange_ReturnsMatchingLogs()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", GenerateJwtToken(100, UserRoles.Admin));

        var response = await client.GetAsync("/api/v1/admin/audit-logs?from=2026-10-06&to=2026-10-06");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<List<AdminAuditLogListItemDto>>>();

        body.Should().NotBeNull();
        body!.Data.Should().NotBeNull();
        body.Data!.Should().ContainSingle(l => l.AuditId == 5002);
    }

    [Theory]
    [InlineData("?page=0")]
    [InlineData("?size=0")]
    [InlineData("?size=150")]
    [InlineData("?actorUserId=-1")]
    [InlineData("?from=invalid-date")]
    [InlineData("?from=2026-10-20&to=2026-10-10")]
    public async Task ListAuditLogs_InvalidParameters_Returns400ValidationError(string query)
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", GenerateJwtToken(100, UserRoles.Admin));

        var response = await client.GetAsync($"/api/v1/admin/audit-logs{query}");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadFromJsonAsync<ApiErrorResponse>();
        body.Should().NotBeNull();
        body!.ErrorCode.Should().Be(ErrorCodes.ErrValidation);
    }

    [Fact]
    public async Task AuditLogs_AppendOnly_PutOrDeleteNotAllowed()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", GenerateJwtToken(100, UserRoles.Admin));

        // Kiểm tra PUT /api/v1/admin/audit-logs/5001 không được phép (405 Method Not Allowed hoặc 404)
        var putResponse = await client.PutAsync("/api/v1/admin/audit-logs/5001", JsonContent.Create(new { note = "update" }));
        ((int)putResponse.StatusCode).Should().BeOneOf(404, 405);

        // Kiểm tra DELETE /api/v1/admin/audit-logs/5001 không được phép
        var deleteResponse = await client.DeleteAsync("/api/v1/admin/audit-logs/5001");
        ((int)deleteResponse.StatusCode).Should().BeOneOf(404, 405);
    }
}
