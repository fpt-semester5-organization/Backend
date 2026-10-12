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
using VeggieLife.BLL.DTOs.Recipes.Requests;
using VeggieLife.BLL.DTOs.Recipes.Responses;
using VeggieLife.BLL.Services.Cache;
using VeggieLife.DAL.Context;
using VeggieLife.DAL.Entities;
using Xunit;

namespace VeggieLife.Tests.Integration.Controllers;

/// <summary>
/// Bộ kiểm thử tích hợp (Integration Tests) gọi qua HTTP pipeline cho các endpoint đăng, sửa, gỡ, xóa công thức (FN-POST-03, FN-POST-04).
/// Kiểm tra trọn vẹn: Không token (401), Khách vãng lai (403), Lưu nháp (201), Red Alert cấm trứng (422),
/// Sửa vi phạm giữ nguyên bản cũ (422), Sửa hợp lệ (200), Gỡ (archive) và Xóa (all).
/// </summary>
public class RecipesMutationEndpointIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly Mock<IRedisService> _mockRedis = new();
    private readonly string _testDbName = "VeggieLifeRecipesMutationIntegrationDb_" + Guid.NewGuid();

    public RecipesMutationEndpointIntegrationTests(WebApplicationFactory<Program> factory)
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
        var member1 = new User
        {
            UserId = 301,
            Email = "member301@veggielife.vn",
            FullName = "Tác giả 301",
            Role = UserRoles.Member,
            AccountStatus = "ACTIVE",
            CreatedAt = DateTime.UtcNow.AddDays(-10)
        };
        var member2 = new User
        {
            UserId = 302,
            Email = "member302@veggielife.vn",
            FullName = "Tác giả 302",
            Role = UserRoles.Member,
            AccountStatus = "ACTIVE",
            CreatedAt = DateTime.UtcNow.AddDays(-10)
        };
        db.Users.AddRange(member1, member2);

        // 2. DietarySchools
        db.DietarySchools.AddRange(
            new DietarySchool
            {
                DietarySchoolId = 7,
                SchoolCode = "VEGAN",
                SchoolName = "Thuần chay (Vegan)",
                ForbiddenFoodRules = "{\"forbiddenFoodGroups\":[\"EGG\",\"DAIRY\",\"POULTRY\",\"SEAFOOD\"]}"
            },
            new DietarySchool
            {
                DietarySchoolId = 1,
                SchoolCode = "BUDDHIST",
                SchoolName = "Chay Phật giáo",
                ForbiddenFoodRules = "{\"forbiddenFoodGroups\":[\"EGG\",\"DAIRY\",\"POULTRY\",\"SEAFOOD\"],\"forbidAllium\":true}"
            }
        );

        // 3. Categories
        db.FoodCategories.AddRange(
            new FoodCategory { CategoryId = 1, CategoryName = "Món canh", IsActive = true },
            new FoodCategory { CategoryId = 2, CategoryName = "Món xào", IsActive = true }
        );

        // 4. Ingredients
        db.Ingredients.AddRange(
            new Ingredient { IngredientId = 11, IngredientName = "Đậu phụ tươi", FoodGroup = "PLANT", StandardUnit = "g", CaloriesPer100g = 76m, DefaultStallType = "PLANT_PROTEIN" },
            new Ingredient { IngredientId = 12, IngredientName = "Nấm đùi gà tươi", FoodGroup = "PLANT", StandardUnit = "g", CaloriesPer100g = 34m, DefaultStallType = "PRODUCE" },
            new Ingredient { IngredientId = 13, IngredientName = "Trứng gà ta", FoodGroup = "EGG", StandardUnit = "quả", CaloriesPer100g = 155m, DefaultStallType = "DAIRY_EGG" },
            new Ingredient { IngredientId = 14, IngredientName = "Hành tây", FoodGroup = "ALLIUM", StandardUnit = "g", CaloriesPer100g = 40m, DefaultStallType = "PRODUCE" }
        );

        // 5. Existing Recipes for testing update/delete
        var r801 = new Recipe
        {
            RecipeId = 801,
            UserId = 301,
            CategoryId = 1,
            DietarySchoolId = 7,
            Title = "Canh đậu hũ nấm thuần chay đã duyệt",
            Instructions = "[\"Bước 1: Nấu canh sôi\", \"Bước 2: Nêm nếm muối\"]",
            CookTimeMinutes = 20,
            PrepTimeMinutes = 10,
            Servings = 2,
            CaloriesKcal = 110m,
            DifficultyLevel = "EASY",
            Source = "USER",
            Status = "APPROVED_ACTIVE",
            CreatedAt = DateTime.UtcNow.AddDays(-2)
        };
        db.Recipes.Add(r801);
        db.RecipeIngredients.Add(new RecipeIngredient
        {
            RecipeId = 801,
            IngredientId = 11,
            QuantityNeeded = 100,
            Unit = "g"
        });

        // Recipe 802 for testing Delete mode=archive
        var r802 = new Recipe
        {
            RecipeId = 802,
            UserId = 301,
            CategoryId = 1,
            DietarySchoolId = 7,
            Title = "Công thức chuẩn bị gỡ profile",
            Instructions = "[\"Bước 1: Sơ chế\", \"Bước 2: Nấu chín\"]",
            DifficultyLevel = "MEDIUM",
            Source = "USER",
            CookTimeMinutes = 15,
            Status = "APPROVED_ACTIVE",
            CreatedAt = DateTime.UtcNow.AddDays(-2)
        };
        db.Recipes.Add(r802);

        // Recipe 803 for testing Delete mode=all
        var r803 = new Recipe
        {
            RecipeId = 803,
            UserId = 301,
            CategoryId = 1,
            DietarySchoolId = 7,
            Title = "Công thức chuẩn bị xóa mềm",
            Instructions = "[\"Bước 1: Sơ chế\", \"Bước 2: Nấu chín\"]",
            DifficultyLevel = "MEDIUM",
            Source = "USER",
            CookTimeMinutes = 15,
            Status = "APPROVED_ACTIVE",
            CreatedAt = DateTime.UtcNow.AddDays(-2)
        };
        db.Recipes.Add(r803);

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
    public async Task CreateRecipe_WithoutToken_ShouldReturn401Unauthorized()
    {
        // Arrange
        var client = _factory.CreateClient();
        var req = new CreateRecipeRequest
        {
            Title = "Công thức không kèm token hợp lệ",
            CategoryId = 1,
            DietarySchoolId = 7,
            Instructions = new List<string> { "Bước 1", "Bước 2" },
            CookTimeMinutes = 15,
            Submit = false
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/v1/recipes", req);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CreateRecipe_WithGuestRole_ShouldReturn403Forbidden()
    {
        // Arrange
        var client = _factory.CreateClient();
        var guestToken = GenerateJwtToken(999, "GUEST");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", guestToken);

        var req = new CreateRecipeRequest
        {
            Title = "Công thức của khách vãng lai",
            CategoryId = 1,
            DietarySchoolId = 7,
            Instructions = new List<string> { "Bước 1", "Bước 2" },
            CookTimeMinutes = 15,
            Submit = false
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/v1/recipes", req);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task CreateRecipe_DraftMode_ShouldReturn201Created()
    {
        // Arrange
        var client = _factory.CreateClient();
        var token = GenerateJwtToken(301, UserRoles.Member);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var req = new CreateRecipeRequest
        {
            Title = "Bản nháp canh đậu hũ nấm tươi",
            CategoryId = 1,
            DietarySchoolId = 7,
            Instructions = new List<string> { "Bước 1: Nấu thử nghiệm" },
            CookTimeMinutes = 10,
            Submit = false
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/v1/recipes", req);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var envelope = await response.Content.ReadFromJsonAsync<ApiResponse<CreateRecipeResponseDto>>();
        envelope.Should().NotBeNull();
        envelope!.StatusCode.Should().Be(201);
        envelope.Data.Should().NotBeNull();
        envelope.Data!.Status.Should().Be("DRAFT");
        envelope.Data.RecipeId.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task CreateRecipe_VeganWithEgg_ShouldReturn422UnprocessableEntity_ErrRecipeRedAlert()
    {
        // Arrange: Đăng công thức thuần chay nhưng có trứng gà (IngredientId = 13)
        var client = _factory.CreateClient();
        var token = GenerateJwtToken(301, UserRoles.Member);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var req = new CreateRecipeRequest
        {
            Title = "Canh nấm trứng gà thuần chay vi phạm",
            CategoryId = 1,
            DietarySchoolId = 7, // VEGAN
            Instructions = new List<string> { "Bước 1: Đánh trứng", "Bước 2: Nấu canh" },
            CookTimeMinutes = 15,
            Ingredients = new List<RecipeIngredientItemRequest>
            {
                new() { IngredientId = 11, Quantity = 100, Unit = "g" },
                new() { IngredientId = 13, Quantity = 1, Unit = "quả" } // Trứng gà -> Bị cấm
            },
            Submit = true
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/v1/recipes", req);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        var error = await response.Content.ReadFromJsonAsync<ApiErrorResponse>();
        error.Should().NotBeNull();
        error!.StatusCode.Should().Be(422);
        error.ErrorCode.Should().Be(ErrorCodes.ErrRecipeRedAlert);
    }

    [Fact]
    public async Task UpdateRecipe_NotOwner_ShouldReturn403Forbidden()
    {
        // Arrange: Recipe 801 thuộc về User 301, User 302 cố tình sửa
        var client = _factory.CreateClient();
        var token = GenerateJwtToken(302, UserRoles.Member);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var req = new UpdateRecipeRequest
        {
            Title = "Sửa công thức của người khác trái phép"
        };

        // Act
        var response = await client.PutAsJsonAsync("/api/v1/recipes/801", req);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        var error = await response.Content.ReadFromJsonAsync<ApiErrorResponse>();
        error!.ErrorCode.Should().Be(ErrorCodes.ErrContentNotOwner);
    }

    [Fact]
    public async Task UpdateRecipe_ApprovedActive_AddForbiddenEgg_ShouldReturn422AndOriginalRemainsUnchanged()
    {
        // Arrange: Sửa Recipe 801 (APPROVED_ACTIVE) thêm trứng vào công thức thuần chay
        var client = _factory.CreateClient();
        var token = GenerateJwtToken(301, UserRoles.Member);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var req = new UpdateRecipeRequest
        {
            Title = "Canh đậu hũ nấm cho thêm trứng gà",
            Instructions = new List<string> { "Bước 1: Nấu sôi", "Bước 2: Cho trứng vào" },
            Ingredients = new List<RecipeIngredientItemRequest>
            {
                new() { IngredientId = 11, Quantity = 100, Unit = "g" },
                new() { IngredientId = 13, Quantity = 2, Unit = "quả" } // Trứng gà
            }
        };

        // Act
        var response = await client.PutAsJsonAsync("/api/v1/recipes/801", req);

        // Assert: Trả về 422 Red Alert
        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        var error = await response.Content.ReadFromJsonAsync<ApiErrorResponse>();
        error!.ErrorCode.Should().Be(ErrorCodes.ErrRecipeRedAlert);

        // Kiểm tra tính toàn vẹn: Bản đang đăng giữ nguyên trong CSDL
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var recipeInDb = await db.Recipes.AsNoTracking().FirstOrDefaultAsync(r => r.RecipeId == 801);
        recipeInDb.Should().NotBeNull();
        recipeInDb!.Title.Should().Be("Canh đậu hũ nấm thuần chay đã duyệt"); // KHÔNG THAY ĐỔI
        recipeInDb.Status.Should().Be("APPROVED_ACTIVE");
    }

    [Fact]
    public async Task UpdateRecipe_MetadataOnly_ShouldReturn200OK()
    {
        // Arrange: Sửa metadata hợp lệ của Recipe 801
        var client = _factory.CreateClient();
        var token = GenerateJwtToken(301, UserRoles.Member);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var req = new UpdateRecipeRequest
        {
            CookTimeMinutes = 25,
            Servings = 3,
            DifficultyLevel = "MEDIUM"
        };

        // Act
        var response = await client.PutAsJsonAsync("/api/v1/recipes/801", req);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var envelope = await response.Content.ReadFromJsonAsync<ApiResponse<UpdateRecipeResponseDto>>();
        envelope.Should().NotBeNull();
        envelope!.Data!.Status.Should().Be("APPROVED_ACTIVE");
        envelope.Data.Moderated.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteRecipe_ModeArchive_ShouldReturn200OK_RecipeIsArchivedAndUserIdNull()
    {
        // Arrange
        var client = _factory.CreateClient();
        var token = GenerateJwtToken(301, UserRoles.Member);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await client.DeleteAsync("/api/v1/recipes/802?mode=archive");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var envelope = await response.Content.ReadFromJsonAsync<ApiResponse<DeleteRecipeResponseDto>>();
        envelope.Should().NotBeNull();
        envelope!.Data!.Status.Should().Be("ARCHIVED");

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var inDb = await db.Recipes.AsNoTracking().FirstOrDefaultAsync(r => r.RecipeId == 802);
        inDb.Should().NotBeNull();
        inDb!.Status.Should().Be("ARCHIVED");
        inDb.UserId.Should().BeNull(); // Gỡ khỏi trang cá nhân
    }

    [Fact]
    public async Task DeleteRecipe_ModeAll_ShouldReturn200OK_RecipeIsDeleted()
    {
        // Arrange
        var client = _factory.CreateClient();
        var token = GenerateJwtToken(301, UserRoles.Member);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await client.DeleteAsync("/api/v1/recipes/803?mode=all");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var envelope = await response.Content.ReadFromJsonAsync<ApiResponse<DeleteRecipeResponseDto>>();
        envelope.Should().NotBeNull();
        envelope!.Data!.Status.Should().Be("DELETED");

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var inDb = await db.Recipes.AsNoTracking().FirstOrDefaultAsync(r => r.RecipeId == 803);
        inDb.Should().NotBeNull();
        inDb!.Status.Should().Be("DELETED");
    }
}
