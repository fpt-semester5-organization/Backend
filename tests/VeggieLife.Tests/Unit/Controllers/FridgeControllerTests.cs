using System.Collections.Generic;
using System.IO;
using System.Security.Claims;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using VeggieLife.API.Controllers;
using VeggieLife.API.DTOs.Fridge;
using VeggieLife.BLL.Common.Constants;
using VeggieLife.BLL.Common.Exceptions;
using VeggieLife.BLL.Common.Responses;
using VeggieLife.BLL.DTOs.Fridge.Requests;
using VeggieLife.BLL.DTOs.Fridge.Responses;
using VeggieLife.BLL.Services.Fridge;
using Xunit;

namespace VeggieLife.Tests.Unit.Controllers;

/// <summary>
/// Bộ kiểm thử đơn vị cho FridgeController (FN-VIS-01, FN-VIS-02).
/// Kiểm thử cấu trúc envelope response chuẩn, mã HTTP và phân quyền tác nhân (Khách, Thành viên).
/// </summary>
public class FridgeControllerTests
{
    private readonly Mock<IFridgeService> _mockService;
    private readonly FridgeController _controller;

    public FridgeControllerTests()
    {
        _mockService = new Mock<IFridgeService>();
        _controller = new FridgeController(_mockService.Object);

        var httpContext = new DefaultHttpContext();
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };
    }

    [Fact]
    public async Task CreateFridgeDetection_MemberAuthenticated_Returns200Ok()
    {
        // Arrange
        var userId = 25;
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId.ToString()),
            new(ClaimTypes.Role, UserRoles.Member)
        };
        _controller.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));

        var fileMock = new Mock<IFormFile>();
        var content = "dummy-image-data";
        var ms = new MemoryStream(Encoding.UTF8.GetBytes(content));
        fileMock.Setup(f => f.OpenReadStream()).Returns(ms);
        fileMock.Setup(f => f.FileName).Returns("fridge.jpg");
        fileMock.Setup(f => f.Length).Returns(ms.Length);
        fileMock.Setup(f => f.ContentType).Returns("image/jpeg");

        var form = new CreateFridgeDetectionForm { Image = fileMock.Object };

        var serviceResponse = new CreateFridgeDetectionResponseData
        {
            Saved = true,
            Detected = new List<DetectedFridgeIngredientDto>
            {
                new() { InventoryId = 55, IngredientId = 12, Name = "Nấm rơm", FreshnessStatus = "FRESH" }
            },
            Quota = new FridgeDetectionQuotaDto { Used = 1, Limit = 10, Remaining = 9 }
        };

        _mockService.Setup(s => s.DetectIngredientsAsync(
                It.IsAny<FridgeDetectionInputDto>(),
                userId,
                UserRoles.Member,
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(serviceResponse);

        // Act
        var result = await _controller.CreateFridgeDetection(form, CancellationToken.None);

        // Assert
        var okResult = result as OkObjectResult;
        okResult.Should().NotBeNull();
        okResult!.StatusCode.Should().Be(StatusCodes.Status200OK);

        var response = okResult.Value as ApiResponse<CreateFridgeDetectionResponseData>;
        response.Should().NotBeNull();
        response!.StatusCode.Should().Be(StatusCodes.Status200OK);
        response.Message.Should().Be("Đã nhận diện nguyên liệu.");
        response.Data!.Saved.Should().BeTrue();
        response.Data.Detected.Should().HaveCount(1);
        response.Data.Detected[0].Name.Should().Be("Nấm rơm");
    }

    [Fact]
    public async Task CreateFridgeDetection_GuestAuthenticated_Returns200Ok()
    {
        // Arrange
        var guestUuid = "guest-1234-uuid";
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, guestUuid),
            new("Guest_UUID", guestUuid),
            new(ClaimTypes.Role, UserRoles.Guest)
        };
        _controller.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));

        var fileMock = new Mock<IFormFile>();
        var ms = new MemoryStream(Encoding.UTF8.GetBytes("guest-image"));
        fileMock.Setup(f => f.OpenReadStream()).Returns(ms);
        fileMock.Setup(f => f.FileName).Returns("fridge.png");
        fileMock.Setup(f => f.Length).Returns(ms.Length);
        fileMock.Setup(f => f.ContentType).Returns("image/png");

        var form = new CreateFridgeDetectionForm { Image = fileMock.Object };

        var serviceResponse = new CreateFridgeDetectionResponseData
        {
            Saved = false,
            Detected = new List<DetectedFridgeIngredientDto>
            {
                new() { InventoryId = null, IngredientId = 12, Name = "Nấm rơm", FreshnessStatus = "FRESH" }
            },
            Quota = new FridgeDetectionQuotaDto { Used = 1, Limit = 3, Remaining = 2 }
        };

        _mockService.Setup(s => s.DetectIngredientsAsync(
                It.IsAny<FridgeDetectionInputDto>(),
                null,
                UserRoles.Guest,
                guestUuid,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(serviceResponse);

        // Act
        var result = await _controller.CreateFridgeDetection(form, CancellationToken.None);

        // Assert
        var okResult = result as OkObjectResult;
        okResult.Should().NotBeNull();
        okResult!.StatusCode.Should().Be(StatusCodes.Status200OK);

        var response = okResult.Value as ApiResponse<CreateFridgeDetectionResponseData>;
        response.Should().NotBeNull();
        response!.Data!.Saved.Should().BeFalse();
        response.Data.Quota!.Limit.Should().Be(3);
    }

    [Fact]
    public async Task CreateFridgeDetection_Unauthenticated_Throws401Unauthorized()
    {
        // Arrange
        _controller.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity()); // Not authenticated
        var form = new CreateFridgeDetectionForm();

        // Act & Assert
        var act = async () => await _controller.CreateFridgeDetection(form, CancellationToken.None);
        var ex = await act.Should().ThrowAsync<AppException>();
        ex.Which.StatusCode.Should().Be(StatusCodes.Status401Unauthorized);
        ex.Which.ErrorCode.Should().Be(ErrorCodes.ErrAuthInvalidToken);
    }

    [Fact]
    public async Task CreateFridgeDetection_MissingImageFile_Throws400BadRequest()
    {
        // Arrange
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, "10"),
            new(ClaimTypes.Role, UserRoles.Member)
        };
        _controller.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));
        var form = new CreateFridgeDetectionForm { Image = null! };

        // Act & Assert
        var act = async () => await _controller.CreateFridgeDetection(form, CancellationToken.None);
        var ex = await act.Should().ThrowAsync<AppException>();
        ex.Which.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
        ex.Which.ErrorCode.Should().Be(ErrorCodes.ErrValidation);
    }

    [Fact]
    public async Task ListFridgeItems_MemberAuthenticated_Returns200Ok()
    {
        // Arrange
        var userId = 10;
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId.ToString()),
            new(ClaimTypes.Role, UserRoles.Member)
        };
        _controller.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));

        var items = new List<FridgeItemDto>
        {
            new() { InventoryId = 55, IngredientId = 12, Name = "Nấm rơm", FreshnessStatus = "FRESH" }
        };

        _mockService.Setup(s => s.GetFridgeItemsAsync(userId, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(items);

        // Act
        var result = await _controller.ListFridgeItems(null, CancellationToken.None);

        // Assert
        var okResult = result as OkObjectResult;
        okResult.Should().NotBeNull();
        okResult!.StatusCode.Should().Be(StatusCodes.Status200OK);

        var response = okResult.Value as ApiResponse<IReadOnlyList<FridgeItemDto>>;
        response.Should().NotBeNull();
        response!.Data.Should().HaveCount(1);
    }

    [Fact]
    public async Task ListFridgeItems_GuestRole_Throws403GuestPersonalizationRestricted()
    {
        // Arrange
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, "1"),
            new(ClaimTypes.Role, UserRoles.Guest)
        };
        _controller.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));

        // Act & Assert
        var act = async () => await _controller.ListFridgeItems(null, CancellationToken.None);
        var ex = await act.Should().ThrowAsync<AppException>();
        ex.Which.StatusCode.Should().Be(StatusCodes.Status403Forbidden);
        ex.Which.ErrorCode.Should().Be(ErrorCodes.ErrGuestPersonalizationRestricted);
    }

    [Fact]
    public async Task CreateFridgeItem_MemberAuthenticated_Returns201Created()
    {
        // Arrange
        var userId = 10;
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId.ToString()),
            new(ClaimTypes.Role, UserRoles.Member)
        };
        _controller.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));

        var request = new CreateFridgeItemRequest { IngredientId = 12, CustomItemName = "Nấm rơm tươi" };
        var createdItem = new FridgeItemDto
        {
            InventoryId = 55,
            IngredientId = 12,
            Name = "Nấm rơm tươi",
            FreshnessStatus = "FRESH"
        };

        _mockService.Setup(s => s.AddFridgeItemAsync(userId, request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(createdItem);

        // Act
        var result = await _controller.CreateFridgeItem(request, CancellationToken.None);

        // Assert
        var statusResult = result as ObjectResult;
        statusResult.Should().NotBeNull();
        statusResult!.StatusCode.Should().Be(StatusCodes.Status201Created);

        var response = statusResult.Value as ApiResponse<FridgeItemDto>;
        response.Should().NotBeNull();
        response!.Message.Should().Be("Đã thêm.");
        response.Data!.InventoryId.Should().Be(55);
    }

    [Fact]
    public async Task UpdateFridgeItem_MemberAuthenticated_Returns200Ok()
    {
        // Arrange
        var userId = 10;
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId.ToString()),
            new(ClaimTypes.Role, UserRoles.Member)
        };
        _controller.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));

        var request = new UpdateFridgeItemRequest { FreshnessStatus = "NON-FRESH" };
        var updatedItem = new FridgeItemDto
        {
            InventoryId = 55,
            IngredientId = 12,
            Name = "Nấm rơm",
            FreshnessStatus = "NON-FRESH"
        };

        _mockService.Setup(s => s.UpdateFridgeItemAsync(userId, 55, request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(updatedItem);

        // Act
        var result = await _controller.UpdateFridgeItem(55, request, CancellationToken.None);

        // Assert
        var okResult = result as OkObjectResult;
        okResult.Should().NotBeNull();
        okResult!.StatusCode.Should().Be(StatusCodes.Status200OK);

        var response = okResult.Value as ApiResponse<FridgeItemDto>;
        response.Should().NotBeNull();
        response!.Message.Should().Be("Đã cập nhật.");
        response.Data!.FreshnessStatus.Should().Be("NON-FRESH");
    }

    [Fact]
    public async Task DeleteFridgeItem_MemberAuthenticated_Returns200Ok()
    {
        // Arrange
        var userId = 10;
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId.ToString()),
            new(ClaimTypes.Role, UserRoles.Member)
        };
        _controller.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));

        _mockService.Setup(s => s.DeleteFridgeItemAsync(userId, 55, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.DeleteFridgeItem(55, CancellationToken.None);

        // Assert
        var okResult = result as OkObjectResult;
        okResult.Should().NotBeNull();
        okResult!.StatusCode.Should().Be(StatusCodes.Status200OK);

        var response = okResult.Value as ApiResponse<object?>;
        response.Should().NotBeNull();
        response!.Message.Should().Be("Đã xóa.");
    }
}
