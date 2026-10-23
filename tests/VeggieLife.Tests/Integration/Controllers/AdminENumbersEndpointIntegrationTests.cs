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
using VeggieLife.BLL.DTOs.Admin.ENumbers;
using VeggieLife.BLL.Services.Cache;
using VeggieLife.BLL.Services.Knowledge;
using VeggieLife.DAL.Context;
using VeggieLife.DAL.Entities;
using Xunit;

namespace VeggieLife.Tests.Integration.Controllers;

/// <summary>
/// Bộ kiểm thử tích hợp cho API quản trị từ điển phụ gia thực phẩm E-number (FN-ADM-11).
/// </summary>
public class AdminENumbersEndpointIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly Mock<IRedisService> _mockRedis = new();
    private readonly Mock<IKnowledgeSyncQueue> _mockKnowledgeSyncQueue = new();
    private readonly string _testDbName = "VeggieLifeAdminENumbersIntegrationDb_" + Guid.NewGuid();

    public AdminENumbersEndpointIntegrationTests(WebApplicationFactory<Program> factory)
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

                var knowledgeDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IKnowledgeSyncQueue));
                if (knowledgeDescriptor != null)
                {
                    services.Remove(knowledgeDescriptor);
                }
                services.AddSingleton(_mockKnowledgeSyncQueue.Object);
            });
        });

        SeedTestData();
    }

    private void SeedTestData()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        db.Users.AddRange(
            new User { UserId = 1, Email = "admin_enum@veggielife.vn", FullName = "Admin Phụ Gia", Role = UserRoles.Admin, AccountStatus = "ACTIVE", CreatedAt = DateTime.UtcNow },
            new User { UserId = 2, Email = "member_enum@veggielife.vn", FullName = "Member Phụ Gia", Role = UserRoles.Member, AccountStatus = "ACTIVE", CreatedAt = DateTime.UtcNow }
        );

        db.EnumberDictionaries.AddRange(
            new EnumberDictionary { Code = "E100", Name = "Curcumin", CategoryType = "Chất tạo màu", OriginSource = "Nghệ", VeganStatus = "VEGAN", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
            new EnumberDictionary { Code = "E120", Name = "Carmine", CategoryType = "Chất tạo màu", OriginSource = "Rệp son", VeganStatus = "NON_VEGAN", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow }
        );

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

    [Fact]
    public async Task ListENumbers_WithoutToken_Returns401()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/v1/admin/e-numbers");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ListENumbers_AdminToken_Returns200WithPagedItems()
    {
        var client = _factory.CreateClient();
        var adminToken = GenerateJwtToken(1, UserRoles.Admin);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

        var response = await client.GetAsync("/api/v1/admin/e-numbers?veganStatus=VEGAN");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var envelope = await response.Content.ReadFromJsonAsync<ApiResponse<IReadOnlyList<AdminENumberListItemDto>>>();
        envelope!.Data.Should().HaveCount(1);
        envelope.Data.First().Code.Should().Be("E100");
    }

    [Fact]
    public async Task GetENumber_Existing_Returns200()
    {
        var client = _factory.CreateClient();
        var adminToken = GenerateJwtToken(1, UserRoles.Admin);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

        var response = await client.GetAsync("/api/v1/admin/e-numbers/E120");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var envelope = await response.Content.ReadFromJsonAsync<ApiResponse<AdminENumberListItemDto>>();
        envelope.Should().NotBeNull();
        envelope!.Data!.Code.Should().Be("E120");
        envelope.Data.VeganStatus.Should().Be("NON_VEGAN");
    }

    [Fact]
    public async Task CreateENumber_Valid_Returns201Created()
    {
        var client = _factory.CreateClient();
        var adminToken = GenerateJwtToken(1, UserRoles.Admin);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

        var request = new CreateAdminENumberRequest
        {
            Code = "E441",
            Name = "Gelatin",
            CategoryType = "Chất làm đông",
            OriginSource = "Xương da động vật",
            VeganStatus = "NON_VEGAN",
            HealthRiskNotes = "Không dùng cho người ăn chay"
        };

        var response = await client.PostAsJsonAsync("/api/v1/admin/e-numbers", request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var envelope = await response.Content.ReadFromJsonAsync<ApiResponse<CreateAdminENumberResponseData>>();
        envelope.Should().NotBeNull();
        envelope!.Data!.Code.Should().Be("E441");

        // Verify in DB
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var entity = await db.EnumberDictionaries.FindAsync("E441");
        entity.Should().NotBeNull();
        entity!.Name.Should().Be("Gelatin");
    }

    [Fact]
    public async Task CreateENumber_DuplicateCode_Returns409Conflict()
    {
        var client = _factory.CreateClient();
        var adminToken = GenerateJwtToken(1, UserRoles.Admin);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

        var request = new CreateAdminENumberRequest
        {
            Code = "E100", // Đã tồn tại
            Name = "Trùng lặp Curcumin",
            CategoryType = "Màu",
            OriginSource = "Nghệ",
            VeganStatus = "VEGAN"
        };

        var response = await client.PostAsJsonAsync("/api/v1/admin/e-numbers", request);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var err = await response.Content.ReadFromJsonAsync<ApiErrorResponse>();
        err!.ErrorCode.Should().Be(ErrorCodes.ErrEnumberDuplicate);
    }

    [Fact]
    public async Task UpdateENumber_Valid_Returns200OK()
    {
        var client = _factory.CreateClient();
        var adminToken = GenerateJwtToken(1, UserRoles.Admin);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

        var request = new UpdateAdminENumberRequest
        {
            Name = "Carmine chiết xuất rệp son Dactylopius coccus",
            HealthRiskNotes = "Ghi chú mới về độc tính"
        };

        var response = await client.PutAsJsonAsync("/api/v1/admin/e-numbers/E120", request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var envelope = await response.Content.ReadFromJsonAsync<ApiResponse<UpdateAdminENumberResponseData>>();
        envelope.Should().NotBeNull();
        envelope!.Data!.Code.Should().Be("E120");

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var entity = await db.EnumberDictionaries.FindAsync("E120");
        entity!.Name.Should().Be("Carmine chiết xuất rệp son Dactylopius coccus");
    }
}
