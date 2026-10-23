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
using VeggieLife.BLL.DTOs.Admin.DietarySchools;
using VeggieLife.BLL.Services.Cache;
using VeggieLife.DAL.Context;
using VeggieLife.DAL.Entities;
using Xunit;

namespace VeggieLife.Tests.Integration.Controllers;

/// <summary>
/// Bộ kiểm thử tích hợp cho API quản lý 07 trường phái ăn chay (FN-ADM-10).
/// </summary>
public class AdminDietarySchoolsEndpointIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly Mock<IRedisService> _mockRedis = new();
    private readonly string _testDbName = "VeggieLifeAdminDietarySchoolsIntegrationDb_" + Guid.NewGuid();

    public AdminDietarySchoolsEndpointIntegrationTests(WebApplicationFactory<Program> factory)
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

        db.Users.AddRange(
            new User { UserId = 1, Email = "admin_diet@veggielife.vn", FullName = "Admin Chay", Role = UserRoles.Admin, AccountStatus = "ACTIVE", CreatedAt = DateTime.UtcNow },
            new User { UserId = 2, Email = "member_diet@veggielife.vn", FullName = "Member Chay", Role = UserRoles.Member, AccountStatus = "ACTIVE", CreatedAt = DateTime.UtcNow }
        );

        db.DietarySchools.AddRange(
            new DietarySchool { DietarySchoolId = 1, SchoolCode = "VEGAN", SchoolName = "Thuần chay", ForbiddenFoodRules = "{\"forbiddenFoodGroups\":[\"EGG\",\"DAIRY\",\"POULTRY\",\"SEAFOOD\"],\"forbidAllium\":false}", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
            new DietarySchool { DietarySchoolId = 2, SchoolCode = "OVO", SchoolName = "Chay có trứng", ForbiddenFoodRules = "{\"forbiddenFoodGroups\":[\"DAIRY\",\"POULTRY\",\"SEAFOOD\"],\"forbidAllium\":false}", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
            new DietarySchool { DietarySchoolId = 3, SchoolCode = "LACTO", SchoolName = "Chay có sữa", ForbiddenFoodRules = "{\"forbiddenFoodGroups\":[\"EGG\",\"POULTRY\",\"SEAFOOD\"],\"forbidAllium\":false}", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
            new DietarySchool { DietarySchoolId = 4, SchoolCode = "LACTO_OVO", SchoolName = "Chay trứng sữa", ForbiddenFoodRules = "{\"forbiddenFoodGroups\":[\"POULTRY\",\"SEAFOOD\"],\"forbidAllium\":false}", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
            new DietarySchool { DietarySchoolId = 5, SchoolCode = "BUDDHIST", SchoolName = "Chay Phật giáo", ForbiddenFoodRules = "{\"forbiddenFoodGroups\":[\"EGG\",\"DAIRY\",\"POULTRY\",\"SEAFOOD\"],\"forbidAllium\":true}", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
            new DietarySchool { DietarySchoolId = 6, SchoolCode = "POLLOTARIAN", SchoolName = "Bán phần gia cầm", ForbiddenFoodRules = "{\"forbiddenFoodGroups\":[\"SEAFOOD\"],\"forbidAllium\":false}", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
            new DietarySchool { DietarySchoolId = 7, SchoolCode = "PESCATARIAN", SchoolName = "Bán phần hải sản", ForbiddenFoodRules = "{\"forbiddenFoodGroups\":[\"POULTRY\"],\"forbidAllium\":false}", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow }
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
    public async Task ListDietarySchools_WithoutToken_Returns401()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/v1/admin/dietary-schools");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ListDietarySchools_MemberToken_Returns403()
    {
        var client = _factory.CreateClient();
        var memberToken = GenerateJwtToken(2, UserRoles.Member);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", memberToken);

        var response = await client.GetAsync("/api/v1/admin/dietary-schools");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task ListDietarySchools_AdminToken_Returns200With7Schools()
    {
        var client = _factory.CreateClient();
        var adminToken = GenerateJwtToken(1, UserRoles.Admin);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

        var response = await client.GetAsync("/api/v1/admin/dietary-schools");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var envelope = await response.Content.ReadFromJsonAsync<ApiResponse<IReadOnlyList<AdminDietarySchoolItemDto>>>();
        envelope!.Data.Should().HaveCount(7);
        envelope.Data.First(d => d.SchoolCode == "VEGAN").SchoolName.Should().Be("Thuần chay");
    }

    [Fact]
    public async Task UpdateDietarySchool_ValidRequest_Returns200AndUpdatesDatabase()
    {
        var client = _factory.CreateClient();
        var adminToken = GenerateJwtToken(1, UserRoles.Admin);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

        var request = new UpdateAdminDietarySchoolRequest
        {
            SchoolName = "Thuần chay nâng cao",
            ForbiddenFoodRules = new ForbiddenFoodRulesDto
            {
                ForbiddenFoodGroups = new List<string> { "EGG", "DAIRY", "POULTRY", "SEAFOOD" },
                ForbidAllium = true,
                Description = "Bản cập nhật cấm cả ngũ vị tân."
            }
        };

        var response = await client.PutAsJsonAsync("/api/v1/admin/dietary-schools/1", request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var envelope = await response.Content.ReadFromJsonAsync<ApiResponse<UpdateAdminDietarySchoolResponseData>>();
        envelope!.Data.DietarySchoolId.Should().Be(1);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var updated = await db.DietarySchools.FindAsync(1);
        updated.Should().NotBeNull();
        updated!.SchoolName.Should().Be("Thuần chay nâng cao");
        using var doc = JsonDocument.Parse(updated.ForbiddenFoodRules);
        doc.RootElement.GetProperty("description").GetString().Should().Be("Bản cập nhật cấm cả ngũ vị tân.");
    }

    [Fact]
    public async Task UpdateDietarySchool_InvalidForbiddenGroup_Returns400()
    {
        var client = _factory.CreateClient();
        var adminToken = GenerateJwtToken(1, UserRoles.Admin);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

        var request = new UpdateAdminDietarySchoolRequest
        {
            ForbiddenFoodRules = new ForbiddenFoodRulesDto
            {
                ForbiddenFoodGroups = new List<string> { "MEAT_INVALID" },
                ForbidAllium = false
            }
        };

        var response = await client.PutAsJsonAsync("/api/v1/admin/dietary-schools/1", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var err = await response.Content.ReadFromJsonAsync<ApiErrorResponse>();
        err!.ErrorCode.Should().Be(ErrorCodes.ErrValidation);
    }

    [Fact]
    public async Task PostOrDelete_DietarySchools_Returns405MethodNotAllowed()
    {
        var client = _factory.CreateClient();
        var adminToken = GenerateJwtToken(1, UserRoles.Admin);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

        // Không cho phép thêm mới (BR-ADM-MAS-001)
        var postResponse = await client.PostAsJsonAsync("/api/v1/admin/dietary-schools", new { });
        postResponse.StatusCode.Should().Be(HttpStatusCode.MethodNotAllowed);

        // Không cho phép xóa (BR-ADM-MAS-001)
        var deleteResponse = await client.DeleteAsync("/api/v1/admin/dietary-schools/1");
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.MethodNotAllowed);
    }
}
