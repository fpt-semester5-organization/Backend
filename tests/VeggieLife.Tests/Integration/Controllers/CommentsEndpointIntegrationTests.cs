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
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Moq;
using VeggieLife.BLL.Common.Constants;
using VeggieLife.BLL.Common.Responses;
using VeggieLife.BLL.DTOs.Community.Requests;
using VeggieLife.BLL.DTOs.Community.Responses;
using VeggieLife.BLL.Services.Cache;
using VeggieLife.DAL.Context;
using VeggieLife.DAL.Entities;
using Xunit;

namespace VeggieLife.Tests.Integration.Controllers;

/// <summary>
/// Bộ kiểm thử tích hợp (Integration Tests) gọi qua HTTP pipeline cho các endpoint bình luận (FN-COMM-01, FN-COMM-02, FN-COMM-03).
/// </summary>
public class CommentsEndpointIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly Mock<IRedisService> _mockRedis = new();
    private readonly string _testDbName = "VeggieLifeCommentsIntegrationDb_" + Guid.NewGuid();

    public CommentsEndpointIntegrationTests(WebApplicationFactory<Program> factory)
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

        if (db.Users.Any()) return;

        var member1 = new User
        {
            UserId = 101,
            Email = "member101@veggielife.vn",
            FullName = "Nguyễn Văn Một",
            Role = UserRoles.Member,
            AccountStatus = "ACTIVE",
            CreatedAt = DateTime.UtcNow.AddDays(-10)
        };
        var member2 = new User
        {
            UserId = 102,
            Email = "member102@veggielife.vn",
            FullName = "Trần Thị Hai",
            Role = UserRoles.Member,
            AccountStatus = "ACTIVE",
            CreatedAt = DateTime.UtcNow.AddDays(-10)
        };
        db.Users.AddRange(member1, member2);

        var school = new DietarySchool
        {
            DietarySchoolId = 1,
            SchoolCode = "VEGAN",
            SchoolName = "Thuần chay",
            ForbiddenFoodRules = "Không thịt cá",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        db.DietarySchools.Add(school);

        var category = new FoodCategory
        {
            CategoryId = 1,
            CategoryName = "Món Canh",
            IsCore = true,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        db.FoodCategories.Add(category);

        var post = new Post
        {
            PostId = 501,
            UserId = 101,
            Title = "Bài viết món chay thanh đạm",
            Content = "Nội dung bài viết",
            Visibility = "PUBLIC",
            ModerationStatus = "APPROVED_ACTIVE",
            CreatedAt = DateTime.UtcNow.AddDays(-2)
        };
        db.Posts.Add(post);

        var recipe = new Recipe
        {
            RecipeId = 601,
            UserId = 101,
            CategoryId = 1,
            DietarySchoolId = 1,
            Title = "Công thức canh chua chay",
            Instructions = "Nấu canh chua với dứa và cà chua",
            DifficultyLevel = "EASY",
            Source = "USER",
            Status = "APPROVED_ACTIVE",
            CreatedAt = DateTime.UtcNow.AddDays(-2)
        };
        db.Recipes.Add(recipe);

        // Bình luận có sẵn trên bài viết 501
        var comment = new Comment
        {
            CommentId = 701,
            PostId = 501,
            UserId = 101,
            Content = "Bình luận ban đầu của tác giả 101",
            Status = "ACTIVE",
            IsSoftDeleted = false,
            CreatedAt = DateTime.UtcNow.AddDays(-1)
        };
        var reply = new Comment
        {
            CommentId = 702,
            PostId = 501,
            UserId = 102,
            ParentCommentId = 701,
            Content = "Phản hồi của thành viên 102",
            Status = "ACTIVE",
            IsSoftDeleted = false,
            CreatedAt = DateTime.UtcNow.AddHours(-10)
        };
        db.Comments.AddRange(comment, reply);

        db.SaveChanges();
    }

    private string GenerateJwt(int userId, string email, string role)
    {
        using var scope = _factory.Services.CreateScope();
        var config = scope.ServiceProvider.GetRequiredService<Microsoft.Extensions.Configuration.IConfiguration>();
        var secretKey = config["Jwt:SecretKey"]
            ?? config["JwtSettings:SecretKey"]
            ?? throw new InvalidOperationException("Cấu hình 'Jwt:SecretKey' chưa được thiết lập.");
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
                new Claim(ClaimTypes.Email, email)
            }),
            Expires = DateTime.UtcNow.AddHours(2),
            Issuer = issuer,
            Audience = audience,
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }

    private HttpClient CreateAuthenticatedClient(int userId, string email, string role = UserRoles.Member)
    {
        var client = _factory.CreateClient();
        var token = GenerateJwt(userId, email, role);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    // =========================================================================
    // FN-COMM-01: GET /api/v1/posts/{id}/comments & GET /api/v1/recipes/{id}/comments
    // =========================================================================

    [Fact]
    public async Task GetPostComments_PublicClient_Returns200WithNestedTree()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/v1/posts/501/comments?page=1&size=20");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<ApiResponse<List<CommentDto>>>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        result.Should().NotBeNull();
        result!.StatusCode.Should().Be(200);
        result.Data.Should().HaveCount(1); // 1 bình luận gốc 701
        result.Data![0].CommentId.Should().Be(701);
        result.Data[0].Replies.Should().HaveCount(1); // 1 câu trả lời 702
        result.Data[0].Replies[0].CommentId.Should().Be(702);
    }

    [Fact]
    public async Task GetPostComments_InvalidId_Returns400()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/v1/posts/0/comments");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain(ErrorCodes.ErrValidation);
    }

    [Fact]
    public async Task GetPostComments_NonExistentPost_Returns404()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/v1/posts/99999/comments");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain(ErrorCodes.ErrContentNotFound);
    }

    [Fact]
    public async Task GetRecipeComments_ValidRecipe_Returns200()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/v1/recipes/601/comments");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<ApiResponse<List<CommentDto>>>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        result.Should().NotBeNull();
        result!.StatusCode.Should().Be(200);
        result.Data.Should().BeEmpty(); // Chưa có bình luận nào
    }

    // =========================================================================
    // FN-COMM-02: POST /api/v1/posts/{id}/comments & POST /api/v1/recipes/{id}/comments
    // =========================================================================

    [Fact]
    public async Task CreatePostComment_NoToken_Returns401()
    {
        var client = _factory.CreateClient();
        var req = new CreateCommentRequest { Content = "Bình luận không token" };

        var response = await client.PostAsJsonAsync("/api/v1/posts/501/comments", req);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CreatePostComment_GuestToken_Returns403()
    {
        var client = CreateAuthenticatedClient(999, "guest@veggielife.vn", UserRoles.Guest);
        var req = new CreateCommentRequest { Content = "Bình luận bằng guest" };

        var response = await client.PostAsJsonAsync("/api/v1/posts/501/comments", req);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain(ErrorCodes.ErrGuestCommunityActionDenied);
    }

    [Fact]
    public async Task CreatePostComment_MemberSuccess_Returns201()
    {
        var client = CreateAuthenticatedClient(102, "member102@veggielife.vn", UserRoles.Member);
        var req = new CreateCommentRequest { Content = "Bình luận mới của member 102" };

        var response = await client.PostAsJsonAsync("/api/v1/posts/501/comments", req);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<ApiResponse<CommentDto>>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        result.Should().NotBeNull();
        result!.StatusCode.Should().Be(201);
        result.Message.Should().Be("Đã bình luận.");
        result.Data!.Content.Should().Be(req.Content);
        result.Data.Author.FullName.Should().Be("Trần Thị Hai");
    }

    [Fact]
    public async Task CreatePostComment_CooldownActive_Returns429()
    {
        var client = CreateAuthenticatedClient(101, "member101@veggielife.vn", UserRoles.Member);
        var req = new CreateCommentRequest { Content = "Bình luận nhanh liên tiếp" };

        // Giả lập Redis báo cooldown đang kích hoạt cho user 101
        _mockRedis
            .Setup(r => r.ExistsAsync("comment_cooldown:101", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var response = await client.PostAsJsonAsync("/api/v1/posts/501/comments", req);

        response.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain(ErrorCodes.ErrCommentTooFast);
    }

    [Fact]
    public async Task CreateRecipeComment_MemberSuccess_Returns201()
    {
        var client = CreateAuthenticatedClient(102, "member102@veggielife.vn", UserRoles.Member);
        var req = new CreateCommentRequest { Content = "Bình luận công thức canh chua rất ngon!" };

        var response = await client.PostAsJsonAsync("/api/v1/recipes/601/comments", req);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Đã bình luận.");
    }

    // =========================================================================
    // FN-COMM-03: PUT /api/v1/comments/{id} & DELETE /api/v1/comments/{id}
    // =========================================================================

    [Fact]
    public async Task UpdateComment_AuthorSuccess_Returns200()
    {
        // 701 thuộc user 101
        var client = CreateAuthenticatedClient(101, "member101@veggielife.vn", UserRoles.Member);
        var req = new UpdateCommentRequest { Content = "Nội dung bình luận sau khi tác giả sửa đổi" };

        var response = await client.PutAsJsonAsync("/api/v1/comments/701", req);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<ApiResponse<CommentDto>>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        result.Should().NotBeNull();
        result!.StatusCode.Should().Be(200);
        result.Message.Should().Be("Đã cập nhật.");
        result.Data!.Content.Should().Be(req.Content);
    }

    [Fact]
    public async Task UpdateComment_NotAuthor_Returns403()
    {
        // 701 thuộc user 101, user 102 cố tình sửa
        var client = CreateAuthenticatedClient(102, "member102@veggielife.vn", UserRoles.Member);
        var req = new UpdateCommentRequest { Content = "Cố sửa bình luận của người khác" };

        var response = await client.PutAsJsonAsync("/api/v1/comments/701", req);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain(ErrorCodes.ErrContentNotOwner);
    }

    [Fact]
    public async Task DeleteComment_NotAuthor_Returns403()
    {
        // 701 thuộc user 101, user 102 cố tình xóa
        var client = CreateAuthenticatedClient(102, "member102@veggielife.vn", UserRoles.Member);

        var response = await client.DeleteAsync("/api/v1/comments/701");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain(ErrorCodes.ErrContentNotOwner);
    }

    [Fact]
    public async Task DeleteComment_AuthorSuccess_Returns200WithHiddenReplies()
    {
        // 701 thuộc user 101, có 1 câu trả lời 702 bên dưới
        var client = CreateAuthenticatedClient(101, "member101@veggielife.vn", UserRoles.Member);

        var response = await client.DeleteAsync("/api/v1/comments/701");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<ApiResponse<DeleteCommentResponseData>>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        result.Should().NotBeNull();
        result!.StatusCode.Should().Be(200);
        result.Message.Should().Be("Đã xóa bình luận.");
        result.Data!.HiddenReplies.Should().Be(1); // 702 bị ẩn theo cascade

        // Gọi lại GET /api/v1/posts/501/comments -> Danh sách rỗng vì đã xóa mềm cả cây
        var getResp = await client.GetAsync("/api/v1/posts/501/comments");
        var getContent = await getResp.Content.ReadAsStringAsync();
        var getResult = JsonSerializer.Deserialize<ApiResponse<List<CommentDto>>>(getContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        getResult!.Data.Should().BeEmpty();
    }
}
