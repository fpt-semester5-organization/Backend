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
using VeggieLife.BLL.DTOs.Admin.Categories;
using VeggieLife.BLL.Services.Cache;
using VeggieLife.DAL.Context;
using VeggieLife.DAL.Entities;
using Xunit;

namespace VeggieLife.Tests.Integration.Controllers;

/// <summary>
/// Bộ kiểm thử tích hợp (Integration Tests) gọi qua HTTP pipeline cho các endpoint quản trị danh mục (FN-ADM-07, FN-ADM-08).
/// </summary>
public class AdminCategoriesEndpointIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly Mock<IRedisService> _mockRedis = new();
    private readonly string _testDbName = "VeggieLifeAdminCategoriesIntegrationDb_" + Guid.NewGuid();

    public AdminCategoriesEndpointIntegrationTests(WebApplicationFactory<Program> factory)
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

        // 1. Users
        var admin = new User
        {
            UserId = 1,
            Email = "admin@veggielife.vn",
            FullName = "Admin Quản Trị",
            Role = UserRoles.Admin,
            AccountStatus = "ACTIVE",
            CreatedAt = DateTime.UtcNow
        };
        var member = new User
        {
            UserId = 2,
            Email = "member@veggielife.vn",
            FullName = "Hội Viên 2",
            Role = UserRoles.Member,
            AccountStatus = "ACTIVE",
            CreatedAt = DateTime.UtcNow
        };
        db.Users.AddRange(admin, member);

        // 2. Categories
        db.FoodCategories.AddRange(
            new FoodCategory { CategoryId = 1, CategoryName = "Món kho cốt lõi", IsCore = true, IsActive = true, CreatedAt = DateTime.UtcNow },
            new FoodCategory { CategoryId = 2, CategoryName = "Món canh thường", IsCore = false, IsActive = true, CreatedAt = DateTime.UtcNow },
            new FoodCategory { CategoryId = 3, CategoryName = "Món xào đích", IsCore = false, IsActive = true, CreatedAt = DateTime.UtcNow }
        );

        // 3. Recipes linked to Category 2
        db.Recipes.AddRange(
            new Recipe
            {
                RecipeId = 201,
                Title = "Canh chua chay",
                Instructions = "[\"B1\", \"B2\"]",
                CookTimeMinutes = 20,
                DifficultyLevel = "EASY",
                Source = "USER",
                Status = "APPROVED_ACTIVE",
                CreatedAt = DateTime.UtcNow
            },
            new Recipe
            {
                RecipeId = 202,
                Title = "Canh nấm bào ngư",
                Instructions = "[\"B1\", \"B2\"]",
                CookTimeMinutes = 15,
                DifficultyLevel = "EASY",
                Source = "USER",
                Status = "APPROVED_ACTIVE",
                CreatedAt = DateTime.UtcNow
            }
        );

        // 4. Venues linked to Category 2
        db.VeganVenues.Add(new VeganVenue { VenueId = 10, ExternalSource = "MANUAL", VenueName = "Quán Chay Thanh Đạm", Address = "123 Đỗ Xuân Hợp", Latitude = 10.8m, Longitude = 106.7m, IsActive = true, CreatedAt = DateTime.UtcNow });

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
    public async Task PostCategory_WithoutToken_Returns401Unauthorized()
    {
        var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/v1/admin/categories", new CreateAdminCategoryRequest { CategoryName = "Món mới" });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        var err = await response.Content.ReadFromJsonAsync<ApiErrorResponse>();
        err!.ErrorCode.Should().Be(ErrorCodes.ErrAuthInvalidToken);
    }

    [Fact]
    public async Task PostCategory_WithMemberToken_Returns403Forbidden()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", GenerateJwtToken(2, UserRoles.Member));

        var response = await client.PostAsJsonAsync("/api/v1/admin/categories", new CreateAdminCategoryRequest { CategoryName = "Món mới" });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        var err = await response.Content.ReadFromJsonAsync<ApiErrorResponse>();
        err!.ErrorCode.Should().Be(ErrorCodes.ErrAdmForbidden);
    }

    [Fact]
    public async Task PostCategory_InvalidInput_Returns400BadRequest()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", GenerateJwtToken(1, UserRoles.Admin));

        var response = await client.PostAsJsonAsync("/api/v1/admin/categories", new CreateAdminCategoryRequest { CategoryName = "A" });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var err = await response.Content.ReadFromJsonAsync<ApiErrorResponse>();
        err!.ErrorCode.Should().Be(ErrorCodes.ErrValidation);
    }

    [Fact]
    public async Task PostCategory_DuplicateName_Returns409Conflict()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", GenerateJwtToken(1, UserRoles.Admin));

        var response = await client.PostAsJsonAsync("/api/v1/admin/categories", new CreateAdminCategoryRequest { CategoryName = "Món kho cốt lõi" });

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var err = await response.Content.ReadFromJsonAsync<ApiErrorResponse>();
        err!.ErrorCode.Should().Be(ErrorCodes.ErrCategoryDuplicate);
    }

    [Fact]
    public async Task PostCategory_ValidInput_Returns201CreatedWithEnvelope()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", GenerateJwtToken(1, UserRoles.Admin));

        var response = await client.PostAsJsonAsync("/api/v1/admin/categories", new CreateAdminCategoryRequest
        {
            CategoryName = "Món hấp dưỡng sinh",
            Description = "Món hấp ít gia vị tốt cho sức khỏe"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<CreateAdminCategoryResponseData>>();
        body.Should().NotBeNull();
        body!.StatusCode.Should().Be(201);
        body.Message.Should().Be("Đã tạo danh mục.");
        body.Data!.CategoryName.Should().Be("Món hấp dưỡng sinh");
        body.Data.CategoryId.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task PutCategory_WithoutToken_Returns401Unauthorized()
    {
        var client = _factory.CreateClient();
        var response = await client.PutAsJsonAsync("/api/v1/admin/categories/2", new UpdateAdminCategoryRequest { IsActive = false });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task PutCategory_WithMemberToken_Returns403Forbidden()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", GenerateJwtToken(2, UserRoles.Member));

        var response = await client.PutAsJsonAsync("/api/v1/admin/categories/2", new UpdateAdminCategoryRequest { IsActive = false });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        var err = await response.Content.ReadFromJsonAsync<ApiErrorResponse>();
        err!.ErrorCode.Should().Be(ErrorCodes.ErrAdmForbidden);
    }

    [Fact]
    public async Task PutCategory_NotFound_Returns404NotFound()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", GenerateJwtToken(1, UserRoles.Admin));

        var response = await client.PutAsJsonAsync("/api/v1/admin/categories/9999", new UpdateAdminCategoryRequest { IsActive = false });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var err = await response.Content.ReadFromJsonAsync<ApiErrorResponse>();
        err!.ErrorCode.Should().Be(ErrorCodes.ErrAdmNotFound);
    }

    [Fact]
    public async Task PutCategory_ValidInput_Returns200OKWithEnvelope()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", GenerateJwtToken(1, UserRoles.Admin));

        var response = await client.PutAsJsonAsync("/api/v1/admin/categories/2", new UpdateAdminCategoryRequest
        {
            CategoryName = "Món canh thanh tịnh",
            IsActive = true
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<UpdateAdminCategoryResponseData>>();
        body.Should().NotBeNull();
        body!.StatusCode.Should().Be(200);
        body.Message.Should().Be("Đã cập nhật.");
        body.Data!.CategoryId.Should().Be(2);
        body.Data.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task DeleteCategory_WithoutToken_Returns401Unauthorized()
    {
        var client = _factory.CreateClient();
        var response = await client.DeleteAsync("/api/v1/admin/categories/2");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task DeleteCategory_WithMemberToken_Returns403Forbidden()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", GenerateJwtToken(2, UserRoles.Member));

        var response = await client.DeleteAsync("/api/v1/admin/categories/2");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        var err = await response.Content.ReadFromJsonAsync<ApiErrorResponse>();
        err!.ErrorCode.Should().Be(ErrorCodes.ErrAdmForbidden);
    }

    [Fact]
    public async Task DeleteCategory_CoreCategory_Returns403ForbiddenCategoryCoreProtected()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", GenerateJwtToken(1, UserRoles.Admin));

        var response = await client.DeleteAsync("/api/v1/admin/categories/1?targetCategoryId=3");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        var err = await response.Content.ReadFromJsonAsync<ApiErrorResponse>();
        err!.ErrorCode.Should().Be(ErrorCodes.ErrCategoryCoreProtected);
    }

    [Fact]
    public async Task DeleteCategory_HasDependentRecipes_MissingTargetCategoryId_Returns409ConflictCategoryNeedsMigration()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", GenerateJwtToken(1, UserRoles.Admin));

        var response = await client.DeleteAsync("/api/v1/admin/categories/2");

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var err = await response.Content.ReadFromJsonAsync<ApiErrorResponse>();
        err!.ErrorCode.Should().Be(ErrorCodes.ErrCategoryNeedsMigration);
    }

    [Fact]
    public async Task DeleteCategory_ValidMigration_Returns200OKWithEnvelope()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", GenerateJwtToken(1, UserRoles.Admin));

        var response = await client.DeleteAsync("/api/v1/admin/categories/2?targetCategoryId=3");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<DeleteAdminCategoryResponseData>>();
        body.Should().NotBeNull();
        body!.StatusCode.Should().Be(200);
        body.Message.Should().Be("Đã xóa danh mục.");
        body.Data!.MigratedRecipes.Should().Be(2);
        body.Data.MigratedVenues.Should().Be(1);
    }
}
