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
using Moq;
using VeggieLife.BLL.Common.Constants;
using VeggieLife.BLL.Common.Responses;
using VeggieLife.BLL.DTOs.Admin.Venues;
using VeggieLife.BLL.DTOs.Venues.Responses;
using VeggieLife.BLL.Services.Cache;
using VeggieLife.DAL.Context;
using VeggieLife.DAL.Entities;
using Xunit;

namespace VeggieLife.Tests.Integration.Controllers;

/// <summary>
/// Bộ kiểm thử tích hợp (Integration Tests) gọi qua HTTP pipeline cho các endpoint quản trị quán chay (FN-ADM-14).
/// </summary>
public class AdminVenuesEndpointIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly Mock<IRedisService> _mockRedis = new();
    private readonly string _testDbName = "VeggieLifeAdminVenuesIntegrationDb_" + Guid.NewGuid();

    public AdminVenuesEndpointIntegrationTests(WebApplicationFactory<Program> factory)
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

                var sp = services.BuildServiceProvider();
                using var scope = sp.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                db.Database.EnsureDeleted();
                db.Database.EnsureCreated();
                SeedTestData(db);
            });
        });

        _mockRedis.Setup(r => r.RemoveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
    }

    private static void SeedTestData(AppDbContext db)
    {
        var admin = new User
        {
            UserId = 1,
            Email = "admin.venues@veggielife.vn",
            FullName = "Admin Quán Chay",
            Role = UserRoles.Admin,
            AccountStatus = "ACTIVE",
            CreatedAt = DateTime.UtcNow
        };
        var member = new User
        {
            UserId = 2,
            Email = "member.venues@veggielife.vn",
            FullName = "Hội Viên 2",
            Role = UserRoles.Member,
            AccountStatus = "ACTIVE",
            CreatedAt = DateTime.UtcNow
        };
        db.Users.AddRange(admin, member);

        var venue1 = new VeganVenue
        {
            VenueId = 301,
            ExternalSource = "HAPPYCOW",
            ExternalPlaceId = "hc_301",
            VenueName = "Cơm chay Tâm An",
            Address = "123 Cách Mạng Tháng Tám, Quận 3, TP.HCM",
            Latitude = 10.776m,
            Longitude = 106.690m,
            Rating = 4.5m,
            IsFullyVegan = true,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        var venue2 = new VeganVenue
        {
            VenueId = 302,
            ExternalSource = "GOOGLE_PLACES",
            ExternalPlaceId = "gp_302",
            VenueName = "Ẩm Thực Chay Giác Ngộ",
            Address = "213 Hùng Vương, Quận 5, TP.HCM",
            Latitude = 10.758m,
            Longitude = 106.667m,
            Rating = 4.2m,
            IsFullyVegan = true,
            IsActive = false, // Seeded as hidden
            CreatedAt = DateTime.UtcNow
        };
        db.VeganVenues.AddRange(venue1, venue2);

        db.SaveChanges();
    }

    private string GenerateJwtToken(int userId, string role)
    {
        using var scope = _factory.Services.CreateScope();
        var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        var secretKey = config["Jwt:SecretKey"]
            ?? config["JwtSettings:SecretKey"]
            ?? "veggielife-development-super-secret-jwt-key-32-chars-long";
        var issuer = config["Jwt:Issuer"] ?? config["JwtSettings:Issuer"] ?? "VeggieLife.API";
        var audience = config["Jwt:Audience"] ?? config["JwtSettings:Audience"] ?? "VeggieLife.Client";

        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes(secretKey);

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                new Claim(ClaimTypes.Role, role),
                new Claim(ClaimTypes.Email, $"user{userId}@veggielife.vn")
            }),
            Expires = DateTime.UtcNow.AddHours(2),
            Issuer = issuer,
            Audience = audience,
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }

    private HttpClient CreateAdminClient()
    {
        var client = _factory.CreateClient();
        var token = GenerateJwtToken(1, UserRoles.Admin);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    private HttpClient CreateMemberClient()
    {
        var client = _factory.CreateClient();
        var token = GenerateJwtToken(2, UserRoles.Member);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    #region GET /api/v1/admin/venues

    [Fact]
    public async Task GetVenues_WithoutToken_Returns401Unauthorized()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/v1/admin/venues");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain(ErrorCodes.ErrAuthInvalidToken);
    }

    [Fact]
    public async Task GetVenues_WithMemberToken_Returns403Forbidden()
    {
        var client = CreateMemberClient();
        var response = await client.GetAsync("/api/v1/admin/venues");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain(ErrorCodes.ErrAdmForbidden);
    }

    [Fact]
    public async Task GetVenues_WithAdminToken_Returns200AndValidList()
    {
        var client = CreateAdminClient();
        var response = await client.GetAsync("/api/v1/admin/venues?page=1&size=20");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<List<AdminVenueListItemDto>>>();
        body.Should().NotBeNull();
        body!.StatusCode.Should().Be(200);
        body.Data.Should().NotBeEmpty();
        body.Meta.Should().NotBeNull();
        body.Meta!.Total.Should().BeGreaterThanOrEqualTo(2);

        var activeVenue = body.Data.FirstOrDefault(v => v.VenueId == 301);
        activeVenue.Should().NotBeNull();
        activeVenue!.Name.Should().Be("Cơm chay Tâm An");
        activeVenue.Source.Should().Be("HAPPYCOW");
        activeVenue.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task GetVenues_WithIsActiveFilter_ReturnsOnlyMatchingVenues()
    {
        var client = CreateAdminClient();
        var response = await client.GetAsync("/api/v1/admin/venues?isActive=false");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<List<AdminVenueListItemDto>>>();
        body!.Data.Should().ContainSingle();
        body.Data[0].VenueId.Should().Be(302);
        body.Data[0].IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task GetVenues_WithSourceFilter_ReturnsOnlyMatchingVenues()
    {
        var client = CreateAdminClient();
        var response = await client.GetAsync("/api/v1/admin/venues?source=HAPPYCOW");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<List<AdminVenueListItemDto>>>();
        body!.Data.Should().ContainSingle();
        body.Data[0].Source.Should().Be("HAPPYCOW");
    }

    [Fact]
    public async Task GetVenues_WithKeywordFilter_ReturnsMatchingVenues()
    {
        var client = CreateAdminClient();
        var response = await client.GetAsync("/api/v1/admin/venues?keyword=Tâm An");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<List<AdminVenueListItemDto>>>();
        body!.Data.Should().ContainSingle();
        body.Data[0].VenueId.Should().Be(301);
    }

    [Fact]
    public async Task GetVenues_WithInvalidSource_Returns400BadRequest()
    {
        var client = CreateAdminClient();
        var response = await client.GetAsync("/api/v1/admin/venues?source=UNKNOWN_SOURCE");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain(ErrorCodes.ErrValidation);
    }

    #endregion

    #region PUT /api/v1/admin/venues/{id} & Public User Visibility

    [Fact]
    public async Task UpdateVenue_WithoutToken_Returns401()
    {
        var client = _factory.CreateClient();
        var response = await client.PutAsJsonAsync("/api/v1/admin/venues/301", new UpdateAdminVenueRequest { IsActive = false });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task UpdateVenue_WithMemberToken_Returns403()
    {
        var client = CreateMemberClient();
        var response = await client.PutAsJsonAsync("/api/v1/admin/venues/301", new UpdateAdminVenueRequest { IsActive = false });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task UpdateVenue_NonExistentId_Returns404NotFound()
    {
        var client = CreateAdminClient();
        var response = await client.PutAsJsonAsync("/api/v1/admin/venues/9999", new UpdateAdminVenueRequest { IsActive = false });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain(ErrorCodes.ErrAdmNotFound);
    }

    [Fact]
    public async Task HideVenue_ThenPublicUserCannotSeeVenue_AndReactivatingRestoresVisibility()
    {
        var adminClient = CreateAdminClient();
        var publicClient = _factory.CreateClient();

        // 1. Quán 301 ban đầu đang Active (true) -> Người dùng xem chi tiết thành công (200 OK)
        var userCheck1 = await publicClient.GetAsync("/api/v1/venues/301");
        userCheck1.StatusCode.Should().Be(HttpStatusCode.OK);

        // 2. Admin ẩn quán 301: PUT /api/v1/admin/venues/301 với isActive = false
        var hideResponse = await adminClient.PutAsJsonAsync("/api/v1/admin/venues/301", new UpdateAdminVenueRequest { IsActive = false });
        hideResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var hideBody = await hideResponse.Content.ReadFromJsonAsync<ApiResponse<UpdateAdminVenueResponseData>>();
        hideBody.Should().NotBeNull();
        hideBody!.Data!.VenueId.Should().Be(301);
        hideBody.Data!.IsActive.Should().BeFalse();

        // Verify DB và AuditLog
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var venue = await db.VeganVenues.FindAsync(301);
            venue!.IsActive.Should().BeFalse();

            var audit = await db.AuditLogs.FirstOrDefaultAsync(a => a.Action == "UPDATE_VENUE_STATUS" && a.TargetId == "301");
            audit.Should().NotBeNull();
            audit!.ActorRole.Should().Be(UserRoles.Admin);
        }

        // 3. Tiêu chí nghiệm thu: "quán bị ẩn không hiện ở trang người dùng."
        // Người dùng gọi GET /api/v1/venues/301 -> Phải nhận về 404 (ERR_VENUE_NOT_FOUND)
        var userCheck2 = await publicClient.GetAsync("/api/v1/venues/301");
        userCheck2.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var errContent = await userCheck2.Content.ReadAsStringAsync();
        errContent.Should().Contain(ErrorCodes.ErrVenueNotFound);

        // 4. Admin hiện lại quán 301: PUT /api/v1/admin/venues/301 với isActive = true
        var restoreResponse = await adminClient.PutAsJsonAsync("/api/v1/admin/venues/301", new UpdateAdminVenueRequest { IsActive = true });
        restoreResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // 5. Người dùng xem lại quán 301 thành công (200 OK)
        var userCheck3 = await publicClient.GetAsync("/api/v1/venues/301");
        userCheck3.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    #endregion

    #region POST /api/v1/admin/venues/sync

    [Fact]
    public async Task SyncVenues_WithoutToken_Returns401()
    {
        var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/v1/admin/venues/sync", new SyncAdminVenuesRequest { Province = "TP.HCM" });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task SyncVenues_WithMemberToken_Returns403()
    {
        var client = CreateMemberClient();
        var response = await client.PostAsJsonAsync("/api/v1/admin/venues/sync", new SyncAdminVenuesRequest { Province = "TP.HCM" });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task SyncVenues_ValidProvince_Returns202AcceptedAndAudited()
    {
        var client = CreateAdminClient();
        var req = new SyncAdminVenuesRequest { Province = "Đà Nẵng" };

        var response = await client.PostAsJsonAsync("/api/v1/admin/venues/sync", req);
        response.StatusCode.Should().Be(HttpStatusCode.Accepted);

        var body = await response.Content.ReadFromJsonAsync<ApiResponse<SyncAdminVenuesResponseData>>();
        body.Should().NotBeNull();
        body!.StatusCode.Should().Be(202);
        body.Data!.JobId.Should().StartWith("sync_");

        // Verify AuditLog
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var audit = await db.AuditLogs.FirstOrDefaultAsync(a => a.Action == "SYNC_VENUES");
        audit.Should().NotBeNull();
        audit!.ActorRole.Should().Be(UserRoles.Admin);
        audit.Note.Should().Contain("Đà Nẵng");
    }

    [Fact]
    public async Task SyncVenues_EmptyProvince_Returns400BadRequest()
    {
        var client = CreateAdminClient();
        var req = new SyncAdminVenuesRequest { Province = "" };

        var response = await client.PostAsJsonAsync("/api/v1/admin/venues/sync", req);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain(ErrorCodes.ErrValidation);
    }

    #endregion
}
