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
using VeggieLife.BLL.DTOs.Admin.Ingredients;
using VeggieLife.BLL.Services.Cache;
using VeggieLife.BLL.Services.Knowledge;
using VeggieLife.DAL.Context;
using VeggieLife.DAL.Entities;
using Xunit;

namespace VeggieLife.Tests.Integration.Controllers;

/// <summary>
/// Bộ kiểm thử tích hợp (Integration Tests) gọi qua HTTP pipeline cho các endpoint quản trị nguyên liệu và dị ứng (FN-ADM-12).
/// </summary>
public class AdminIngredientsEndpointIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly Mock<IRedisService> _mockRedis = new();
    private readonly Mock<IKnowledgeSyncQueue> _mockKnowledgeSync = new();
    private readonly string _testDbName = "VeggieLifeAdminIngredientsIntegrationDb_" + Guid.NewGuid();

    public AdminIngredientsEndpointIntegrationTests(WebApplicationFactory<Program> factory)
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
                services.AddSingleton(_mockKnowledgeSync.Object);

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

        _mockKnowledgeSync.Setup(k => k.EnqueueUpsertAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
    }

    private static void SeedTestData(AppDbContext db)
    {
        var admin = new User
        {
            UserId = 1,
            Email = "admin.ingredients@veggielife.vn",
            FullName = "Admin Nguyên Liệu",
            Role = UserRoles.Admin,
            AccountStatus = "ACTIVE",
            CreatedAt = DateTime.UtcNow
        };
        var member = new User
        {
            UserId = 2,
            Email = "member.ingredients@veggielife.vn",
            FullName = "Hội Viên 2",
            Role = UserRoles.Member,
            AccountStatus = "ACTIVE",
            CreatedAt = DateTime.UtcNow
        };
        db.Users.AddRange(admin, member);

        var allergen1 = new Allergen
        {
            AllergenId = 1,
            AllergenName = "Đậu phộng",
            Code = "PEANUT",
            Description = "Dị ứng các loại lạc và đậu phộng",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        var allergen2 = new Allergen
        {
            AllergenId = 2,
            AllergenName = "Đậu nành",
            Code = "SOY",
            Description = "Dị ứng đậu tương và chế phẩm",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        db.Allergens.AddRange(allergen1, allergen2);

        var ing1 = new Ingredient
        {
            IngredientId = 10,
            IngredientName = "Nấm đùi gà",
            StandardUnit = "g",
            DefaultStallType = "PRODUCE",
            FoodGroup = "PLANT",
            CaloriesPer100g = 35.0m,
            IsAllium = false,
            CreatedAt = DateTime.UtcNow
        };
        var ing2 = new Ingredient
        {
            IngredientId = 11,
            IngredientName = "Tỏi ta",
            StandardUnit = "g",
            DefaultStallType = "CONDIMENTS",
            FoodGroup = "PLANT",
            CaloriesPer100g = 149.0m,
            IsAllium = true,
            CreatedAt = DateTime.UtcNow
        };
        db.Ingredients.AddRange(ing1, ing2);

        db.IngredientAllergens.Add(new IngredientAllergen
        {
            IngredientId = 10,
            AllergenId = 1,
            CreatedAt = DateTime.UtcNow
        });

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

    #region GET /api/v1/admin/ingredients

    [Fact]
    public async Task GetIngredients_WithoutToken_Returns401Unauthorized()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/v1/admin/ingredients");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain(ErrorCodes.ErrAuthInvalidToken);
    }

    [Fact]
    public async Task GetIngredients_WithMemberToken_Returns403Forbidden()
    {
        var client = CreateMemberClient();
        var response = await client.GetAsync("/api/v1/admin/ingredients");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain(ErrorCodes.ErrAdmForbidden);
    }

    [Fact]
    public async Task GetIngredients_WithAdminToken_Returns200AndValidList()
    {
        var client = CreateAdminClient();
        var response = await client.GetAsync("/api/v1/admin/ingredients?page=1&size=20");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<List<AdminIngredientListItemDto>>>();
        body.Should().NotBeNull();
        body!.StatusCode.Should().Be(200);
        body.Data.Should().NotBeEmpty();
        body.Meta.Should().NotBeNull();
        body.Meta!.Total.Should().BeGreaterThanOrEqualTo(2);

        var first = body.Data.First(i => i.IngredientId == 10);
        first.Name.Should().Be("Nấm đùi gà");
        first.IsAllium.Should().BeFalse();
        first.Allergens.Should().Contain(a => a.AllergenId == 1);
    }

    [Fact]
    public async Task GetIngredients_WithKeywordFilter_ReturnsFilteredResults()
    {
        var client = CreateAdminClient();
        var response = await client.GetAsync("/api/v1/admin/ingredients?keyword=Tỏi");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<List<AdminIngredientListItemDto>>>();
        body!.Data.Should().ContainSingle();
        body.Data[0].Name.Should().Be("Tỏi ta");
    }

    [Fact]
    public async Task GetIngredients_WithAllergenFilter_ReturnsFilteredResults()
    {
        var client = CreateAdminClient();
        var response = await client.GetAsync("/api/v1/admin/ingredients?allergenId=1");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<List<AdminIngredientListItemDto>>>();
        body!.Data.Should().ContainSingle();
        body.Data[0].IngredientId.Should().Be(10);
    }

    [Fact]
    public async Task GetIngredients_WithInvalidParams_Returns400BadRequest()
    {
        var client = CreateAdminClient();
        var response = await client.GetAsync("/api/v1/admin/ingredients?page=0");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain(ErrorCodes.ErrValidation);
    }

    #endregion

    #region POST /api/v1/admin/ingredients

    [Fact]
    public async Task CreateIngredient_WithoutToken_Returns401()
    {
        var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/v1/admin/ingredients", new CreateAdminIngredientRequest
        {
            Name = "Đậu hũ non",
            DefaultStallType = "PLANT_PROTEIN",
            CaloriesPer100g = 76
        });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CreateIngredient_WithMemberToken_Returns403()
    {
        var client = CreateMemberClient();
        var response = await client.PostAsJsonAsync("/api/v1/admin/ingredients", new CreateAdminIngredientRequest
        {
            Name = "Đậu hũ non",
            DefaultStallType = "PLANT_PROTEIN",
            CaloriesPer100g = 76
        });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task CreateIngredient_ValidData_Returns201CreatedAndAudited()
    {
        var client = CreateAdminClient();
        var req = new CreateAdminIngredientRequest
        {
            Name = "Hành lá",
            StandardUnit = "g",
            DefaultStallType = "CONDIMENTS",
            FoodGroup = "PLANT",
            CaloriesPer100g = 32.0m,
            IsAllium = true
        };

        var response = await client.PostAsJsonAsync("/api/v1/admin/ingredients", req);
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var body = await response.Content.ReadFromJsonAsync<ApiResponse<CreateAdminIngredientResponseData>>();
        body.Should().NotBeNull();
        body!.StatusCode.Should().Be(201);
        body.Data!.IngredientId.Should().BeGreaterThan(0);

        // Verify DB
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var inDb = await db.Ingredients.FindAsync(body.Data!.IngredientId);
        inDb.Should().NotBeNull();
        inDb!.IngredientName.Should().Be("Hành lá");
        inDb.IsAllium.Should().BeTrue();

        // Verify AuditLog
        var audit = await db.AuditLogs.FirstOrDefaultAsync(a => a.Action == "CREATE_INGREDIENT" && a.TargetId == body.Data!.IngredientId.ToString());
        audit.Should().NotBeNull();
        audit!.ActorRole.Should().Be(UserRoles.Admin);

        // Verify KnowledgeSync Queue called
        _mockKnowledgeSync.Verify(k => k.EnqueueUpsertAsync("INGREDIENT", body.Data!.IngredientId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateIngredient_DuplicateName_Returns409Conflict()
    {
        var client = CreateAdminClient();
        var req = new CreateAdminIngredientRequest
        {
            Name = "Nấm đùi gà", // Already seeded
            DefaultStallType = "PRODUCE",
            CaloriesPer100g = 35.0m
        };

        var response = await client.PostAsJsonAsync("/api/v1/admin/ingredients", req);
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);

        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain(ErrorCodes.ErrIngredientDuplicate);
    }

    [Theory]
    [InlineData("", "PRODUCE", 100)] // Empty name
    [InlineData("Nguyên liệu test", "INVALID_STALL", 100)] // Invalid stall
    [InlineData("Nguyên liệu test", "PRODUCE", -5)] // Negative calories
    public async Task CreateIngredient_InvalidData_Returns400BadRequest(string name, string stall, decimal calories)
    {
        var client = CreateAdminClient();
        var req = new CreateAdminIngredientRequest
        {
            Name = name,
            DefaultStallType = stall,
            CaloriesPer100g = calories
        };

        var response = await client.PostAsJsonAsync("/api/v1/admin/ingredients", req);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain(ErrorCodes.ErrValidation);
    }

    #endregion

    #region PUT /api/v1/admin/ingredients/{id}

    [Fact]
    public async Task UpdateIngredient_ValidData_Returns200OK()
    {
        var client = CreateAdminClient();
        var req = new UpdateAdminIngredientRequest
        {
            Name = "Nấm đùi gà baby",
            CaloriesPer100g = 38.0m,
            IsAllium = false
        };

        var response = await client.PutAsJsonAsync("/api/v1/admin/ingredients/10", req);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<ApiResponse<UpdateAdminIngredientResponseData>>();
        body.Should().NotBeNull();
        body!.Data!.Name.Should().Be("Nấm đùi gà baby");
        body.Data!.CaloriesPer100g.Should().Be(38.0m);

        // Verify KnowledgeSync Queue called
        _mockKnowledgeSync.Verify(k => k.EnqueueUpsertAsync("INGREDIENT", 10, It.IsAny<CancellationToken>()), Times.AtLeastOnce);
    }

    [Fact]
    public async Task UpdateIngredient_NonExistentId_Returns404NotFound()
    {
        var client = CreateAdminClient();
        var req = new UpdateAdminIngredientRequest { Name = "Không tồn tại" };

        var response = await client.PutAsJsonAsync("/api/v1/admin/ingredients/9999", req);
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);

        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain(ErrorCodes.ErrAdmNotFound);
    }

    [Fact]
    public async Task UpdateIngredient_DuplicateNameWithOther_Returns409Conflict()
    {
        var client = CreateAdminClient();
        var req = new UpdateAdminIngredientRequest { Name = "Tỏi ta" }; // Id 11 is Tỏi ta

        var response = await client.PutAsJsonAsync("/api/v1/admin/ingredients/10", req);
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);

        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain(ErrorCodes.ErrIngredientDuplicate);
    }

    #endregion

    #region PUT /api/v1/admin/ingredients/{id}/allergens

    [Fact]
    public async Task ReplaceIngredientAllergens_ValidData_Returns200OKAndReplaces()
    {
        var client = CreateAdminClient();
        var req = new ReplaceAdminIngredientAllergensRequest
        {
            AllergenIds = new List<int> { 2 } // Replace allergen 1 with allergen 2
        };

        var response = await client.PutAsJsonAsync("/api/v1/admin/ingredients/10/allergens", req);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<ApiResponse<ReplaceAdminIngredientAllergensResponseData>>();
        body.Should().NotBeNull();
        body!.Data!.IngredientId.Should().Be(10);
        body.Data!.AllergenIds.Should().ContainSingle(a => a == 2);

        // Verify DB
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var links = await db.IngredientAllergens.Where(ia => ia.IngredientId == 10).ToListAsync();
        links.Should().ContainSingle(l => l.AllergenId == 2);

        // Verify KnowledgeSync Queue called
        _mockKnowledgeSync.Verify(k => k.EnqueueUpsertAsync("INGREDIENT", 10, It.IsAny<CancellationToken>()), Times.AtLeastOnce);
    }

    [Fact]
    public async Task ReplaceIngredientAllergens_NonExistentIngredient_Returns404NotFound()
    {
        var client = CreateAdminClient();
        var req = new ReplaceAdminIngredientAllergensRequest { AllergenIds = new List<int> { 1 } };

        var response = await client.PutAsJsonAsync("/api/v1/admin/ingredients/9999/allergens", req);
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain(ErrorCodes.ErrAdmNotFound);
    }

    [Fact]
    public async Task ReplaceIngredientAllergens_NonExistentAllergen_Returns400BadRequest()
    {
        var client = CreateAdminClient();
        var req = new ReplaceAdminIngredientAllergensRequest { AllergenIds = new List<int> { 9999 } };

        var response = await client.PutAsJsonAsync("/api/v1/admin/ingredients/10/allergens", req);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain(ErrorCodes.ErrValidation);
    }

    #endregion

    #region POST /api/v1/admin/allergens

    [Fact]
    public async Task CreateAllergen_WithoutToken_Returns401()
    {
        var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/v1/admin/allergens", new CreateAdminAllergenRequest
        {
            AllergenName = "Gluten",
            Code = "GLUTEN"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CreateAllergen_ValidData_Returns201CreatedAndAudited()
    {
        var client = CreateAdminClient();
        var req = new CreateAdminAllergenRequest
        {
            AllergenName = "Hạt mè (vừng)",
            Code = "SESAME",
            Description = "Dị ứng mè đen, mè trắng và dầu mè"
        };

        var response = await client.PostAsJsonAsync("/api/v1/admin/allergens", req);
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var body = await response.Content.ReadFromJsonAsync<ApiResponse<CreateAdminAllergenResponseData>>();
        body.Should().NotBeNull();
        body!.StatusCode.Should().Be(201);
        body.Data!.AllergenId.Should().BeGreaterThan(0);

        // Verify DB
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var inDb = await db.Allergens.FindAsync(body.Data!.AllergenId);
        inDb.Should().NotBeNull();
        inDb!.AllergenName.Should().Be("Hạt mè (vừng)");
        inDb.Code.Should().Be("SESAME");

        // Verify KnowledgeSync Queue called
        _mockKnowledgeSync.Verify(k => k.EnqueueUpsertAsync("ALLERGEN", body.Data!.AllergenId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAllergen_DuplicateNameOrCode_Returns409Conflict()
    {
        var client = CreateAdminClient();
        var req = new CreateAdminAllergenRequest
        {
            AllergenName = "Đậu phộng", // Already seeded
            Code = "NEW_CODE"
        };

        var response = await client.PostAsJsonAsync("/api/v1/admin/allergens", req);
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);

        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain(ErrorCodes.ErrAllergenDuplicate);
    }

    [Fact]
    public async Task CreateAllergen_InvalidData_Returns400BadRequest()
    {
        var client = CreateAdminClient();
        var req = new CreateAdminAllergenRequest
        {
            AllergenName = "",
            Code = ""
        };

        var response = await client.PostAsJsonAsync("/api/v1/admin/allergens", req);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain(ErrorCodes.ErrValidation);
    }

    #endregion
}
