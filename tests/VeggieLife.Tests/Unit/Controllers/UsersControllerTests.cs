using System.Collections.Generic;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using VeggieLife.API.Controllers;
using VeggieLife.BLL.Common.Constants;
using VeggieLife.BLL.Common.Exceptions;
using VeggieLife.BLL.Common.Responses;
using VeggieLife.BLL.DTOs.Profile.Requests;
using VeggieLife.BLL.DTOs.Profile.Responses;
using VeggieLife.BLL.Services.Profile;
using Xunit;

namespace VeggieLife.Tests.Unit.Controllers;

/// <summary>
/// Unit Tests kiểm thử các hành vi của UsersController (FN-PROF-01, 02, 04, 05).
/// </summary>
public class UsersControllerTests
{
    private readonly Mock<IProfileService> _mockService;
    private readonly UsersController _controller;

    public UsersControllerTests()
    {
        _mockService = new Mock<IProfileService>();
        _controller = new UsersController(_mockService.Object);

        SetupUserClaims(userId: 101, role: UserRoles.Member);
    }

    private void SetupUserClaims(int userId, string role)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId.ToString()),
            new(ClaimTypes.Role, role)
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = principal }
        };
    }

    [Fact]
    public async Task GetCurrentUser_WhenMember_ShouldReturnOkEnvelope()
    {
        var expectedResponse = new CurrentUserProfileResponse
        {
            UserId = 101,
            FullName = "Nguyễn Văn Member",
            Role = UserRoles.Member
        };

        _mockService.Setup(s => s.GetCurrentUserAsync(101, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        var actionResult = await _controller.GetCurrentUser(CancellationToken.None);

        var okResult = actionResult as OkObjectResult;
        okResult.Should().NotBeNull();
        okResult!.StatusCode.Should().Be(StatusCodes.Status200OK);

        var envelope = okResult.Value as ApiResponse<CurrentUserProfileResponse>;
        envelope.Should().NotBeNull();
        envelope!.StatusCode.Should().Be(200);
        envelope.Message.Should().Be("Lấy hồ sơ thành công.");
        envelope.Data!.UserId.Should().Be(101);
    }

    [Fact]
    public async Task GetCurrentUser_WhenGuest_ShouldThrowForbidden()
    {
        SetupUserClaims(userId: 999, role: UserRoles.Guest);

        var act = async () => await _controller.GetCurrentUser(CancellationToken.None);

        var ex = await act.Should().ThrowAsync<AppException>();
        ex.Which.StatusCode.Should().Be(403);
        ex.Which.ErrorCode.Should().Be(ErrorCodes.ErrGuestPersonalizationRestricted);
    }

    [Fact]
    public async Task UpdateCurrentUserHealthProfile_WhenValid_ShouldReturnOkEnvelope()
    {
        var request = new UpdateCurrentUserHealthProfileRequest
        {
            HeightCm = 170m,
            WeightKg = 65m,
            DietarySchoolId = 1,
            HealthGoal = "MAINTAIN"
        };

        var expectedDto = new HealthProfileDto
        {
            HeightCm = 170m,
            CurrentWeightKg = 65m,
            Bmi = 22.5m
        };

        _mockService.Setup(s => s.UpdateHealthProfileAsync(101, request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedDto);

        var actionResult = await _controller.UpdateCurrentUserHealthProfile(request, CancellationToken.None);

        var okResult = actionResult as OkObjectResult;
        okResult.Should().NotBeNull();
        var envelope = okResult!.Value as ApiResponse<HealthProfileDto>;
        envelope.Should().NotBeNull();
        envelope!.StatusCode.Should().Be(200);
        envelope.Message.Should().Be("Cập nhật hồ sơ sức khỏe thành công.");
        envelope.Data!.Bmi.Should().Be(22.5m);
    }

    [Fact]
    public async Task ReplaceCurrentUserAllergies_WhenValid_ShouldReturnOkEnvelope()
    {
        var request = new ReplaceCurrentUserAllergiesRequest
        {
            Allergens = new List<UserAllergyItemRequest> { new() { AllergenId = 1 } }
        };

        var expectedResponse = new ReplaceCurrentUserAllergiesResponse
        {
            Allergens = new List<AllergenBriefDto> { new() { AllergenId = 1, Name = "Đậu nành" } }
        };

        _mockService.Setup(s => s.ReplaceAllergiesAsync(101, request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        var actionResult = await _controller.ReplaceCurrentUserAllergies(request, CancellationToken.None);

        var okResult = actionResult as OkObjectResult;
        okResult.Should().NotBeNull();
        var envelope = okResult!.Value as ApiResponse<ReplaceCurrentUserAllergiesResponse>;
        envelope.Should().NotBeNull();
        envelope!.StatusCode.Should().Be(200);
        envelope.Message.Should().Be("Cập nhật dị ứng thành công.");
    }

    [Fact]
    public async Task GetCurrentUserHealthProfile_WhenCalled_ShouldReturnHealthProfileEnvelope()
    {
        var userProfile = new CurrentUserProfileResponse
        {
            UserId = 101,
            Health = new HealthProfileDto
            {
                HeightCm = 170m,
                CurrentWeightKg = 65m,
                Bmi = 22.5m
            }
        };

        _mockService.Setup(s => s.GetCurrentUserAsync(101, It.IsAny<CancellationToken>()))
            .ReturnsAsync(userProfile);

        var actionResult = await _controller.GetCurrentUserHealthProfile(CancellationToken.None);

        var okResult = actionResult as OkObjectResult;
        okResult.Should().NotBeNull();
        var envelope = okResult!.Value as ApiResponse<HealthProfileDto>;
        envelope.Should().NotBeNull();
        envelope!.StatusCode.Should().Be(200);
        envelope.Message.Should().Be("Lấy hồ sơ sức khỏe thành công.");
        envelope.Data!.Bmi.Should().Be(22.5m);
    }

    [Fact]
    public async Task GetCurrentUserAllergies_WhenCalled_ShouldReturnAllergiesEnvelope()
    {
        var userProfile = new CurrentUserProfileResponse
        {
            UserId = 101,
            Allergens = new List<AllergenBriefDto>
            {
                new() { AllergenId = 1, Name = "Đậu nành" }
            }
        };

        _mockService.Setup(s => s.GetCurrentUserAsync(101, It.IsAny<CancellationToken>()))
            .ReturnsAsync(userProfile);

        var actionResult = await _controller.GetCurrentUserAllergies(CancellationToken.None);

        var okResult = actionResult as OkObjectResult;
        okResult.Should().NotBeNull();
        var envelope = okResult!.Value as ApiResponse<ReplaceCurrentUserAllergiesResponse>;
        envelope.Should().NotBeNull();
        envelope!.StatusCode.Should().Be(200);
        envelope.Message.Should().Be("Lấy danh sách dị ứng thành công.");
        envelope.Data!.Allergens.Should().HaveCount(1);
        envelope.Data.Allergens[0].Name.Should().Be("Đậu nành");
    }

    #region FN-PROF-03: Upload Avatar Tests

    [Fact]
    public async Task UploadCurrentUserAvatar_WhenValidFile_ShouldReturnOkEnvelope()
    {
        var content = new byte[] { 1, 2, 3, 4 };
        var fileName = "avatar.png";
        var stream = new System.IO.MemoryStream(content);
        var mockFile = new Mock<IFormFile>();
        mockFile.Setup(f => f.OpenReadStream()).Returns(stream);
        mockFile.Setup(f => f.FileName).Returns(fileName);
        mockFile.Setup(f => f.Length).Returns(content.Length);

        var expectedResponse = new UploadCurrentUserAvatarResponse
        {
            AvatarUrl = "https://res.cloudinary.com/veggielife/avatars/user101_500x500.webp"
        };

        _mockService.Setup(s => s.UploadAvatarAsync(101, It.IsAny<System.IO.Stream>(), fileName, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        var actionResult = await _controller.UploadCurrentUserAvatar(mockFile.Object, CancellationToken.None);

        var okResult = actionResult as OkObjectResult;
        okResult.Should().NotBeNull();
        var envelope = okResult!.Value as ApiResponse<UploadCurrentUserAvatarResponse>;
        envelope.Should().NotBeNull();
        envelope!.StatusCode.Should().Be(200);
        envelope.Message.Should().Be("Cập nhật ảnh đại diện thành công.");
        envelope.Data!.AvatarUrl.Should().Be(expectedResponse.AvatarUrl);
    }

    [Fact]
    public async Task UploadCurrentUserAvatar_WhenFileNull_ShouldThrowBadRequest()
    {
        var act = async () => await _controller.UploadCurrentUserAvatar(null, CancellationToken.None);

        var ex = await act.Should().ThrowAsync<AppException>();
        ex.Which.StatusCode.Should().Be(400);
        ex.Which.ErrorCode.Should().Be(ErrorCodes.ErrMediaInvalidFile);
    }

    [Fact]
    public async Task UploadCurrentUserAvatar_WhenFileOver5MB_ShouldThrowPayloadTooLarge()
    {
        var mockFile = new Mock<IFormFile>();
        mockFile.Setup(f => f.Length).Returns(6 * 1024 * 1024); // 6 MB

        var act = async () => await _controller.UploadCurrentUserAvatar(mockFile.Object, CancellationToken.None);

        var ex = await act.Should().ThrowAsync<AppException>();
        ex.Which.StatusCode.Should().Be(413);
        ex.Which.ErrorCode.Should().Be(ErrorCodes.ErrMediaFileTooLarge);
    }

    [Fact]
    public async Task UploadCurrentUserAvatar_WhenGuest_ShouldThrowForbidden()
    {
        SetupUserClaims(userId: 101, role: UserRoles.Guest);
        var mockFile = new Mock<IFormFile>();
        mockFile.Setup(f => f.Length).Returns(1024);

        var act = async () => await _controller.UploadCurrentUserAvatar(mockFile.Object, CancellationToken.None);

        var ex = await act.Should().ThrowAsync<AppException>();
        ex.Which.StatusCode.Should().Be(403);
        ex.Which.ErrorCode.Should().Be(ErrorCodes.ErrGuestPersonalizationRestricted);
    }

    #endregion

    #region FN-PROF-06: Weight History Tests

    [Fact]
    public async Task CreateCurrentUserWeightEntry_WhenValid_ShouldReturnCreatedEnvelope()
    {
        var request = new CreateCurrentUserWeightEntryRequest
        {
            WeightKg = 54.0m,
            LoggedDate = new System.DateOnly(2026, 10, 5),
            Note = "Ghi mốc"
        };

        var expectedResponse = new CreateCurrentUserWeightEntryResponse
        {
            LogId = 88,
            WeightKg = 54.0m,
            Bmi = 20.6m,
            TargetDailyCalories = 1200.0m
        };

        _mockService.Setup(s => s.CreateWeightEntryAsync(101, request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        var actionResult = await _controller.CreateCurrentUserWeightEntry(request, CancellationToken.None);

        var objectResult = actionResult as ObjectResult;
        objectResult.Should().NotBeNull();
        objectResult!.StatusCode.Should().Be(201);
        var envelope = objectResult.Value as ApiResponse<CreateCurrentUserWeightEntryResponse>;
        envelope.Should().NotBeNull();
        envelope!.StatusCode.Should().Be(201);
        envelope.Message.Should().Be("Đã ghi cân nặng.");
        envelope.Data!.LogId.Should().Be(88);
        envelope.Data.WeightKg.Should().Be(54.0m);
    }

    [Fact]
    public async Task ListCurrentUserWeightHistory_WhenValid_ShouldReturnPagedEnvelope()
    {
        var items = new List<WeightHistoryEntryDto>
        {
            new() { LogId = 88, WeightKg = 54.0m, LoggedDate = new System.DateOnly(2026, 10, 5) }
        };
        var meta = new PagedMeta(1, 20, 1);

        _mockService.Setup(s => s.ListWeightHistoryAsync(101, null, null, 1, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync((items, meta));

        var actionResult = await _controller.ListCurrentUserWeightHistory(null, null, 1, 20, CancellationToken.None);

        var okResult = actionResult as OkObjectResult;
        okResult.Should().NotBeNull();
        var envelope = okResult!.Value as ApiResponse<IReadOnlyList<WeightHistoryEntryDto>>;
        envelope.Should().NotBeNull();
        envelope!.StatusCode.Should().Be(200);
        envelope.Message.Should().Be("OK");
        envelope.Data.Should().HaveCount(1);
        envelope.Meta.Should().NotBeNull();
        envelope.Meta!.Total.Should().Be(1);
    }

    #endregion
}
