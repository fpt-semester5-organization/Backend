using System;
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
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Moq;
using VeggieLife.BLL.Common.Constants;
using VeggieLife.BLL.Common.Exceptions;
using VeggieLife.BLL.Common.Responses;
using VeggieLife.BLL.DTOs.Media.Responses;
using VeggieLife.BLL.DTOs.Profile.Requests;
using VeggieLife.BLL.DTOs.Profile.Responses;
using VeggieLife.BLL.Services.Cache;
using VeggieLife.BLL.Services.Media;
using VeggieLife.DAL.Context;
using VeggieLife.DAL.Entities;
using Xunit;

namespace VeggieLife.Tests.Integration.Controllers;

/// <summary>
/// Bộ kiểm thử tích hợp đầu cuối (Integration Tests) kiểm chứng toàn bộ luồng HTTP Pipeline
/// cho 2 chức năng FN-PROF-03 (Cập nhật ảnh đại diện) và FN-PROF-06 (Ghi nhận & lịch sử cân nặng).
/// </summary>
public class UsersAvatarAndWeightHistoryIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly Mock<IMediaService> _mockMediaService = new();
    private readonly Mock<IRedisService> _mockRedisService = new();
    private readonly string _testDbName = "VeggieLifeIntegrationDb_" + Guid.NewGuid();

    private const string JwtSecret = "VeggieLifeSecretKeyForDevelopmentMustBe32CharsLong!";
    private const string JwtIssuer = "VeggieLife.API";
    private const string JwtAudience = "VeggieLife.Client";

    public UsersAvatarAndWeightHistoryIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Development");
            builder.ConfigureServices(services =>
            {
                // Thay thế DbContext bằng InMemory database riêng biệt
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

                // Thay thế IMediaService bằng Mock
                var mediaDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IMediaService));
                if (mediaDescriptor != null)
                {
                    services.Remove(mediaDescriptor);
                }
                services.AddSingleton(_mockMediaService.Object);

                // Thay thế IRedisService bằng Mock
                var redisDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IRedisService));
                if (redisDescriptor != null)
                {
                    services.Remove(redisDescriptor);
                }
                services.AddSingleton(_mockRedisService.Object);
            });
        });

        // Thiết lập seed dữ liệu ban đầu
        SeedDatabase();
    }

    private void SeedDatabase()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // Xóa sạch để đảm bảo tính cô lập
        db.Database.EnsureDeleted();
        db.Database.EnsureCreated();

        // 1. DietarySchool
        db.DietarySchools.Add(new DietarySchool
        {
            DietarySchoolId = 1,
            SchoolCode = "VEGAN",
            SchoolName = "Thuần chay",
            ForbiddenFoodRules = "{}"
        });

        // 2. Member User 101 có HealthProfile
        var memberUser = new User
        {
            UserId = 101,
            Email = "member101@veggielife.vn",
            FullName = "Nguyễn Văn Member",
            Role = UserRoles.Member,
            AccountStatus = "ACTIVE",
            CreatedAt = DateTime.UtcNow
        };
        db.Users.Add(memberUser);

        var memberProfile = new HealthProfile
        {
            UserId = 101,
            DietarySchoolId = 1,
            Gender = "FEMALE",
            DateOfBirth = new DateOnly(1998, 5, 20),
            HeightCm = 160m,
            CurrentWeightKg = 56m,
            TargetWeightKg = 50m,
            BmiValue = 21.9m,
            BmrKcal = 1300m,
            TdeeKcal = 1560m,
            TargetDailyCalories = 1200m,
            HealthGoal = "LOSE_WEIGHT",
            RegionPreference = "SOUTH",
            IsStaleMetrics = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        db.HealthProfiles.Add(memberProfile);

        // 3. Member User 102 chưa có HealthProfile
        var userNoProfile = new User
        {
            UserId = 102,
            Email = "noprofile102@veggielife.vn",
            FullName = "Người Chưa Có Hồ Sơ",
            Role = UserRoles.Member,
            AccountStatus = "ACTIVE",
            CreatedAt = DateTime.UtcNow
        };
        db.Users.Add(userNoProfile);

        db.SaveChanges();
    }

    private static string GenerateJwt(int userId, string role)
    {
        var tokenHandler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes(JwtSecret);
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId.ToString()),
            new(ClaimTypes.Role, role),
            new(ClaimTypes.Email, $"{role.ToLower()}@veggielife.vn"),
            new("sub", userId.ToString()),
            new("role", role)
        };
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddHours(2),
            Issuer = JwtIssuer,
            Audience = JwtAudience,
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };
        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }

    #region FN-PROF-03: Cập nhật ảnh đại diện (POST /api/v1/users/me/avatar)

    [Fact]
    public async Task UploadAvatar_WhenFakeJpg_ShouldReturnBadRequest400_WithErrMediaInvalidFile()
    {
        // Sắp xếp
        var client = _factory.CreateClient();
        var token = GenerateJwt(101, UserRoles.Member);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Giả lập IMediaService ném AppException 400 ERR_MEDIA_INVALID_FILE khi magic bytes sai
        _mockMediaService
            .Setup(m => m.UploadImageAsync(It.IsAny<Stream>(), "fake_avatar.jpg", "veggielife/avatars", It.IsAny<CancellationToken>()))
            .ThrowsAsync(AppException.BadRequest(ErrorCodes.ErrMediaInvalidFile, "Chữ ký tệp (magic bytes) không khớp với định dạng ảnh yêu cầu."));

        using var content = new MultipartFormDataContent();
        var fakeJpgBytes = Encoding.UTF8.GetBytes("THIS IS NOT A REAL JPEG IMAGE CONTENT");
        var byteContent = new ByteArrayContent(fakeJpgBytes);
        byteContent.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");
        content.Add(byteContent, "file", "fake_avatar.jpg");

        // Hành động
        var response = await client.PostAsync("/api/v1/users/me/avatar", content);

        var errorJson = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        errorJson.Should().Contain(ErrorCodes.ErrMediaInvalidFile);
    }

    [Fact]
    public async Task UploadAvatar_WhenFileOver5MB_ShouldReturnPayloadTooLarge413_WithErrMediaFileTooLarge()
    {
        // Sắp xếp
        var client = _factory.CreateClient();
        var token = GenerateJwt(101, UserRoles.Member);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        using var content = new MultipartFormDataContent();
        // Tạo payload 5.5 MB (> 5 MB)
        var largeBytes = new byte[5 * 1024 * 1024 + 512 * 1024];
        var byteContent = new ByteArrayContent(largeBytes);
        byteContent.Headers.ContentType = new MediaTypeHeaderValue("image/png");
        content.Add(byteContent, "file", "too_large.png");

        // Hành động
        var response = await client.PostAsync("/api/v1/users/me/avatar", content);

        // Khẳng định: Controller bắt kích thước > 5MB và trả đúng HTTP 413
        response.StatusCode.Should().Be(HttpStatusCode.RequestEntityTooLarge);
        var errorJson = await response.Content.ReadAsStringAsync();
        errorJson.Should().Contain(ErrorCodes.ErrMediaFileTooLarge);
    }

    [Fact]
    public async Task UploadAvatar_WithoutAuthToken_ShouldReturnUnauthorized401()
    {
        // Sắp xếp
        var client = _factory.CreateClient();

        using var content = new MultipartFormDataContent();
        var dummyBytes = new byte[] { 1, 2, 3 };
        var byteContent = new ByteArrayContent(dummyBytes);
        content.Add(byteContent, "file", "avatar.png");

        // Hành động
        var response = await client.PostAsync("/api/v1/users/me/avatar", content);

        // Khẳng định
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        var errorJson = await response.Content.ReadAsStringAsync();
        errorJson.Should().Contain(ErrorCodes.ErrAuthInvalidToken);
    }

    [Fact]
    public async Task UploadAvatar_AsGuest_ShouldReturnForbidden403()
    {
        // Sắp xếp
        var client = _factory.CreateClient();
        var guestToken = GenerateJwt(999, UserRoles.Guest);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", guestToken);

        using var content = new MultipartFormDataContent();
        var dummyBytes = new byte[] { 1, 2, 3 };
        var byteContent = new ByteArrayContent(dummyBytes);
        content.Add(byteContent, "file", "avatar.png");

        // Hành động
        var response = await client.PostAsync("/api/v1/users/me/avatar", content);

        // Khẳng định
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        var errorJson = await response.Content.ReadAsStringAsync();
        errorJson.Should().Contain(ErrorCodes.ErrGuestPersonalizationRestricted);
    }

    [Fact]
    public async Task UploadAvatar_WithValidImage_ShouldReturnOk200_WithOptimized500x500Url()
    {
        // Sắp xếp
        var client = _factory.CreateClient();
        var token = GenerateJwt(101, UserRoles.Member);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var publicId = "veggielife/avatars/user_101_avatar";
        var resizedUrl = "https://res.cloudinary.com/veggielife/image/upload/c_fill,h_500,w_500/veggielife/avatars/user_101_avatar.webp";

        _mockMediaService
            .Setup(m => m.UploadImageAsync(It.IsAny<Stream>(), "valid_avatar.png", "veggielife/avatars", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MediaUploadResponse
            {
                PublicId = publicId,
                Url = "http://res.cloudinary.com/veggielife/avatars/user_101_avatar.png",
                SecureUrl = "https://res.cloudinary.com/veggielife/avatars/user_101_avatar.png",
                Format = "png",
                Bytes = 2048
            });

        _mockMediaService
            .Setup(m => m.GetImageUrl(publicId, 500, 500, "fill", "webp"))
            .Returns(resizedUrl);

        using var content = new MultipartFormDataContent();
        // PNG magic bytes
        byte[] validPngBytes = { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00, 0x00, 0x00, 0x0D };
        var byteContent = new ByteArrayContent(validPngBytes);
        byteContent.Headers.ContentType = new MediaTypeHeaderValue("image/png");
        content.Add(byteContent, "file", "valid_avatar.png");

        // Hành động
        var response = await client.PostAsync("/api/v1/users/me/avatar", content);

        // Khẳng định
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<UploadCurrentUserAvatarResponse>>();
        body.Should().NotBeNull();
        body!.StatusCode.Should().Be(200);
        body.Message.Should().Be("Cập nhật ảnh đại diện thành công.");
        body.Data!.AvatarUrl.Should().Be(resizedUrl);
    }

    #endregion

    #region FN-PROF-06: Ghi nhận & Lịch sử cân nặng (POST & GET /api/v1/users/me/weight-history)

    [Fact]
    public async Task CreateWeightEntry_WhenValid_ShouldReturnCreated201_AndCalculatedMetrics()
    {
        // Sắp xếp
        var client = _factory.CreateClient();
        var token = GenerateJwt(101, UserRoles.Member);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var request = new CreateCurrentUserWeightEntryRequest
        {
            WeightKg = 54.0m,
            LoggedDate = new DateOnly(2026, 10, 5),
            Note = "Cân buổi sáng"
        };

        // Hành động
        var response = await client.PostAsJsonAsync("/api/v1/users/me/weight-history", request);

        // Khẳng định
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<CreateCurrentUserWeightEntryResponse>>();
        body.Should().NotBeNull();
        body!.StatusCode.Should().Be(201);
        body.Message.Should().Be("Đã ghi cân nặng.");
        body.Data!.WeightKg.Should().Be(54.0m);
        body.Data.Bmi.Should().BeGreaterThan(0);
        body.Data.TargetDailyCalories.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task CreateWeightEntry_WhenWeightOutOfRange_ShouldReturnBadRequest400()
    {
        var client = _factory.CreateClient();
        var token = GenerateJwt(101, UserRoles.Member);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var request = new CreateCurrentUserWeightEntryRequest
        {
            WeightKg = 15.0m // < 20 kg
        };

        var response = await client.PostAsJsonAsync("/api/v1/users/me/weight-history", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var errorJson = await response.Content.ReadAsStringAsync();
        errorJson.Should().Contain(ErrorCodes.ErrValidation);
    }

    [Fact]
    public async Task CreateWeightEntry_WhenDateInFuture_ShouldReturnBadRequest400()
    {
        var client = _factory.CreateClient();
        var token = GenerateJwt(101, UserRoles.Member);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var tomorrow = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1));
        var request = new CreateCurrentUserWeightEntryRequest
        {
            WeightKg = 55.0m,
            LoggedDate = tomorrow
        };

        var response = await client.PostAsJsonAsync("/api/v1/users/me/weight-history", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var errorJson = await response.Content.ReadAsStringAsync();
        errorJson.Should().Contain(ErrorCodes.ErrValidation);
    }

    [Fact]
    public async Task CreateWeightEntry_WhenUserHasNoHealthProfile_ShouldReturnConflict409()
    {
        // User 102 chưa có HealthProfile
        var client = _factory.CreateClient();
        var token = GenerateJwt(102, UserRoles.Member);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var request = new CreateCurrentUserWeightEntryRequest
        {
            WeightKg = 55.0m
        };

        var response = await client.PostAsJsonAsync("/api/v1/users/me/weight-history", request);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var errorJson = await response.Content.ReadAsStringAsync();
        errorJson.Should().Contain(ErrorCodes.ErrProfileNotCompleted);
    }

    [Fact]
    public async Task ListWeightHistory_WhenValid_ShouldReturnPagedOk200()
    {
        var client = _factory.CreateClient();
        var token = GenerateJwt(101, UserRoles.Member);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Thêm 1 bản ghi trước khi đọc
        await client.PostAsJsonAsync("/api/v1/users/me/weight-history", new CreateCurrentUserWeightEntryRequest
        {
            WeightKg = 54.0m,
            LoggedDate = new DateOnly(2026, 10, 5)
        });

        // Hành động
        var response = await client.GetAsync("/api/v1/users/me/weight-history?page=1&size=20");

        // Khẳng định
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<List<WeightHistoryEntryDto>>>();
        body.Should().NotBeNull();
        body!.StatusCode.Should().Be(200);
        body.Message.Should().Be("OK");
        body.Data.Should().NotBeEmpty();
        body.Meta.Should().NotBeNull();
        body.Meta!.Total.Should().BeGreaterThanOrEqualTo(1);
    }

    [Fact]
    public async Task ListWeightHistory_WhenFromGreaterThanTo_ShouldReturnBadRequest400()
    {
        var client = _factory.CreateClient();
        var token = GenerateJwt(101, UserRoles.Member);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.GetAsync("/api/v1/users/me/weight-history?from=2026-10-20&to=2026-10-01");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var errorJson = await response.Content.ReadAsStringAsync();
        errorJson.Should().Contain(ErrorCodes.ErrValidation);
    }

    #endregion
}
