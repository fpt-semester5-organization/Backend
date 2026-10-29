using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using VeggieLife.API.Controllers.Admin;
using VeggieLife.BLL.Common.Constants;
using VeggieLife.BLL.Common.Exceptions;
using VeggieLife.BLL.Common.Responses;
using VeggieLife.BLL.DTOs.Admin.Settings;
using VeggieLife.BLL.Services.Settings;
using Xunit;

namespace VeggieLife.Tests.Unit.Controllers;

public class AdminSettingsControllerTests
{
    private readonly Mock<ISystemSettingService> _mockService;
    private readonly AdminSettingsController _controller;
    private const int AdminUserId = 1;

    public AdminSettingsControllerTests()
    {
        _mockService = new Mock<ISystemSettingService>();
        _controller = new AdminSettingsController(_mockService.Object);

        var httpContext = new DefaultHttpContext();
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };
    }

    private void SetUserContext(int userId, string role)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId.ToString()),
            new(ClaimTypes.Role, role)
        };
        _controller.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));
    }

    [Fact]
    public async Task ListSettings_WhenAdmin_ReturnsOkWithList()
    {
        // Arrange
        SetUserContext(AdminUserId, UserRoles.Admin);
        var settings = new List<SystemSettingDto>
        {
            new() { SettingKey = "MEAL_SWAP_LIMIT_DRAFT", SettingValue = "21", Category = "MEAL_PLAN" }
        };
        _mockService.Setup(s => s.GetAllSettingsAsync("MEAL_PLAN", It.IsAny<CancellationToken>()))
            .ReturnsAsync(settings);

        // Act
        var response = await _controller.ListSettings("MEAL_PLAN", CancellationToken.None);

        // Assert
        var okResult = response.Should().BeOfType<OkObjectResult>().Subject;
        var apiResponse = okResult.Value.Should().BeOfType<ApiResponse<IReadOnlyList<SystemSettingDto>>>().Subject;
        apiResponse.Data.Should().HaveCount(1);
        apiResponse.Data[0].SettingKey.Should().Be("MEAL_SWAP_LIMIT_DRAFT");
    }

    [Fact]
    public async Task GetSetting_WhenAdmin_ReturnsOkWithDetail()
    {
        // Arrange
        SetUserContext(AdminUserId, UserRoles.Admin);
        var setting = new SystemSettingDto { SettingKey = "MEAL_SWAP_LIMIT_DRAFT", SettingValue = "21" };
        _mockService.Setup(s => s.GetSettingByKeyAsync("MEAL_SWAP_LIMIT_DRAFT", It.IsAny<CancellationToken>()))
            .ReturnsAsync(setting);

        // Act
        var response = await _controller.GetSetting("MEAL_SWAP_LIMIT_DRAFT", CancellationToken.None);

        // Assert
        var okResult = response.Should().BeOfType<OkObjectResult>().Subject;
        var apiResponse = okResult.Value.Should().BeOfType<ApiResponse<SystemSettingDto>>().Subject;
        apiResponse.Data.SettingKey.Should().Be("MEAL_SWAP_LIMIT_DRAFT");
    }

    [Fact]
    public async Task UpdateSetting_WhenAdmin_ReturnsOkWithUpdatedSetting()
    {
        // Arrange
        SetUserContext(AdminUserId, UserRoles.Admin);
        var request = new UpdateSystemSettingRequest { SettingValue = "25" };
        var updated = new SystemSettingDto { SettingKey = "MEAL_SWAP_LIMIT_DRAFT", SettingValue = "25" };
        _mockService.Setup(s => s.UpdateSettingAsync("MEAL_SWAP_LIMIT_DRAFT", request, AdminUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(updated);

        // Act
        var response = await _controller.UpdateSetting("MEAL_SWAP_LIMIT_DRAFT", request, CancellationToken.None);

        // Assert
        var okResult = response.Should().BeOfType<OkObjectResult>().Subject;
        var apiResponse = okResult.Value.Should().BeOfType<ApiResponse<SystemSettingDto>>().Subject;
        apiResponse.Data.SettingValue.Should().Be("25");
    }

    [Fact]
    public async Task ResetSetting_WhenAdmin_ReturnsOkWithDefaultSetting()
    {
        // Arrange
        SetUserContext(AdminUserId, UserRoles.Admin);
        var reset = new SystemSettingDto { SettingKey = "MEAL_SWAP_LIMIT_DRAFT", SettingValue = "21" };
        _mockService.Setup(s => s.ResetToDefaultAsync("MEAL_SWAP_LIMIT_DRAFT", AdminUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(reset);

        // Act
        var response = await _controller.ResetSetting("MEAL_SWAP_LIMIT_DRAFT", CancellationToken.None);

        // Assert
        var okResult = response.Should().BeOfType<OkObjectResult>().Subject;
        var apiResponse = okResult.Value.Should().BeOfType<ApiResponse<SystemSettingDto>>().Subject;
        apiResponse.Data.SettingValue.Should().Be("21");
    }

    [Fact]
    public async Task EnsureAdmin_WhenNonAdminRole_ThrowsForbiddenException()
    {
        // Arrange
        SetUserContext(10, UserRoles.Member);

        // Act
        var act = async () => await _controller.ListSettings(null, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<AppException>()
            .Where(e => e.ErrorCode == ErrorCodes.ErrAdmForbidden);
    }

    [Fact]
    public async Task EnsureAdmin_WhenUnauthenticated_ThrowsUnauthorizedException()
    {
        // Arrange: Không có claim
        _controller.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity());

        // Act
        var act = async () => await _controller.ListSettings(null, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<AppException>()
            .Where(e => e.ErrorCode == ErrorCodes.ErrAuthInvalidToken);
    }
}
