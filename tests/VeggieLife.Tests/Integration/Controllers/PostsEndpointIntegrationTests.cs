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
using VeggieLife.BLL.DTOs.Posts.Requests;
using VeggieLife.BLL.DTOs.Posts.Responses;
using VeggieLife.BLL.Services.Cache;
using VeggieLife.DAL.Context;
using VeggieLife.DAL.Entities;
using Xunit;

namespace VeggieLife.Tests.Integration.Controllers;

/// <summary>
/// Bộ kiểm thử tích hợp (Integration Tests) gọi qua HTTP pipeline cho các endpoint bài viết FN-POST-07.
/// Kiểm thử đầy đủ các ca: Không token (401), Guest (403), Đăng thành công (201), Vượt hạn mức bài thứ 3 cho tài khoản mới (429),
/// Sửa bài (200), Xóa bài (200), Không phải tác giả (403), Lỗi validation (400).
/// </summary>
public class PostsEndpointIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly Mock<IRedisService> _mockRedis = new();
    private readonly string _testDbName = "VeggieLifePostsIntegrationDb_" + Guid.NewGuid();

    public PostsEndpointIntegrationTests(WebApplicationFactory<Program> factory)
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

        // 1. Thành viên cũ (tạo > 3 ngày trước)
        var member1 = new User
        {
            UserId = 101,
            Email = "member101@veggielife.vn",
            FullName = "Nguyễn Văn Một",
            Role = UserRoles.Member,
            AccountStatus = "ACTIVE",
            CreatedAt = DateTime.UtcNow.AddDays(-10)
        };

        // 2. Thành viên khác
        var member2 = new User
        {
            UserId = 102,
            Email = "member102@veggielife.vn",
            FullName = "Trần Thị Hai",
            Role = UserRoles.Member,
            AccountStatus = "ACTIVE",
            CreatedAt = DateTime.UtcNow.AddDays(-10)
        };

        // 3. Tân binh (< 3 ngày)
        var newMember = new User
        {
            UserId = 103,
            Email = "tanbinh103@veggielife.vn",
            FullName = "Lê Tân Binh",
            Role = UserRoles.Member,
            AccountStatus = "ACTIVE",
            CreatedAt = DateTime.UtcNow.AddDays(-1) // 1 ngày tuổi
        };

        db.Users.AddRange(member1, member2, newMember);

        // Seed bài viết 501 cho test sửa bài
        var post501 = new Post
        {
            PostId = 501,
            UserId = 101,
            Title = "Bài viết có sẵn để cập nhật của 101",
            Content = "Nội dung bài viết có sẵn để kiểm thử sửa...",
            Visibility = "PUBLIC",
            ModerationStatus = "APPROVED_ACTIVE",
            ViewCount = 10,
            CreatedAt = DateTime.UtcNow.AddDays(-2)
        };

        // Seed bài viết 502 cho test không phải tác giả (403)
        var post502 = new Post
        {
            PostId = 502,
            UserId = 101,
            Title = "Bài viết để test phân quyền sửa của 101",
            Content = "Nội dung kiểm thử phân quyền sửa không phải tác giả...",
            Visibility = "PUBLIC",
            ModerationStatus = "APPROVED_ACTIVE",
            ViewCount = 5,
            CreatedAt = DateTime.UtcNow.AddDays(-2)
        };

        // Seed bài viết 503 cho test xóa bài
        var post503 = new Post
        {
            PostId = 503,
            UserId = 101,
            Title = "Bài viết để test xóa của 101",
            Content = "Nội dung kiểm thử xóa bài viết...",
            Visibility = "PUBLIC",
            ModerationStatus = "APPROVED_ACTIVE",
            ViewCount = 3,
            CreatedAt = DateTime.UtcNow.AddDays(-2)
        };

        db.Posts.AddRange(post501, post502, post503);
        db.SaveChanges();
    }

    private string GenerateJwtToken(int userId, string role)
    {
        using var scope = _factory.Services.CreateScope();
        var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();
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
    public async Task Post_WithoutToken_ShouldReturn401Unauthorized()
    {
        // Arrange
        var client = _factory.CreateClient();
        var req = new CreatePostRequest
        {
            Title = "Tiêu đề bài viết hợp lệ không token",
            Content = "Nội dung bài viết..."
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/v1/posts", req);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        var error = await response.Content.ReadFromJsonAsync<ApiErrorResponse>();
        error.Should().NotBeNull();
        error!.ErrorCode.Should().Be(ErrorCodes.ErrAuthInvalidToken);
    }

    [Fact]
    public async Task Post_WithGuestToken_ShouldReturn403Forbidden()
    {
        // Arrange: Token của Khách vãng lai
        var client = _factory.CreateClient();
        var guestToken = GenerateJwtToken(999, UserRoles.Guest);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", guestToken);

        var req = new CreatePostRequest
        {
            Title = "Khách thử đăng bài cộng đồng",
            Content = "Nội dung bài viết từ khách..."
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/v1/posts", req);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        var error = await response.Content.ReadFromJsonAsync<ApiErrorResponse>();
        error.Should().NotBeNull();
        error!.ErrorCode.Should().Be(ErrorCodes.ErrGuestCommunityActionDenied);
    }

    [Fact]
    public async Task Post_MemberSubmitCleanPost_ShouldReturn201Created()
    {
        // Arrange: Member hợp lệ
        var client = _factory.CreateClient();
        var memberToken = GenerateJwtToken(101, UserRoles.Member);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", memberToken);

        var req = new CreatePostRequest
        {
            Title = "Một tuần trải nghiệm ăn chay thanh đạm",
            Content = "Mỗi bữa ăn mình đều cảm thấy tinh thần sảng khoái và tràn đầy năng lượng tích cực.",
            Visibility = "PUBLIC",
            Submit = true
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/v1/posts", req);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var envelope = await response.Content.ReadFromJsonAsync<ApiResponse<CreatePostResponseDto>>();
        envelope.Should().NotBeNull();
        envelope!.StatusCode.Should().Be(201);
        envelope.Message.Should().Be("Đã đăng bài.");
        envelope.Data.Should().NotBeNull();
        envelope.Data!.PostId.Should().BeGreaterThan(0);
        envelope.Data.ModerationStatus.Should().Be("APPROVED_ACTIVE");
    }

    [Fact]
    public async Task Post_NewAccount_ThirdPostInDay_ShouldReturn429TooManyRequests()
    {
        // Arrange: Tài khoản mới (User 103, < 3 ngày tuổi)
        var client = _factory.CreateClient();
        var token = GenerateJwtToken(103, UserRoles.Member);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var req1 = new CreatePostRequest
        {
            Title = "Tân binh đăng bài thứ 1 trong ngày",
            Content = "Nội dung chia sẻ bài 1...",
            Submit = true
        };
        var req2 = new CreatePostRequest
        {
            Title = "Tân binh đăng bài thứ 2 trong ngày",
            Content = "Nội dung chia sẻ bài 2...",
            Submit = true
        };
        var req3 = new CreatePostRequest
        {
            Title = "Tân binh đăng bài thứ 3 trong ngày",
            Content = "Nội dung chia sẻ bài 3 vượt ngưỡng...",
            Submit = true
        };

        // Act 1: Đăng bài 1 -> 201 Created
        var resp1 = await client.PostAsJsonAsync("/api/v1/posts", req1);
        resp1.StatusCode.Should().Be(HttpStatusCode.Created);

        // Act 2: Đăng bài 2 -> 201 Created
        var resp2 = await client.PostAsJsonAsync("/api/v1/posts", req2);
        resp2.StatusCode.Should().Be(HttpStatusCode.Created);

        // Act 3: Đăng bài 3 -> 429 Too Many Requests (ERR_POST_DAILY_LIMIT_EXCEEDED)
        var resp3 = await client.PostAsJsonAsync("/api/v1/posts", req3);
        resp3.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);

        var error = await resp3.Content.ReadFromJsonAsync<ApiErrorResponse>();
        error.Should().NotBeNull();
        error!.ErrorCode.Should().Be(ErrorCodes.ErrPostDailyLimitExceeded);
        error.Message.Should().Contain("BR-USR-04");
    }

    [Fact]
    public async Task Post_VisibilityFollowers_ShouldReturn400Unsupported()
    {
        var client = _factory.CreateClient();
        var token = GenerateJwtToken(101, UserRoles.Member);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var req = new CreatePostRequest
        {
            Title = "Thử nghiệm chế độ FOLLOWERS",
            Content = "Nội dung bài viết...",
            Visibility = "FOLLOWERS"
        };

        var response = await client.PostAsJsonAsync("/api/v1/posts", req);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var error = await response.Content.ReadFromJsonAsync<ApiErrorResponse>();
        error!.ErrorCode.Should().Be(ErrorCodes.ErrPostVisibilityUnsupported);
    }

    [Fact]
    public async Task Put_AuthorUpdatesPost_ShouldReturn200Ok()
    {
        var client = _factory.CreateClient();
        var token = GenerateJwtToken(101, UserRoles.Member);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var req = new UpdatePostRequest
        {
            Title = "Tiêu đề đã được cập nhật qua API",
            Content = "Nội dung mới đã sửa qua endpoint PUT..."
        };

        var response = await client.PutAsJsonAsync("/api/v1/posts/501", req);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var envelope = await response.Content.ReadFromJsonAsync<ApiResponse<UpdatePostResponseDto>>();
        envelope.Should().NotBeNull();
        envelope!.StatusCode.Should().Be(200);
        envelope.Message.Should().Be("Đã cập nhật.");
        envelope.Data!.PostId.Should().Be(501);
        envelope.Data.ModerationStatus.Should().Be("PENDING_REVIEW");
    }

    [Fact]
    public async Task Put_NonAuthor_ShouldReturn403Forbidden()
    {
        // Member 102 cố tình sửa bài 502 của Member 101
        var client = _factory.CreateClient();
        var token = GenerateJwtToken(102, UserRoles.Member);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var req = new UpdatePostRequest
        {
            Title = "Tiêu đề sửa trộm của người khác"
        };

        var response = await client.PutAsJsonAsync("/api/v1/posts/502", req);
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        var error = await response.Content.ReadFromJsonAsync<ApiErrorResponse>();
        error!.ErrorCode.Should().Be(ErrorCodes.ErrContentNotOwner);
    }

    [Fact]
    public async Task Delete_AuthorDeletesPost_ShouldReturn200Ok()
    {
        var client = _factory.CreateClient();
        var token = GenerateJwtToken(101, UserRoles.Member);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.DeleteAsync("/api/v1/posts/503");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var envelope = await response.Content.ReadFromJsonAsync<ApiResponse<object?>>();
        envelope.Should().NotBeNull();
        envelope!.StatusCode.Should().Be(200);
        envelope.Message.Should().Be("Đã xóa bài viết.");

        // Thử xóa lại -> 404
        var respAgain = await client.DeleteAsync("/api/v1/posts/503");
        respAgain.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
