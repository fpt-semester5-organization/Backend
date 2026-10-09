using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
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
using VeggieLife.BLL.DTOs.Media.Responses;
using VeggieLife.BLL.DTOs.Videos.Responses;
using VeggieLife.BLL.Services.Cache;
using VeggieLife.BLL.Services.Media;
using VeggieLife.DAL.Context;
using VeggieLife.DAL.Entities;
using VeggieLife.DAL.External.Cloudinary;
using Xunit;

namespace VeggieLife.Tests.Integration.Controllers;

/// <summary>
/// Bộ kiểm thử tích hợp đầu cuối (Integration Tests) kiểm chứng toàn bộ HTTP pipeline
/// cho chức năng FN-POST-08 (Tải video/ảnh lên Cloudinary và lưu CSDL với trạng thái PROCESSING).
/// </summary>
public class VideosEndpointIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly Mock<ICloudinaryClient> _mockCloudinaryClient = new();
    private readonly Mock<IRedisService> _mockRedisService = new();
    private readonly string _testDbName = "VeggieLifeVideosIntegrationDb_" + Guid.NewGuid();

    private const string JwtSecret = "YOUR_DEV_JWT_SECRET_KEY_MIN_32_CHARACTERS_LONG";
    private const string JwtIssuer = "VeggieLife";
    private const string JwtAudience = "VeggieLifeUsers";

    public VideosEndpointIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Development");
            builder.ConfigureServices(services =>
            {
                // Thay thế DbContext bằng InMemory database
                var dbContextDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
                if (dbContextDescriptor != null)
                {
                    services.Remove(dbContextDescriptor);
                }

                services.AddDbContext<AppDbContext>(options =>
                {
                    options.UseInMemoryDatabase(_testDbName);
                    options.ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning));
                });

                // Thay thế ICloudinaryClient bằng Mock
                var cloudinaryDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(ICloudinaryClient));
                if (cloudinaryDescriptor != null)
                {
                    services.Remove(cloudinaryDescriptor);
                }
                services.AddSingleton(_mockCloudinaryClient.Object);

                // Thay thế IRedisService bằng Mock
                var redisDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IRedisService));
                if (redisDescriptor != null)
                {
                    services.Remove(redisDescriptor);
                }
                services.AddSingleton(_mockRedisService.Object);
            });
        });

        SeedTestData();
    }

    private void SeedTestData()
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        if (!context.Users.Any(u => u.UserId == 1))
        {
            var user1 = new User
            {
                UserId = 1,
                Email = "author@veggielife.vn",
                PasswordHash = "hash1",
                FullName = "Tác Giả Một",
                Role = UserRoles.Member,
                AccountStatus = "ACTIVE",
                CreatedAt = DateTime.UtcNow
            };

            var user2 = new User
            {
                UserId = 2,
                Email = "other@veggielife.vn",
                PasswordHash = "hash2",
                FullName = "Người Khác",
                Role = UserRoles.Member,
                AccountStatus = "ACTIVE",
                CreatedAt = DateTime.UtcNow
            };

            context.Users.AddRange(user1, user2);

            var postUser1 = new Post
            {
                PostId = 101,
                UserId = 1,
                Title = "Bài viết thuần chay của User 1",
                Content = "Chia sẻ bí quyết",
                Visibility = "PUBLIC",
                ModerationStatus = "APPROVED_ACTIVE",
                CreatedAt = DateTime.UtcNow
            };

            var postUser2 = new Post
            {
                PostId = 102,
                UserId = 2,
                Title = "Bài viết của User 2",
                Content = "Nội dung khác",
                Visibility = "PUBLIC",
                ModerationStatus = "APPROVED_ACTIVE",
                CreatedAt = DateTime.UtcNow
            };

            context.Posts.AddRange(postUser1, postUser2);

            var category = new FoodCategory
            {
                CategoryId = 1,
                CategoryName = "Món kho",
                Description = "Món kho đậm đà",
                IsCore = true
            };
            context.FoodCategories.Add(category);

            var dietarySchool = new DietarySchool
            {
                DietarySchoolId = 1,
                SchoolName = "Thuần chay (Vegan)",
                SchoolCode = "VEGAN",
                ForbiddenFoodRules = "[]",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            context.DietarySchools.Add(dietarySchool);

            var recipeUser1 = new Recipe
            {
                RecipeId = 201,
                UserId = 1,
                CategoryId = 1,
                DietarySchoolId = 1,
                Title = "Nấm đùi gà kho tiêu",
                Instructions = "Bước 1 kho nấm...",
                CaloriesKcal = 220,
                DifficultyLevel = "MEDIUM",
                Source = "USER",
                Status = "APPROVED_ACTIVE",
                CreatedAt = DateTime.UtcNow
            };

            context.Recipes.Add(recipeUser1);
            context.SaveChanges();
        }
    }

    private string GenerateJwtToken(int userId, string role)
    {
        var tokenHandler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes(JwtSecret);

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                new Claim(ClaimTypes.Role, role),
                new Claim(ClaimTypes.Email, $"user{userId}@veggielife.vn")
            }),
            Expires = DateTime.UtcNow.AddHours(2),
            Issuer = JwtIssuer,
            Audience = JwtAudience,
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }

    private static byte[] CreateValidMp4Bytes(double? durationSeconds = null)
    {
        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);

        // ftyp header
        Span<byte> ftyp = stackalloc byte[16];
        BinaryPrimitives.WriteUInt32BigEndian(ftyp[..4], 16);
        Encoding.ASCII.GetBytes("ftyp").CopyTo(ftyp.Slice(4, 4));
        Encoding.ASCII.GetBytes("isom").CopyTo(ftyp.Slice(8, 4));
        BinaryPrimitives.WriteUInt32BigEndian(ftyp.Slice(12, 4), 512);
        writer.Write(ftyp);

        if (durationSeconds.HasValue)
        {
            uint timescale = 1000;
            uint duration = (uint)(durationSeconds.Value * timescale);

            var mvhdPayload = new byte[20];
            mvhdPayload[0] = 0;
            BinaryPrimitives.WriteUInt32BigEndian(mvhdPayload.AsSpan(12, 4), timescale);
            BinaryPrimitives.WriteUInt32BigEndian(mvhdPayload.AsSpan(16, 4), duration);

            var mvhdBox = new byte[28];
            BinaryPrimitives.WriteUInt32BigEndian(mvhdBox.AsSpan(0, 4), 28);
            Encoding.ASCII.GetBytes("mvhd").CopyTo(mvhdBox.AsSpan(4, 4));
            mvhdPayload.CopyTo(mvhdBox, 8);

            var moovBox = new byte[36];
            BinaryPrimitives.WriteUInt32BigEndian(moovBox.AsSpan(0, 4), 36);
            Encoding.ASCII.GetBytes("moov").CopyTo(moovBox.AsSpan(4, 4));
            mvhdBox.CopyTo(moovBox, 8);

            writer.Write(moovBox);
        }

        return ms.ToArray();
    }

    private static byte[] CreateValidJpegBytes()
    {
        // JPEG magic bytes: FF D8 FF E0 + padding
        var bytes = new byte[64];
        bytes[0] = 0xFF;
        bytes[1] = 0xD8;
        bytes[2] = 0xFF;
        bytes[3] = 0xE0;
        return bytes;
    }

    [Fact]
    public async Task PostVideos_WithoutToken_Returns401Unauthorized()
    {
        // Arrange
        var client = _factory.CreateClient();
        using var content = new MultipartFormDataContent();
        content.Add(new ByteArrayContent(CreateValidMp4Bytes()), "file", "test.mp4");
        content.Add(new StringContent("101"), "postId");

        // Act
        var response = await client.PostAsync("/api/v1/videos", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        var err = await response.Content.ReadFromJsonAsync<ApiErrorResponse>();
        err!.ErrorCode.Should().Be(ErrorCodes.ErrAuthInvalidToken);
    }

    [Fact]
    public async Task PostVideos_WithGuestRole_Returns403CommunityActionDenied()
    {
        // Arrange
        var client = _factory.CreateClient();
        var guestToken = GenerateJwtToken(999, UserRoles.Guest);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", guestToken);

        using var content = new MultipartFormDataContent();
        content.Add(new ByteArrayContent(CreateValidMp4Bytes()), "file", "test.mp4");
        content.Add(new StringContent("101"), "postId");

        // Act
        var response = await client.PostAsync("/api/v1/videos", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        var err = await response.Content.ReadFromJsonAsync<ApiErrorResponse>();
        err!.ErrorCode.Should().Be(ErrorCodes.ErrGuestCommunityActionDenied);
    }

    [Fact]
    public async Task PostVideos_WhenMissingBothPostIdAndRecipeId_Returns400BadRequest()
    {
        // Arrange
        var client = _factory.CreateClient();
        var token = GenerateJwtToken(1, UserRoles.Member);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        using var content = new MultipartFormDataContent();
        content.Add(new ByteArrayContent(CreateValidMp4Bytes()), "file", "test.mp4");

        // Act
        var response = await client.PostAsync("/api/v1/videos", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var err = await response.Content.ReadFromJsonAsync<ApiErrorResponse>();
        err!.ErrorCode.Should().Be(ErrorCodes.ErrValidation);
    }

    [Fact]
    public async Task PostVideos_WhenNotOwnerOfPost_Returns403ContentNotOwner()
    {
        // Arrange: User 1 gọi upload cho Post 102 của User 2
        var client = _factory.CreateClient();
        var token = GenerateJwtToken(1, UserRoles.Member);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        using var content = new MultipartFormDataContent();
        content.Add(new ByteArrayContent(CreateValidMp4Bytes()), "file", "test.mp4");
        content.Add(new StringContent("102"), "postId");

        // Act
        var response = await client.PostAsync("/api/v1/videos", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        var err = await response.Content.ReadFromJsonAsync<ApiErrorResponse>();
        err!.ErrorCode.Should().Be(ErrorCodes.ErrContentNotOwner);
    }

    [Fact]
    public async Task PostVideos_WhenFileIsInvalidFakeSignature_Returns400InvalidFile()
    {
        // Arrange: Tệp giả mạo đuôi .mp4 với byte rác
        var client = _factory.CreateClient();
        var token = GenerateJwtToken(1, UserRoles.Member);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var fakeBytes = new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x0A, 0x0B, 0x0C };
        using var content = new MultipartFormDataContent();
        content.Add(new ByteArrayContent(fakeBytes), "file", "fake_video.mp4");
        content.Add(new StringContent("101"), "postId");

        // Act
        var response = await client.PostAsync("/api/v1/videos", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var err = await response.Content.ReadFromJsonAsync<ApiErrorResponse>();
        err!.ErrorCode.Should().Be(ErrorCodes.ErrMediaInvalidFile);
    }

    [Fact]
    public async Task PostVideos_WhenVideoIs6Minutes_RejectsWith422SttDurationInvalid()
    {
        // Arrange: Video dài 6 phút (360 giây > 300 giây tối đa)
        var client = _factory.CreateClient();
        var token = GenerateJwtToken(1, UserRoles.Member);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Video nhúng thời lượng 360 giây
        var mp4Bytes = CreateValidMp4Bytes(durationSeconds: 360.0);

        using var content = new MultipartFormDataContent();
        content.Add(new ByteArrayContent(mp4Bytes), "file", "video_6min.mp4");
        content.Add(new StringContent("101"), "postId");
        content.Add(new StringContent("Video nấu ăn 6 phút"), "title");

        // Act
        var response = await client.PostAsync("/api/v1/videos", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        var err = await response.Content.ReadFromJsonAsync<ApiErrorResponse>();
        err!.ErrorCode.Should().Be(ErrorCodes.ErrSttDurationInvalid);
        err.Message.Should().Contain("10 giây đến tối đa 5 phút");
    }

    [Fact]
    public async Task PostVideos_WhenVideoLessThan10Seconds_RejectsWith422SttDurationInvalid()
    {
        // Arrange: Video ngắn 5 giây (< 10 giây)
        var client = _factory.CreateClient();
        var token = GenerateJwtToken(1, UserRoles.Member);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var mp4Bytes = CreateValidMp4Bytes(durationSeconds: 5.0);

        using var content = new MultipartFormDataContent();
        content.Add(new ByteArrayContent(mp4Bytes), "file", "video_5sec.mp4");
        content.Add(new StringContent("101"), "postId");

        // Act
        var response = await client.PostAsync("/api/v1/videos", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        var err = await response.Content.ReadFromJsonAsync<ApiErrorResponse>();
        err!.ErrorCode.Should().Be(ErrorCodes.ErrSttDurationInvalid);
    }

    [Fact]
    public async Task PostVideos_WhenCloudinaryFails_Returns502BadGateway()
    {
        // Arrange: Video bình thường nhưng Cloudinary báo lỗi kết nối
        var client = _factory.CreateClient();
        var token = GenerateJwtToken(1, UserRoles.Member);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var mp4Bytes = CreateValidMp4Bytes(); // không set mvhd duration

        _mockCloudinaryClient.Setup(c => c.UploadVideoAsync(
            It.IsAny<Stream>(),
            "cloud_err.mp4",
            "veggielife/videos",
            It.IsAny<CancellationToken>()))
            .ThrowsAsync(new CloudinaryException("Cloudinary service unavailable"));

        using var content = new MultipartFormDataContent();
        content.Add(new ByteArrayContent(mp4Bytes), "file", "cloud_err.mp4");
        content.Add(new StringContent("101"), "postId");

        // Act
        var response = await client.PostAsync("/api/v1/videos", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadGateway);
        var err = await response.Content.ReadFromJsonAsync<ApiErrorResponse>();
        err!.ErrorCode.Should().Be(ErrorCodes.ErrExtCloudinaryFailed);
    }

    [Fact]
    public async Task PostVideos_WhenValidVideoUploaded_Returns202AcceptedAndSavesProcessingRecordInDb()
    {
        // Arrange: Video hợp lệ (thời lượng 90 giây)
        var client = _factory.CreateClient();
        var token = GenerateJwtToken(1, UserRoles.Member);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var mp4Bytes = CreateValidMp4Bytes();

        _mockCloudinaryClient.Setup(c => c.UploadVideoAsync(
            It.IsAny<Stream>(),
            "delicious_tofu.mp4",
            "veggielife/videos",
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CloudinaryMediaResult
            {
                PublicId = "demo_tofu_vid",
                Url = "http://res.cloudinary.com/veggie/video/upload/demo_tofu_vid.mp4",
                SecureUrl = "https://res.cloudinary.com/veggie/video/upload/demo_tofu_vid.mp4",
                Duration = 90.0,
                ResourceType = "video"
            });

        _mockCloudinaryClient.Setup(c => c.GetImageUrl("demo_tofu_vid", null, null, "fill", "jpg"))
            .Returns("https://res.cloudinary.com/veggie/video/upload/demo_tofu_vid.jpg");

        using var content = new MultipartFormDataContent();
        content.Add(new ByteArrayContent(mp4Bytes), "file", "delicious_tofu.mp4");
        content.Add(new StringContent("101"), "postId");
        content.Add(new StringContent("Cách làm đậu phụ kho nấm"), "title");

        // Act
        var response = await client.PostAsync("/api/v1/videos", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Accepted);
        var envelope = await response.Content.ReadFromJsonAsync<ApiResponse<CreateVideoResponseData>>();
        envelope.Should().NotBeNull();
        envelope!.StatusCode.Should().Be(202);
        envelope.Message.Should().Be("Đã nhận video, đang xử lý.");
        envelope.Data!.VideoId.Should().BePositive();
        envelope.Data.Status.Should().Be("PROCESSING");

        // Kiểm tra cơ sở dữ liệu đã lưu Video với Status = PROCESSING
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var savedVideo = await context.Videos.FirstOrDefaultAsync(v => v.VideoId == envelope.Data.VideoId);
        savedVideo.Should().NotBeNull();
        savedVideo!.Status.Should().Be("PROCESSING");
        savedVideo.DurationSeconds.Should().Be(90);
        savedVideo.PostId.Should().Be(101);
        savedVideo.Title.Should().Be("Cách làm đậu phụ kho nấm");
        savedVideo.VideoUrl.Should().Be("https://res.cloudinary.com/veggie/video/upload/demo_tofu_vid.mp4");
    }

    [Fact]
    public async Task PostVideos_WhenValidImageUploaded_Returns202AcceptedAndUpdatesPostImageUrl()
    {
        // Arrange: Ảnh JPEG hợp lệ <= 5 MB
        var client = _factory.CreateClient();
        var token = GenerateJwtToken(1, UserRoles.Member);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var jpegBytes = CreateValidJpegBytes();

        _mockCloudinaryClient.Setup(c => c.UploadImageAsync(
            It.IsAny<Stream>(),
            "cover.jpg",
            "veggielife/images",
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CloudinaryMediaResult
            {
                PublicId = "demo_cover_img",
                Url = "http://res.cloudinary.com/veggie/image/upload/demo_cover_img.jpg",
                SecureUrl = "https://res.cloudinary.com/veggie/image/upload/demo_cover_img.jpg",
                ResourceType = "image"
            });

        using var content = new MultipartFormDataContent();
        content.Add(new ByteArrayContent(jpegBytes), "file", "cover.jpg");
        content.Add(new StringContent("101"), "postId");

        // Act
        var response = await client.PostAsync("/api/v1/videos", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Accepted);
        var envelope = await response.Content.ReadFromJsonAsync<ApiResponse<CreateVideoResponseData>>();
        envelope.Should().NotBeNull();
        envelope!.StatusCode.Should().Be(202);
        envelope.Message.Should().Be("Đã tải ảnh lên thành công.");
        envelope.Data!.Status.Should().Be("READY");
        envelope.Data.ImageUrl.Should().Be("https://res.cloudinary.com/veggie/image/upload/demo_cover_img.jpg");

        // Kiểm tra post đã được cập nhật Image_Url
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var updatedPost = await context.Posts.FindAsync(101);
        updatedPost!.ImageUrl.Should().Be("https://res.cloudinary.com/veggie/image/upload/demo_cover_img.jpg");
    }

    [Fact]
    public async Task SwaggerEndpoint_GeneratesJsonWithoutExceptions()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/swagger/v1/swagger.json");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await response.Content.ReadAsStringAsync();
        json.Should().Contain("/api/v1/videos");
    }
}
