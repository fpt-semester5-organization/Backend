using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using VeggieLife.BLL.Common.Constants;
using VeggieLife.BLL.Common.Responses;
using VeggieLife.BLL.DTOs.Recipes.Responses;
using VeggieLife.DAL.Context;
using VeggieLife.DAL.Entities;
using Xunit;

namespace VeggieLife.Tests.Integration;

/// <summary>
/// Bộ kiểm thử tích hợp (Integration Tests) gọi trực tiếp qua pipeline HTTP của Web API đối với endpoint Recipes (FN-POST-01, FN-POST-02).
/// Đảm bảo tính toàn vẹn của Middleware, Routing, Exception Handling và Envelope chuẩn D52.
/// </summary>
public class RecipesEndpointIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;
    private readonly string _inMemoryDbName = Guid.NewGuid().ToString();

    public RecipesEndpointIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Development");
            builder.ConfigureServices(services =>
            {
                // Thay thế DbContext bằng InMemoryDatabase để test độc lập và ổn định
                var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
                if (descriptor != null)
                {
                    services.Remove(descriptor);
                }

                services.AddDbContext<AppDbContext>(options =>
                {
                    options.UseInMemoryDatabase(_inMemoryDbName);
                });

                // Seed dữ liệu ban đầu cho database test
                var sp = services.BuildServiceProvider();
                using var scope = sp.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                SeedDatabase(db);
            });
        }).CreateClient();
    }

    private static void SeedDatabase(AppDbContext db)
    {
        db.Database.EnsureDeleted();
        db.Database.EnsureCreated();

        // 1. Dietary Schools
        db.DietarySchools.AddRange(
            new DietarySchool { DietarySchoolId = 7, SchoolCode = "VEGAN", SchoolName = "Thuần chay", ForbiddenFoodRules = "{}" },
            new DietarySchool { DietarySchoolId = 1, SchoolCode = "BUDDHIST", SchoolName = "Chay Phật giáo", ForbiddenFoodRules = "{}" },
            new DietarySchool { DietarySchoolId = 2, SchoolCode = "LACTO", SchoolName = "Chay có sữa", ForbiddenFoodRules = "{}" }
        );

        // 2. Categories
        db.FoodCategories.AddRange(
            new FoodCategory { CategoryId = 3, CategoryName = "Món Canh", IsActive = true },
            new FoodCategory { CategoryId = 9, CategoryName = "Món Kho", IsActive = true },
            new FoodCategory { CategoryId = 14, CategoryName = "Món Xào", IsActive = true },
            new FoodCategory { CategoryId = 99, CategoryName = "Danh mục ẩn", IsActive = false }
        );

        // 3. Ingredients
        var tofu = new Ingredient { IngredientId = 39, IngredientName = "Đậu hũ non", StandardUnit = "g", CaloriesPer100g = 76m, FoodGroup = "PLANT", DefaultStallType = "PLANT_PROTEIN" };
        var mushroom = new Ingredient { IngredientId = 90, IngredientName = "Nấm rơm tươi", StandardUnit = "g", CaloriesPer100g = 34m, FoodGroup = "PLANT", DefaultStallType = "PRODUCE" };
        var corn = new Ingredient { IngredientId = 5, IngredientName = "Bắp ngọt tươi", StandardUnit = "g", CaloriesPer100g = 86m, FoodGroup = "PLANT", DefaultStallType = "PRODUCE" };
        db.Ingredients.AddRange(tofu, mushroom, corn);

        // 4. Recipes
        var r1 = new Recipe
        {
            RecipeId = 201,
            CategoryId = 3,
            DietarySchoolId = 7,
            Title = "Canh nấm đậu phụ rong biển",
            Description = "Món canh thanh mát",
            Instructions = "[\"Bước 1: Rửa sạch nấm.\", \"Bước 2: Nấu sôi canh.\"]",
            CookTimeMinutes = 25,
            PrepTimeMinutes = 10,
            Servings = 2,
            CaloriesKcal = 310m,
            DifficultyLevel = "EASY",
            IsVerified = true,
            Status = "APPROVED_ACTIVE",
            ViewCount = 100,
            Source = "USER",
            CreatedAt = DateTime.UtcNow.AddDays(-3)
        };

        var r2 = new Recipe
        {
            RecipeId = 202,
            CategoryId = 9,
            DietarySchoolId = 1,
            Title = "Nấm kho tiêu chay",
            Description = "Món kho đậm đà",
            Instructions = "Bước 1: Ướp nấm.\nBước 2: Kho trên lửa nhỏ.",
            CookTimeMinutes = 35,
            PrepTimeMinutes = 10,
            Servings = 4,
            CaloriesKcal = 240m,
            DifficultyLevel = "MEDIUM",
            IsVerified = true,
            Status = "APPROVED_ACTIVE",
            ViewCount = 200,
            Source = "USER",
            CreatedAt = DateTime.UtcNow.AddDays(-2)
        };

        var r3 = new Recipe
        {
            RecipeId = 203,
            CategoryId = 14,
            DietarySchoolId = 7,
            Title = "Mì xào nấm rau củ",
            Description = "Món xào thơm ngon có video",
            Instructions = "Bước 1: Xào chín mì.",
            CookTimeMinutes = 15,
            PrepTimeMinutes = 5,
            Servings = 2,
            CaloriesKcal = 350m,
            DifficultyLevel = "EASY",
            IsVerified = false,
            Status = "APPROVED_ACTIVE",
            ViewCount = 50,
            Source = "USER",
            CreatedAt = DateTime.UtcNow.AddDays(-1)
        };

        var rDraft = new Recipe
        {
            RecipeId = 204,
            CategoryId = 3,
            DietarySchoolId = 7,
            Title = "Bản nháp không xuất hiện",
            Instructions = "Nháp",
            CookTimeMinutes = 20,
            Servings = 1,
            CaloriesKcal = 100m,
            DifficultyLevel = "EASY",
            Status = "DRAFT",
            Source = "USER",
            CreatedAt = DateTime.UtcNow
        };

        db.Recipes.AddRange(r1, r2, r3, rDraft);

        // RecipeIngredients
        db.RecipeIngredients.AddRange(
            new RecipeIngredient { RecipeId = 201, IngredientId = 39, QuantityNeeded = 200, Unit = "g" },
            new RecipeIngredient { RecipeId = 201, IngredientId = 90, QuantityNeeded = 150, Unit = "g" },
            new RecipeIngredient { RecipeId = 202, IngredientId = 90, QuantityNeeded = 200, Unit = "g" }
        );

        // Posts
        var post1 = new Post { PostId = 501, RecipeId = 201, Title = r1.Title, Content = "Content 1", Visibility = "PUBLIC", ModerationStatus = "APPROVED_ACTIVE", CreatedAt = DateTime.UtcNow };
        var post2 = new Post { PostId = 502, RecipeId = 202, Title = r2.Title, Content = "Content 2", Visibility = "PUBLIC", ModerationStatus = "APPROVED_ACTIVE", CreatedAt = DateTime.UtcNow };
        var post3 = new Post { PostId = 503, RecipeId = 203, Title = r3.Title, Content = "Content 3", Visibility = "PUBLIC", ModerationStatus = "APPROVED_ACTIVE", CreatedAt = DateTime.UtcNow };
        db.Posts.AddRange(post1, post2, post3);

        // Votes
        db.Votes.Add(new Vote { VoteId = 1, PostId = 501, UserId = 1, CreatedAt = DateTime.UtcNow });
        db.Votes.Add(new Vote { VoteId = 2, PostId = 501, UserId = 2, CreatedAt = DateTime.UtcNow });
        db.Votes.Add(new Vote { VoteId = 3, PostId = 502, UserId = 1, CreatedAt = DateTime.UtcNow });

        // Video READY cho Post 3
        var video3 = new Video { VideoId = 901, PostId = 503, Title = "Video mì xào", VideoUrl = "https://v.vn/1.mp4", Status = "READY", CreatedAt = DateTime.UtcNow };
        db.Videos.Add(video3);

        db.SaveChanges();
    }

    [Fact]
    public async Task GetRecipes_List_ReturnsOkEnvelopeWithMeta()
    {
        // Act
        var response = await _client.GetAsync("/api/v1/recipes");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadFromJsonAsync<ApiResponse<List<RecipeSummaryDto>>>(new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        content.Should().NotBeNull();
        content!.StatusCode.Should().Be(200);
        content.Message.Should().Be("OK");
        content.Data.Should().HaveCount(3); // Không chứa công thức 204 (DRAFT)
        content.Meta.Should().NotBeNull();
        content.Meta!.Total.Should().Be(3);
        content.Meta.SignUpPromptAtItem.Should().Be(5); // Guest prompt
    }

    [Fact]
    public async Task GetRecipes_FilterByDietarySchool_ReturnsMatchingOnly()
    {
        // Act: Trường phái VEGAN (ID 7) -> Recipe 201 và 203
        var response = await _client.GetAsync("/api/v1/recipes?dietarySchoolId=7");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadFromJsonAsync<ApiResponse<List<RecipeSummaryDto>>>(new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        content!.Data.Should().HaveCount(2);
        content.Data!.All(r => r.DietarySchool == "VEGAN").Should().BeTrue();
    }

    [Fact]
    public async Task GetRecipes_FilterByIngredient_ReturnsMatchingOnly()
    {
        // Act: Nguyên liệu Đậu hũ -> chỉ Recipe 201 có Đậu hũ non
        var response = await _client.GetAsync("/api/v1/recipes?ingredient=Đậu hũ");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadFromJsonAsync<ApiResponse<List<RecipeSummaryDto>>>(new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        content!.Data.Should().HaveCount(1);
        content.Data![0].RecipeId.Should().Be(201);
    }

    [Fact]
    public async Task GetRecipes_FilterByContentTypeVideo_ReturnsOnlyVideo()
    {
        // Act: contentType = VIDEO -> Recipe 203 có video READY
        var response = await _client.GetAsync("/api/v1/recipes?contentType=VIDEO");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadFromJsonAsync<ApiResponse<List<RecipeSummaryDto>>>(new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        content!.Data.Should().HaveCount(1);
        content.Data![0].RecipeId.Should().Be(203);
        content.Data[0].Type.Should().Be("VIDEO");
    }

    [Fact]
    public async Task GetRecipe_Detail_ReturnsOkEnvelopeWithFullFields()
    {
        // Act
        var response = await _client.GetAsync("/api/v1/recipes/201");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadFromJsonAsync<ApiResponse<RecipeDetailDto>>(new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        content.Should().NotBeNull();
        content!.StatusCode.Should().Be(200);
        content.Data.Should().NotBeNull();
        content.Data!.RecipeId.Should().Be(201);
        content.Data.Title.Should().Be("Canh nấm đậu phụ rong biển");
        content.Data.DietarySchool.Should().Be("VEGAN");
        content.Data.Instructions.Should().HaveCount(2);
        content.Data.Ingredients.Should().HaveCount(2);
        content.Data.VoteCount.Should().Be(2);

        // Khách truy cập -> memberActions toàn bộ false
        content.Data.MemberActions.CanVote.Should().BeFalse();
        content.Data.MemberActions.CanComment.Should().BeFalse();
        content.Data.MemberActions.CanReport.Should().BeFalse();
    }

    [Fact]
    public async Task GetRecipe_DraftRecipe_ReturnsNotFoundEnvelope()
    {
        // Act: Recipe 204 là DRAFT
        var response = await _client.GetAsync("/api/v1/recipes/204");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);

        var error = await response.Content.ReadFromJsonAsync<ApiErrorResponse>(new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        error.Should().NotBeNull();
        error!.StatusCode.Should().Be(404);
        error.ErrorCode.Should().Be(ErrorCodes.ErrContentNotFound);
    }

    [Fact]
    public async Task GetRecipes_InvalidMaxCookTime_ReturnsBadRequestEnvelope()
    {
        // Act: maxCookTime = 999 (> 600)
        var response = await _client.GetAsync("/api/v1/recipes?maxCookTime=999");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var error = await response.Content.ReadFromJsonAsync<ApiErrorResponse>(new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        error.Should().NotBeNull();
        error!.StatusCode.Should().Be(400);
        error.ErrorCode.Should().Be(ErrorCodes.ErrValidation);
    }
}
