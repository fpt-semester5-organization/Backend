using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net;
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
using VeggieLife.BLL.DTOs.Admin.Knowledge;
using VeggieLife.BLL.Services.AI;
using VeggieLife.BLL.Services.Cache;
using VeggieLife.BLL.Services.Knowledge;
using VeggieLife.DAL.Context;
using VeggieLife.DAL.Entities;
using Xunit;

namespace VeggieLife.Tests.Integration.Controllers;

/// <summary>
/// Integration Tests cho Endpoints Quản trị Kho tri thức RAG (FN-CHAT-07):
/// - GET /api/v1/admin/knowledge/status
/// - POST /api/v1/admin/knowledge/reindex
/// </summary>
public class AdminKnowledgeEndpointIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly Mock<IAiServiceClient> _mockAiClient = new();
    private readonly Mock<IRedisService> _mockRedis = new();
    private readonly string _testDbName = "VeggieLifeAdminKnowledgeIntegrationDb_" + Guid.NewGuid();

    public AdminKnowledgeEndpointIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Development");
            builder.ConfigureServices(services =>
            {
                var dbDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
                if (dbDescriptor != null)
                {
                    services.Remove(dbDescriptor);
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

                var aiClientDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IAiServiceClient));
                if (aiClientDescriptor != null)
                {
                    services.Remove(aiClientDescriptor);
                }
                services.AddSingleton(_mockAiClient.Object);
            });
        });

        SeedTestData();
    }

    private void SeedTestData()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        if (!db.Users.Any(u => u.Email == "admin.knowledge@veggielife.vn"))
        {
            db.Users.AddRange(
                new User
                {
                    UserId = 105,
                    Email = "admin.knowledge@veggielife.vn",
                    FullName = "Admin Knowledge",
                    Role = UserRoles.Admin,
                    AccountStatus = "ACTIVE",
                    CreatedAt = DateTime.UtcNow
                },
                new User
                {
                    UserId = 205,
                    Email = "member.knowledge@veggielife.vn",
                    FullName = "Member User",
                    Role = UserRoles.Member,
                    AccountStatus = "ACTIVE",
                    CreatedAt = DateTime.UtcNow
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
    public async Task GetKnowledgeStatus_WithoutToken_Returns401Unauthorized()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/v1/admin/knowledge/status");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetKnowledgeStatus_WithNonAdminToken_Returns403Forbidden()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", GenerateJwtToken(205, UserRoles.Member));

        var response = await client.GetAsync("/api/v1/admin/knowledge/status");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetKnowledgeStatus_WithAdminToken_Returns200AndValidDto()
    {
        // Arrange
        _mockAiClient
            .Setup(c => c.GetKnowledgeStatusAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AdminKnowledgeStatusResponseData
            {
                Collection = "veggielife_knowledge",
                TotalPoints = 2667,
                BySource = new Dictionary<string, int>
                {
                    ["NUTRITION_DOC"] = 2667,
                    ["RECIPE"] = 0
                },
                LastIndexedAt = "2026-11-03T18:00:00Z"
            });

        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", GenerateJwtToken(105, UserRoles.Admin));

        // Act
        var response = await client.GetAsync("/api/v1/admin/knowledge/status");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var envelope = await response.Content.ReadFromJsonAsync<ApiResponse<AdminKnowledgeStatusResponseData>>();
        envelope.Should().NotBeNull();
        envelope!.Data.Should().NotBeNull();
        envelope.Data!.Collection.Should().Be("veggielife_knowledge");
        envelope.Data.TotalPoints.Should().Be(2667);
        envelope.Data.BySource.Should().ContainKey("NUTRITION_DOC");
    }

    [Fact]
    public async Task ReindexKnowledge_WithAdminToken_Returns202Accepted()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", GenerateJwtToken(105, UserRoles.Admin));

        var req = new CreateAdminKnowledgeReindexRequest { Scope = "RECIPE" };
        var response = await client.PostAsJsonAsync("/api/v1/admin/knowledge/reindex", req);

        response.StatusCode.Should().Be(HttpStatusCode.Accepted);
        var envelope = await response.Content.ReadFromJsonAsync<ApiResponse<CreateAdminKnowledgeReindexResponseData>>();
        envelope.Should().NotBeNull();
        envelope!.Data.Should().NotBeNull();
        envelope.Data!.JobId.Should().StartWith("kb_");
        envelope.Data.Scope.Should().Be("RECIPE");
    }

    [Fact]
    public async Task GetNutritionDocuments_WithAdminToken_Returns200WithDocuments()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", GenerateJwtToken(105, UserRoles.Admin));

        var response = await client.GetAsync("/api/v1/admin/knowledge/documents");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var envelope = await response.Content.ReadFromJsonAsync<ApiResponse<List<AdminNutritionDocumentDto>>>();
        envelope.Should().NotBeNull();
        envelope!.Data.Should().NotBeNull();
        envelope.Data!.Should().NotBeEmpty();
        envelope.Data!.Should().Contain(d => d.Id == "DOC_BOOK_1");
    }
}
