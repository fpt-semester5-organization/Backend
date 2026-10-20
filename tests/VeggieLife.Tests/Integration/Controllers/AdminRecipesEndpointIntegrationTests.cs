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
using VeggieLife.BLL.DTOs.Admin.Recipes;
using VeggieLife.BLL.Services.Cache;
using VeggieLife.DAL.Context;
using VeggieLife.DAL.Entities;
using Xunit;

namespace VeggieLife.Tests.Integration.Controllers;

/// <summary>
/// Bộ kiểm thử tích hợp (Integration Tests) gọi qua HTTP pipeline cho các endpoint xác minh công thức chuẩn chay (FN-ADM-09).
/// </summary>
public class AdminRecipesEndpointIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly Mock<IRedisService> _mockRedis = new();
    private readonly string _testDbName = "VeggieLifeAdminRecipesIntegrationDb_" + Guid.NewGuid();

    public AdminRecipesEndpointIntegrationTests(WebApplicationFactory<Program> factory)
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

        // 2. Dietary School
        db.DietarySchools.Add(new DietarySchool
        {
            DietarySchoolId = 7,
            SchoolCode = "VEGAN",
            SchoolName = "Thuần chay (Vegan)",
            ForbiddenFoodRules = "{\"forbiddenFoodGroups\":[\"EGG\",\"DAIRY\",\"POULTRY\",\"SEAFOOD\"]}"
        });

        // 3. Category & Ingredients
        db.FoodCategories.Add(new FoodCategory { CategoryId = 1, CategoryName = "Món kho chay", IsActive = true });
        db.Ingredients.AddRange(
            new Ingredient { IngredientId = 1, IngredientName = "Nấm đông cô", FoodGroup = "PLANT", StandardUnit = "g", CaloriesPer100g = 34m, DefaultStallType = "PRODUCE" },
            new Ingredient { IngredientId = 2, IngredientName = "Đậu hũ chiên", FoodGroup = "PLANT", StandardUnit = "g", CaloriesPer100g = 120m, DefaultStallType = "PLANT_PROTEIN" }
        );

        // 4. Recipe 301 (Fully eligible with 10 votes)
        var r301 = new Recipe
        {
            RecipeId = 301,
            CategoryId = 1,
            DietarySchoolId = 7,
            Title = "Nấm kho tiêu chay thuần chuẩn",
            Instructions = "[\"Bước 1: Rửa sạch nấm và đậu hũ\", \"Bước 2: Kho nhỏ lửa với tiêu đen và nước tương\"]",
            CookTimeMinutes = 20,
            PrepTimeMinutes = 10,
            Servings = 2,
            CaloriesKcal = 160m,
            DifficultyLevel = "EASY",
            Source = "USER",
            Status = "APPROVED_ACTIVE",
            IsVerified = false,
            CreatedAt = DateTime.UtcNow
        };
        db.Recipes.Add(r301);
        db.RecipeIngredients.AddRange(
            new RecipeIngredient { RecipeId = 301, IngredientId = 1, QuantityNeeded = 150, Unit = "g" },
            new RecipeIngredient { RecipeId = 301, IngredientId = 2, QuantityNeeded = 100, Unit = "g" }
        );

        var post301 = new Post
        {
            PostId = 301,
            RecipeId = 301,
            UserId = 2,
            Title = "Post 301",
            Content = "Content 301",
            ModerationStatus = "APPROVED_ACTIVE",
            Visibility = "PUBLIC"
        };
        db.Posts.Add(post301);

        for (int i = 1; i <= 10; i++)
        {
            db.Votes.Add(new Vote { VoteId = 1000 + i, PostId = 301, UserId = 2000 + i, CreatedAt = DateTime.UtcNow });
        }

        // 5. Recipe 302 (Ineligible: only 0 votes)
        var r302 = new Recipe
        {
            RecipeId = 302,
            CategoryId = 1,
            DietarySchoolId = 7,
            Title = "Đậu hũ luộc chay",
            Instructions = "[\"Bước 1: Nấu nước sôi\", \"Bước 2: Luộc đậu hũ\"]",
            CookTimeMinutes = 10,
            Status = "APPROVED_ACTIVE",
            Source = "USER",
            DifficultyLevel = "EASY",
            CreatedAt = DateTime.UtcNow
        };
        db.Recipes.Add(r302);
        db.RecipeIngredients.Add(new RecipeIngredient { RecipeId = 302, IngredientId = 2, QuantityNeeded = 100, Unit = "g" });

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
    public async Task VerifyRecipe_WithoutToken_Returns401Unauthorized()
    {
        var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/v1/admin/recipes/301/verify", new VerifyAdminRecipeRequest());

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task VerifyRecipe_WithMemberToken_Returns403Forbidden()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", GenerateJwtToken(2, UserRoles.Member));

        var response = await client.PostAsJsonAsync("/api/v1/admin/recipes/301/verify", new VerifyAdminRecipeRequest());

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        var err = await response.Content.ReadFromJsonAsync<ApiErrorResponse>();
        err!.ErrorCode.Should().Be(ErrorCodes.ErrAdmForbidden);
    }

    [Fact]
    public async Task VerifyRecipe_NotFound_Returns404NotFound()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", GenerateJwtToken(1, UserRoles.Admin));

        var response = await client.PostAsJsonAsync("/api/v1/admin/recipes/9999/verify", new VerifyAdminRecipeRequest());

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var err = await response.Content.ReadFromJsonAsync<ApiErrorResponse>();
        err!.ErrorCode.Should().Be(ErrorCodes.ErrAdmNotFound);
    }

    [Fact]
    public async Task VerifyRecipe_IneligibleRecipe_Returns422UnprocessableEntityWithFailedCriteria()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", GenerateJwtToken(1, UserRoles.Admin));

        var response = await client.PostAsJsonAsync("/api/v1/admin/recipes/302/verify", new VerifyAdminRecipeRequest());

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        var err = await response.Content.ReadFromJsonAsync<ApiErrorResponse>();
        err!.ErrorCode.Should().Be(ErrorCodes.ErrRecipeNotEligible);

        // Check data.failedCriteria / details
        var json = await response.Content.ReadAsStringAsync();
        json.Should().Contain("failedCriteria");
        json.Should().Contain("Chưa nhận được tối thiểu 10 lượt thả tim");
    }

    [Fact]
    public async Task VerifyRecipe_EligibleRecipe_Returns200OKWithEnvelope()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", GenerateJwtToken(1, UserRoles.Admin));

        var response = await client.PostAsJsonAsync("/api/v1/admin/recipes/301/verify", new VerifyAdminRecipeRequest
        {
            Note = "Công thức chuẩn chay xuất sắc"
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<VerifyAdminRecipeResponseData>>();
        body.Should().NotBeNull();
        body!.StatusCode.Should().Be(200);
        body.Message.Should().Be("Đã cấp huy hiệu.");
        body.Data.RecipeId.Should().Be(301);
        body.Data.IsVerified.Should().BeTrue();
    }

    [Fact]
    public async Task UnverifyRecipe_WithoutToken_Returns401Unauthorized()
    {
        var client = _factory.CreateClient();
        var response = await client.DeleteAsync("/api/v1/admin/recipes/301/verify");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task UnverifyRecipe_WithMemberToken_Returns403Forbidden()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", GenerateJwtToken(2, UserRoles.Member));

        var response = await client.DeleteAsync("/api/v1/admin/recipes/301/verify");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        var err = await response.Content.ReadFromJsonAsync<ApiErrorResponse>();
        err!.ErrorCode.Should().Be(ErrorCodes.ErrAdmForbidden);
    }

    [Fact]
    public async Task UnverifyRecipe_NotFound_Returns404NotFound()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", GenerateJwtToken(1, UserRoles.Admin));

        var response = await client.DeleteAsync("/api/v1/admin/recipes/9999/verify");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var err = await response.Content.ReadFromJsonAsync<ApiErrorResponse>();
        err!.ErrorCode.Should().Be(ErrorCodes.ErrAdmNotFound);
    }

    [Fact]
    public async Task UnverifyRecipe_ValidInput_Returns200OKWithEnvelope()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", GenerateJwtToken(1, UserRoles.Admin));

        var response = await client.DeleteAsync("/api/v1/admin/recipes/301/verify");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<UnverifyAdminRecipeResponseData>>();
        body.Should().NotBeNull();
        body!.StatusCode.Should().Be(200);
        body.Message.Should().Be("Đã gỡ huy hiệu.");
        body.Data.RecipeId.Should().Be(301);
        body.Data.IsVerified.Should().BeFalse();
    }
}
