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
}
